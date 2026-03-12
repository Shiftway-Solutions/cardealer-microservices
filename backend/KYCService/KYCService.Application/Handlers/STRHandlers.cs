using MediatR;
using KYCService.Domain.Entities;
using KYCService.Domain.Interfaces;
using KYCService.Application.Commands;
using KYCService.Application.DTOs;

namespace KYCService.Application.Handlers;

/// <summary>
/// Handler para crear reporte de transacción sospechosa
/// </summary>
public class CreateSTRHandler : IRequestHandler<CreateSuspiciousTransactionReportCommand, SuspiciousTransactionReportDto>
{
    private readonly ISuspiciousTransactionReportRepository _repository;

    public CreateSTRHandler(ISuspiciousTransactionReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<SuspiciousTransactionReportDto> Handle(CreateSuspiciousTransactionReportCommand request, CancellationToken cancellationToken)
    {
        var reportNumber = await _repository.GenerateReportNumberAsync(cancellationToken);

        var report = new SuspiciousTransactionReport
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            KYCProfileId = request.KYCProfileId,
            TransactionId = request.TransactionId,
            ReportNumber = reportNumber,
            SuspiciousActivityType = request.SuspiciousActivityType,
            Description = request.Description,
            Amount = request.Amount,
            Currency = request.Currency,
            RedFlags = request.RedFlags,
            Status = STRStatus.Draft,
            DetectedAt = request.DetectedAt,
            ReportingDeadline = request.DetectedAt.AddDays(15), // Ley 155-17: 15 días hábiles para reportar
            CreatedBy = request.CreatedBy,
            CreatedByName = request.CreatedByName,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(report, cancellationToken);
        return MapToDto(created);
    }

    private static SuspiciousTransactionReportDto MapToDto(SuspiciousTransactionReport r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        KYCProfileId = r.KYCProfileId,
        ReportNumber = r.ReportNumber,
        SuspiciousActivityType = r.SuspiciousActivityType,
        Description = r.Description,
        Amount = r.Amount,
        Currency = r.Currency,
        RedFlags = r.RedFlags,
        Status = r.Status,
        DetectedAt = r.DetectedAt,
        ReportingDeadline = r.ReportingDeadline,
        UAFReportNumber = r.UAFReportNumber,
        SentToUAFAt = r.SentToUAFAt,
        CreatedByName = r.CreatedByName,
        CreatedAt = r.CreatedAt
    };
}

/// <summary>
/// Handler para aprobar reporte de transacción sospechosa
/// </summary>
public class ApproveSTRHandler : IRequestHandler<ApproveSTRCommand, SuspiciousTransactionReportDto>
{
    private readonly ISuspiciousTransactionReportRepository _repository;

    public ApproveSTRHandler(ISuspiciousTransactionReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<SuspiciousTransactionReportDto> Handle(ApproveSTRCommand request, CancellationToken cancellationToken)
    {
        var report = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"STR with ID {request.Id} not found");

        if (report.Status != STRStatus.PendingReview)
        {
            throw new InvalidOperationException($"STR must be in PendingReview status to approve. Current status: {report.Status}");
        }

        report.Status = STRStatus.Approved;
        report.ApprovedBy = request.ApprovedBy;
        report.ApprovedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(report, cancellationToken);
        return MapToDto(updated);
    }

    private static SuspiciousTransactionReportDto MapToDto(SuspiciousTransactionReport r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        KYCProfileId = r.KYCProfileId,
        ReportNumber = r.ReportNumber,
        SuspiciousActivityType = r.SuspiciousActivityType,
        Description = r.Description,
        Amount = r.Amount,
        Currency = r.Currency,
        RedFlags = r.RedFlags,
        Status = r.Status,
        DetectedAt = r.DetectedAt,
        ReportingDeadline = r.ReportingDeadline,
        UAFReportNumber = r.UAFReportNumber,
        SentToUAFAt = r.SentToUAFAt,
        CreatedByName = r.CreatedByName,
        CreatedAt = r.CreatedAt
    };
}

/// <summary>
/// Handler para enviar reporte a UAF
/// </summary>
public class SendSTRToUAFHandler : IRequestHandler<SendSTRToUAFCommand, SuspiciousTransactionReportDto>
{
    private readonly ISuspiciousTransactionReportRepository _repository;

