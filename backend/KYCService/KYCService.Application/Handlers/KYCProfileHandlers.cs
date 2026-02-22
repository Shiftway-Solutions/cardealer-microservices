using MediatR;
using Microsoft.Extensions.Logging;
using KYCService.Domain.Entities;
using KYCService.Domain.Interfaces;
using KYCService.Application.Commands;
using KYCService.Application.DTOs;
using KYCService.Application.Exceptions;
using CarDealer.Contracts.Events.KYC;

namespace KYCService.Application.Handlers;

/// <summary>
/// Handler para crear perfil KYC
/// </summary>
public class CreateKYCProfileHandler : IRequestHandler<CreateKYCProfileCommand, KYCProfileDto>
{
    private readonly IKYCProfileRepository _repository;

    public CreateKYCProfileHandler(IKYCProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<KYCProfileDto> Handle(CreateKYCProfileCommand request, CancellationToken cancellationToken)
    {
        // ============================================================================
        // SECURITY: Check for existing profile to prevent duplicates
        // ============================================================================
        var existingProfile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existingProfile != null)
        {
            // If profile exists and is in certain states, don't allow creating another
            var blockedStatuses = new[] { 
                KYCStatus.Pending, 
                KYCStatus.InProgress, 
                KYCStatus.UnderReview, 
                KYCStatus.Approved 
            };
            
            if (blockedStatuses.Contains(existingProfile.Status))
            {
                throw new DuplicateProfileException(
                    $"User {request.UserId} already has a KYC profile with status {existingProfile.Status}. " +
                    $"Profile ID: {existingProfile.Id}");
            }
            
            // ============================================================================
            // RESUBMISSION: If previous profile was Rejected/Expired/Suspended,
            // update the existing profile instead of creating a new one
            // ============================================================================
            var resubmittableStatuses = new[] { KYCStatus.Rejected, KYCStatus.Expired, KYCStatus.Suspended };
            if (resubmittableStatuses.Contains(existingProfile.Status))
            {
                // Check duplicate document for different user
                if (!string.IsNullOrEmpty(request.PrimaryDocumentNumber))
                {
                    var documentExists = await _repository.GetByDocumentNumberAsync(
                        request.PrimaryDocumentNumber, cancellationToken);
                    
                    if (documentExists != null && documentExists.UserId != request.UserId)
                    {
                        throw new DuplicateDocumentException(
                            $"Document number {MaskDocumentNumber(request.PrimaryDocumentNumber)} is already registered to another user.");
                    }
                }
                
                // Update existing profile for resubmission
                existingProfile.Status = KYCStatus.Pending;
                existingProfile.FullName = request.FullName;
                existingProfile.MiddleName = request.MiddleName;
                existingProfile.LastName = request.LastName;
                existingProfile.DateOfBirth = ToUtc(request.DateOfBirth);
                existingProfile.PlaceOfBirth = request.PlaceOfBirth;
                existingProfile.Nationality = request.Nationality;
                existingProfile.Gender = request.Gender;
                existingProfile.PrimaryDocumentType = request.PrimaryDocumentType;
                existingProfile.PrimaryDocumentNumber = request.PrimaryDocumentNumber;
                existingProfile.PrimaryDocumentExpiry = ToUtc(request.PrimaryDocumentExpiry);
                existingProfile.PrimaryDocumentCountry = request.PrimaryDocumentCountry;
                existingProfile.Email = request.Email;
                existingProfile.Phone = request.Phone;
                existingProfile.MobilePhone = request.MobilePhone;
                existingProfile.Address = request.Address;
                existingProfile.City = request.City;
                existingProfile.Province = request.Province;
                existingProfile.PostalCode = request.PostalCode;
                existingProfile.Country = request.Country ?? "DO";
                existingProfile.Occupation = request.Occupation;
                existingProfile.EmployerName = request.EmployerName;
                existingProfile.SourceOfFunds = request.SourceOfFunds;
                existingProfile.ExpectedTransactionVolume = request.ExpectedTransactionVolume;
                existingProfile.EstimatedAnnualIncome = request.EstimatedAnnualIncome;
                existingProfile.IsPEP = request.IsPEP;
                existingProfile.PEPPosition = request.PEPPosition;
                existingProfile.PEPRelationship = request.PEPRelationship;
                existingProfile.BusinessName = request.BusinessName;
                existingProfile.RNC = request.RNC;
                existingProfile.BusinessType = request.BusinessType;
                existingProfile.IncorporationDate = ToUtc(request.IncorporationDate);
                existingProfile.LegalRepresentative = request.LegalRepresentative;
                existingProfile.UpdatedAt = DateTime.UtcNow;
                // Clear previous rejection data
                existingProfile.RejectionReason = null;
                existingProfile.RejectedAt = null;
                existingProfile.RejectedBy = null;
                // Reset risk assessment for new review
                existingProfile.RiskLevel = request.IsPEP ? RiskLevel.High : RiskLevel.Low;
                existingProfile.RiskScore = request.IsPEP ? 70 : 0;
                if (request.IsPEP && !existingProfile.RiskFactors.Contains("PEP Status"))
                {
                    existingProfile.RiskFactors.Add("PEP Status");
                }
                
                var updated = await _repository.UpdateAsync(existingProfile, cancellationToken);
                return MapToDto(updated);
            }
        }

        // ============================================================================
        // SECURITY: Check for duplicate document number (different user, same cédula)
        // ============================================================================
        if (!string.IsNullOrEmpty(request.PrimaryDocumentNumber))
        {
            var documentExists = await _repository.GetByDocumentNumberAsync(
                request.PrimaryDocumentNumber, cancellationToken);
            
            if (documentExists != null && documentExists.UserId != request.UserId)
            {
                throw new DuplicateDocumentException(
                    $"Document number {MaskDocumentNumber(request.PrimaryDocumentNumber)} is already registered to another user.");
            }
        }

        var profile = new KYCProfile
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            EntityType = request.EntityType,
            Status = KYCStatus.Pending,
            RiskLevel = RiskLevel.Low,
            RiskScore = 0,
            FullName = request.FullName,
            MiddleName = request.MiddleName,
            LastName = request.LastName,
            DateOfBirth = ToUtc(request.DateOfBirth),
            PlaceOfBirth = request.PlaceOfBirth,
            Nationality = request.Nationality,
            Gender = request.Gender,
            PrimaryDocumentType = request.PrimaryDocumentType,
            PrimaryDocumentNumber = request.PrimaryDocumentNumber,
            PrimaryDocumentExpiry = ToUtc(request.PrimaryDocumentExpiry),
            PrimaryDocumentCountry = request.PrimaryDocumentCountry,
            Email = request.Email,
            Phone = request.Phone,
            MobilePhone = request.MobilePhone,
            Address = request.Address,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode,
            Country = request.Country ?? "DO",
            Occupation = request.Occupation,
            EmployerName = request.EmployerName,
            SourceOfFunds = request.SourceOfFunds,
            ExpectedTransactionVolume = request.ExpectedTransactionVolume,
            EstimatedAnnualIncome = request.EstimatedAnnualIncome,
            IsPEP = request.IsPEP,
            PEPPosition = request.PEPPosition,
            PEPRelationship = request.PEPRelationship,
            BusinessName = request.BusinessName,
            RNC = request.RNC,
            BusinessType = request.BusinessType,
            IncorporationDate = ToUtc(request.IncorporationDate),
            LegalRepresentative = request.LegalRepresentative,
            CreatedAt = DateTime.UtcNow
        };

