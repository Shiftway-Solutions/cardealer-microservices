using System;
using System.Threading.Tasks;

namespace UserService.Application.Interfaces
{
    public interface INotificationServiceClient
    {
        Task SendWelcomeEmailAsync(string email, string firstName, string lastName);
        Task SendRoleAssignedNotificationAsync(string email, string roleName);
        Task SendPasswordResetEmailAsync(string email, string resetToken);

        // ========================================
        // DEALER ONBOARDING EMAILS
        // ========================================

        /// <summary>
        /// Envía email de verificación para dealer onboarding
        /// </summary>
        Task SendDealerVerificationEmailAsync(
            string email,
            string businessName,
            string verificationToken,
            DateTime tokenExpiry);

        /// <summary>
        /// Envía notificación a admins cuando un dealer sube documentos
        /// </summary>
        Task NotifyAdminsNewDealerApplicationAsync(
            string businessName,
            string rnc,
            string email,
            Guid dealerId);

        /// <summary>
        /// Envía email de aprobación al dealer
        /// </summary>
        Task SendDealerApprovalEmailAsync(
            string email,
            string businessName,
            string requestedPlan);

        /// <summary>
        /// Envía email de rechazo al dealer con razón
        /// </summary>
        Task SendDealerRejectionEmailAsync(
            string email,
            string businessName,
            string rejectionReason);

        /// <summary>
        /// Envía email de bienvenida cuando el dealer es activado
        /// </summary>
        Task SendDealerWelcomeEmailAsync(
            string email,
            string businessName,
            string plan,
            bool isEarlyBird);

        // ========================================
        // ARCO PRIVACY EMAILS (Ley 172-13)
        // ========================================

        /// <summary>
        /// Envía código de confirmación para eliminación de cuenta
        /// </summary>
        Task SendAccountDeletionConfirmationCodeAsync(
            string email,
            string firstName,
            string confirmationCode,
            DateTime gracePeriodEndsAt);

        /// <summary>
        /// Notifica que la cuenta será eliminada (recordatorio)
        /// </summary>
        Task SendAccountDeletionReminderAsync(
            string email,
            string firstName,
            DateTime deletionDate,
            int daysRemaining);

        /// <summary>
        /// Confirma que la cuenta ha sido eliminada
        /// </summary>
        Task SendAccountDeletedConfirmationAsync(
            string email,
            string firstName);
    }
}
