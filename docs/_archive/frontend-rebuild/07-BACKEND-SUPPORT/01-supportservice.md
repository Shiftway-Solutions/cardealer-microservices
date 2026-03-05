# 🎧 SupportService - Especificación Completa

> **Tiempo estimado:** 4-6 horas (backend)
> **Prerrequisitos:** Proyecto .NET 8, PostgreSQL, RabbitMQ
> **Puerto:** 5063

---

## 📋 OBJETIVO

Crear el microservicio de Soporte al Cliente que permita:

- Sistema de tickets con estados y prioridades
- Categorización de tickets
- Historial de conversaciones
- Asignación a agentes
- SLA tracking
- Base de conocimiento (FAQ)
- Integración con NotificationService

---

## 🏗️ ARQUITECTURA

```
SupportService/
├── SupportService.Api/
│   ├── Controllers/
│   │   ├── TicketsController.cs
│   │   ├── CategoriesController.cs
│   │   ├── FaqController.cs
│   │   └── AgentsController.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── SupportService.Application/
│   ├── Features/
│   │   ├── Tickets/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateTicketCommand.cs
│   │   │   │   ├── UpdateTicketCommand.cs
│   │   │   │   ├── AssignTicketCommand.cs
│   │   │   │   ├── CloseTicketCommand.cs
│   │   │   │   └── AddMessageCommand.cs
│   │   │   └── Queries/
│   │   │       ├── GetTicketByIdQuery.cs
│   │   │       ├── GetTicketsQuery.cs
│   │   │       └── GetUserTicketsQuery.cs
│   │   ├── Categories/
│   │   ├── Faq/
│   │   └── Agents/
│   ├── DTOs/
│   └── Validators/
├── SupportService.Domain/
│   ├── Entities/
│   │   ├── Ticket.cs
│   │   ├── TicketMessage.cs
│   │   ├── TicketCategory.cs
│   │   ├── FaqArticle.cs
│   │   └── SupportAgent.cs
│   ├── Enums/
│   │   ├── TicketStatus.cs
│   │   ├── TicketPriority.cs
│   │   └── MessageSender.cs
│   └── Interfaces/
├── SupportService.Infrastructure/
│   ├── Persistence/
│   │   ├── SupportDbContext.cs
│   │   └── Repositories/
│   └── Services/
└── SupportService.Tests/
```

---

## 🔧 PASO 1: Domain Layer

### Enums

```csharp
// filepath: SupportService.Domain/Enums/TicketStatus.cs
namespace SupportService.Domain.Enums;

public enum TicketStatus
{
    Open = 0,
    InProgress = 1,
    WaitingForCustomer = 2,
    WaitingForAgent = 3,
    Resolved = 4,
    Closed = 5,
    Reopened = 6
}
```

```csharp
// filepath: SupportService.Domain/Enums/TicketPriority.cs
namespace SupportService.Domain.Enums;

public enum TicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3,
    Critical = 4
}
```

```csharp
// filepath: SupportService.Domain/Enums/MessageSender.cs
namespace SupportService.Domain.Enums;

public enum MessageSender
{
    Customer = 0,
    Agent = 1,
    System = 2
}
```

### Entities

```csharp
// filepath: SupportService.Domain/Entities/TicketCategory.cs
using System.ComponentModel.DataAnnotations;

namespace SupportService.Domain.Entities;

public class TicketCategory
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty;

    public Guid? ParentCategoryId { get; set; }
    public TicketCategory? ParentCategory { get; set; }

    public ICollection<TicketCategory> SubCategories { get; set; } = new List<TicketCategory>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<FaqArticle> FaqArticles { get; set; } = new List<FaqArticle>();

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

```csharp
// filepath: SupportService.Domain/Entities/SupportAgent.cs
using System.ComponentModel.DataAnnotations;

namespace SupportService.Domain.Entities;

public class SupportAgent
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public bool IsAvailable { get; set; } = true;
    public bool IsAdmin { get; set; } = false;

    public int MaxConcurrentTickets { get; set; } = 10;
    public int CurrentTicketCount { get; set; } = 0;

    public ICollection<Guid> AssignedCategoryIds { get; set; } = new List<Guid>();
    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();

    public DateTime? LastActiveAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