        // Si es PEP, incrementar nivel de riesgo inicial
        if (profile.IsPEP)
        {
            profile.RiskLevel = RiskLevel.High;
            profile.RiskScore = 70;
            profile.RiskFactors.Add("PEP Status");
        }

        var created = await _repository.CreateAsync(profile, cancellationToken);
        return MapToDto(created);
    }

    /// <summary>
    /// Converts a DateTime to UTC. If the DateTime is null or already UTC, returns as-is.
    /// For Unspecified kind, assumes the value is already in UTC and specifies it as such.
    /// </summary>
    private static DateTime? ToUtc(DateTime? dateTime)
    {
        if (dateTime == null) return null;
        
        var dt = dateTime.Value;
        return dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => dt
        };
    }

    /// <summary>
    /// Masks a document number for secure logging (shows only last 4 characters)
    /// </summary>
    private static string MaskDocumentNumber(string documentNumber)
    {
        if (string.IsNullOrEmpty(documentNumber) || documentNumber.Length <= 4)
            return "****";
        return new string('*', documentNumber.Length - 4) + documentNumber[^4..];
    }

    private static KYCProfileDto MapToDto(KYCProfile p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        EntityType = p.EntityType,
        Status = p.Status,
        RiskLevel = p.RiskLevel,
        RiskScore = p.RiskScore,
        RiskFactors = p.RiskFactors,
        FullName = p.FullName,
        MiddleName = p.MiddleName,
        LastName = p.LastName,
        DateOfBirth = p.DateOfBirth,
        PlaceOfBirth = p.PlaceOfBirth,
        Nationality = p.Nationality,
        PrimaryDocumentType = p.PrimaryDocumentType,
        PrimaryDocumentNumber = p.PrimaryDocumentNumber,
        PrimaryDocumentExpiry = p.PrimaryDocumentExpiry,
        Email = p.Email,
        Phone = p.Phone,
        Address = p.Address,
        City = p.City,
        Province = p.Province,
        Country = p.Country,
        IsPEP = p.IsPEP,
        PEPPosition = p.PEPPosition,
        BusinessName = p.BusinessName,
        RNC = p.RNC,
        IdentityVerifiedAt = p.IdentityVerifiedAt,
        AddressVerifiedAt = p.AddressVerifiedAt,
        CreatedAt = p.CreatedAt,
        ApprovedAt = p.ApprovedAt,
        ExpiresAt = p.ExpiresAt,
        NextReviewAt = p.NextReviewAt
    };
}

