namespace UserService.Domain.Entities;

/// <summary>
/// Representa el progreso de onboarding de un nuevo usuario
/// </summary>
public class UserOnboarding
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public bool WasSkipped { get; private set; }
    public DateTime? SkippedAt { get; private set; }

    // Steps del onboarding
    public bool StepProfileCompleted { get; private set; }
    public DateTime? StepProfileCompletedAt { get; private set; }

    public bool StepPreferencesCompleted { get; private set; }
    public DateTime? StepPreferencesCompletedAt { get; private set; }

    public bool StepFirstSearchCompleted { get; private set; }
    public DateTime? StepFirstSearchCompletedAt { get; private set; }

    public bool StepTourCompleted { get; private set; }
    public DateTime? StepTourCompletedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // For EF Core
    private UserOnboarding() { }

    public UserOnboarding(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        IsCompleted = false;
        WasSkipped = false;
        StepProfileCompleted = false;
        StepPreferencesCompleted = false;
        StepFirstSearchCompleted = false;
        StepTourCompleted = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca un step como completado
    /// </summary>
    public void CompleteStep(OnboardingStep step)
    {
        if (IsCompleted || WasSkipped)
            return;

        var now = DateTime.UtcNow;

        switch (step)
        {
            case OnboardingStep.Profile:
                StepProfileCompleted = true;
                StepProfileCompletedAt = now;
                break;
            case OnboardingStep.Preferences:
                StepPreferencesCompleted = true;
                StepPreferencesCompletedAt = now;
                break;
            case OnboardingStep.FirstSearch:
                StepFirstSearchCompleted = true;
                StepFirstSearchCompletedAt = now;
                break;
            case OnboardingStep.Tour:
                StepTourCompleted = true;
                StepTourCompletedAt = now;
                break;
        }

        UpdatedAt = now;

        // Auto-completar si todos los steps están hechos
        if (AllStepsCompleted())
        {
            CompleteOnboarding();
        }
    }

    /// <summary>
    /// Completa el onboarding manualmente
    /// </summary>
    public void CompleteOnboarding()
    {
        if (IsCompleted || WasSkipped)
            return;

        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Permite al usuario saltar el onboarding
    /// </summary>
    public void Skip()
    {
        if (IsCompleted || WasSkipped)
            return;

        WasSkipped = true;
        SkippedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Verifica si todos los steps están completados
    /// </summary>
    public bool AllStepsCompleted()
    {
        return StepProfileCompleted
            && StepPreferencesCompleted
            && StepFirstSearchCompleted
            && StepTourCompleted;
    }

    /// <summary>
    /// Obtiene el progreso como porcentaje (0-100)
    /// </summary>
    public int GetProgressPercentage()
    {
        if (IsCompleted || WasSkipped)
            return 100;

        int completedSteps = 0;
        if (StepProfileCompleted) completedSteps++;
        if (StepPreferencesCompleted) completedSteps++;
        if (StepFirstSearchCompleted) completedSteps++;
        if (StepTourCompleted) completedSteps++;

        return (completedSteps * 100) / 4; // 4 steps total
    }

    /// <summary>
    /// Obtiene el siguiente step pendiente
    /// </summary>
    public OnboardingStep? GetNextStep()
    {
        if (IsCompleted || WasSkipped)
            return null;

        if (!StepProfileCompleted) return OnboardingStep.Profile;
        if (!StepPreferencesCompleted) return OnboardingStep.Preferences;
        if (!StepFirstSearchCompleted) return OnboardingStep.FirstSearch;
        if (!StepTourCompleted) return OnboardingStep.Tour;

        return null;
    }
}

public enum OnboardingStep
{
    Profile = 1,        // Completar perfil (nombre, foto, ubicación)
    Preferences = 2,    // Preferencias de búsqueda (marcas favoritas, presupuesto)
    FirstSearch = 3,    // Hacer primera búsqueda
    Tour = 4            // Tour guiado de la plataforma
}
