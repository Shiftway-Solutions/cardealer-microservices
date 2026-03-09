using FluentAssertions;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;
using ChatbotService.Infrastructure.Services;
using ChatbotService.Infrastructure.Services.Strategies;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChatbotService.Tests.Unit.Audit;

// ════════════════════════════════════════════════════════════════════════════
// System Prompt Audit — Structure, Role, Dominican Tone, Output Format
// ════════════════════════════════════════════════════════════════════════════

#region ═══ A. GeneralChatStrategy Prompt Audit ═══

/// <summary>
/// Audits the GeneralChatStrategy system prompt for:
/// Dominican tone, anti-hallucination, legal compliance, structure.
/// </summary>
public class GeneralChatStrategyPromptAuditTests
{
    private readonly GeneralChatStrategy _strategy;

    public GeneralChatStrategyPromptAuditTests()
    {
        _strategy = new GeneralChatStrategy();
    }

    private static ChatSession CreateSession() => new()
    {
        Id = Guid.NewGuid(),
        ChatbotConfigurationId = Guid.NewGuid(),
        ChatMode = ChatMode.General,
        Status = SessionStatus.Active
    };

    private static ChatbotConfiguration CreateConfig(string? botName = null) => new()
    {
        Id = Guid.NewGuid(),
        DealerId = Guid.NewGuid(),
        BotName = botName ?? "Asistente OKLA", // domain default; null path tested below
        Name = "Test Dealer"
    };

    // ── 1. Dominican slang glossary is present ──
    [Theory]
    [InlineData("yipeta")]
    [InlineData("guagua")]
    [InlineData("pasola")]
    [InlineData("carro")]
    [InlineData("pela'o")]
    [InlineData("chivo")]
    [InlineData("un palo")]
    [InlineData("tato")]
    [InlineData("vaina")]
    public async Task Prompt_ContainsDominicanSlang(string slangWord)
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "hola", CancellationToken.None);

        prompt.Should().Contain(slangWord,
            $"prompt must include Dominican slang '{slangWord}' for natural interaction");
    }

    // ── 2. Anti-hallucination section exists ──
    [Fact]
    public async Task Prompt_HasAntiHallucinationSection()
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "hola", CancellationToken.None);

        prompt.Should().Contain("ANTI-ALUCINACIÓN",
            "prompt must have an anti-hallucination section");
        prompt.Should().Contain("NUNCA inventes",
            "must explicitly prohibit hallucination");
    }

    // ── 3. URL whitelist is present ──
    [Theory]
    [InlineData("okla.com.do")]
    [InlineData("okla.com.do/vehiculos")]
    [InlineData("okla.com.do/cuenta/verificacion")]
    [InlineData("okla.com.do/publicar")]
    [InlineData("okla.com.do/planes")]
    public async Task Prompt_ContainsAuthorizedUrls(string url)
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "hola", CancellationToken.None);

        prompt.Should().Contain(url,
            $"prompt must include authorized URL '{url}'");
    }

    // ── 4. Legal compliance — Ley 358-05 ──
    [Fact]
    public async Task Prompt_ReferencesLey35805_ConsumerProtection()
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "hola", CancellationToken.None);

        prompt.Should().Contain("358-05",
            "must reference Ley 358-05 (Protección al Consumidor)");
    }

    // ── 5. Legal compliance — Ley 172-13 ──
    [Fact]
    public async Task Prompt_ReferencesLey17213_DataProtection()
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "hola", CancellationToken.None);

        prompt.Should().Contain("172-13",
            "must reference Ley 172-13 (Protección de Datos Personales)");
    }

    // ── 6. Legal compliance — DGII taxes ──
    [Fact]
    public async Task Prompt_ReferencesDGII_TaxDisclaimer()
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "hola", CancellationToken.None);

        prompt.Should().Contain("DGII",
            "must reference DGII for tax/traspaso disclaimer");
        prompt.Should().Contain("traspaso",
            "must mention traspaso is not included");
    }

    // ── 7. No PII collection rule ──
    [Fact]
    public async Task Prompt_ProhibitsPIICollection()
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "hola", CancellationToken.None);

        prompt.Should().Contain("cédula",
            "must explicitly prohibit asking for cédula");
        prompt.Should().Contain("NUNCA",
            "must use strong prohibition language");
    }

    // ── 8. Uses configurable bot name ──
    [Theory]
    [InlineData("María", "María")]
    [InlineData(null, "Asistente OKLA")]  // null => domain default "Asistente OKLA" is kept by strategy
    public async Task Prompt_UsesConfigurableBotName(string? configName, string expectedName)
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(configName), "hola", CancellationToken.None);

        prompt.Should().Contain($"Eres {expectedName}",
            $"prompt should address bot as '{expectedName}'");
    }

    // ── 9. Custom system prompt text appended ──
    [Fact]
    public async Task Prompt_AppendsCustomInstructions_WhenConfigured()
    {
        var config = CreateConfig();
        config.SystemPromptText = "Siempre menciona financiamiento con BHD León.";

        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), config, "hola", CancellationToken.None);

        prompt.Should().Contain("BHD León");
        prompt.Should().Contain("Instrucciones adicionales");
    }

    // ── 10. Personality section exists ──
    [Fact]
    public async Task Prompt_HasPersonalitySection()
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "hola", CancellationToken.None);

        prompt.Should().Contain("PERSONALIDAD",
            "prompt must define bot personality");
        prompt.Should().Contain("español dominicano",
            "must specify Dominican Spanish");
    }

    // ── 11. Ley 155-17 anti-money laundering ──
    [Fact]
    public async Task Prompt_ReferencesLey15517_AntiMoneyLaundering()
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "hola", CancellationToken.None);

        prompt.Should().Contain("155-17",
            "must reference Ley 155-17 (Prevención Lavado de Activos)");
    }
}