```csharp
// filepath: SupportService.Domain/Entities/Ticket.cs
using System.ComponentModel.DataAnnotations;
using SupportService.Domain.Enums;

namespace SupportService.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string TicketNumber { get; set; } = string.Empty; // OKLA-2024-0001

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public Guid CategoryId { get; set; }
    public TicketCategory Category { get; set; } = null!;

    public Guid? AssignedAgentId { get; set; }
    public SupportAgent? AssignedAgent { get; set; }

    // Related to
    public Guid? VehicleId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? DealerId { get; set; }

    // Contact info (can be different from user)
    [MaxLength(100)]
    public string ContactName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(255)]
    public string ContactEmail { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    // SLA Tracking
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? SlaMinutesToFirstResponse { get; set; }
    public int? SlaMinutesToResolution { get; set; }
    public bool IsSlaBreached { get; set; } = false;

    // Metadata
    [MaxLength(50)]
    public string? Source { get; set; } // web, mobile, email, whatsapp

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
    public ICollection<string> Tags { get; set; } = new List<string>();
    public ICollection<string> AttachmentUrls { get; set; } = new List<string>();

    // Satisfaction
    public int? SatisfactionRating { get; set; } // 1-5
    public string? SatisfactionComment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Helper methods
    public void MarkAsInProgress(Guid agentId)
    {
        Status = TicketStatus.InProgress;
        AssignedAgentId = agentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFirstResponse()
    {
        if (FirstResponseAt == null)
        {
            FirstResponseAt = DateTime.UtcNow;
            var responseTime = (FirstResponseAt.Value - CreatedAt).TotalMinutes;
            SlaMinutesToFirstResponse = (int)responseTime;
        }
    }

    public void Resolve()
    {
        Status = TicketStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        var resolutionTime = (ResolvedAt.Value - CreatedAt).TotalMinutes;
        SlaMinutesToResolution = (int)resolutionTime;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        Status = TicketStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        Status = TicketStatus.Reopened;
        ResolvedAt = null;
        ClosedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public static string GenerateTicketNumber()
    {
        var year = DateTime.UtcNow.Year;
        var random = new Random();
        var number = random.Next(1, 99999);
        return $"OKLA-{year}-{number:D5}";
    }
}
```

```csharp
// filepath: SupportService.Domain/Entities/TicketMessage.cs
using System.ComponentModel.DataAnnotations;
using SupportService.Domain.Enums;

namespace SupportService.Domain.Entities;

public class TicketMessage
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    [Required]
    public string Content { get; set; } = string.Empty;

    public MessageSender SenderType { get; set; }

    public Guid? SenderId { get; set; } // UserId or AgentId

    [MaxLength(100)]
    public string SenderName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? SenderAvatarUrl { get; set; }

    public ICollection<string> AttachmentUrls { get; set; } = new List<string>();

    public bool IsInternal { get; set; } = false; // Internal notes not visible to customer

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
}
```

```csharp
// filepath: SupportService.Domain/Entities/FaqArticle.cs
using System.ComponentModel.DataAnnotations;

namespace SupportService.Domain.Entities;

public class FaqArticle
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }
    public TicketCategory Category { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty; // Markdown

    [MaxLength(300)]
    public string? Summary { get; set; }

    public ICollection<string> Tags { get; set; } = new List<string>();

    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; } = false;

    public int ViewCount { get; set; } = 0;
    public int HelpfulCount { get; set; } = 0;
    public int NotHelpfulCount { get; set; } = 0;

    public int SortOrder { get; set; } = 0;

    public Guid? AuthorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
}
```

### Interfaces

