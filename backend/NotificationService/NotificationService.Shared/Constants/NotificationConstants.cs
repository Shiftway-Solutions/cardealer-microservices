namespace NotificationService.Shared.Constants;

public static class NotificationConstants
{
    public const int MaxRetryCount = 3;
    public const int QueueBatchSize = 100;
    public const int DefaultPageSize = 20;
    public const int MaxEmailSubjectLength = 200;
    public const int MaxSmsMessageLength = 160;
    public const int MaxPushTitleLength = 100;
    public const int MaxPushBodyLength = 200;

    public static class Templates
    {
        public const string WelcomeEmail = "WelcomeEmail";
        public const string PasswordReset = "PasswordReset";
        public const string ContactNotification = "ContactNotification";
        public const string OrderConfirmation = "OrderConfirmation";
        public const string PaymentReceipt = "PaymentReceipt";
        public const string SecurityAlert = "SecurityAlert";
    }

    public static class Providers
    {
        public const string SendGrid = "SendGrid";
        public const string Twilio = "Twilio";
        public const string Firebase = "Firebase";
    }

    public static class QueueNames
    {
        public const string EmailQueue = "notification_email_queue";
        public const string SmsQueue = "notification_sms_queue";
        public const string PushQueue = "notification_push_queue";
        public const string RetryQueue = "notification_retry_queue";
    }

    public static class Validation
    {
        public const string EmailRequired = "Email address is required";
        public const string EmailInvalid = "Invalid email address format";
        public const string PhoneRequired = "Phone number is required";
        public const string SubjectRequired = "Subject is required";
        public const string MessageRequired = "Message content is required";
        public const string DeviceTokenRequired = "Device token is required";
    }
}