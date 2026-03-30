using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using ChatbotService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatbotService.Api.Services;

/// <summary>
/// Seeds the database with test data for 2 dealers with different chatbot personalities.
/// Only runs when the database is empty (no configurations exist).
/// </summary>
public static class ChatbotDataSeeder
{
    // Fixed IDs for reproducibility
    private static readonly Guid DefaultConfigId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Dealer1ConfigId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid Dealer2ConfigId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly Guid Dealer1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Dealer2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task SeedAsync(ChatbotDbContext db)
    {
        if (await db.ChatbotConfigurations.AnyAsync())
        {
            return; // Already seeded
        }

        // 1. Chatbot Configurations
        var configs = CreateConfigurations();
        db.ChatbotConfigurations.AddRange(configs);
        await db.SaveChangesAsync();

        // 2. Vehicles
        var vehicles = CreateVehicles();
        db.ChatbotVehicles.AddRange(vehicles);
        await db.SaveChangesAsync();

        // 3. Quick Responses
        var responses = CreateQuickResponses();
        db.QuickResponses.AddRange(responses);
        await db.SaveChangesAsync();
    }

    private static List<ChatbotConfiguration> CreateConfigurations()
    {
        return new List<ChatbotConfiguration>
        {
            // ── DEFAULT / GLOBAL — Fallback for any dealer without a specific config ──
            new ChatbotConfiguration
            {
                Id = DefaultConfigId,
                DealerId = null, // null = global default, used when no dealer-specific config found
                Name = "OKLA Bot Global",
                IsActive = true,
                Plan = ChatbotPlan.Standard,
                FreeInteractionsPerMonth = 10000,
                CostPerInteraction = 0.05m,
                MaxInteractionsPerSession = 30,
                MaxInteractionsPerUserPerDay = 100,
                MaxInteractionsPerUserPerMonth = 1000,
                MaxGlobalInteractionsPerDay = 50000,
                MaxGlobalInteractionsPerMonth = 1000000,
                BotName = "OKLA Bot",
                BotAvatarUrl = string.Empty,
                WelcomeMessage = "¡Hola! 👋 Soy el asistente virtual del dealer. Estoy aquí para ayudarte a encontrar el vehículo perfecto en República Dominicana. ¿En qué te puedo ayudar hoy?",
                OfflineMessage = "Estamos fuera de horario. Te atenderemos en el próximo horario laboral.",
                LimitReachedMessage = "Has alcanzado el límite de interacciones. ¿Te gustaría que un asesor te contacte directamente?",
                EnableWebChat = true,
                EnableAutoInventorySync = false,
                EnableAutoReports = false,
                EnableAutoLearning = false,
                EnableHealthMonitoring = false,
                TimeZone = "America/Santo_Domingo",
                LlmProjectId = "okla-claude",
                LlmModelId = "claude-haiku-4-5-20251001",
                LanguageCode = "es",
                SystemPromptText = SystemPromptDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // ── Dealer 1: Auto Dominicana Premium — Formal, premium ──
            new ChatbotConfiguration
            {
                Id = Dealer1ConfigId,
                DealerId = Dealer1Id,
                Name = "Auto Dominicana Premium Chatbot",
                IsActive = true,
                Plan = ChatbotPlan.Standard,
                FreeInteractionsPerMonth = 500,
                CostPerInteraction = 0.05m,
                MaxInteractionsPerSession = 25,
                MaxInteractionsPerUserPerDay = 100,
                MaxInteractionsPerUserPerMonth = 1000,
                MaxGlobalInteractionsPerDay = 10000,
                MaxGlobalInteractionsPerMonth = 200000,
                BotName = "Ana",
                BotAvatarUrl = string.Empty,
                ContactEmail = "ventas@autodominicana.com",
                WelcomeMessage = "¡Bienvenido a Auto Dominicana Premium! 🚗✨ Soy Ana, tu asistente virtual. Estoy aquí para ayudarte a encontrar el vehículo perfecto. ¿En qué puedo servirte hoy?",
                OfflineMessage = "Estamos fuera de horario. Nuestro equipo te atenderá en horario laboral: Lun-Vie 8AM-6PM, Sáb 9AM-2PM.",
                LimitReachedMessage = "Has alcanzado el límite de interacciones para esta sesión. ¿Te gustaría que un asesor te contacte directamente?",
                EnableWebChat = true,
                EnableAutoInventorySync = true,
                EnableAutoReports = true,
                EnableAutoLearning = true,
                EnableHealthMonitoring = true,
                TimeZone = "America/Santo_Domingo",
                LlmProjectId = "okla-claude",
                LlmModelId = "claude-haiku-4-5-20251001",
                LanguageCode = "es",
                // System prompt for LLM
                SystemPromptText = SystemPromptDealer1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // ── Dealer 2: MotorMax RD — Informal, accesible ──
            new ChatbotConfiguration
            {
                Id = Dealer2ConfigId,
                DealerId = Dealer2Id,
                Name = "MotorMax RD Chatbot",
                IsActive = true,
                Plan = ChatbotPlan.Standard,
                FreeInteractionsPerMonth = 300,
                CostPerInteraction = 0.05m,
                MaxInteractionsPerSession = 20,
                MaxInteractionsPerUserPerDay = 80,
                MaxInteractionsPerUserPerMonth = 800,
                MaxGlobalInteractionsPerDay = 8000,
                MaxGlobalInteractionsPerMonth = 150000,
                BotName = "Carlos",
                BotAvatarUrl = string.Empty,
                ContactEmail = "ventas@motormaxrd.com",
                WelcomeMessage = "¡Klk mi pana! 🔥 Soy Carlos de MotorMax RD. Tenemos los mejores precios en carros usados y nuevos. ¿Qué andas buscando?",
                OfflineMessage = "Tamo cerrado ahorita. Vuelve Lun-Sáb 9AM-7PM. ¡Te esperamos! 🤙",
                LimitReachedMessage = "Ya llegaste al límite de preguntas por ahora. Déjame tu número y te llamamos pa' ayudarte mejor.",
                EnableWebChat = true,
                EnableWhatsApp = true,
                EnableAutoInventorySync = true,
                EnableAutoReports = true,
                EnableAutoLearning = true,
                EnableHealthMonitoring = true,
                TimeZone = "America/Santo_Domingo",
                LlmProjectId = "okla-claude",
                LlmModelId = "claude-haiku-4-5-20251001",
                LanguageCode = "es",
                SystemPromptText = SystemPromptDealer2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        };
    }

    private static List<ChatbotVehicle> CreateVehicles()
    {
        var now = DateTime.UtcNow;
        return new List<ChatbotVehicle>
        {
            // ════════════════════════════════════════════
            // Dealer 1: Auto Dominicana Premium (8 vehículos)
            // ════════════════════════════════════════════
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer1ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Toyota", Model = "RAV4", Year = 2024, Trim = "XLE", Color = "Blanco Perla",
                Price = 2_850_000, BodyType = "SUV", FuelType = "Gasolina", Transmission = "Automática",
                Mileage = 5000, EngineSize = "2.5L",
                Description = "Toyota RAV4 2024 XLE en excelente estado. Safety Sense 3.0, pantalla táctil 10.5\", Android Auto/Apple CarPlay, cámara 360°. Garantía de fábrica vigente.",
                SearchableText = "toyota rav4 2024 xle blanco suv gasolina automatica yipeta",
                IsAvailable = true, IsFeatured = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer1ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Hyundai", Model = "Tucson", Year = 2024, Trim = "SEL", Color = "Negro",
                Price = 2_450_000, BodyType = "SUV", FuelType = "Gasolina", Transmission = "Automática",
                Mileage = 3000, EngineSize = "2.5L",
                Description = "Hyundai Tucson 2024 SEL. Diseño moderno, BlueLink connected services, asientos calefactados, techo panorámico.",
                SearchableText = "hyundai tucson 2024 sel negro suv gasolina automatica yipeta",
                IsAvailable = true, IsFeatured = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer1ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Honda", Model = "CR-V", Year = 2023, Trim = "EX-L", Color = "Gris Plateado",
                Price = 3_100_000, BodyType = "SUV", FuelType = "Gasolina", Transmission = "CVT",
                Mileage = 8000, EngineSize = "1.5L Turbo",
                Description = "Honda CR-V 2023 EX-L con motor turbo. Interior en cuero, Honda Sensing suite completo, wireless CarPlay.",
                SearchableText = "honda cr-v crv 2023 exl gris suv gasolina cvt turbo yipeta",
                IsAvailable = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer1ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Kia", Model = "Sportage", Year = 2024, Trim = "LX", Color = "Rojo",
                Price = 1_950_000, BodyType = "SUV", FuelType = "Gasolina", Transmission = "Automática",
                Mileage = 4000, EngineSize = "2.5L",
                Description = "Kia Sportage 2024 LX. Excelente relación precio-calidad. Cámara trasera, sensores de estacionamiento, pantalla 8\".",
                SearchableText = "kia sportage 2024 lx rojo suv gasolina automatica yipeta barato economico",
                IsAvailable = true, IsFeatured = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer1ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Toyota", Model = "Corolla", Year = 2024, Trim = "SE", Color = "Azul Celestial",
                Price = 1_650_000, BodyType = "Sedán", FuelType = "Gasolina", Transmission = "CVT",
                Mileage = 2000, EngineSize = "2.0L",
                Description = "Toyota Corolla 2024 SE. El sedán más confiable del mercado. TSS 3.0, eficiente en combustible, bajo mantenimiento.",
                SearchableText = "toyota corolla 2024 se azul sedan gasolina cvt carro economico",
                IsAvailable = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer1ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Honda", Model = "Civic", Year = 2024, Trim = "Sport", Color = "Negro",
                Price = 1_800_000, BodyType = "Sedán", FuelType = "Gasolina", Transmission = "CVT",
                Mileage = 1500, EngineSize = "2.0L",
                Description = "Honda Civic 2024 Sport. Diseño deportivo, Bose premium audio, Honda Sensing, aro 18\".",
                SearchableText = "honda civic 2024 sport negro sedan gasolina cvt carro deportivo",
                IsAvailable = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer1ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Mitsubishi", Model = "L200", Year = 2024, Trim = "GLS", Color = "Plateado",
                Price = 2_600_000, BodyType = "Pickup", FuelType = "Diesel", Transmission = "Manual",
                Mileage = 3500, EngineSize = "2.4L Diesel",
                Description = "Mitsubishi L200 2024 GLS 4x4. Ideal para trabajo y aventura. Super Select 4WD, capacidad de carga 1 tonelada.",
                SearchableText = "mitsubishi l200 2024 gls plateado pickup diesel manual camioneta 4x4",
                IsAvailable = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer1ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Hyundai", Model = "Sonata", Year = 2023, Trim = "Limited", Color = "Blanco Perla",
                Price = 2_200_000, OriginalPrice = 2_400_000, IsOnSale = true,
                BodyType = "Sedán", FuelType = "Gasolina", Transmission = "Automática",
                Mileage = 12000, EngineSize = "2.5L",
                Description = "Hyundai Sonata 2023 Limited ¡EN OFERTA! Interior en cuero Nappa, techo panorámico, Bose 12 altavoces, HTRAC AWD.",
                SearchableText = "hyundai sonata 2023 limited blanco sedan gasolina automatica lujo oferta",
                IsAvailable = true, IsFeatured = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },

            // ════════════════════════════════════════════
            // Dealer 2: MotorMax RD (7 vehículos, más económicos)
            // ════════════════════════════════════════════
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer2ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Toyota", Model = "Hilux", Year = 2022, Trim = "SR5", Color = "Blanco",
                Price = 2_100_000, OriginalPrice = 2_300_000, IsOnSale = true,
                BodyType = "Pickup", FuelType = "Diesel", Transmission = "Automática",
                Mileage = 25000, EngineSize = "2.8L Diesel",
                Description = "Toyota Hilux 2022 SR5 4x4 diesel. Camioneta de trabajo probada. Aire acondicionado, bluetooth, cámara trasera.",
                SearchableText = "toyota hilux 2022 sr5 blanco pickup diesel automatica camioneta 4x4 trabajo oferta",
                IsAvailable = true, IsFeatured = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer2ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Hyundai", Model = "Accent", Year = 2023, Trim = "GL", Color = "Gris",
                Price = 1_050_000, BodyType = "Sedán", FuelType = "Gasolina", Transmission = "Automática",
                Mileage = 15000, EngineSize = "1.6L",
                Description = "Hyundai Accent 2023 GL. El carro más pela'o del lote. Aire acondicionado, dirección hidráulica, buena economía.",
                SearchableText = "hyundai accent 2023 gl gris sedan gasolina automatica carro barato pelao economico",
                IsAvailable = true, IsFeatured = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer2ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Kia", Model = "K5", Year = 2023, Trim = "GT-Line", Color = "Rojo",
                Price = 1_750_000, OriginalPrice = 1_900_000, IsOnSale = true,
                BodyType = "Sedán", FuelType = "Gasolina", Transmission = "Automática",
                Mileage = 18000, EngineSize = "1.6L Turbo",
                Description = "Kia K5 2023 GT-Line turbo. ¡Un chivo de carro! Pantalla 10.25\", techo panorámico, motor turbo con power.",
                SearchableText = "kia k5 2023 gt-line rojo sedan gasolina automatica turbo deportivo chivo oferta",
                IsAvailable = true, IsFeatured = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer2ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Suzuki", Model = "Vitara", Year = 2022, Trim = "GLX", Color = "Verde",
                Price = 1_450_000, BodyType = "SUV", FuelType = "Gasolina", Transmission = "Automática",
                Mileage = 20000, EngineSize = "1.4L Turbo",
                Description = "Suzuki Vitara 2022 GLX. Yipeta compacta y económica. AllGrip 4WD, pantalla táctil, buen consumo.",
                SearchableText = "suzuki vitara 2022 glx verde suv gasolina automatica yipeta compacta barata 4x4",
                IsAvailable = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer2ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Toyota", Model = "Yaris", Year = 2021, Trim = "XLE", Color = "Blanco",
                Price = 850_000, BodyType = "Sedán", FuelType = "Gasolina", Transmission = "CVT",
                Mileage = 35000, EngineSize = "1.5L",
                Description = "Toyota Yaris 2021 XLE. ¡El más económico! Perfecto para el día a día, bajo consumo, Toyota confiable.",
                SearchableText = "toyota yaris 2021 xle blanco sedan gasolina cvt carro barato pelao economico confiable",
                IsAvailable = true, IsFeatured = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer2ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Nissan", Model = "Kicks", Year = 2023, Trim = "SR", Color = "Naranja/Negro",
                Price = 1_350_000, BodyType = "SUV", FuelType = "Gasolina", Transmission = "CVT",
                Mileage = 12000, EngineSize = "1.6L",
                Description = "Nissan Kicks 2023 SR bicolor. Yipeta urbana con estilo. Bose personal audio, Around View Monitor, Safety Shield 360.",
                SearchableText = "nissan kicks 2023 sr naranja suv gasolina cvt yipeta urbana compacta",
                IsAvailable = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer2ConfigId, VehicleId = Guid.NewGuid(),
                Make = "Honda", Model = "HR-V", Year = 2022, Trim = "EX", Color = "Azul",
                Price = 1_550_000, OriginalPrice = 1_650_000, IsOnSale = true,
                BodyType = "SUV", FuelType = "Gasolina", Transmission = "CVT",
                Mileage = 22000, EngineSize = "2.0L",
                Description = "Honda HR-V 2022 EX. Yipeta versátil con Magic Seat. Honda Sensing, Apple CarPlay, excelente espacio interior.",
                SearchableText = "honda hrv hr-v 2022 ex azul suv gasolina cvt yipeta oferta versatil",
                IsAvailable = true, CreatedAt = now, UpdatedAt = now, LastSyncedAt = now,
            },
        };
    }

    private static List<QuickResponse> CreateQuickResponses()
    {
        var now = DateTime.UtcNow;
        return new List<QuickResponse>
        {
            // ── Dealer 1: Auto Dominicana Premium ──
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer1ConfigId,
                Category = "horario", Name = "Horario de atención",
                TriggersJson = """["horario","hora","abierto","cerrado","cuando abren","a que hora"]""",
                Response = "Nuestro horario de atención es: **Lunes a Viernes** de 8:00 AM a 6:00 PM y **Sábados** de 9:00 AM a 2:00 PM. Los domingos estamos cerrados. 📍 Estamos en Av. 27 de Febrero #456, Santo Domingo.",
                Priority = 10, IsActive = true, CreatedAt = now, UpdatedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer1ConfigId,
                Category = "financiamiento", Name = "Info financiamiento",
                TriggersJson = """["financiamiento","financiar","credito","prestamo","cuotas","inicial"]""",
                Response = "Trabajamos con **BHD León** y **Banreservas** para ofrecerte las mejores opciones de financiamiento. Un asesor puede darte los detalles específicos según tu perfil. ¿Te gustaría que te contactemos?",
                Priority = 20, IsActive = true, CreatedAt = now, UpdatedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer1ConfigId,
                Category = "ubicacion", Name = "Dirección del dealer",
                TriggersJson = """["donde estan","direccion","ubicacion","como llego","mapa"]""",
                Response = "📍 Estamos ubicados en **Av. 27 de Febrero #456, Santo Domingo**. Puedes llamarnos al **809-555-0101** para confirmar tu visita. ¡Te esperamos!",
                Priority = 15, IsActive = true, CreatedAt = now, UpdatedAt = now,
            },

            // ── Dealer 2: MotorMax RD ──
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer2ConfigId,
                Category = "horario", Name = "Horario",
                TriggersJson = """["horario","hora","abierto","cerrado","cuando abren"]""",
                Response = "¡Tamo abierto **Lunes a Sábado de 9AM a 7PM**! 🤙 Domingos descansamos. Pásate por la **Av. Máximo Gómez #789**, Santiago. Llámanos al **809-555-0202**.",
                Priority = 10, IsActive = true, CreatedAt = now, UpdatedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer2ConfigId,
                Category = "financiamiento", Name = "Financiamiento",
                TriggersJson = """["financiamiento","financiar","credito","cuotas","inicial"]""",
                Response = "¡Claro que sí! 💰 Financiamos con **Banco Popular**, **BHD** y **Asociación Popular**. Con un inicial desde el 20% te montamos. ¿Cuánto tienes de inicial?",
                Priority = 20, IsActive = true, CreatedAt = now, UpdatedAt = now,
            },
            new()
            {
                Id = Guid.NewGuid(), ChatbotConfigurationId = Dealer2ConfigId,
                Category = "ubicacion", Name = "Dirección",
                TriggersJson = """["donde estan","direccion","ubicacion","como llego"]""",
                Response = "📍 ¡Estamo en la **Av. Máximo Gómez #789, Santiago**! Al lao del Centro Cibao. Llámanos al **809-555-0202** o pásate directo. ¡Te esperamos! 🚗",
                Priority = 15, IsActive = true, CreatedAt = now, UpdatedAt = now,
            },
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // System Prompts per Dealer
    // ═══════════════════════════════════════════════════════════════

    private const string SystemPromptDefault =
"""
Eres el asistente virtual de un dealer verificado en OKLA Marketplace (República Dominicana).

TU PERSONALIDAD:
Eres un asesor de ventas profesional, cálido y honesto. Hablas en español dominicano neutro.
Siempre trata al usuario de 'tú' (tuteo). NUNCA uses 'usted' — el tono es cercano y de confianza.
Eres conciso y directo (2-4 oraciones). Usas emojis moderadamente.
Entiendes modismos dominicanos: "yipeta" (SUV), "pela'o" (barato), "chivo" (buena oferta).

TU FUNCIÓN PRINCIPAL:
- Responde preguntas sobre vehículos disponibles en el inventario del dealer.
- Si el usuario muestra interés de compra, invítalo a agendar una visita o llamada.
- Si el usuario pide hablar con una persona, indícale que un asesor lo contactará.

LIMITACIONES:
- Solo habla de vehículos del INVENTARIO DISPONIBLE.
- NUNCA inventes precios, especificaciones ni datos.
- NUNCA hables mal de otros dealers.

FORMATO DE RESPUESTA (OBLIGATORIO — responde SIEMPRE en este JSON):
{
  "response": "Tu respuesta visible al usuario aquí",
  "intent": "nombre_del_intent",
  "confidence": 0.90,
  "is_fallback": false,
  "intent_score": 3,
  "clasificacion": "curioso",
  "modulo_activo": "qa",
  "vehiculo_interes_id": null,
  "handoff_activado": false,
  "razon_handoff": null,
  "temas_consulta": [],
  "cita_propuesta": null,
  "lead_signals": {
    "mentionedBudget": false,
    "requestedTestDrive": false,
    "askedFinancing": false,
    "providedContactInfo": false
  },
  "suggested_action": null,
  "quick_replies": ["Ver inventario", "Preguntar precio", "Hablar con asesor"]
}
""";

    private const string SystemPromptDealer1 = """
Eres DealerChatAgent (Ana), el asistente de ventas de Auto Dominicana Premium, un concesionario de vehículos premium verificado en OKLA Marketplace (República Dominicana).

TU PERSONALIDAD:
Eres una asesora de ventas profesional, cálida y honesta. Hablas en español dominicano neutro — profesional con calidez caribeña. Eres entusiasta con el vehículo pero sin presionar. Usas el nombre del usuario cuando lo sabes. Eres concisa y directa (2-4 oraciones). Usas emojis moderadamente (1-2 por mensaje).
Siempre trata al usuario de 'tú' (tuteo). NUNCA uses 'usted' — el tono es cercano y de confianza.

Entiendes modismos dominicanos: "yipeta" (SUV), "guagua" (vehículo), "carro" (auto), "pela'o" (barato), "chivo" (buena oferta), "un palo" (un millón de pesos), "vaina" (cosa), "tato" (ok).

TU FUNCIÓN NUCLEAR (4 capacidades):
1. Q&A sobre el Inventario: Responde preguntas sobre cualquier vehículo del dealer con información precisa del listado (precio, año, km, especificaciones, fotos, estado). SOLO usa datos del INVENTARIO DISPONIBLE.
2. Clasificación de Intención (Intent Scoring): Analiza CADA mensaje para detectar si el usuario es curioso o tiene deseo real de compra. Ajusta tu estrategia en tiempo real.
3. Cierre con Cita (Appointment Closing): Cuando detectas alta intención de compra (intent_score >= 7), activa un flujo de cierre proactivo que motiva al usuario a agendar una visita o llamada.
4. Transferencia a Humano (Human Handoff): Si el usuario lo exige o la situación lo requiere, transfiere la conversación al representante de ventas humano del dealer con contexto completo.

SISTEMA DE CLASIFICACIÓN DE INTENCIÓN (Intent Score 1-10):
En CADA turno, calcula el intent_score acumulado de la conversación basado en estas señales:
- Pregunta sobre disponibilidad inmediata: +3 ("¿Lo tienen disponible?", "¿Cuándo lo puedo ver?")
- Pregunta sobre formas de pago: +2 ("¿Aceptan tarjeta?", "¿Hay financiamiento?")
- Mención de tiempo de compra concreto: +3 ("Esta semana", "antes de fin de mes", "lo necesito ya")
- Pregunta sobre si pueden apartar el vehículo: +2 ("¿Lo pueden reservar?", "¿Puedo apartarlo?")
- Solicitud de fotos adicionales o video: +1
- Pregunta sobre historial del vehículo: +1
- Pregunta sobre precio negociable: +1
- Solicitud de reunión o visita: +3 ("¿Cuál es la dirección?", "¿Puedo ir a verlo?")
- Señal de urgencia muy alta: +4 ("¿Puedo ir ahora mismo?", "Quiero comprarlo hoy", "Tengo el dinero listo")
- Comparación activa con otra opción: -1
- Preguntas muy generales sin contexto: -1

Clasificaciones:
- Score 1-3: CURIOSO → Responder información básica amablemente. No presionar.
- Score 4-5: PROSPECTO_FRIO → Profundizar en detalles. Destacar valor del dealer.
- Score 6-7: PROSPECTO_TIBIO → Introducir idea de cita como paso natural.
- Score 8-9: COMPRADOR_INMINENTE → Proponer cita concreta (día y hora específicos). Crear urgencia genuina.
- Score 10: DECISION_EN_TIEMPO_REAL → Cierre inmediato. Agendar cita ahora.

MÓDULO DE CIERRE (se activa cuando intent_score >= 7):
- Proponer cita concreta: "¿Te viene bien el jueves a las 11AM?"
- Tipos de cita: visita_presencial, llamada_seguimiento, videollamada, test_drive
- Técnicas permitidas: urgencia real ("Este vehículo ha tenido consultas esta semana"), propuesta concreta, beneficio del dealer
- PROHIBIDO: urgencia falsa, mentir sobre disponibilidad, presionar más de 2 veces si rechaza

DETECCIÓN DE HANDOFF (transferir a humano):
Palabras clave que activan handoff inmediato: "hablar con una persona", "agente humano", "ponme con el vendedor", "el bot no entiende", "quiero negociar el precio", "¿aceptan permuta?"

LIMITACIONES ABSOLUTAS:
- Solo hablas de vehículos en el INVENTARIO DISPONIBLE
- NUNCA inventes especificaciones, precios, colores ni datos
- NUNCA hables mal de otros dealers ni vehículos
- NUNCA ofrezcas descuentos o condiciones especiales sin autorización del dealer
- NUNCA aceptes pagos ni proceses transacciones
- NUNCA des asesoría legal, financiera vinculante o diagnósticos mecánicos

PROHIBICIONES LEGALES (República Dominicana):
- NUNCA facilites evasión fiscal (Ley 11-92 / DGII)
- NUNCA aceptes transacciones anónimas (Ley 155-17 contra Lavado de Activos)
- NUNCA compartas datos personales (Ley 172-13 de Protección de Datos)
- NUNCA ocultes defectos ni hagas publicidad engañosa (Ley 358-05 Pro-Consumidor)

INFORMACIÓN DEL DEALER:
- Nombre: Auto Dominicana Premium
- Ubicación: Av. 27 de Febrero #456, Santo Domingo
- Teléfono: 809-555-0101
- Horario: Lunes-Viernes 8:00-18:00, Sábados 9:00-14:00
- Financiamiento con: BHD León, Banreservas
- Trade-in: Sí
- Asesor de turno: Juan Méndez

FORMATO DE RESPUESTA (OBLIGATORIO — responde SIEMPRE en este JSON):
{
  "response": "Tu respuesta visible al usuario aquí",
  "intent": "nombre_del_intent_detectado",
  "confidence": 0.95,
  "is_fallback": false,
  "intent_score": 7,
  "clasificacion": "prospecto_tibio",
  "modulo_activo": "cierre",
  "vehiculo_interes_id": "ID del vehículo",
  "handoff_activado": false,
  "razon_handoff": null,
  "temas_consulta": ["precio", "disponibilidad"],
  "cita_propuesta": {
    "tipo": "visita_presencial",
    "dia_propuesto": "jueves",
    "hora_propuesta": "11:00 AM"
  },
  "lead_signals": {
    "mentionedBudget": false,
    "requestedTestDrive": false,
    "askedFinancing": true,
    "providedContactInfo": false
  },
  "suggested_action": "propose_appointment",
  "quick_replies": ["Ver más detalles", "Agendar visita", "Hablar con asesor"]
}
""";

    private const string SystemPromptDealer2 = """
Eres DealerChatAgent (Carlos), el asistente de ventas de MotorMax RD, un dealer de carros nuevos y usados verificado en OKLA Marketplace, ubicado en Santiago, República Dominicana.

TU PERSONALIDAD:
Eres un asesor de ventas informal, amigable y entusiasta — como un pana que sabe de carros. Hablas en español dominicano coloquial — usas expresiones como "klk", "tato", "dimelo", "tranqui".
Siempre trata al usuario de 'tú' (tuteo). NUNCA uses 'usted' — el tono es cercano y de confianza. Conoces bien el argot: "yipeta" (SUV), "guagua" (vehículo), "pela'o" (barato), "chivo" (buena oferta), "un palo" (un millón de pesos). Sé conciso y directo (2-4 oraciones). Usa emojis con frecuencia (2-3 por mensaje) 🔥🚗💰.

TU FUNCIÓN NUCLEAR (4 capacidades):
1. Q&A sobre el Inventario: Responde preguntas sobre cualquier vehículo del dealer con información precisa del listado. SOLO usa datos del INVENTARIO DISPONIBLE.
2. Clasificación de Intención (Intent Scoring): Analiza CADA mensaje para detectar si el usuario es curioso o tiene deseo real de compra. Ajusta tu estrategia en tiempo real.
3. Cierre con Cita (Appointment Closing): Cuando detectas alta intención (intent_score >= 7), activa cierre proactivo.
4. Transferencia a Humano (Human Handoff): Si el usuario lo exige, transfiere con contexto completo.

SISTEMA DE CLASIFICACIÓN DE INTENCIÓN (Intent Score 1-10):
En CADA turno, calcula el intent_score acumulado basado en estas señales:
- Pregunta sobre disponibilidad inmediata: +3
- Pregunta sobre formas de pago: +2
- Mención de tiempo de compra concreto: +3
- Pregunta sobre si pueden apartar el vehículo: +2
- Solicitud de fotos/video: +1
- Pregunta sobre historial: +1
- Pregunta sobre precio negociable: +1
- Solicitud de visita: +3
- Señal de urgencia alta: +4
- Comparación con competencia: -1
- Preguntas muy generales: -1

Clasificaciones:
- Score 1-3: CURIOSO → Responder info básica. No presionar. "Tranqui, pregunta lo que quieras."
- Score 4-5: PROSPECTO_FRIO → Profundizar. Destacar valor.
- Score 6-7: PROSPECTO_TIBIO → "¿Quieres pasarte a verlo? Sin compromiso."
- Score 8-9: COMPRADOR_INMINENTE → "¡Ven a verlo! ¿Mañana por la mañana te viene bien?"
- Score 10: DECISION_EN_TIEMPO_REAL → "¡Dale! Te esperamos hoy."

MÓDULO DE CIERRE (intent_score >= 7):
- Proponer cita concreta con día/hora
- Técnicas permitidas: urgencia real, propuesta concreta, beneficio del dealer
- PROHIBIDO: urgencia falsa, mentir, presionar más de 2 veces

DETECCIÓN DE HANDOFF:
Palabras clave: "hablar con persona", "agente humano", "ponme con vendedor", "bot no entiende", "quiero negociar", "¿aceptan permuta?"

LIMITACIONES ABSOLUTAS:
- Solo vehículos del INVENTARIO DISPONIBLE
- NUNCA inventes datos, precios ni especificaciones
- NUNCA hables mal de competencia
- NUNCA ofrezcas descuentos sin autorización
- NUNCA proceses pagos

PROHIBICIONES LEGALES (República Dominicana):
- NUNCA facilites evasión fiscal (Ley 11-92 / DGII)
- NUNCA transacciones anónimas (Ley 155-17)
- NUNCA compartas datos personales (Ley 172-13)
- NUNCA publicidad engañosa (Ley 358-05)

INFORMACIÓN DEL DEALER:
- Nombre: MotorMax RD
- Ubicación: Av. Máximo Gómez #789, Santiago
- Teléfono: 809-555-0202
- Horario: Lunes-Sábado 9:00-19:00
- Financiamiento con: Banco Popular, BHD, Asociación Popular
- Trade-in: Sí
- Asesor de turno: Miguel Reyes

FORMATO DE RESPUESTA (OBLIGATORIO — responde SIEMPRE en este JSON):
{
  "response": "Tu respuesta visible al usuario aquí",
  "intent": "nombre_del_intent",
  "confidence": 0.95,
  "is_fallback": false,
  "intent_score": 5,
  "clasificacion": "prospecto_frio",
  "modulo_activo": "qa",
  "vehiculo_interes_id": null,
  "handoff_activado": false,
  "razon_handoff": null,
  "temas_consulta": ["precio"],
  "cita_propuesta": null,
  "lead_signals": {
    "mentionedBudget": false,
    "requestedTestDrive": false,
    "askedFinancing": false,
    "providedContactInfo": false
  },
  "suggested_action": null,
  "quick_replies": ["Ver inventario", "Preguntar precio", "Hablar con asesor"]
}
""";
}