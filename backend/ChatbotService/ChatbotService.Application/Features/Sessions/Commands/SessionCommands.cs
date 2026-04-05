using MediatR;
using ChatbotService.Application.DTOs;
using ChatbotService.Domain.Enums;

namespace ChatbotService.Application.Features.Sessions.Commands;

/// <summary>
/// Comando para iniciar una nueva sesión de chat.
/// Soporta los dos modos: SingleVehicle y DealerInventory.
/// </summary>
public record StartSessionCommand(
    Guid? UserId,
    string? UserName,
    string? UserEmail,
    string? UserPhone,
    string SessionType,
    string Channel,
    string? ChannelUserId,
    string? UserAgent,
    string? IpAddress,
    string? DeviceType,
    string Language,
    Guid? DealerId,
    /// <summary>Modo de chat: "single_vehicle", "dealer_inventory", "general"</summary>
    string? ChatMode = null,
    /// <summary>ID del vehículo específico (requerido para modo SingleVehicle)</summary>
    Guid? VehicleId = null,
    /// <summary>Nombre del dealer enviado por el frontend (fallback cuando el config global no tiene nombre del dealer real)</summary>
    string? DealerName = null
) : IRequest<StartSessionResponse>;

/// <summary>
/// Comando para enviar un mensaje al chatbot.
/// El handler selecciona automáticamente la estrategia según el ChatMode de la sesión.
/// </summary>
public record SendMessageCommand(
    string SessionToken,
    string Message,
    string MessageType,
    string? MediaUrl
) : IRequest<ChatbotResponse>;

/// <summary>
/// Comando para finalizar una sesión
/// </summary>
public record EndSessionCommand(
    string SessionToken,
    string? Reason
) : IRequest<bool>;

/// <summary>
/// Comando para transferir a un agente humano
/// </summary>
public record TransferToAgentCommand(
    string SessionToken,
    string? Reason,
    Guid? PreferredAgentId
) : IRequest<TransferToAgentResult>;

public record TransferToAgentResult(
    bool Success,
    string? AgentName,
    string? Message,
    int? EstimatedWaitTimeMinutes
);

/// <summary>
/// Comando para que un dealer tome control de una sesión WhatsApp (handoff bot→humano)
/// </summary>
public record TakeOverSessionCommand(
    string SessionToken,
    Guid AgentId,
    string AgentName,
    string? Reason
) : IRequest<HandoffResult>;

/// <summary>
/// Comando para devolver control al bot (handoff humano→bot)
/// </summary>
public record ReturnToBotCommand(
    string SessionToken
) : IRequest<HandoffResult>;

/// <summary>
/// Comando para aceptar el disclosure de bot y política de privacidad.
/// Debe ejecutarse después de StartSession y antes del primer SendMessage.
/// </summary>
public record AcceptDisclosureCommand(
    string SessionToken
) : IRequest<AcceptDisclosureResult>;

public record AcceptDisclosureResult(
    bool Success,
    string Message,
    DateTime? ConsentAcceptedAt
);

public record HandoffResult(
    bool Success,
    string Message,
    HandoffStatus NewStatus
);
