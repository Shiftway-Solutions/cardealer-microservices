using System.Text.RegularExpressions;

namespace ChatbotService.Application.Services;

/// <summary>
/// Content moderation filter for chatbot messages.
/// Constitutional AI: Ensures all responses stay within OKLA's behavioral policy.
/// 
/// Categories with active pattern detection:
/// - Violence/threats
/// - Sexual content  
/// - Hate speech / discrimination
/// - Scam/fraud attempts
/// - Off-topic solicitation
/// - Unauthorized professional advice (bot output)
/// - Identity deception (bot output)
/// - Profanity / offensive language (bot output)
/// </summary>
public static class ContentModerationFilter
{
    // ── Compiled regex patterns for better performance ────────────────
    private static readonly Regex[] HateSpeechPatterns =
    {
        // Racial/ethnic slurs (Dominican Spanish context)
        new(@"\b(?:malditos?|cochinos?|asquerosos?)\s+(?:haitianos?|negros?|prietos?|blancos?|indios?|chinos?|gringos?)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(?:fuera|vayan(?:se)?|larguen(?:se)?)\s+(?:haitianos?|extranjeros?|ilegales?)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Homophobic slurs
        new(@"\b(?:maric[oó]n|maricones|pato|pájaro|bugarr[oó]n|lesbiana\s+asquerosa)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Xenophobic content
        new(@"\b(?:raza\s+(?:inferior|superior)|limpieza\s+(?:étnica|racial|social))", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Religious hate
        new(@"\b(?:malditos?|muerte\s+a(?:l|\s+los))\s+(?:musulmanes?|judíos?|cristianos?|católicos?|evangélicos?)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Generic discrimination
        new(@"\b(?:esos?\s+(?:muertos\s+de\s+hambre|animales|basura\s+humana))", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    private static readonly Regex[] SexualContentPatterns =
    {
        new(@"\b(?:sexo|acto\s+sexual|relaciones?\s+sexuales?|coger(?:me|te|la|lo)?|follar|culear)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(?:desnud[oa]s?|pornograf[ií]a|porno|xxx|onlyfans)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(?:prostitut[oa]s?|escort|prepago|putas?|ramera)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(?:nudes?|dick\s*pic|fotos?\s+íntimas?|contenido\s+(?:adulto|explícito|sexual))", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    // ── Bot output profanity/discrimination patterns ──────────────────
    private static readonly Regex[] BotProfanityPatterns =
    {
        new(@"\b(?:mierda|carajo|coño|joder|culo|maric[oó]n|puta|cabrón|pendejo|idiota|estúpido|imbécil)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(?:te\s+(?:mereces|lo\s+buscaste)|es\s+tu\s+culpa|no\s+sirves)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    private static readonly Regex[] BotDiscriminationPatterns =
    {
        new(@"\b(?:los|las|esos|esas)\s+(?:haitianos?|negros?|mujeres|hombres)\s+(?:son|siempre|nunca|no\s+pueden)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\bpor\s+ser\s+(?:mujer|hombre|negro|haitiano|pobre|viejo)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    /// <summary>
    /// Checks user message for content that should be blocked.
    /// Returns user-friendly messages in SuggestedAction.
    /// </summary>
    public static ModerationResult ModerateUserMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return ModerationResult.Safe();

        var lower = message.ToLowerInvariant();

        // ── Hate Speech / Discrimination ─────────────────────────────
        foreach (var pattern in HateSpeechPatterns)
        {
            if (pattern.IsMatch(lower))
            {
                return new ModerationResult
                {
                    IsSafe = false,
                    Category = ModerationCategory.HateSpeech,
                    Reason = "Hate speech or discriminatory content detected",
                    SuggestedAction = "En OKLA promovemos el respeto. No podemos procesar mensajes con contenido discriminatorio. ¿Puedo ayudarte con algo sobre vehículos? 🚗"
                };
            }
        }

        // ── Sexual Content ───────────────────────────────────────────
        foreach (var pattern in SexualContentPatterns)
        {
            if (pattern.IsMatch(lower))
            {
                return new ModerationResult
                {
                    IsSafe = false,
                    Category = ModerationCategory.SexualContent,
                    Reason = "Sexual or explicit content detected",
                    SuggestedAction = "Este es un marketplace de vehículos. No puedo procesar ese tipo de contenido. ¿Te ayudo a encontrar un vehículo? 🚗"
                };
            }
        }

        // ── Scam/fraud patterns (common in RD marketplace) ──────────
        var scamPatterns = new[]
        {
            "envíame tu contraseña",
            "dame tu clave",
            "western union",
            "transferencia adelantada",
            "pago por adelantado",
            "envía depósito",
            "gana dinero fácil",
            "inversión segura",
            "bitcoin",
            "crypto",
            "número de tarjeta",
            "código de seguridad",
            "pin del banco",
        };

        foreach (var pattern in scamPatterns)
        {
            if (lower.Contains(pattern))
            {
                return new ModerationResult
                {
                    IsSafe = false,
                    Category = ModerationCategory.Scam,
                    Reason = $"Potential scam/fraud content detected: '{pattern}'",
                    SuggestedAction = "🚨 Nunca compartas contraseñas, datos bancarios o hagas transferencias adelantadas. En OKLA usamos canales seguros. ¿Necesitas ayuda con algo del marketplace?"
                };
            }
        }

        // ── Violence/threats ─────────────────────────────────────────
        var violencePatterns = new[]
        {
            "te voy a matar", "voy a matarte", "amenaza de muerte",
            "te voy a buscar", "sé dónde vives",
        };

        foreach (var pattern in violencePatterns)
        {
            if (lower.Contains(pattern))
            {
                return new ModerationResult
                {
                    IsSafe = false,
                    Category = ModerationCategory.Violence,
                    Reason = "Violence/threat detected",
                    SuggestedAction = "No podemos procesar mensajes con amenazas o contenido violento. Si tienes un problema con una transacción, usa okla.com.do/reportar para reportarlo. 🛡️"
                };
            }
        }

        // ── Off-topic solicitation ───────────────────────────────────
        var offTopicPatterns = new[]
        {
            "busco novia", "busco novio", "quieres salir",
            "vendo drogas", "marihuana", "cocaína",
        };

        foreach (var pattern in offTopicPatterns)
        {
            if (lower.Contains(pattern))
            {
                return new ModerationResult
                {
                    IsSafe = false,
                    Category = ModerationCategory.OffTopic,
                    Reason = "Off-topic content not related to vehicles",
                    SuggestedAction = "Soy un asistente de ventas de vehículos. Solo puedo ayudarte con temas relacionados al marketplace automotriz. ¿Buscas algún vehículo en particular? 🚗"
                };
            }
        }

        return ModerationResult.Safe();
    }

    /// <summary>
    /// Moderates LLM output before sending to user.
    /// Constitutional AI: Catches cases where the model generates content
    /// that violates OKLA's behavioral principles.
    /// </summary>
    public static ModerationResult ModerateBotResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return ModerationResult.Safe();

        var lower = response.ToLowerInvariant();

        // ── Bot profanity filter ─────────────────────────────────────
        foreach (var pattern in BotProfanityPatterns)
        {
            if (pattern.IsMatch(lower))
            {
                return new ModerationResult
                {
                    IsSafe = false,
                    Category = ModerationCategory.Violence,
                    Reason = "Bot generated profane or offensive content",
                    SuggestedAction = "¿Hay algo más sobre el vehículo en lo que pueda ayudarte? 😊"
                };
            }
        }

        // ── Bot discrimination filter ────────────────────────────────
        foreach (var pattern in BotDiscriminationPatterns)
        {
            if (pattern.IsMatch(lower))
            {
                return new ModerationResult
                {
                    IsSafe = false,
                    Category = ModerationCategory.HateSpeech,
                    Reason = "Bot generated discriminatory content",
                    SuggestedAction = "¿En qué más puedo ayudarte con nuestros vehículos? 🚗"
                };
            }
        }

        // ── Bot unauthorized professional advice ─────────────────────
        var advicePatterns = new[]
        {
            "te recomiendo como abogado",
            "legalmente deberías",
            "medicamente te sugiero",
            "invierte en",
            "préstamo personal te conviene",
        };

        foreach (var pattern in advicePatterns)
        {
            if (lower.Contains(pattern))
            {
                return new ModerationResult
                {
                    IsSafe = false,
                    Category = ModerationCategory.UnauthorizedAdvice,
                    Reason = "Bot generated unauthorized professional advice",
                    SuggestedAction = "No puedo dar asesoría legal o financiera profesional. Te recomiendo consultar con un especialista. ¿Hay algo sobre nuestros vehículos en lo que pueda ayudarte? 😊"
                };
            }
        }

        // ── Bot identity deception ───────────────────────────────────
        var identityPatterns = new[]
        {
            "soy una persona real",
            "no soy un bot",
            "soy humano",
        };

        foreach (var pattern in identityPatterns)
        {
            if (lower.Contains(pattern))
            {
                return new ModerationResult
                {
                    IsSafe = false,
                    Category = ModerationCategory.IdentityDeception,
                    Reason = "Bot claimed to be human",
                    SuggestedAction = "Soy un asistente virtual de OKLA. Estoy aquí para ayudarte con información sobre vehículos. ¿En qué puedo asistirte? 🤖"
                };
            }
        }

        // ── Bot hate speech (LLM-generated) ──────────────────────────
        foreach (var pattern in HateSpeechPatterns)
        {
            if (pattern.IsMatch(lower))
            {
                return new ModerationResult
                {
                    IsSafe = false,
                    Category = ModerationCategory.HateSpeech,
                    Reason = "Bot generated hate speech content",
                    SuggestedAction = "¿En qué más puedo ayudarte con nuestros vehículos? 🚗"
                };
            }
        }

        // ── Bot sexual content (LLM-generated) ──────────────────────
        foreach (var pattern in SexualContentPatterns)
        {
            if (pattern.IsMatch(lower))
            {
                return new ModerationResult
                {
                    IsSafe = false,
                    Category = ModerationCategory.SexualContent,
                    Reason = "Bot generated sexual/explicit content",
                    SuggestedAction = "¿Puedo ayudarte a encontrar un vehículo? 🚗"
                };
            }
        }

        return ModerationResult.Safe();
    }
}

public enum ModerationCategory
{
    None,
    Scam,
    Violence,
    HateSpeech,
    SexualContent,
    OffTopic,
    UnauthorizedAdvice,
    IdentityDeception
}

public class ModerationResult
{
    public bool IsSafe { get; set; } = true;
    public ModerationCategory Category { get; set; } = ModerationCategory.None;
    public string? Reason { get; set; }
    /// <summary>
    /// User-friendly message to show when content is moderated.
    /// This message is returned directly to the user.
    /// </summary>
    public string? SuggestedAction { get; set; }

    public static ModerationResult Safe() => new() { IsSafe = true };
}
