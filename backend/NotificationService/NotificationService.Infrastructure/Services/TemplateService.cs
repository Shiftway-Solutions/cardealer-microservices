using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Interfaces;
using NotificationService.Shared;
using System.Net;
using System.Text.RegularExpressions;

namespace NotificationService.Infrastructure.Services;

public class TemplateService : ITemplateEngine
{
    private readonly string _templatesPath;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TemplateService> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly Regex PlaceholderRegex = new(@"\{\{(\s*[\w\.]+\s*)\}\}", RegexOptions.Compiled);

    public TemplateService(
        IOptions<NotificationSettings> settings,
        IMemoryCache cache,
        ILogger<TemplateService> logger)
    {
        _templatesPath = settings.Value.TemplatesPath ?? "Templates";
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, object> parameters)
    {
        try
        {
            var templateContent = await GetTemplateContentAsync(templateName);
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
        var cacheKey = $"template:{templateName}";
        if (_cache.TryGetValue<string>(cacheKey, out var cachedContent))
        {
            return cachedContent!;
        }

        var templatePath = Path.Combine(_templatesPath, templateName);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template {templateName} not found at {templatePath}");

        var content = await File.ReadAllTextAsync(templatePath);
        _cache.Set(cacheKey, content, CacheExpiration);

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
        var parts = key.Split('.');
        object? current = parameters;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> dict)
            {
                if (!dict.TryGetValue(part, out current))
                    return string.Empty;
            }
            else if (current != null)
            {
                var property = current.GetType().GetProperty(part);
                if (property != null)
                    current = property.GetValue(current);
                else
                    return string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }

        return WebUtility.HtmlEncode(current?.ToString() ?? string.Empty);
    }

    public void ClearCache()
    {
        _logger.LogInformation("Clearing template cache");
    }

    public bool ValidateTemplate(string templateContent, out List<string> errors)
    {
        errors = new List<string>();

        try
        {
            var openCount = templateContent.Count(c => c == '{');
            var closeCount = templateContent.Count(c => c == '}');

            if (openCount != closeCount)
                errors.Add("Mismatched braces in template");

            var matches = PlaceholderRegex.Matches(templateContent);
            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value.Trim();
                if (string.IsNullOrWhiteSpace(key))
                    errors.Add($"Empty placeholder at position {match.Index}");
                else if (!IsValidPlaceholderName(key))
                    errors.Add($"Invalid placeholder name '{key}' at position {match.Index}");
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
                placeholders.Add(key);
        }

        return placeholders;
    }
}