```csharp
// filepath: SupportService.Domain/Interfaces/ITicketRepository.cs
using SupportService.Domain.Entities;
using SupportService.Domain.Enums;

namespace SupportService.Domain.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Ticket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken ct = default);

    Task<(IEnumerable<Ticket> Items, int TotalCount)> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        TicketStatus? status = null,
        TicketPriority? priority = null,
        Guid? categoryId = null,
        Guid? assignedAgentId = null,
        string? searchTerm = null,
        CancellationToken ct = default);

    Task<(IEnumerable<Ticket> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        TicketStatus? status = null,
        CancellationToken ct = default);

    Task<Ticket> CreateAsync(Ticket ticket, CancellationToken ct = default);
    Task<Ticket> UpdateAsync(Ticket ticket, CancellationToken ct = default);

    Task<TicketMessage> AddMessageAsync(TicketMessage message, CancellationToken ct = default);
    Task<IEnumerable<TicketMessage>> GetMessagesAsync(Guid ticketId, CancellationToken ct = default);

    // Statistics
    Task<int> GetOpenTicketsCountAsync(CancellationToken ct = default);
    Task<int> GetAgentTicketCountAsync(Guid agentId, CancellationToken ct = default);
    Task<double> GetAverageResolutionTimeAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
```

```csharp
// filepath: SupportService.Domain/Interfaces/IFaqRepository.cs
using SupportService.Domain.Entities;

namespace SupportService.Domain.Interfaces;

public interface IFaqRepository
{
    Task<FaqArticle?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FaqArticle?> GetBySlugAsync(string slug, CancellationToken ct = default);

    Task<(IEnumerable<FaqArticle> Items, int TotalCount)> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        Guid? categoryId = null,
        bool? isPublished = true,
        string? searchTerm = null,
        CancellationToken ct = default);

    Task<IEnumerable<FaqArticle>> GetFeaturedAsync(int count = 5, CancellationToken ct = default);
    Task<IEnumerable<FaqArticle>> GetPopularAsync(int count = 10, CancellationToken ct = default);

    Task<FaqArticle> CreateAsync(FaqArticle article, CancellationToken ct = default);
    Task<FaqArticle> UpdateAsync(FaqArticle article, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task IncrementViewCountAsync(Guid id, CancellationToken ct = default);
    Task IncrementHelpfulCountAsync(Guid id, bool isHelpful, CancellationToken ct = default);
}
```

---

## 🔧 PASO 2: Application Layer

### DTOs

```csharp
// filepath: SupportService.Application/DTOs/TicketDtos.cs
using SupportService.Domain.Enums;

namespace SupportService.Application.DTOs;

public record TicketDto(
    Guid Id,
    string TicketNumber,
    Guid UserId,
    string Subject,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid CategoryId,
    string CategoryName,
    Guid? AssignedAgentId,
    string? AssignedAgentName,
    string ContactName,
    string ContactEmail,
    string? ContactPhone,
    Guid? VehicleId,
    Guid? OrderId,
    DateTime? FirstResponseAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    bool IsSlaBreached,
    string? Source,
    IEnumerable<string> Tags,
    IEnumerable<string> AttachmentUrls,
    int? SatisfactionRating,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record TicketSummaryDto(
    Guid Id,
    string TicketNumber,
    string Subject,
    TicketStatus Status,
    TicketPriority Priority,
    string CategoryName,
    string? AssignedAgentName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record TicketMessageDto(
    Guid Id,
    Guid TicketId,
    string Content,
    MessageSender SenderType,
    Guid? SenderId,
    string SenderName,
    string? SenderAvatarUrl,
    IEnumerable<string> AttachmentUrls,
    bool IsInternal,
    DateTime CreatedAt,
    DateTime? EditedAt
);

public record CreateTicketDto(
    string Subject,
    string Description,
    Guid CategoryId,
    TicketPriority Priority = TicketPriority.Medium,
    string ContactName = "",
    string ContactEmail = "",
    string? ContactPhone = null,
    Guid? VehicleId = null,
    Guid? OrderId = null,
    Guid? DealerId = null,
    string? Source = null,
    IEnumerable<string>? Tags = null,
    IEnumerable<string>? AttachmentUrls = null
);

public record UpdateTicketDto(
    string? Subject = null,
    string? Description = null,
    Guid? CategoryId = null,
    TicketPriority? Priority = null,
    TicketStatus? Status = null,
    IEnumerable<string>? Tags = null
);

public record AddMessageDto(
    string Content,
    IEnumerable<string>? AttachmentUrls = null,
    bool IsInternal = false
);

public record AssignTicketDto(
    Guid AgentId
);

public record RateTicketDto(
    int Rating, // 1-5
    string? Comment = null
);
```

