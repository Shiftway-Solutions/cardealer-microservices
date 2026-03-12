using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators;

/// <summary>
/// Validador para CreateSellerProfileRequest
/// Valida que los datos del perfil sean válidos antes de crear el perfil
/// </summary>
public class CreateSellerProfileRequestValidator : AbstractValidator<CreateSellerProfileRequest>
{
    public CreateSellerProfileRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId es requerido");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("FullName es requerido")
            .MaximumLength(255)
            .WithMessage("FullName no puede exceder 255 caracteres")
            .NoSqlInjection()
            .NoXss();

        // FASE 3: Phone and Email removed - use User entity properties instead

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .WithMessage("Address no puede exceder 500 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.City)
            .MaximumLength(100)
            .WithMessage("City no puede exceder 100 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.State)
            .MaximumLength(100)
            .WithMessage("State no puede exceder 100 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.State));

        RuleFor(x => x.ZipCode)
            .MaximumLength(20)
            .WithMessage("ZipCode no puede exceder 20 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.ZipCode));

        RuleFor(x => x.Nationality)
            .MaximumLength(100)
            .WithMessage("Nationality no puede exceder 100 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Nationality));

        // FASE 3: AlternatePhone removed - use User entity properties instead

        RuleFor(x => x.WhatsApp)
            .Matches(@"^\+?[\d\s\-\(\)]{10,}$")
            .WithMessage("WhatsApp debe ser un número válido (mínimo 10 dígitos)")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.WhatsApp));

        // Validación de especialidades
        RuleFor(x => x.Specialties)
            .Custom((specialties, context) =>
            {
                if (specialties == null || specialties.Length == 0)
                {
                    return; // Es opcional
                }

                // Máximo 10 especialidades
                if (specialties.Length > 10)
                {
                    context.AddFailure("Specialties", "No puedes seleccionar más de 10 especialidades");
                }

                // Validar cada especialidad
                foreach (var specialty in specialties)
                {
                    if (string.IsNullOrWhiteSpace(specialty))
                    {
                        context.AddFailure("Specialties", "Las especialidades no pueden estar vacías");
                        break;
                    }

                    if (specialty.Length > 100)
                    {
                        context.AddFailure("Specialties", $"La especialidad '{specialty}' no puede exceder 100 caracteres");
                        break;
                    }
                }
            });

        RuleFor(x => x.PreferredContactMethod)
            .Must(m => m == null || new[] { "phone", "whatsapp", "email", "chat" }.Contains(m.ToLower()))
            .WithMessage("PreferredContactMethod debe ser: phone, whatsapp, email o chat")
            .When(x => !string.IsNullOrEmpty(x.PreferredContactMethod));
    }
}

/// <summary>
/// Validador para UpdateSellerProfileRequest
/// Valida que los datos actualizados sean válidos
/// </summary>
public class UpdateSellerProfileRequestValidator : AbstractValidator<UpdateSellerProfileRequest>
{
    public UpdateSellerProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(255)
            .WithMessage("FullName no puede exceder 255 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.FullName));

        // FASE 3: Phone and Email removed - use User entity properties instead

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .WithMessage("Address no puede exceder 500 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.City)
            .MaximumLength(100)
            .WithMessage("City no puede exceder 100 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.State)
            .MaximumLength(100)
            .WithMessage("State no puede exceder 100 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.State));

        RuleFor(x => x.ZipCode)
            .MaximumLength(20)
            .WithMessage("ZipCode no puede exceder 20 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.ZipCode));

        RuleFor(x => x.Nationality)
            .MaximumLength(100)
            .WithMessage("Nationality no puede exceder 100 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Nationality));

        // FASE 3: AlternatePhone removed - use User entity properties instead

        RuleFor(x => x.WhatsApp)
            .Matches(@"^\+?[\d\s\-\(\)]{10,}$")
            .WithMessage("WhatsApp debe ser un número válido (mínimo 10 dígitos)")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.WhatsApp));

        RuleFor(x => x.Bio)
            .MaximumLength(1000)
            .WithMessage("Bio no puede exceder 1000 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Bio));

        // Validación de especialidades
        RuleFor(x => x.Specialties)
            .Custom((specialties, context) =>
            {
                if (specialties == null || specialties.Length == 0)
                {
                    return; // Es opcional
                }

                // Máximo 10 especialidades
                if (specialties.Length > 10)
                {
                    context.AddFailure("Specialties", "No puedes seleccionar más de 10 especialidades");
                }

                // Validar cada especialidad
                foreach (var specialty in specialties)
                {
                    if (string.IsNullOrWhiteSpace(specialty))
                    {
                        context.AddFailure("Specialties", "Las especialidades no pueden estar vacías");
                        break;
                    }

                    if (specialty.Length > 100)
                    {
                        context.AddFailure("Specialties", $"La especialidad '{specialty}' no puede exceder 100 caracteres");
                        break;
                    }
                }
            });

        RuleFor(x => x.PreferredContactMethod)
            .Must(m => m == null || new[] { "phone", "whatsapp", "email", "chat" }.Contains(m.ToLower()))
            .WithMessage("PreferredContactMethod debe ser: phone, whatsapp, email o chat")
            .When(x => !string.IsNullOrEmpty(x.PreferredContactMethod));
    }
}