/// <summary>
/// Handler para actualizar perfil KYC
/// </summary>
public class UpdateKYCProfileHandler : IRequestHandler<UpdateKYCProfileCommand, KYCProfileDto>
{
    private readonly IKYCProfileRepository _repository;

    public UpdateKYCProfileHandler(IKYCProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<KYCProfileDto> Handle(UpdateKYCProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"KYC Profile with ID {request.Id} not found");

        if (request.FullName != null) profile.FullName = request.FullName;
        if (request.Email != null) profile.Email = request.Email;
        if (request.Phone != null) profile.Phone = request.Phone;
        if (request.MobilePhone != null) profile.MobilePhone = request.MobilePhone;
        if (request.Address != null) profile.Address = request.Address;
        if (request.City != null) profile.City = request.City;
        if (request.Province != null) profile.Province = request.Province;
        if (request.Occupation != null) profile.Occupation = request.Occupation;
        if (request.EmployerName != null) profile.EmployerName = request.EmployerName;
        if (request.SourceOfFunds != null) profile.SourceOfFunds = request.SourceOfFunds;
        if (request.EstimatedAnnualIncome.HasValue) profile.EstimatedAnnualIncome = request.EstimatedAnnualIncome;

        profile.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(profile, cancellationToken);
        return MapToDto(updated);
    }

    private static KYCProfileDto MapToDto(KYCProfile p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        EntityType = p.EntityType,
        Status = p.Status,
        RiskLevel = p.RiskLevel,
        RiskScore = p.RiskScore,
        RiskFactors = p.RiskFactors,
        FullName = p.FullName,
        Email = p.Email,
        Phone = p.Phone,
        Address = p.Address,
        City = p.City,
        Province = p.Province,
        Country = p.Country,
        IsPEP = p.IsPEP,
        CreatedAt = p.CreatedAt,
        ApprovedAt = p.ApprovedAt,
        ExpiresAt = p.ExpiresAt,
        NextReviewAt = p.NextReviewAt
    };
}

/// <summary>
/// Handler para enviar perfil KYC para revisión
/// </summary>
public class SubmitKYCForReviewHandler : IRequestHandler<SubmitKYCForReviewCommand, KYCProfileDto>
{
    private readonly IKYCProfileRepository _repository;

    public SubmitKYCForReviewHandler(IKYCProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<KYCProfileDto> Handle(SubmitKYCForReviewCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"KYC Profile with ID {request.Id} not found");

        // Solo permitir enviar a revisión si está en estado Pending o InProgress
        var allowedStatuses = new[] { KYCStatus.Pending, KYCStatus.InProgress };
        if (!allowedStatuses.Contains(profile.Status))
        {
            throw new InvalidOperationException(
                $"Cannot submit profile for review. Current status is {profile.Status}. " +
                $"Only profiles with status Pending or InProgress can be submitted.");
        }

        profile.Status = KYCStatus.UnderReview;
        profile.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(profile, cancellationToken);
        return MapToDto(updated);
    }