```csharp
// filepath: SupportService.Application/DTOs/FaqDtos.cs
namespace SupportService.Application.DTOs;

public record FaqArticleDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Title,
    string Slug,
    string Content,
    string? Summary,
    IEnumerable<string> Tags,
    bool IsFeatured,
    int ViewCount,
    int HelpfulCount,
    int NotHelpfulCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt
);

public record FaqArticleSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    string CategoryName,
    int ViewCount,
    bool IsFeatured
);

public record CreateFaqArticleDto(
    Guid CategoryId,
    string Title,
    string Content,
    string? Summary = null,
    IEnumerable<string>? Tags = null,
    bool IsPublished = true,
    bool IsFeatured = false
);

public record UpdateFaqArticleDto(
    string? Title = null,
    string? Content = null,
    string? Summary = null,
    Guid? CategoryId = null,
    IEnumerable<string>? Tags = null,
    bool? IsPublished = null,
    bool? IsFeatured = null
);
```

### Commands

```csharp
// filepath: SupportService.Application/Features/Tickets/Commands/CreateTicketCommand.cs
using MediatR;
using SupportService.Application.DTOs;
using SupportService.Domain.Entities;
using SupportService.Domain.Interfaces;

namespace SupportService.Application.Features.Tickets.Commands;

public record CreateTicketCommand(
    Guid UserId,
    CreateTicketDto Data
) : IRequest<TicketDto>;

public class CreateTicketHandler : IRequestHandler<CreateTicketCommand, TicketDto>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreateTicketHandler(
        ITicketRepository ticketRepository,
        ICategoryRepository categoryRepository)
    {
        _ticketRepository = ticketRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<TicketDto> Handle(CreateTicketCommand request, CancellationToken ct)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Data.CategoryId, ct)
            ?? throw new ArgumentException("Category not found");

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TicketNumber = Ticket.GenerateTicketNumber(),
            UserId = request.UserId,
            Subject = request.Data.Subject,
            Description = request.Data.Description,
            CategoryId = request.Data.CategoryId,
            Priority = request.Data.Priority,
            ContactName = request.Data.ContactName,
            ContactEmail = request.Data.ContactEmail,
            ContactPhone = request.Data.ContactPhone,
            VehicleId = request.Data.VehicleId,
            OrderId = request.Data.OrderId,
            DealerId = request.Data.DealerId,
            Source = request.Data.Source,
            Tags = request.Data.Tags?.ToList() ?? new List<string>(),
            AttachmentUrls = request.Data.AttachmentUrls?.ToList() ?? new List<string>()
        };

        // Add initial message from user
        var initialMessage = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Content = request.Data.Description,
            SenderType = Domain.Enums.MessageSender.Customer,
            SenderId = request.UserId,
            SenderName = request.Data.ContactName,
            AttachmentUrls = request.Data.AttachmentUrls?.ToList() ?? new List<string>()
        };

        ticket.Messages.Add(initialMessage);

        var created = await _ticketRepository.CreateAsync(ticket, ct);

        // TODO: Publish TicketCreatedEvent to RabbitMQ
        // TODO: Send confirmation email via NotificationService

        return MapToDto(created, category.Name);
    }

    private static TicketDto MapToDto(Ticket ticket, string categoryName)
    {
        return new TicketDto(
            ticket.Id,
            ticket.TicketNumber,
            ticket.UserId,
            ticket.Subject,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            ticket.CategoryId,
            categoryName,
            ticket.AssignedAgentId,
            ticket.AssignedAgent?.DisplayName,
            ticket.ContactName,
            ticket.ContactEmail,
            ticket.ContactPhone,
            ticket.VehicleId,
            ticket.OrderId,
            ticket.FirstResponseAt,
            ticket.ResolvedAt,
            ticket.ClosedAt,
            ticket.IsSlaBreached,
            ticket.Source,
            ticket.Tags,
            ticket.AttachmentUrls,
            ticket.SatisfactionRating,
            ticket.CreatedAt,
            ticket.UpdatedAt
        );
    }
}
```