    public SendSTRToUAFHandler(ISuspiciousTransactionReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<SuspiciousTransactionReportDto> Handle(SendSTRToUAFCommand request, CancellationToken cancellationToken)
    {
        var report = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"STR with ID {request.Id} not found");

        if (report.Status != STRStatus.Approved)
        {
            throw new InvalidOperationException($"STR must be Approved before sending to UAF. Current status: {report.Status}");
        }

        report.Status = STRStatus.SentToUAF;
        report.UAFReportNumber = request.UAFReportNumber;
        report.SentToUAFAt = DateTime.UtcNow;
        report.SentBy = request.SentBy;

        var updated = await _repository.UpdateAsync(report, cancellationToken);
        return MapToDto(updated);
    }

    private static SuspiciousTransactionReportDto MapToDto(SuspiciousTransactionReport r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        KYCProfileId = r.KYCProfileId,
        ReportNumber = r.ReportNumber,
        SuspiciousActivityType = r.SuspiciousActivityType,
        Description = r.Description,
        Amount = r.Amount,
        Currency = r.Currency,
        RedFlags = r.RedFlags,
        Status = r.Status,
        DetectedAt = r.DetectedAt,
        ReportingDeadline = r.ReportingDeadline,
        UAFReportNumber = r.UAFReportNumber,
        SentToUAFAt = r.SentToUAFAt,
        CreatedByName = r.CreatedByName,
        CreatedAt = r.CreatedAt
    };
}

/// <summary>
/// Handler para agregar entrada a lista de control
/// </summary>
public class AddWatchlistEntryHandler : IRequestHandler<AddWatchlistEntryCommand, WatchlistEntryDto>
{
    private readonly IWatchlistRepository _repository;

    public AddWatchlistEntryHandler(IWatchlistRepository repository)
    {
        _repository = repository;
    }

    public async Task<WatchlistEntryDto> Handle(AddWatchlistEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = new WatchlistEntry
        {
            Id = Guid.NewGuid(),
            ListType = request.ListType,
            Source = request.Source,
            FullName = request.FullName,
            Aliases = request.Aliases ?? new List<string>(),
            DocumentNumber = request.DocumentNumber,
            DateOfBirth = request.DateOfBirth,
            Nationality = request.Nationality,
            Details = request.Details,
            ListedDate = DateTime.UtcNow,
            IsActive = true
        };

        var created = await _repository.CreateAsync(entry, cancellationToken);

        return new WatchlistEntryDto
        {
            Id = created.Id,
            ListType = created.ListType,
            Source = created.Source,
            FullName = created.FullName,
            Aliases = created.Aliases,
            DocumentNumber = created.DocumentNumber,
            DateOfBirth = created.DateOfBirth,
            Nationality = created.Nationality,
            Details = created.Details,
            ListedDate = created.ListedDate,
            IsActive = created.IsActive
        };
    }
}

/// <summary>
/// Handler para screening de watchlist
/// </summary>
public class ScreenWatchlistHandler : IRequestHandler<ScreenWatchlistCommand, ScreeningResultDto>
{
    private readonly IWatchlistRepository _repository;

    public ScreenWatchlistHandler(IWatchlistRepository repository)
    {
        _repository = repository;
    }

    public async Task<ScreeningResultDto> Handle(ScreenWatchlistCommand request, CancellationToken cancellationToken)
    {
        var matches = await _repository.ScreenAsync(
            request.FullName,
            request.DocumentNumber,
            request.DateOfBirth,
            cancellationToken);

        return new ScreeningResultDto
        {
            HasMatches = matches.Count > 0,
            TotalMatches = matches.Count,
            Matches = matches.Select(m => new WatchlistMatchDto
            {
                Entry = new WatchlistEntryDto
                {
                    Id = m.Entry.Id,
                    ListType = m.Entry.ListType,
                    Source = m.Entry.Source,
                    FullName = m.Entry.FullName,
                    Aliases = m.Entry.Aliases,
                    DocumentNumber = m.Entry.DocumentNumber,
                    DateOfBirth = m.Entry.DateOfBirth,
                    Nationality = m.Entry.Nationality,
                    Details = m.Entry.Details,
                    ListedDate = m.Entry.ListedDate,
                    IsActive = m.Entry.IsActive
                },
                MatchScore = m.MatchScore,
                MatchedFields = m.MatchedFields,
                IsExactMatch = m.IsExactMatch
            }).ToList(),
            ScreenedAt = DateTime.UtcNow
        };
    }
}
