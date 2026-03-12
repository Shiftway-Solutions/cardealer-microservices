using System.Text.Json;
using KYCService.Application.Clients;
using KYCService.Domain.Entities;
using KYCService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KYCService.Application.Services;

/// <summary>
/// Service for managing KYC submission sagas with automatic rollback
/// </summary>
public interface IKYCSagaOrchestrator
{
    /// <summary>
    /// Start a new KYC submission saga
    /// </summary>
    Task<KYCSagaState> StartSagaAsync(Guid userId, int totalSteps, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a completed step
    /// </summary>
    Task RecordStepCompletedAsync(Guid correlationId, int step, string stepName, object? stepData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record profile creation for rollback tracking
    /// </summary>
    Task RecordProfileCreatedAsync(Guid correlationId, Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record document creation for rollback tracking
    /// </summary>
    Task RecordDocumentCreatedAsync(Guid correlationId, Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark saga as completed successfully
    /// </summary>
    Task CompleteSagaAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark saga as failed and trigger rollback
    /// </summary>
    Task FailSagaAsync(Guid correlationId, int failedAtStep, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute rollback for a failed saga
    /// </summary>
    Task<bool> RollbackSagaAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get saga state
    /// </summary>
    Task<KYCSagaState?> GetSagaAsync(Guid correlationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Internal model for tracking completed steps
/// </summary>
public class CompletedStep
{
    public int Step { get; set; }
    public string StepName { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public string? Data { get; set; }
}

/// <summary>
/// Implementation of KYC saga orchestrator using centralized AuditService
/// </summary>
public class KYCSagaOrchestrator : IKYCSagaOrchestrator
{
    private readonly IKYCSagaRepository _sagaRepository;
    private readonly IKYCProfileRepository _profileRepository;
    private readonly IKYCDocumentRepository _documentRepository;
    private readonly IAuditServiceClient _auditClient;
    private readonly ILogger<KYCSagaOrchestrator> _logger;

    public KYCSagaOrchestrator(
        IKYCSagaRepository sagaRepository,
        IKYCProfileRepository profileRepository,
        IKYCDocumentRepository documentRepository,
        IAuditServiceClient auditClient,
        ILogger<KYCSagaOrchestrator> logger)
    {
        _sagaRepository = sagaRepository;
        _profileRepository = profileRepository;
        _documentRepository = documentRepository;
        _auditClient = auditClient;
        _logger = logger;
    }

    public async Task<KYCSagaState> StartSagaAsync(Guid userId, int totalSteps, CancellationToken cancellationToken = default)
    {
        var saga = new KYCSagaState
        {
            CorrelationId = Guid.NewGuid(),
            UserId = userId,
            Status = SagaStatus.Started,
            CurrentStep = 0,
            TotalSteps = totalSteps
        };

        await _sagaRepository.CreateAsync(saga, cancellationToken);

        _logger.LogInformation("Started KYC saga {CorrelationId} for user {UserId}",
            saga.CorrelationId, userId);

        // Log to centralized audit service (fire and forget)
        _ = _auditClient.LogKYCEventAsync(
            userId.ToString(),
            "Saga.Started",
            $"kyc-saga:{saga.CorrelationId}",
            "internal",
            additionalData: new Dictionary<string, object>
            {
                { "correlationId", saga.CorrelationId.ToString() },
                { "totalSteps", totalSteps }
            });

        return saga;
    }

    public async Task RecordStepCompletedAsync(Guid correlationId, int step, string stepName, object? stepData, CancellationToken cancellationToken = default)
    {
        var saga = await _sagaRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
        if (saga == null)
        {
            _logger.LogWarning("Saga {CorrelationId} not found for step completion", correlationId);
            return;
        }

        saga.Status = SagaStatus.InProgress;
        saga.CurrentStep = step;

        // Parse existing completed steps
        var completedSteps = JsonSerializer.Deserialize<List<CompletedStep>>(saga.CompletedStepsData) ?? new();
        completedSteps.Add(new CompletedStep
        {
            Step = step,
            StepName = stepName,
            CompletedAt = DateTime.UtcNow,
            Data = stepData != null ? JsonSerializer.Serialize(stepData) : null
        });
        saga.CompletedStepsData = JsonSerializer.Serialize(completedSteps);

        await _sagaRepository.UpdateAsync(saga, cancellationToken);

        _logger.LogInformation("Saga {CorrelationId} completed step {Step}: {StepName}",
            correlationId, step, stepName);
    }

    public async Task RecordProfileCreatedAsync(Guid correlationId, Guid profileId, CancellationToken cancellationToken = default)
    {
        var saga = await _sagaRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
        if (saga == null) return;

        saga.CreatedProfileId = profileId;
        await _sagaRepository.UpdateAsync(saga, cancellationToken);

        _logger.LogInformation("Saga {CorrelationId} recorded profile {ProfileId}", correlationId, profileId);
    }

    public async Task RecordDocumentCreatedAsync(Guid correlationId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var saga = await _sagaRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
        if (saga == null) return;

        saga.CreatedDocumentIds.Add(documentId);
        await _sagaRepository.UpdateAsync(saga, cancellationToken);

        _logger.LogInformation("Saga {CorrelationId} recorded document {DocumentId}", correlationId, documentId);
    }

    public async Task CompleteSagaAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        var saga = await _sagaRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
        if (saga == null) return;

        saga.Status = SagaStatus.Completed;
        saga.CompletedAt = DateTime.UtcNow;
        await _sagaRepository.UpdateAsync(saga, cancellationToken);

        // Log to centralized audit service
        await _auditClient.LogKYCEventAsync(
            saga.UserId.ToString(),
            KYCAuditActions.ProfileSubmittedForReview,
            $"kyc-profile:{saga.CreatedProfileId}",
            "internal",
            success: true,
            additionalData: new Dictionary<string, object>
            {
                { "correlationId", correlationId.ToString() },
                { "totalSteps", saga.TotalSteps },
                { "documentsCreated", saga.CreatedDocumentIds.Count }
            });

        _logger.LogInformation("Saga {CorrelationId} completed successfully", correlationId);
    }

    public async Task FailSagaAsync(Guid correlationId, int failedAtStep, string errorMessage, CancellationToken cancellationToken = default)
    {
        var saga = await _sagaRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
        if (saga == null) return;

        saga.Status = SagaStatus.Failed;
        saga.FailedAtStep = failedAtStep;
        saga.ErrorMessage = errorMessage;
        await _sagaRepository.UpdateAsync(saga, cancellationToken);

        // Log to centralized audit service
        await _auditClient.LogKYCEventAsync(
            saga.UserId.ToString(),
            KYCAuditActions.IdentityVerificationFailed,
            $"kyc-saga:{correlationId}",
            "internal",
            success: false,
            errorMessage: errorMessage,
            additionalData: new Dictionary<string, object>
            {
                { "correlationId", correlationId.ToString() },
                { "failedAtStep", failedAtStep },
                { "currentStep", saga.CurrentStep },
                { "totalSteps", saga.TotalSteps }
            });

        _logger.LogWarning("Saga {CorrelationId} failed at step {Step}: {Error}",
            correlationId, failedAtStep, errorMessage);

        // Trigger automatic rollback
        await RollbackSagaAsync(correlationId, cancellationToken);
    }

    public async Task<bool> RollbackSagaAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        var saga = await _sagaRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
        if (saga == null)
        {
            _logger.LogWarning("Cannot rollback - saga {CorrelationId} not found", correlationId);
            return false;
        }

        saga.Status = SagaStatus.RollingBack;
        await _sagaRepository.UpdateAsync(saga, cancellationToken);

        _logger.LogInformation("Starting rollback for saga {CorrelationId}", correlationId);

        var rollbackErrors = new List<string>();

        try
        {
            // Rollback documents (delete them)
            foreach (var documentId in saga.CreatedDocumentIds)
            {
                try
                {
                    var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
                    if (document != null)
                    {
                        await _documentRepository.DeleteAsync(documentId, cancellationToken);
                        _logger.LogInformation("Rolled back document {DocumentId}", documentId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rollback document {DocumentId}", documentId);
                    rollbackErrors.Add($"Document {documentId}: {ex.Message}");
                }
            }

            // Rollback profile (set to suspended, don't delete)
            if (saga.CreatedProfileId.HasValue)
            {
                try
                {
                    var profile = await _profileRepository.GetByIdAsync(saga.CreatedProfileId.Value, cancellationToken);
                    if (profile != null)
                    {
                        profile.Status = KYCStatus.Suspended;
                        profile.RejectionReason = $"Automatic rollback due to saga failure: {saga.ErrorMessage}";
                        profile.UpdatedAt = DateTime.UtcNow;
                        await _profileRepository.UpdateAsync(profile, cancellationToken);
                        _logger.LogInformation("Rolled back profile {ProfileId} (set to Suspended)", saga.CreatedProfileId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rollback profile {ProfileId}", saga.CreatedProfileId);
                    rollbackErrors.Add($"Profile {saga.CreatedProfileId}: {ex.Message}");
                }
            }

            // Update saga status
            saga.Status = rollbackErrors.Count == 0 ? SagaStatus.RolledBack : SagaStatus.PartiallyRolledBack;
            saga.RolledBackAt = DateTime.UtcNow;
            saga.ErrorMessage = rollbackErrors.Count > 0 ? string.Join("; ", rollbackErrors) : saga.ErrorMessage;
            await _sagaRepository.UpdateAsync(saga, cancellationToken);

            // Log rollback to audit service
            await _auditClient.LogKYCEventAsync(
                saga.UserId.ToString(),
                "Saga.RolledBack",
                $"kyc-saga:{correlationId}",
                "internal",
                success: rollbackErrors.Count == 0,
                errorMessage: rollbackErrors.Count > 0 ? string.Join("; ", rollbackErrors) : null,
                additionalData: new Dictionary<string, object>
                {
                    { "correlationId", correlationId.ToString() },
                    { "documentsRolledBack", saga.CreatedDocumentIds.Count },
                    { "profileSuspended", saga.CreatedProfileId.HasValue },
                    { "rollbackErrors", rollbackErrors.Count }
                });

            _logger.LogInformation("Rollback for saga {CorrelationId} completed with {ErrorCount} errors",
                correlationId, rollbackErrors.Count);

            return rollbackErrors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during rollback of saga {CorrelationId}", correlationId);

            saga.Status = SagaStatus.PartiallyRolledBack;
            saga.ErrorMessage = $"Critical rollback failure: {ex.Message}";
            saga.RolledBackAt = DateTime.UtcNow;
            await _sagaRepository.UpdateAsync(saga, cancellationToken);

            return false;
        }
    }

    public async Task<KYCSagaState?> GetSagaAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return await _sagaRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
    }
}