```csharp
// filepath: SupportService.Application/Features/Tickets/Commands/AddMessageCommand.cs
using MediatR;
using SupportService.Application.DTOs;
using SupportService.Domain.Entities;
using SupportService.Domain.Enums;
using SupportService.Domain.Interfaces;

namespace SupportService.Application.Features.Tickets.Commands;

public record AddMessageCommand(
    Guid TicketId,
    Guid SenderId,
    MessageSender SenderType,
    string SenderName,
    string? SenderAvatarUrl,
    AddMessageDto Data
) : IRequest<TicketMessageDto>;

public class AddMessageHandler : IRequestHandler<AddMessageCommand, TicketMessageDto>
{
    private readonly ITicketRepository _ticketRepository;

    public AddMessageHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<TicketMessageDto> Handle(AddMessageCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct)
            ?? throw new ArgumentException("Ticket not found");

        // Update ticket status based on sender
        if (request.SenderType == MessageSender.Agent)
        {
            ticket.MarkFirstResponse();
            if (ticket.Status == TicketStatus.Open)
            {
                ticket.Status = TicketStatus.InProgress;
            }
            else if (ticket.Status == TicketStatus.WaitingForAgent)
            {
                ticket.Status = TicketStatus.WaitingForCustomer;
            }
        }
        else if (request.SenderType == MessageSender.Customer)
        {
            if (ticket.Status == TicketStatus.WaitingForCustomer)
            {
                ticket.Status = TicketStatus.WaitingForAgent;
            }
            else if (ticket.Status == TicketStatus.Resolved)
            {
                ticket.Reopen();
            }
        }

        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            Content = request.Data.Content,
            SenderType = request.SenderType,
            SenderId = request.SenderId,
            SenderName = request.SenderName,
            SenderAvatarUrl = request.SenderAvatarUrl,
            AttachmentUrls = request.Data.AttachmentUrls?.ToList() ?? new List<string>(),
            IsInternal = request.Data.IsInternal
        };

        await _ticketRepository.UpdateAsync(ticket, ct);
        var created = await _ticketRepository.AddMessageAsync(message, ct);

        // TODO: Send notification to other party

        return new TicketMessageDto(
            created.Id,
            created.TicketId,
            created.Content,
            created.SenderType,
            created.SenderId,
            created.SenderName,
            created.SenderAvatarUrl,
            created.AttachmentUrls,
            created.IsInternal,
            created.CreatedAt,
            created.EditedAt
        );
    }
}
```

---

## 🔧 PASO 3: API Layer

### Controller