#endregion

#region ═══ B. DealerInventoryStrategy Prompt Audit ═══

/// <summary>
/// Audits the DealerInventoryStrategy system prompt.
/// </summary>
public class DealerInventoryStrategyPromptAuditTests
{
    private readonly DealerInventoryStrategy _strategy;
    private readonly Mock<IChatbotVehicleRepository> _vehicleRepoMock;

    public DealerInventoryStrategyPromptAuditTests()
    {
        var vectorSearchMock = new Mock<IVectorSearchService>();
        _vehicleRepoMock = new Mock<IChatbotVehicleRepository>();
        var configRepoMock = new Mock<IChatbotConfigurationRepository>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var loggerMock = new Mock<ILogger<DealerInventoryStrategy>>();

        _vehicleRepoMock
            .Setup(r => r.GetByConfigurationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatbotVehicle>());

        _strategy = new DealerInventoryStrategy(
            vectorSearchMock.Object,
            _vehicleRepoMock.Object,
            configRepoMock.Object,
            httpClientFactoryMock.Object,
            loggerMock.Object);
    }

    private static ChatSession CreateSession() => new()
    {
        Id = Guid.NewGuid(),
        ChatbotConfigurationId = Guid.NewGuid(),
        DealerId = Guid.NewGuid(),
        ChatMode = ChatMode.DealerInventory,
        Status = SessionStatus.Active
    };

    private static ChatbotConfiguration CreateConfig() => new()
    {
        Id = Guid.NewGuid(),
        DealerId = Guid.NewGuid(),
        BotName = "Ana",
        Name = "Test Dealer RD"
    };

    // ── 1. Dominican slang glossary ──
    [Theory]
    [InlineData("yipeta")]
    [InlineData("guagua")]
    [InlineData("pela'o")]
    [InlineData("chivo")]
    [InlineData("un palo")]
    public async Task Prompt_ContainsDominicanSlang(string slangWord)
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "busco yipeta", CancellationToken.None);

        prompt.Should().Contain(slangWord,
            $"DealerInventory prompt must include Dominican slang '{slangWord}'");
    }

    // ── 2. Anti-hallucination section ──
    [Fact]
    public async Task Prompt_HasAntiHallucinationSection()
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "busco carro", CancellationToken.None);

        prompt.Should().Contain("ANTI-ALUCINACIÓN",
            "DealerInventory prompt must have anti-hallucination section");
    }

    // ── 3. Legal compliance — all 3 laws ──
    [Theory]
    [InlineData("358-05")]
    [InlineData("172-13")]
    [InlineData("DGII")]
    [InlineData("155-17")]
    public async Task Prompt_ReferencesLaw(string lawRef)
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "busco carro", CancellationToken.None);

        prompt.Should().Contain(lawRef,
            $"DealerInventory prompt must reference '{lawRef}'");
    }

    // ── 4. No PII collection ──
    [Fact]
    public async Task Prompt_ProhibitsPIICollection()
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "hola", CancellationToken.None);

        prompt.Should().Contain("NUNCA pidas nombre ni teléfono",
            "must explicitly prohibit asking for personal info");
    }

    // ── 5. Personality section ──
    [Fact]
    public async Task Prompt_HasPersonalitySection()
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "hola", CancellationToken.None);

        prompt.Should().Contain("PERSONALIDAD");
        prompt.Should().Contain("español dominicano");
    }

    // ── 6. Dealer info is injected ──
    [Fact]
    public async Task Prompt_IncludesDealerInfo()
    {
        var config = CreateConfig();
        config.Name = "MotorMax RD";

        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), config, "hola", CancellationToken.None);

        prompt.Should().Contain("MotorMax RD");
        prompt.Should().Contain("Información del Dealer");
    }

    // ── 7. Price reference disclaimer ──
    [Fact]
    public async Task Prompt_ContainsPriceReferenceDisclaimer()
    {
        var prompt = await _strategy.BuildSystemPromptAsync(
            CreateSession(), CreateConfig(), "hola", CancellationToken.None);

        prompt.Should().Contain("precio de referencia",
            "must instruct LLM to use 'precio de referencia' not 'precio final'");
    }
}

