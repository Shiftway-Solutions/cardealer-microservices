using System.Text.Json;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Saved search entity — users save search criteria and receive notifications when new vehicles match.
/// </summary>
public class SavedSearch
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON-serialized search criteria.
    /// </summary>
    public string CriteriaJson { get; set; } = "{}";
    
    public bool NotifyOnNewResults { get; set; } = true;
    public bool NotifyByEmail { get; set; } = true;
    public bool NotifyByPush { get; set; } = true;
    public string NotificationFrequency { get; set; } = "daily";
    public int MatchCount { get; set; }
    public DateTime? LastMatchAt { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public SavedSearch()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public static SavedSearch Create(
        Guid userId,
        string name,
        object criteria,
        bool notifyOnNewResults = true,
        bool notifyByEmail = true,
        bool notifyByPush = true,
        string notificationFrequency = "daily")
    {
        return new SavedSearch
        {
            UserId = userId,
            Name = name,
            CriteriaJson = JsonSerializer.Serialize(criteria),
            NotifyOnNewResults = notifyOnNewResults,
            NotifyByEmail = notifyByEmail,
            NotifyByPush = notifyByPush,
            NotificationFrequency = notificationFrequency
        };
    }

    public void IncrementMatchCount()
    {
        MatchCount++;
        LastMatchAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkNotified()
    {
        LastNotifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