```csharp
// filepath: SupportService.Api/Controllers/TicketsController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportService.Application.DTOs;
using SupportService.Application.Features.Tickets.Commands;
using SupportService.Application.Features.Tickets.Queries;
using SupportService.Domain.Enums;

namespace SupportService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated list of tickets (admin/agent only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin,support_agent")]
    public async Task<ActionResult<PaginatedResponse<TicketSummaryDto>>> GetTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? assignedAgentId = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = new GetTicketsQuery(page, pageSize, status, priority, categoryId, assignedAgentId, searchTerm);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get current user's tickets
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<PaginatedResponse<TicketSummaryDto>>> GetMyTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TicketStatus? status = null,
        CancellationToken ct = default)
    {
        var userId = GetUserIdFromClaims();
        var query = new GetUserTicketsQuery(userId, page, pageSize, status);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get ticket by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<TicketDto>> GetTicket(Guid id, CancellationToken ct)
    {
        var query = new GetTicketByIdQuery(id);
        var result = await _mediator.Send(query, ct);

        if (result == null)
            return NotFound();

        // Check access: user can only see their own tickets unless admin/agent
        var userId = GetUserIdFromClaims();
        if (result.UserId != userId && !User.IsInRole("admin") && !User.IsInRole("support_agent"))
            return Forbid();

        return Ok(result);
    }

    /// <summary>
    /// Create new support ticket
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TicketDto>> CreateTicket(
        [FromBody] CreateTicketDto dto,
        CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        var command = new CreateTicketCommand(userId, dto);
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetTicket), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update ticket
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<TicketDto>> UpdateTicket(
        Guid id,
        [FromBody] UpdateTicketDto dto,
        CancellationToken ct)
    {
        var command = new UpdateTicketCommand(id, dto);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Add message to ticket
    /// </summary>
    [HttpPost("{id:guid}/messages")]
    [Authorize]
    public async Task<ActionResult<TicketMessageDto>> AddMessage(
        Guid id,
        [FromBody] AddMessageDto dto,
        CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        var userName = User.FindFirst("name")?.Value ?? "User";
        var avatarUrl = User.FindFirst("picture")?.Value;

        var senderType = User.IsInRole("support_agent") || User.IsInRole("admin")
            ? MessageSender.Agent
            : MessageSender.Customer;

        var command = new AddMessageCommand(id, userId, senderType, userName, avatarUrl, dto);
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetTicket), new { id }, result);
    }

    /// <summary>
    /// Assign ticket to agent
    /// </summary>
    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "admin,support_agent")]
    public async Task<ActionResult<TicketDto>> AssignTicket(
        Guid id,
        [FromBody] AssignTicketDto dto,
        CancellationToken ct)
    {
        var command = new AssignTicketCommand(id, dto.AgentId);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Close ticket
    /// </summary>
    [HttpPost("{id:guid}/close")]
    [Authorize]
    public async Task<ActionResult<TicketDto>> CloseTicket(Guid id, CancellationToken ct)
    {
        var command = new CloseTicketCommand(id);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Rate resolved ticket
    /// </summary>
    [HttpPost("{id:guid}/rate")]
    [Authorize]
    public async Task<ActionResult> RateTicket(
        Guid id,
        [FromBody] RateTicketDto dto,
        CancellationToken ct)
    {
        var command = new RateTicketCommand(id, dto.Rating, dto.Comment);
        await _mediator.Send(command, ct);
        return NoContent();
    }

    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst("id")?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in claims");
        return Guid.Parse(userIdClaim);
    }
}
```

---

## 🔧 PASO 4: Dockerfile

```dockerfile
# filepath: SupportService/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SupportService.Api/SupportService.Api.csproj", "SupportService.Api/"]
COPY ["SupportService.Application/SupportService.Application.csproj", "SupportService.Application/"]
COPY ["SupportService.Domain/SupportService.Domain.csproj", "SupportService.Domain/"]
COPY ["SupportService.Infrastructure/SupportService.Infrastructure.csproj", "SupportService.Infrastructure/"]

RUN dotnet restore "SupportService.Api/SupportService.Api.csproj"

COPY . .
WORKDIR "/src/SupportService.Api"
RUN dotnet build "SupportService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SupportService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SupportService.Api.dll"]
```

---

## 🎨 PASO 5: Frontend - Páginas

### Página de Soporte

```typescript
// filepath: src/app/soporte/page.tsx
import { Metadata } from "next";
import { SupportCenter } from "@/components/support/SupportCenter";
import { FaqSection } from "@/components/support/FaqSection";
import { ContactInfo } from "@/components/support/ContactInfo";

export const metadata: Metadata = {
  title: "Centro de Soporte | OKLA",
  description: "Obtén ayuda, crea tickets de soporte y encuentra respuestas a tus preguntas",
};

export default function SupportPage() {
  return (
    <div className="container py-8 md:py-12">
      {/* Hero */}
      <div className="text-center mb-12">
        <h1 className="text-3xl md:text-4xl font-bold text-gray-900 mb-4">
          ¿Cómo podemos ayudarte?
        </h1>
        <p className="text-lg text-gray-600 max-w-2xl mx-auto">
          Encuentra respuestas en nuestra base de conocimiento o contacta a nuestro equipo de soporte
        </p>
      </div>

      {/* Quick Actions */}
      <div className="grid md:grid-cols-3 gap-6 mb-12">
        <QuickActionCard
          icon={<MessageSquare className="h-8 w-8" />}
          title="Crear Ticket"
          description="Contacta a nuestro equipo de soporte"
          href="/soporte/nuevo-ticket"
          variant="primary"
        />
        <QuickActionCard
          icon={<FileQuestion className="h-8 w-8" />}
          title="Preguntas Frecuentes"
          description="Respuestas a las dudas más comunes"
          href="/soporte/faq"
        />
        <QuickActionCard
          icon={<TicketIcon className="h-8 w-8" />}
          title="Mis Tickets"
          description="Ver el estado de tus solicitudes"
          href="/soporte/mis-tickets"
        />
      </div>

      {/* Featured FAQ */}
      <FaqSection />

      {/* Contact Info */}
      <ContactInfo />
    </div>
  );
}
```