    private static KYCProfileDto MapToDto(KYCProfile p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        EntityType = p.EntityType,
        Status = p.Status,
        RiskLevel = p.RiskLevel,
        RiskScore = p.RiskScore,
        RiskFactors = p.RiskFactors,
        FullName = p.FullName,
        Email = p.Email,
        Phone = p.Phone,
        Address = p.Address,
        City = p.City,
        Province = p.Province,
        Country = p.Country,
        IsPEP = p.IsPEP,
        CreatedAt = p.CreatedAt,
        ApprovedAt = p.ApprovedAt,
        ExpiresAt = p.ExpiresAt,
        NextReviewAt = p.NextReviewAt
    };
}

/// <summary>
/// Handler para aprobar perfil KYC
/// </summary>
public class ApproveKYCProfileHandler : IRequestHandler<ApproveKYCProfileCommand, KYCProfileDto>
{
    private readonly IKYCProfileRepository _repository;
    private readonly IKYCEventPublisher _eventPublisher;
    private readonly ILogger<ApproveKYCProfileHandler> _logger;

    public ApproveKYCProfileHandler(
        IKYCProfileRepository repository,
        IKYCEventPublisher eventPublisher,
        ILogger<ApproveKYCProfileHandler> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<KYCProfileDto> Handle(ApproveKYCProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"KYC Profile with ID {request.Id} not found");

        var previousStatus = profile.Status.ToString();

        profile.Status = KYCStatus.Approved;
        profile.ApprovedAt = DateTime.UtcNow;
        profile.ApprovedBy = request.ApprovedBy;
        profile.ApprovalNotes = request.Notes;
        profile.UpdatedAt = DateTime.UtcNow;
        profile.ExpiresAt = DateTime.UtcNow.AddDays(request.ValidityDays);
        profile.NextReviewAt = DateTime.UtcNow.AddDays(request.ValidityDays - 30); // Revisar 30 días antes de expirar
        profile.LastReviewAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(profile, cancellationToken);

        // Publish RabbitMQ event (NotificationService consumes it)
        await _eventPublisher.PublishStatusChangedAsync(new KYCProfileStatusChangedEvent
        {
            ProfileId    = profile.Id,
            UserId       = profile.UserId,
            Email        = profile.Email ?? string.Empty,
            FullName     = profile.FullName,
            PreviousStatus = previousStatus,
            NewStatus    = "Approved",
            Reason       = request.Notes,
            ChangedBy    = request.ApprovedBy,
            ChangedAt    = DateTime.UtcNow,
            ValidityDays = request.ValidityDays
        }, cancellationToken);

        return MapToDto(updated);
    }

    private static KYCProfileDto MapToDto(KYCProfile p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        EntityType = p.EntityType,
        Status = p.Status,
        RiskLevel = p.RiskLevel,
        RiskScore = p.RiskScore,
        FullName = p.FullName,
        IsPEP = p.IsPEP,
        CreatedAt = p.CreatedAt,
        ApprovedAt = p.ApprovedAt,
        ExpiresAt = p.ExpiresAt,
        NextReviewAt = p.NextReviewAt
    };
}

/// <summary>
/// Handler para rechazar perfil KYC
/// </summary>
public class RejectKYCProfileHandler : IRequestHandler<RejectKYCProfileCommand, KYCProfileDto>
{
    private readonly IKYCProfileRepository _repository;
    private readonly IKYCEventPublisher _eventPublisher;
    private readonly ILogger<RejectKYCProfileHandler> _logger;

    public RejectKYCProfileHandler(
        IKYCProfileRepository repository,
        IKYCEventPublisher eventPublisher,
        ILogger<RejectKYCProfileHandler> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<KYCProfileDto> Handle(RejectKYCProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"KYC Profile with ID {request.Id} not found");

        var previousStatus = profile.Status.ToString();

        profile.Status = KYCStatus.Rejected;
        profile.RejectedAt = DateTime.UtcNow;
        profile.RejectedBy = request.RejectedBy;
        profile.RejectionReason = request.RejectionReason;
        profile.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(profile, cancellationToken);

        // Publish RabbitMQ event (NotificationService consumes it)
        await _eventPublisher.PublishStatusChangedAsync(new KYCProfileStatusChangedEvent
        {
            ProfileId      = profile.Id,
            UserId         = profile.UserId,
            Email          = profile.Email ?? string.Empty,
            FullName       = profile.FullName,
            PreviousStatus = previousStatus,
            NewStatus      = "Rejected",
            Reason         = request.RejectionReason,
            ChangedBy      = request.RejectedBy,
            ChangedAt      = DateTime.UtcNow
        }, cancellationToken);

        return MapToDto(updated);
    }

