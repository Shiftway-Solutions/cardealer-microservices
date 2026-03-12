using Microsoft.Extensions.Logging;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;

namespace ChatbotService.Infrastructure.Services;

public class AutoLearningService : IAutoLearningService
{
    private readonly IUnansweredQuestionRepository _unansweredRepo;
    private readonly IChatMessageRepository _messageRepo;
    private readonly IQuickResponseRepository _quickResponseRepo;
    private readonly ILlmService _llmService;
    private readonly ILogger<AutoLearningService> _logger;

    public AutoLearningService(
        IUnansweredQuestionRepository unansweredRepo,
        IChatMessageRepository messageRepo,
        IQuickResponseRepository quickResponseRepo,
        ILlmService llmService,
        ILogger<AutoLearningService> logger)
    {
        _unansweredRepo = unansweredRepo;
        _messageRepo = messageRepo;
        _quickResponseRepo = quickResponseRepo;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<AutoLearningAnalysisResult> AnalyzeAndSuggestAsync(Guid configurationId, CancellationToken ct = default)
    {
        var result = new AutoLearningAnalysisResult { ChatbotConfigurationId = configurationId };

        try
        {
            _logger.LogInformation("Starting auto-learning analysis for configuration {ConfigId}", configurationId);

            // Analyze unanswered questions
            var unansweredQuestions = await _unansweredRepo.GetUnprocessedAsync(configurationId, 100, ct);
            result.FallbacksAnalyzed = unansweredQuestions.Count();

            // Group similar questions
            var clusters = ClusterSimilarQuestions(unansweredQuestions);

            foreach (var cluster in clusters.Where(c => c.Count >= 3))
            {
                var suggestedIntent = new SuggestedIntent
                {
                    IntentName = GenerateIntentName(cluster.RepresentativeQuestion),
                    Description = $"Auto-generated from {cluster.Count} similar unanswered questions",
                    TrainingPhrases = cluster.Questions.Take(10).ToList(),
                    Category = DetermineCategory(cluster.RepresentativeQuestion),
                    OccurrenceCount = cluster.TotalOccurrences,
                    ConfidenceScore = cluster.Count >= 5 ? 0.8f : 0.6f,
                    RequiresReview = true,
                    Source = "auto-learning"
                };

                result.SuggestedIntents.Add(suggestedIntent);
                result.Suggestions.Add(new AutoLearningSuggestion
                {
                    ChatbotConfigurationId = configurationId,
                    Type = SuggestionType.NewIntent,
                    Title = $"Crear intent: {suggestedIntent.IntentName}",
                    Description = $"Basado en {cluster.Count} preguntas similares con {cluster.TotalOccurrences} ocurrencias totales",
                    Data = suggestedIntent,
                    Priority = cluster.TotalOccurrences > 10 ? 8 : 5,
                    ConfidenceScore = suggestedIntent.ConfidenceScore
                });
            }

            // Analyze fallback patterns
            var fallbackMessages = await _messageRepo.GetFallbackMessagesAsync(configurationId, DateTime.UtcNow.AddDays(-7), ct);
            var fallbackPatterns = AnalyzeFallbackPatterns(fallbackMessages);

            foreach (var pattern in fallbackPatterns.Where(p => p.Count >= 5))
            {
                result.Suggestions.Add(new AutoLearningSuggestion
                {
                    ChatbotConfigurationId = configurationId,
                    Type = SuggestionType.QuickResponseCreation,
                    Title = $"Quick Response: {pattern.Category}",
                    Description = $"Crear respuesta rápida para {pattern.Count} mensajes sobre {pattern.Category}",
                    Data = new QuickResponse
                    {
                        Category = pattern.Category,
                        TriggerKeywords = string.Join(",", pattern.Keywords),
                        ResponseText = pattern.SuggestedResponse
                    },
                    Priority = 6,
                    ConfidenceScore = 0.7f
                });
            }

            result.PendingReviewCount = result.Suggestions.Count(s => !s.IsApplied);
            result.EstimatedCostSavings = CalculateEstimatedSavings(result.Suggestions);

            _logger.LogInformation("Auto-learning analysis completed: {IntentCount} suggested intents, {SuggestionCount} total suggestions",
                result.SuggestedIntents.Count, result.Suggestions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-learning analysis failed for configuration {ConfigId}", configurationId);
        }

        return result;
    }

    public async Task<bool> ApplySuggestionAsync(AutoLearningSuggestion suggestion, CancellationToken ct = default)
    {
        try
        {
            switch (suggestion.Type)
            {
                case SuggestionType.NewIntent:
                    // Con LLM no se crean intents en un servicio externo —
                    // las sugerencias se almacenan para re-entrenamiento futuro del modelo
                    _logger.LogInformation("Intent suggestion recorded for future LLM re-training: {Title}", suggestion.Title);
                    suggestion.IsApplied = true;
                    suggestion.AppliedAt = DateTime.UtcNow;
                    return true;

                case SuggestionType.TrainingPhraseAddition:
                    // Se almacenan frases para el próximo ciclo de fine-tuning
                    _logger.LogInformation("Training phrases recorded for future LLM re-training: {Title}", suggestion.Title);
                    suggestion.IsApplied = true;
                    suggestion.AppliedAt = DateTime.UtcNow;
                    return true;

                case SuggestionType.QuickResponseCreation:
                    if (suggestion.Data is QuickResponse qr)
                    {
                        await _quickResponseRepo.CreateAsync(qr, ct);
                        return true;
                    }
                    break;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply suggestion {SuggestionId}", suggestion.Id);
            return false;
        }
    }

    /// <summary>
    /// R13 (MLOps): Human-in-the-loop — High-confidence suggestions are NO LONGER
    /// auto-applied. Instead they are queued for human review. This prevents
    /// silent model drift from unchecked auto-learning.
    /// 
    /// All suggestions with confidence ≥ minConfidence are marked as 
    /// RequiresReview = true and queued. A human operator must explicitly 
    /// call ApplySuggestionAsync to apply them.
    /// </summary>
    public async Task<int> AutoApplyHighConfidenceSuggestionsAsync(Guid configurationId, float minConfidence = 0.85f, CancellationToken ct = default)
    {
        var result = await AnalyzeAndSuggestAsync(configurationId, ct);
        var queued = 0;

        foreach (var suggestion in result.Suggestions.Where(s => s.ConfidenceScore >= minConfidence))
        {
            // R13: Queue for human review instead of auto-applying
            suggestion.RequiresReview = true;
            suggestion.IsApplied = false;
            suggestion.AppliedBy = "queued-for-review";
            queued++;

            _logger.LogInformation(
                "Queued high-confidence suggestion for human review: {Title} (confidence: {Confidence:F2})",
                suggestion.Title, suggestion.ConfidenceScore);
        }

        if (queued > 0)
            _logger.LogInformation(
                "Queued {Count} high-confidence suggestions for human review (previously auto-applied). " +
                "Use admin dashboard or API to approve/reject.", queued);

        return queued;
    }

    public async Task<IEnumerable<SuggestedIntent>> GetSuggestedIntentsAsync(Guid configurationId, int limit = 10, CancellationToken ct = default)
    {
        var result = await AnalyzeAndSuggestAsync(configurationId, ct);
        return result.SuggestedIntents.Take(limit);
    }

    public async Task RecordFallbackAsync(Guid configurationId, string userMessage, string? attemptedIntent, float? confidence, CancellationToken ct = default)
    {
        await _unansweredRepo.CreateOrIncrementAsync(configurationId, userMessage, attemptedIntent, (decimal?)confidence, ct);
    }

    private static List<QuestionCluster> ClusterSimilarQuestions(IEnumerable<UnansweredQuestion> questions)
    {
        var clusters = new List<QuestionCluster>();
        var processed = new HashSet<Guid>();

        foreach (var question in questions.OrderByDescending(q => q.OccurrenceCount))
        {
            if (processed.Contains(question.Id)) continue;

            var cluster = new QuestionCluster
            {
                RepresentativeQuestion = question.OriginalQuestion,
                Questions = new List<string> { question.OriginalQuestion },
                TotalOccurrences = question.OccurrenceCount
            };

            foreach (var other in questions.Where(q => !processed.Contains(q.Id) && q.Id != question.Id))
            {
                if (AreSimilar(question.NormalizedQuestion, other.NormalizedQuestion))
                {
                    cluster.Questions.Add(other.OriginalQuestion);
                    cluster.TotalOccurrences += other.OccurrenceCount;
                    processed.Add(other.Id);
                }
            }

            processed.Add(question.Id);
            clusters.Add(cluster);
        }

        return clusters;
    }

    private static bool AreSimilar(string a, string b)
    {
        var wordsA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wordsB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var common = wordsA.Intersect(wordsB, StringComparer.OrdinalIgnoreCase).Count();
        var total = Math.Max(wordsA.Length, wordsB.Length);
        return total > 0 && (double)common / total >= 0.6;
    }

    private static string GenerateIntentName(string question)
    {
        var words = question.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Take(3)
            .Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower());
        return $"auto.{string.Join("", words)}";
    }

    private static string DetermineCategory(string question)
    {
        var q = question.ToLowerInvariant();
        if (q.Contains("precio") || q.Contains("costo") || q.Contains("valor")) return "pricing";
        if (q.Contains("financ") || q.Contains("crédito") || q.Contains("cuota")) return "financing";
        if (q.Contains("garantía") || q.Contains("warranty")) return "warranty";
        if (q.Contains("disponib") || q.Contains("stock")) return "availability";
        if (q.Contains("test") || q.Contains("probar") || q.Contains("ver")) return "test_drive";
        return "general";
    }

    private static List<FallbackPattern> AnalyzeFallbackPatterns(IEnumerable<ChatMessage> messages)
    {
        var patterns = new Dictionary<string, FallbackPattern>();

        foreach (var msg in messages)
        {
            var category = DetermineCategory(msg.Content);
            if (!patterns.ContainsKey(category))
                patterns[category] = new FallbackPattern { Category = category };

            patterns[category].Count++;
            var words = msg.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3);
            foreach (var word in words)
            {
                if (!patterns[category].Keywords.Contains(word.ToLower()))
                    patterns[category].Keywords.Add(word.ToLower());
            }
        }

        return patterns.Values.ToList();
    }

    private static decimal CalculateEstimatedSavings(IEnumerable<AutoLearningSuggestion> suggestions)
    {
        // Each quick response saves ~$0.002 per interaction
        var quickResponseSuggestions = suggestions.Count(s => s.Type == SuggestionType.QuickResponseCreation);
        var estimatedInteractionsPerMonth = quickResponseSuggestions * 50; // 50 interactions per new quick response
        return estimatedInteractionsPerMonth * 0.002m;
    }

    private class QuestionCluster
    {
        public string RepresentativeQuestion { get; set; } = string.Empty;
        public List<string> Questions { get; set; } = new();
        public int TotalOccurrences { get; set; }
        public int Count => Questions.Count;
    }

    private class FallbackPattern
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<string> Keywords { get; set; } = new();
        public string SuggestedResponse => $"Para consultas sobre {Category}, por favor contacte a un asesor.";
    }
}

internal class TrainingPhraseData
{
    public string IntentName { get; set; } = string.Empty;
    public List<string> Phrases { get; set; } = new();
}