### Página de Nuevo Ticket

```typescript
// filepath: src/app/soporte/nuevo-ticket/page.tsx
"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Textarea } from "@/components/ui/Textarea";
import { Select } from "@/components/ui/Select";
import { FileUpload } from "@/components/ui/FileUpload";
import { useCreateTicket } from "@/lib/hooks/useSupport";
import { useSupportCategories } from "@/lib/hooks/useSupport";
import { toast } from "sonner";

const ticketSchema = z.object({
  subject: z.string().min(5, "El asunto debe tener al menos 5 caracteres").max(200),
  description: z.string().min(20, "La descripción debe tener al menos 20 caracteres"),
  categoryId: z.string().uuid("Selecciona una categoría"),
  priority: z.enum(["low", "medium", "high", "urgent"]).default("medium"),
  contactName: z.string().min(2, "Ingresa tu nombre"),
  contactEmail: z.string().email("Email inválido"),
  contactPhone: z.string().optional(),
  vehicleId: z.string().uuid().optional(),
  orderId: z.string().uuid().optional(),
});

type TicketFormData = z.infer<typeof ticketSchema>;

export default function NewTicketPage() {
  const router = useRouter();
  const [attachments, setAttachments] = useState<string[]>([]);
  const { data: categories } = useSupportCategories();
  const createTicket = useCreateTicket();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<TicketFormData>({
    resolver: zodResolver(ticketSchema),
    defaultValues: {
      priority: "medium",
    },
  });

  const onSubmit = async (data: TicketFormData) => {
    try {
      const result = await createTicket.mutateAsync({
        ...data,
        attachmentUrls: attachments,
        source: "web",
      });

      toast.success("Ticket creado exitosamente", {
        description: `Número de ticket: ${result.ticketNumber}`,
      });

      router.push(`/soporte/tickets/${result.id}`);
    } catch (error) {
      toast.error("Error al crear el ticket", {
        description: "Por favor intenta de nuevo",
      });
    }
  };

  return (
    <div className="container max-w-2xl py-8 md:py-12">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">
          Crear Nuevo Ticket de Soporte
        </h1>
        <p className="text-gray-600">
          Describe tu problema y te responderemos lo antes posible
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
        {/* Category */}
        <Select
          label="Categoría"
          {...register("categoryId")}
          error={errors.categoryId?.message}
          options={
            categories?.data.map((cat) => ({
              value: cat.id,
              label: cat.name,
            })) ?? []
          }
          placeholder="Selecciona una categoría"
        />

        {/* Subject */}
        <Input
          label="Asunto"
          {...register("subject")}
          error={errors.subject?.message}
          placeholder="Describe brevemente tu problema"
        />

        {/* Description */}
        <Textarea
          label="Descripción"
          {...register("description")}
          error={errors.description?.message}
          placeholder="Proporciona todos los detalles que puedan ayudarnos a resolver tu problema..."
          rows={6}
        />

        {/* Priority */}
        <Select
          label="Prioridad"
          {...register("priority")}
          options={[
            { value: "low", label: "Baja - No urgente" },
            { value: "medium", label: "Media - Normal" },
            { value: "high", label: "Alta - Importante" },
            { value: "urgent", label: "Urgente - Crítico" },
          ]}
        />

        {/* Contact Info */}
        <div className="grid md:grid-cols-2 gap-4">
          <Input
            label="Tu Nombre"
            {...register("contactName")}
            error={errors.contactName?.message}
          />
          <Input
            label="Tu Email"
            type="email"
            {...register("contactEmail")}
            error={errors.contactEmail?.message}
          />
        </div>

        <Input
          label="Teléfono (opcional)"
          {...register("contactPhone")}
          placeholder="809-123-4567"
        />

        {/* Attachments */}
        <FileUpload
          label="Archivos Adjuntos (opcional)"
          accept="image/*,.pdf,.doc,.docx"
          maxFiles={5}
          maxSize={10 * 1024 * 1024} // 10MB
          value={attachments}
          onChange={setAttachments}
          helperText="Máximo 5 archivos, 10MB cada uno"
        />

        {/* Submit */}
        <div className="flex gap-4">
          <Button
            type="button"
            variant="outline"
            onClick={() => router.back()}
          >
            Cancelar
          </Button>
          <Button
            type="submit"
            loading={isSubmitting || createTicket.isPending}
          >
            Crear Ticket
          </Button>
        </div>
      </form>
    </div>
  );
}
```