    private static KYCProfileDto MapToDto(KYCProfile p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        EntityType = p.EntityType,
        Status = p.Status,
        RiskLevel = p.RiskLevel,
        FullName = p.FullName,
        IsPEP = p.IsPEP,
        CreatedAt = p.CreatedAt
    };
}

/// <summary>
/// Handler para subir documento KYC
/// </summary>
public class UploadKYCDocumentHandler : IRequestHandler<UploadKYCDocumentCommand, KYCDocumentDto>
{
    private readonly IKYCDocumentRepository _repository;
    private readonly IKYCProfileRepository _profileRepository;

    public UploadKYCDocumentHandler(IKYCDocumentRepository repository, IKYCProfileRepository profileRepository)
    {
        _repository = repository;
        _profileRepository = profileRepository;
    }

    public async Task<KYCDocumentDto> Handle(UploadKYCDocumentCommand request, CancellationToken cancellationToken)
    {
        // Verificar que el perfil existe
        var profile = await _profileRepository.GetByIdAsync(request.KYCProfileId, cancellationToken)
            ?? throw new InvalidOperationException($"KYC Profile with ID {request.KYCProfileId} not found");

        var document = new KYCDocument
        {
            Id = Guid.NewGuid(),
            KYCProfileId = request.KYCProfileId,
            Type = request.Type,
            DocumentName = request.DocumentName,
            FileName = request.FileName,
            StorageKey = request.StorageKey,
            FileUrl = request.FileUrl,
            FileType = request.FileType,
            FileSize = request.FileSize,
            FileHash = request.FileHash,
            Side = request.Side,
            Status = KYCDocumentStatus.Pending,
            UploadedBy = request.UploadedBy,
            UploadedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(document, cancellationToken);

        // Actualizar estado del perfil si está pendiente
        if (profile.Status == KYCStatus.Pending)
        {
            profile.Status = KYCStatus.InProgress;
            profile.UpdatedAt = DateTime.UtcNow;
            await _profileRepository.UpdateAsync(profile, cancellationToken);
        }

        return new KYCDocumentDto
        {
            Id = created.Id,
            KYCProfileId = created.KYCProfileId,
            Type = created.Type,
            DocumentName = created.DocumentName,
            FileName = created.FileName,
            StorageKey = created.StorageKey,
            FileUrl = created.FileUrl,
            FileType = created.FileType,
            FileSize = created.FileSize,
            Side = created.Side,
            Status = created.Status,
            UploadedAt = created.UploadedAt
        };
    }
}

/// <summary>
/// Handler para verificar documento KYC
/// </summary>
public class VerifyKYCDocumentHandler : IRequestHandler<VerifyKYCDocumentCommand, KYCDocumentDto>
{
    private readonly IKYCDocumentRepository _repository;

    public VerifyKYCDocumentHandler(IKYCDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<KYCDocumentDto> Handle(VerifyKYCDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"KYC Document with ID {request.Id} not found");

        document.Status = request.Approved ? KYCDocumentStatus.Verified : KYCDocumentStatus.Rejected;
        document.VerifiedAt = DateTime.UtcNow;
        document.VerifiedBy = request.VerifiedBy;
        document.RejectionReason = request.RejectionReason;
        document.ExtractedNumber = request.ExtractedNumber;
        document.ExtractedExpiry = request.ExtractedExpiry;
        document.ExtractedName = request.ExtractedName;

        var updated = await _repository.UpdateAsync(document, cancellationToken);

        return new KYCDocumentDto
        {
            Id = updated.Id,
            KYCProfileId = updated.KYCProfileId,
            Type = updated.Type,
            DocumentName = updated.DocumentName,
            FileName = updated.FileName,
            StorageKey = updated.StorageKey,
            FileUrl = updated.FileUrl,
            FileType = updated.FileType,
            FileSize = updated.FileSize,
            Status = updated.Status,
            RejectionReason = updated.RejectionReason,
            UploadedAt = updated.UploadedAt,
            VerifiedAt = updated.VerifiedAt
        };
    }
}

/// <summary>
/// Handler para crear verificación KYC
/// </summary>
public class CreateKYCVerificationHandler : IRequestHandler<CreateKYCVerificationCommand, KYCVerificationDto>
{
    private readonly IKYCVerificationRepository _repository;
    private readonly IKYCProfileRepository _profileRepository;

    public CreateKYCVerificationHandler(IKYCVerificationRepository repository, IKYCProfileRepository profileRepository)
    {
        _repository = repository;
        _profileRepository = profileRepository;
    }

