using System.Text.RegularExpressions;

namespace SupportAgent.Application.Services;

/// <summary>
/// Validates SupportAgent LLM output against grounding data to prevent hallucinations.
/// 
/// Anti-hallucination checks:
/// 1. URL Whitelist — Only known OKLA URLs are allowed
/// 2. Price Whitelist — Only known plan prices are allowed
/// 3. Action Blocklist — Phrases where Claude claims capabilities it doesn't have
/// 4. Legal Citation Validator — Only known Dominican laws are allowed
/// 5. Hallucination Phrase Detection — Generic fabrication patterns
/// </summary>
public static class SupportOutputValidator
{
    // ── 1. URL Whitelist — Known OKLA URLs ───────────────────────────
    private static readonly HashSet<string> AllowedUrlPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "okla.com.do",
        "okla.com.do/registro",
        "okla.com.do/registro/dealer",
        "okla.com.do/login",
        "okla.com.do/recuperar-contrasena",
        "okla.com.do/cuenta",
        "okla.com.do/cuenta/verificacion",
        "okla.com.do/cuenta/seguridad",
        "okla.com.do/cuenta/favoritos",
        "okla.com.do/cuenta/alertas",
        "okla.com.do/cuenta/busquedas",
        "okla.com.do/cuenta/mensajes",
        "okla.com.do/cuenta/convert-to-seller",
        "okla.com.do/vehiculos",
        "okla.com.do/buscar",
        "okla.com.do/comparar",
        "okla.com.do/publicar",
        "okla.com.do/mis-vehiculos",
        "okla.com.do/vender/leads",
        "okla.com.do/precios",
        "okla.com.do/reportar",
        "okla.com.do/dealer",
        "okla.com.do/dealer/inventario",
        "okla.com.do/dealer/empleados",
        "okla.com.do/dealer/configuracion/suscripcion",
        // External trusted URLs from knowledge base
        "dgii.gov.do",
        "intrant.gob.do",
        "proconsumidor.gob.do",
    };

    // ── 2. Known Plan Prices ─────────────────────────────────────────
    private static readonly HashSet<string> KnownPrices = new()
    {
        "1,699", "1699",       // Seller publication
        "499",                  // Boost Básico
        "1,499", "1499",       // Boost Premium
        "2,899", "2899",       // Dealer Starter
        "7,499", "7499",       // Dealer Pro
        "17,499", "17499",     // Dealer Enterprise
    };

    // ── 3. Action Blocklist — Things SupportAgent cannot do ──────────
    private static readonly Regex[] ActionBlocklistPatterns =
    {
        new(@"(?:te\s+)?(?:puedo|voy\s+a|ya)\s+(?:activar|desactivar|eliminar|borrar|cambiar|modificar|cancelar)\s+(?:tu|la|el)\s+(?:cuenta|perfil|plan|suscripci[oó]n)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:te\s+)?(?:hago|realizo|proceso|envío)\s+(?:un\s+)?(?:reembolso|devoluci[oó]n|pago|transferencia)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:ya\s+)?(?:aprobé|rechacé|verifiqué|activé|desactivé|eliminé|cambié)\s+(?:tu|la|el)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:tu\s+)?(?:kyc|verificaci[oó]n)\s+(?:fue|ha\s+sido|está|quedó)\s+(?:aprobad[oa]|rechazad[oa]|verificad[oa])", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"te\s+(?:consigo|garantizo|aseguro|prometo)\s+(?:que|un)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:voy\s+a|puedo)\s+(?:contactar|llamar|escribir)\s+(?:al|a\s+un)\s+(?:vendedor|dealer|admin)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:puedo|voy\s+a)\s+(?:acceder|entrar|revisar|ver)\s+(?:tu|a\s+tu)\s+(?:cuenta|perfil|datos|historial)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"cr[eé]dito\s+pre[-\s]?aprobado", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    // ── 4. Known Dominican Laws ──────────────────────────────────────
    private static readonly HashSet<string> KnownLaws = new(StringComparer.OrdinalIgnoreCase)
    {
        "ley 241",        // Tránsito de Vehículos
        "ley 155-17",     // Prevención Lavado de Activos
        "ley 358-05",     // Protección al Consumidor
        "ley 146-02",     // Seguros
        "ley 659",        // Registro Civil
        "ley 489-08",     // Notariado
    };

    private static readonly Regex LawCitationPattern = new(
        @"ley\s+\d+(?:-\d+)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex UrlPattern = new(
        @"(?:https?://)?(?:www\.)?([a-zA-Z0-9\-]+(?:\.[a-zA-Z0-9\-]+)+(?:/[^\s,\)\]\""']*)?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PricePattern = new(
        @"RD\$\s?([\d,\.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ── 5. Generic Hallucination Phrases ─────────────────────────────
    private static readonly Regex[] HallucinationPhrases =
    {
        new(@"según\s+(?:nuestros?\s+)?(?:registros?|datos|sistema)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:he\s+)?verificado\s+(?:en\s+)?(?:el\s+)?sistema", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:puedo\s+)?confirm(?:o|ar)\s+que\s+(?:tu|su)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:estoy\s+)?(?:viendo|revisando)\s+(?:tu|su)\s+(?:cuenta|perfil|caso)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    /// <summary>
    /// Validates the LLM response against grounding rules.
    /// Returns a result with violations found and sanitized response.
    /// </summary>
    public static SupportGroundingResult Validate(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return SupportGroundingResult.Valid(response);

        var result = new SupportGroundingResult
        {
            OriginalResponse = response,
            SanitizedResponse = response,
            IsGrounded = true,
        };

        // Check 1: URL whitelist
        ValidateUrls(response, result);

        // Check 2: Price whitelist
        ValidatePrices(response, result);

        // Check 3: Action blocklist
        ValidateActions(response, result);

        // Check 4: Legal citations
        ValidateLegalCitations(response, result);

        // Check 5: Hallucination phrases
        ValidateHallucinationPhrases(response, result);

        // If violations found, append disclaimer
        if (result.Violations.Count > 0)
        {
            result.IsGrounded = false;

            // Replace fabricated URLs with the generic support URL
            foreach (var url in result.FabricatedUrls)
            {
                result.SanitizedResponse = result.SanitizedResponse.Replace(
                    url, "okla.com.do/soporte");
            }

            // Remove action claims
            foreach (var pattern in ActionBlocklistPatterns)
            {
                if (pattern.IsMatch(result.SanitizedResponse))
                {
                    result.SanitizedResponse = pattern.Replace(result.SanitizedResponse,
                        "te recomiendo contactar a nuestro equipo de soporte para eso");
                }
            }

            // Replace hallucination phrases
            foreach (var pattern in HallucinationPhrases)
            {
                result.SanitizedResponse = pattern.Replace(result.SanitizedResponse,
                    "basándome en la información disponible");
            }

            // Append disclaimer if multiple violations
            if (result.Violations.Count >= 2)
            {
                result.SanitizedResponse += "\n\n⚠️ _Si necesitas asistencia personalizada, contacta a soporte@okla.com.do_";
            }
        }

        return result;
    }

    private static void ValidateUrls(string response, SupportGroundingResult result)
    {
        var urlMatches = UrlPattern.Matches(response);
        foreach (Match match in urlMatches)
        {
            var fullUrl = match.Value
                .TrimEnd('.', ',', ')', ']', '"', '\'')
                .Replace("https://", "")
                .Replace("http://", "")
                .Replace("www.", "");

            // Check if the URL base is in the whitelist
            var isAllowed = AllowedUrlPaths.Any(allowed =>
                fullUrl.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                result.Violations.Add($"url_fabrication:{match.Value}");
                result.FabricatedUrls.Add(match.Value);
            }
        }
    }

    private static void ValidatePrices(string response, SupportGroundingResult result)
    {
        var priceMatches = PricePattern.Matches(response);
        foreach (Match match in priceMatches)
        {
            var priceValue = match.Groups[1].Value.Replace(".", "").Trim();
            if (!KnownPrices.Contains(priceValue))
            {
                result.Violations.Add($"price_fabrication:RD${priceValue}");
            }
        }
    }

    private static void ValidateActions(string response, SupportGroundingResult result)
    {
        foreach (var pattern in ActionBlocklistPatterns)
        {
            if (pattern.IsMatch(response))
            {
                var match = pattern.Match(response);
                result.Violations.Add($"action_claim:{match.Value}");
            }
        }
    }

    private static void ValidateLegalCitations(string response, SupportGroundingResult result)
    {
        var lawMatches = LawCitationPattern.Matches(response);
        foreach (Match match in lawMatches)
        {
            var citation = match.Value.ToLowerInvariant().Trim();
            if (!KnownLaws.Contains(citation))
            {
                result.Violations.Add($"legal_fabrication:{match.Value}");
            }
        }
    }

    private static void ValidateHallucinationPhrases(string response, SupportGroundingResult result)
    {
        foreach (var pattern in HallucinationPhrases)
        {
            if (pattern.IsMatch(response))
            {
                var match = pattern.Match(response);
                result.Violations.Add($"hallucination_phrase:{match.Value}");
            }
        }
    }
}

/// <summary>
/// Result of output grounding validation for SupportAgent responses.
/// </summary>
public class SupportGroundingResult
{
    public string OriginalResponse { get; set; } = string.Empty;
    public string SanitizedResponse { get; set; } = string.Empty;
    public bool IsGrounded { get; set; } = true;
    public List<string> Violations { get; set; } = new();
    public List<string> FabricatedUrls { get; set; } = new();

    public static SupportGroundingResult Valid(string response) => new()
    {
        OriginalResponse = response,
        SanitizedResponse = response,
        IsGrounded = true,
    };
}