---

## ✅ VALIDACIÓN

### Comandos de verificación

```bash
# Backend
cd backend/SupportService
dotnet build
dotnet test

# Verificar migración
dotnet ef migrations add InitialCreate

# Ejecutar API
dotnet run --project SupportService.Api

# Verificar endpoints
curl http://localhost:5063/health
curl http://localhost:5063/swagger
```

### Endpoints a verificar

| Método | Endpoint                   | Auth        | Descripción             |
| ------ | -------------------------- | ----------- | ----------------------- |
| GET    | /api/tickets               | Admin/Agent | Lista todos los tickets |
| GET    | /api/tickets/my            | User        | Tickets del usuario     |
| GET    | /api/tickets/{id}          | User/Owner  | Detalle de ticket       |
| POST   | /api/tickets               | User        | Crear ticket            |
| PUT    | /api/tickets/{id}          | User/Admin  | Actualizar ticket       |
| POST   | /api/tickets/{id}/messages | User        | Agregar mensaje         |
| POST   | /api/tickets/{id}/assign   | Admin       | Asignar a agente        |
| POST   | /api/tickets/{id}/close    | User        | Cerrar ticket           |
| POST   | /api/tickets/{id}/rate     | User        | Calificar servicio      |
| GET    | /api/faq                   | Public      | Lista FAQ               |
| GET    | /api/faq/{slug}            | Public      | Artículo FAQ            |
| GET    | /api/categories            | Public      | Categorías de soporte   |

---

## 📊 RESUMEN

### Entidades

| Entidad        | Campos Clave                                                         |
| -------------- | -------------------------------------------------------------------- |
| Ticket         | TicketNumber, Subject, Status, Priority, CategoryId, AssignedAgentId |
| TicketMessage  | Content, SenderType, SenderId, IsInternal                            |
| TicketCategory | Name, Slug, ParentCategoryId                                         |
| FaqArticle     | Title, Slug, Content, CategoryId, ViewCount                          |
| SupportAgent   | UserId, DisplayName, IsAvailable, MaxConcurrentTickets               |

### Endpoints

- **Tickets:** 9 endpoints
- **FAQ:** 4 endpoints
- **Categories:** 3 endpoints
- **Agents:** 4 endpoints
- **Total:** ~20 endpoints

### Páginas Frontend

| Ruta                  | Descripción                  |
| --------------------- | ---------------------------- |
| /soporte              | Centro de soporte (home)     |
| /soporte/nuevo-ticket | Crear nuevo ticket           |
| /soporte/mis-tickets  | Lista de tickets del usuario |
| /soporte/tickets/{id} | Detalle y chat del ticket    |
| /soporte/faq          | Lista de artículos FAQ       |
| /soporte/faq/{slug}   | Artículo FAQ individual      |

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/03-COMPONENTES/01-layout.md`