    public async Task<KYCVerificationDto> Handle(CreateKYCVerificationCommand request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByIdAsync(request.KYCProfileId, cancellationToken)
            ?? throw new InvalidOperationException($"KYC Profile with ID {request.KYCProfileId} not found");

        var verification = new KYCVerification
        {
            Id = Guid.NewGuid(),
            KYCProfileId = request.KYCProfileId,
            VerificationType = request.VerificationType,
            Provider = request.Provider,
            Passed = request.Passed,
            FailureReason = request.FailureReason,
            RawResponse = request.RawResponse,
            ConfidenceScore = request.ConfidenceScore,
            VerifiedAt = DateTime.UtcNow,
            VerifiedBy = request.VerifiedBy,
            ExpiresAt = request.ValidityDays.HasValue ? DateTime.UtcNow.AddDays(request.ValidityDays.Value) : null
        };

        var created = await _repository.CreateAsync(verification, cancellationToken);

        // Actualizar timestamps de verificación en el perfil
        if (request.Passed)
        {
            switch (request.VerificationType.ToLower())
            {
                case "identity":
                    profile.IdentityVerifiedAt = DateTime.UtcNow;
                    break;
                case "address":
                    profile.AddressVerifiedAt = DateTime.UtcNow;
                    break;
                case "income":
                    profile.IncomeVerifiedAt = DateTime.UtcNow;
                    break;
                case "pep":
                    profile.PEPCheckedAt = DateTime.UtcNow;
                    break;
                case "sanctions":
                    profile.SanctionsCheckedAt = DateTime.UtcNow;
                    break;
            }
            profile.UpdatedAt = DateTime.UtcNow;
            await _profileRepository.UpdateAsync(profile, cancellationToken);
        }

        return new KYCVerificationDto
        {
            Id = created.Id,
            KYCProfileId = created.KYCProfileId,
            VerificationType = created.VerificationType,
            Provider = created.Provider,
            Passed = created.Passed,
            FailureReason = created.FailureReason,
            ConfidenceScore = created.ConfidenceScore,
            VerifiedAt = created.VerifiedAt,
            ExpiresAt = created.ExpiresAt
        };
    }
}

/// <summary>
/// Handler para evaluar riesgo KYC
/// </summary>
public class AssessKYCRiskHandler : IRequestHandler<AssessKYCRiskCommand, KYCRiskAssessmentDto>
{
    private readonly IKYCRiskAssessmentRepository _repository;
    private readonly IKYCProfileRepository _profileRepository;

    public AssessKYCRiskHandler(IKYCRiskAssessmentRepository repository, IKYCProfileRepository profileRepository)
    {
        _repository = repository;
        _profileRepository = profileRepository;
    }

    public async Task<KYCRiskAssessmentDto> Handle(AssessKYCRiskCommand request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByIdAsync(request.KYCProfileId, cancellationToken)
            ?? throw new InvalidOperationException($"KYC Profile with ID {request.KYCProfileId} not found");

        var assessment = new KYCRiskAssessment
        {
            Id = Guid.NewGuid(),
            KYCProfileId = request.KYCProfileId,
            PreviousLevel = profile.RiskLevel,
            NewLevel = request.NewLevel,
            PreviousScore = profile.RiskScore,
            NewScore = request.NewScore,
            Reason = request.Reason,
            Factors = request.Factors,
            RecommendedActions = request.RecommendedActions,
            AssessedBy = request.AssessedBy,
            AssessedByName = request.AssessedByName,
            AssessedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(assessment, cancellationToken);

        // Actualizar nivel de riesgo en el perfil
        profile.RiskLevel = request.NewLevel;
        profile.RiskScore = request.NewScore;
        profile.RiskFactors = request.Factors;
        profile.UpdatedAt = DateTime.UtcNow;
        await _profileRepository.UpdateAsync(profile, cancellationToken);

        return new KYCRiskAssessmentDto
        {
            Id = created.Id,
            KYCProfileId = created.KYCProfileId,
            PreviousLevel = created.PreviousLevel,
            NewLevel = created.NewLevel,
            PreviousScore = created.PreviousScore,
            NewScore = created.NewScore,
            Reason = created.Reason,
            Factors = created.Factors,
            RecommendedActions = created.RecommendedActions,
            AssessedByName = created.AssessedByName,
            AssessedAt = created.AssessedAt
        };
    }
}