#endregion

#region ═══ C. SingleVehicleStrategy Prompt Audit ═══

/// <summary>
/// Audits the SingleVehicleStrategy system prompt.
/// </summary>
public class SingleVehicleStrategyPromptAuditTests
{
    private readonly SingleVehicleStrategy _strategy;
    private readonly Mock<IChatbotVehicleRepository> _vehicleRepoMock;

    public SingleVehicleStrategyPromptAuditTests()
    {
        _vehicleRepoMock = new Mock<IChatbotVehicleRepository>();
        var loggerMock = new Mock<ILogger<SingleVehicleStrategy>>();
        _strategy = new SingleVehicleStrategy(_vehicleRepoMock.Object, loggerMock.Object);
    }

    private static ChatSession CreateSession(Guid vehicleId) => new()
    {
        Id = Guid.NewGuid(),
        ChatbotConfigurationId = Guid.NewGuid(),
        VehicleId = vehicleId,
        ChatMode = ChatMode.SingleVehicle,
        Status = SessionStatus.Active
    };

    private static ChatbotConfiguration CreateConfig() => new()
    {
        Id = Guid.NewGuid(),
        DealerId = Guid.NewGuid(),
        BotName = "Ana",
        Name = "Test Dealer"
    };

    private ChatbotVehicle CreateVehicle(Guid configId, Guid vehicleId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ChatbotConfigurationId = configId,
            VehicleId = vehicleId,
            Make = "Toyota",
            Model = "Corolla",
            Year = 2023,
            Price = 1_450_000,
            IsAvailable = true,
            Transmission = "Automática",
            FuelType = "Gasolina",
            Color = "Blanco"
        };

