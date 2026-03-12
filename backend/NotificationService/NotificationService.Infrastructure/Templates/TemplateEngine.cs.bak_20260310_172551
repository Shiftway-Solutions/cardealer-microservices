using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Interfaces;
using NotificationService.Shared;
using System.Text.RegularExpressions;
using System.Text;

namespace NotificationService.Infrastructure.Templates;

public class TemplateEngine : ITemplateEngine
{
    private readonly string _templatesPath;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TemplateEngine> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly Regex PlaceholderRegex = new(@"\{\{(\s*[\w\.]+\s*)\}\}", RegexOptions.Compiled);

    public TemplateEngine(
        IOptions<NotificationSettings> settings,
        IMemoryCache cache,
        ILogger<TemplateEngine> logger)
    {
        _templatesPath = settings.Value.TemplatesPath ?? "Templates";
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, object> parameters)
    {
        try
        {
            // Get template content from cache or file
            var templateContent = await GetTemplateContentAsync(templateName);

            // Render with parameters
            return RenderContent(templateContent, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template {TemplateName}", templateName);
            throw;
        }
    }

    private async Task<string> GetTemplateContentAsync(string templateName)
    {
        // Try cache first
        var cacheKey = $"template:{templateName}";
        if (_cache.TryGetValue<string>(cacheKey, out var cachedContent))
        {
            _logger.LogDebug("Template {TemplateName} loaded from cache", templateName);
            return cachedContent!;
        }

        // Load from file
        var templatePath = Path.Combine(_templatesPath, templateName);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template {templateName} not found at {templatePath}");

        var content = await File.ReadAllTextAsync(templatePath);

        // Cache for future use
        _cache.Set(cacheKey, content, CacheExpiration);
        _logger.LogDebug("Template {TemplateName} loaded from file and cached", templateName);

        return content;
    }

    private string RenderContent(string templateContent, Dictionary<string, object> parameters)
    {
        if (parameters == null || !parameters.Any())
            return templateContent;

        return PlaceholderRegex.Replace(templateContent, match =>
        {
            var key = match.Groups[1].Value.Trim();
            return ResolveValue(key, parameters);
        });
    }

    private string ResolveValue(string key, Dictionary<string, object> parameters)
    {
        // Support nested properties: {{user.name}} or {{order.items.count}}
        var parts = key.Split('.');
        object? current = parameters;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> dict)
            {
                if (!dict.TryGetValue(part, out current))
                {
                    _logger.LogWarning("Parameter {Key} not found in template data", key);
                    return string.Empty;
                }
            }
            else if (current != null)
            {
                // Support object properties via reflection
                var property = current.GetType().GetProperty(part);
                if (property != null)
                {
                    current = property.GetValue(current);
                }
                else
                {
                    _logger.LogWarning("Property {Part} not found on object for key {Key}", part, key);
                    return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }
        }

        return current?.ToString() ?? string.Empty;
    }

    public void ClearCache()
    {
        _logger.LogInformation("Clearing template cache");
        // Note: IMemoryCache doesn't have a clear method, but cache will expire naturally
    }

    public bool ValidateTemplate(string templateContent, out List<string> errors)
    {
        errors = new List<string>();

        try
        {
            // Check for unclosed placeholders
            var openCount = templateContent.Count(c => c == '{');
            var closeCount = templateContent.Count(c => c == '}');

            if (openCount != closeCount)
            {
                errors.Add("Mismatched braces in template");
            }

            // Check for valid placeholder syntax
            var matches = PlaceholderRegex.Matches(templateContent);
            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value.Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    errors.Add($"Empty placeholder at position {match.Index}");
                }
                else if (!IsValidPlaceholderName(key))
                {
                    errors.Add($"Invalid placeholder name '{key}' at position {match.Index}");
                }
            }

            return errors.Count == 0;
        }
        catch (Exception ex)
        {
            errors.Add($"Validation error: {ex.Message}");
            return false;
        }
    }

    private bool IsValidPlaceholderName(string name)
    {
        // Allow alphanumeric, underscore, and dots for nested properties
        return Regex.IsMatch(name, @"^[\w\.]+$");
    }

    public List<string> ExtractPlaceholders(string templateContent)
    {
        var placeholders = new List<string>();
        var matches = PlaceholderRegex.Matches(templateContent);

        foreach (Match match in matches)
        {
            var key = match.Groups[1].Value.Trim();
            if (!placeholders.Contains(key))
            {
                placeholders.Add(key);
            }
        }

        return placeholders;
    }
}