using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UserService.Domain.Entities
{
    /// <summary>
    /// Tipos de cuenta en la plataforma
    /// </summary>
    public enum AccountType
    {
        Guest,
        Buyer,
        Dealer,
        DealerEmployee,
        Admin,
        PlatformEmployee,
        Seller
    }

    /// <summary>
    /// Intención del usuario en la plataforma.
    /// Define qué quiere hacer el usuario (comprar, vender, ambos).
    /// Puede cambiar a lo largo del tiempo.
    /// </summary>
    public enum UserIntent
    {
        /// <summary>Solo navega (default al registrarse)</summary>
        Browse,
        /// <summary>Quiere comprar un vehículo (gratis)</summary>
        Buy,
        /// <summary>Quiere vender un vehículo ($29/listing)</summary>
        Sell,
        /// <summary>Quiere comprar y vender</summary>
        BuyAndSell
    }

    /// <summary>
    /// Roles para empleados de dealers
    /// </summary>
    public enum DealerRole
    {
        Owner,
        Manager,
        SalesManager,
        InventoryManager,
        Salesperson,
        Viewer
    }

    /// <summary>
    /// Roles para empleados de plataforma
    /// </summary>
    public enum PlatformRole
    {
        SuperAdmin,
        Admin,
        Moderator,
        Support,
        Analyst
    }

    /// <summary>
    /// Entidad de dominio User
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool EmailConfirmed { get; set; } = false;

        // Profile fields for seller public profile
        public string? ProfilePicture { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? BusinessName { get; set; }
        public string? BusinessPhone { get; set; }
        public string? BusinessAddress { get; set; }
        public string? RNC { get; set; }
        public string? PreferredContactMethod { get; set; } = "email"; // email, phone, both
        public string? BusinessHours { get; set; } = "9:00 AM - 6:00 PM";
        public string? AutoReplyMessage { get; set; }
        public bool IsEmailVerified { get; set; } = false;

        /// <summary>
        /// True when the user's KYC has been approved by an Admin/Compliance officer.
        /// Set by KYCProfileApprovedEventConsumer when kyc.profile.status_changed event is received.
        /// </summary>
        public bool IsVerified { get; set; } = false;

        /// <summary>Timestamp when KYC was approved and IsVerified was set to true.</summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>User's preferred locale (e.g., "es-DO", "en-US"). Affects UI language.</summary>
        public string? PreferredLocale { get; set; } = "es-DO";

        /// <summary>User's preferred currency (e.g., "DOP", "USD").</summary>
        public string? PreferredCurrency { get; set; } = "DOP";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Account type system
        public AccountType AccountType { get; set; } = AccountType.Buyer;

        // User intent - what the user wants to do (buy, sell, both)
        public UserIntent UserIntent { get; set; } = UserIntent.Browse;

        // Platform-level (if admin or platform employee)
        public PlatformRole? PlatformRole { get; set; }
        public string? PlatformPermissions { get; set; } // JSON array of permissions

        // Dealer-level (if dealer or dealer employee)
        public Guid? DealerId { get; set; }
        public DealerRole? DealerRole { get; set; }
        public string? DealerPermissions { get; set; } // JSON array of permissions

        // Employee metadata
        public Guid? EmployerUserId { get; set; } // ID del que lo contrató
        public Guid? CreatedBy { get; set; }

        // Navegación a roles (legacy - mantener para compatibilidad)
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        // Propiedades calculadas
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