    // ── 1. Dominican slang glossary ──
    [Theory]
    [InlineData("yipeta")]
    [InlineData("pela'o")]
    [InlineData("chivo")]
    [InlineData("un palo")]
    public async Task Prompt_ContainsDominicanSlang(string slangWord)
    {
        var config = CreateConfig();
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateVehicle(config.Id, vehicleId);
        var session = CreateSession(vehicleId);
        session.ChatbotConfigurationId = config.Id;

        _vehicleRepoMock.Setup(r => r.GetByVehicleIdAsync(config.Id, vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var prompt = await _strategy.BuildSystemPromptAsync(
            session, config, "cuánto cuesta?", CancellationToken.None);

        prompt.Should().Contain(slangWord,
            $"SingleVehicle prompt must include Dominican slang '{slangWord}'");
    }

    // ── 2. Anti-hallucination section ──
    [Fact]
    public async Task Prompt_HasAntiHallucinationSection()
    {
        var config = CreateConfig();
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateVehicle(config.Id, vehicleId);
        var session = CreateSession(vehicleId);
        session.ChatbotConfigurationId = config.Id;

        _vehicleRepoMock.Setup(r => r.GetByVehicleIdAsync(config.Id, vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var prompt = await _strategy.BuildSystemPromptAsync(
            session, config, "hola", CancellationToken.None);

        prompt.Should().Contain("ANTI-ALUCINACIÓN");
    }

    // ── 3. Legal compliance ──
    [Theory]
    [InlineData("358-05")]
    [InlineData("172-13")]
    [InlineData("DGII")]
    public async Task Prompt_ReferencesLaw(string lawRef)
    {
        var config = CreateConfig();
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateVehicle(config.Id, vehicleId);
        var session = CreateSession(vehicleId);
        session.ChatbotConfigurationId = config.Id;

        _vehicleRepoMock.Setup(r => r.GetByVehicleIdAsync(config.Id, vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var prompt = await _strategy.BuildSystemPromptAsync(
            session, config, "hola", CancellationToken.None);

        prompt.Should().Contain(lawRef,
            $"SingleVehicle prompt must reference '{lawRef}'");
    }

    // ── 4. Vehicle data injected correctly ──
    [Fact]
    public async Task Prompt_InjectsVehicleData()
    {
        var config = CreateConfig();
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateVehicle(config.Id, vehicleId);
        var session = CreateSession(vehicleId);
        session.ChatbotConfigurationId = config.Id;

        _vehicleRepoMock.Setup(r => r.GetByVehicleIdAsync(config.Id, vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var prompt = await _strategy.BuildSystemPromptAsync(
            session, config, "hola", CancellationToken.None);

        prompt.Should().Contain("Toyota");
        prompt.Should().Contain("Corolla");
        prompt.Should().Contain("2023");
        prompt.Should().Contain("1,450,000");
    }

    // ── 5. Fallback prompt is Dominican ──
    [Fact]
    public async Task FallbackPrompt_IsDominican()
    {
        var config = CreateConfig();
        var session = CreateSession(Guid.NewGuid());
        session.VehicleId = null; // no vehicle → fallback

        var prompt = await _strategy.BuildSystemPromptAsync(
            session, config, "hola", CancellationToken.None);

        prompt.Should().Contain("español dominicano",
            "fallback prompt must use Dominican Spanish");
    }

    // ── 6. Price reference disclaimer ──
    [Fact]
    public async Task Prompt_ContainsPriceReferenceDisclaimer()
    {
        var config = CreateConfig();
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateVehicle(config.Id, vehicleId);
        var session = CreateSession(vehicleId);
        session.ChatbotConfigurationId = config.Id;

        _vehicleRepoMock.Setup(r => r.GetByVehicleIdAsync(config.Id, vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var prompt = await _strategy.BuildSystemPromptAsync(
            session, config, "hola", CancellationToken.None);

        prompt.Should().Contain("precio de referencia",
            "must instruct LLM to use 'precio de referencia'");
    }

    // ── 7. Prohibits PII collection ──
    [Fact]
    public async Task Prompt_ProhibitsPIICollection()
    {
        var config = CreateConfig();
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateVehicle(config.Id, vehicleId);
        var session = CreateSession(vehicleId);
        session.ChatbotConfigurationId = config.Id;

        _vehicleRepoMock.Setup(r => r.GetByVehicleIdAsync(config.Id, vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var prompt = await _strategy.BuildSystemPromptAsync(
            session, config, "hola", CancellationToken.None);

        prompt.Should().Contain("cédula",
            "must explicitly mention cédula prohibition");
    }
}

#endregion

#region ═══ D. LlmService Default Prompt Audit ═══

/// <summary>
/// Audits the LlmService default system prompt.
/// </summary>
public class LlmServiceDefaultPromptAuditTests
{
    // ── 1. Default prompt mentions OKLA ──
    [Fact]
    public void DefaultPrompt_MentionsOKLA()
    {
        var settings = new LlmSettings();
        settings.SystemPrompt.Should().Contain("OKLA",
            "default system prompt must identify as OKLA platform");
    }

    // ── 2. Default prompt is in Dominican Spanish ──
    [Fact]
    public void DefaultPrompt_IsDominicanSpanish()
    {
        var settings = new LlmSettings();
        settings.SystemPrompt.Should().Contain("español dominicano",
            "default prompt must specify Dominican Spanish");
    }

    // ── 3. Default prompt has anti-hallucination ──
    [Fact]
    public void DefaultPrompt_HasAntiHallucination()
    {
        var settings = new LlmSettings();
        settings.SystemPrompt.Should().Contain("NUNCA inventes",
            "default prompt must prohibit hallucination");
    }

    // ── 4. Default prompt has PII prohibition ──
    [Fact]
    public void DefaultPrompt_ProhibitsPII()
    {
        var settings = new LlmSettings();
        settings.SystemPrompt.Should().Contain("cédula",
            "default prompt must mention cédula prohibition");
    }

    // ── 5. Default prompt mentions DGII ──
    [Fact]
    public void DefaultPrompt_MentionsDGII()
    {
        var settings = new LlmSettings();
        settings.SystemPrompt.Should().Contain("DGII",
            "default prompt must reference DGII disclaimer");
    }
}

#endregion

#region ═══ E. Cross-Strategy Consistency Audit ═══

/// <summary>
/// Ensures all ChatbotService strategies maintain consistent standards.
/// </summary>
public class CrossStrategyConsistencyAuditTests
{
    private readonly string[] _requiredKeywords = new[]
    {
        "NUNCA", "español dominicano", "358-05", "172-13"
    };

    private readonly string[] _requiredDominicanTerms = new[]
    {
        "yipeta", "pela'o", "chivo"
    };

    // ── 1. All strategies have required legal references ──
    [Theory]
    [InlineData("358-05")]
    [InlineData("172-13")]
    public async Task AllStrategies_ContainLegalReferences(string lawRef)
    {
        var prompts = await GetAllPrompts();

        foreach (var (name, prompt) in prompts)
        {
            prompt.Should().Contain(lawRef,
                $"Strategy '{name}' must reference law '{lawRef}'");
        }
    }

    // ── 2. All strategies use Dominican Spanish ──
    [Fact]
    public async Task AllStrategies_UseDominicanSpanish()
    {
        var prompts = await GetAllPrompts();

        foreach (var (name, prompt) in prompts)
        {
            prompt.Should().Contain("español dominicano",
                $"Strategy '{name}' must specify Dominican Spanish");
        }
    }

    // ── 3. All strategies prohibit data invention ──
    [Fact]
    public async Task AllStrategies_ProhibitHallucination()
    {
        var prompts = await GetAllPrompts();

        foreach (var (name, prompt) in prompts)
        {
            prompt.Should().Contain("NUNCA",
                $"Strategy '{name}' must use NUNCA prohibition");
        }
    }

    private async Task<List<(string Name, string Prompt)>> GetAllPrompts()
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            ChatbotConfigurationId = Guid.NewGuid(),
            ChatMode = ChatMode.General,
            Status = SessionStatus.Active
        };

        var config = new ChatbotConfiguration
        {
            Id = Guid.NewGuid(),
            DealerId = Guid.NewGuid(),
            BotName = "Ana",
            Name = "Test Dealer"
        };

        // General
        var general = new GeneralChatStrategy();
        var generalPrompt = await general.BuildSystemPromptAsync(session, config, "hola", CancellationToken.None);

        // DealerInventory
        var vehicleRepoMock = new Mock<IChatbotVehicleRepository>();
        vehicleRepoMock.Setup(r => r.GetByConfigurationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatbotVehicle>());
        var dealerStrategy = new DealerInventoryStrategy(
            new Mock<IVectorSearchService>().Object,
            vehicleRepoMock.Object,
            new Mock<IChatbotConfigurationRepository>().Object,
            new Mock<IHttpClientFactory>().Object,
            new Mock<ILogger<DealerInventoryStrategy>>().Object);
        var dealerPrompt = await dealerStrategy.BuildSystemPromptAsync(session, config, "hola", CancellationToken.None);

        // SingleVehicle — fallback (no vehicle)
        session.VehicleId = null;
        var singleStrategy = new SingleVehicleStrategy(
            new Mock<IChatbotVehicleRepository>().Object,
            new Mock<ILogger<SingleVehicleStrategy>>().Object);
        // For SingleVehicle, test the main prompt by providing a vehicle
        var vehicleId = Guid.NewGuid();
        session.VehicleId = vehicleId;
        var vehicleRepoMock2 = new Mock<IChatbotVehicleRepository>();
        vehicleRepoMock2.Setup(r => r.GetByVehicleIdAsync(config.Id, vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatbotVehicle
            {
                Id = Guid.NewGuid(),
                ChatbotConfigurationId = config.Id,
                VehicleId = vehicleId,
                Make = "Toyota",
                Model = "Corolla",
                Year = 2023,
                Price = 1_000_000,
                IsAvailable = true
            });
        var singleStrategy2 = new SingleVehicleStrategy(
            vehicleRepoMock2.Object,
            new Mock<ILogger<SingleVehicleStrategy>>().Object);
        session.ChatbotConfigurationId = config.Id; // align session config ID so mock matches
        var singlePrompt = await singleStrategy2.BuildSystemPromptAsync(session, config, "hola", CancellationToken.None);

        return new List<(string, string)>
        {
            ("GeneralChat", generalPrompt),
            ("DealerInventory", dealerPrompt),
            ("SingleVehicle", singlePrompt)
        };
    }
}

#endregion
