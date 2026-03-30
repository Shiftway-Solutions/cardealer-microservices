/**
 * Configuration Service - API client for platform configuration management
 * Connects via API Gateway to ConfigurationService (port 15124)
 */

import { apiClient } from '@/lib/api-client';

// =============================================================================
// TYPES
// =============================================================================

export interface ConfigurationItem {
  id: string;
  key: string;
  value: string;
  environment: string;
  description?: string;
  tenantId?: string;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
  isActive: boolean;
  version: number;
}

export interface FeatureFlag {
  id: string;
  name: string;
  key: string;
  description?: string;
  isEnabled: boolean;
  environment?: string;
  tenantId?: string;
  rolloutPercentage: number;
  startsAt?: string;
  endsAt?: string;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
}

export interface ConfigurationHistory {
  id: string;
  configurationItemId: string;
  key: string;
  oldValue: string;
  newValue: string;
  environment: string;
  changedBy: string;
  changedAt: string;
  changeReason?: string;
}

export interface BulkUpsertRequest {
  environment: string;
  updatedBy: string;
  items: Array<{
    key: string;
    value: string;
    description?: string;
  }>;
  changeReason?: string;
}

export interface CreateFeatureFlagRequest {
  name: string;
  key: string;
  isEnabled: boolean;
  description?: string;
  environment?: string;
  rolloutPercentage?: number;
  createdBy?: string;
}

export interface UpdateFeatureFlagRequest {
  id: string;
  isEnabled: boolean;
  rolloutPercentage?: number;
}

// =============================================================================
// SECRETS TYPES — encrypted secrets stored separately from plain config
// =============================================================================

/** A secret as returned by the API — value is ALWAYS masked, never plaintext */
export interface SecretMaskedItem {
  id: string;
  key: string;
  maskedValue: string; // e.g. "re_B••••••••" — never the real value
  hasValue: boolean;
  environment: string;
  description?: string;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
  expiresAt?: string;
}

export interface UpsertSecretRequest {
  key: string;
  value: string;
  environment?: string;
  updatedBy?: string;
  description?: string;
}

export interface BulkUpsertSecretsRequest {
  environment?: string;
  updatedBy?: string;
  items: Array<{
    key: string;
    value: string;
    description?: string;
  }>;
}

/** Sentinel value the UI uses for unchanged secret fields — backend ignores these */
export const SECRET_MASK_PLACEHOLDER = '••••••••';

/** Check if a config key is defined as a secret field in the categories */
export function isSecretKey(key: string): boolean {
  for (const category of Object.values(CONFIG_CATEGORIES)) {
    for (const field of category.keys) {
      if (field.key === key && field.type === 'secret') return true;
    }
  }
  return false;
}

/** Get all keys that are marked as 'secret' type */
export function getAllSecretKeys(): string[] {
  const keys: string[] = [];
  for (const category of Object.values(CONFIG_CATEGORIES)) {
    for (const field of category.keys) {
      if (field.type === 'secret') keys.push(field.key);
    }
  }
  return keys;
}

// =============================================================================
// CATEGORIES - Define the configuration categories and their keys
// =============================================================================

export const CONFIG_CATEGORIES = {
  general: {
    label: 'General',
    icon: 'Globe',
    keys: [
      { key: 'general.site_name', label: 'Nombre del Sitio', type: 'text' as const },
      { key: 'general.site_url', label: 'URL del Sitio', type: 'text' as const },
      {
        key: 'general.site_description',
        label: 'Descripción del Sitio',
        type: 'textarea' as const,
      },
      { key: 'general.contact_email', label: 'Email de Contacto', type: 'text' as const },
      { key: 'general.support_email', label: 'Email de Soporte', type: 'text' as const },
      { key: 'general.noreply_email', label: 'Email No-Reply', type: 'text' as const },
      { key: 'general.legal_email', label: 'Email Legal', type: 'text' as const },
      { key: 'general.privacy_email', label: 'Email Privacidad', type: 'text' as const },
      { key: 'general.support_phone', label: 'Teléfono de Soporte', type: 'text' as const },
      { key: 'general.whatsapp_number', label: 'WhatsApp (sin +)', type: 'text' as const },
      { key: 'general.address', label: 'Dirección Física', type: 'textarea' as const },
      {
        key: 'general.business_hours',
        label: 'Horario de Atención',
        type: 'textarea' as const,
      },
      { key: 'general.social_facebook', label: 'Facebook URL', type: 'text' as const },
      { key: 'general.social_instagram', label: 'Instagram URL', type: 'text' as const },
      { key: 'general.social_twitter', label: 'Twitter/X URL', type: 'text' as const },
      { key: 'general.social_youtube', label: 'YouTube URL', type: 'text' as const },
    ],
  },
  pricing: {
    label: 'Precios y Comisiones',
    icon: 'DollarSign',
    keys: [
      // --- Publicaciones ---
      {
        key: 'pricing.basic_listing',
        label: 'Publicación Básica',
        type: 'currency' as const,
        hint: 'Gratis para usuarios',
      },
      {
        key: 'pricing.featured_listing',
        label: 'Publicación Destacada',
        type: 'currency' as const,
        hint: 'Por 7 días',
      },
      {
        key: 'pricing.premium_listing',
        label: 'Publicación Premium',
        type: 'currency' as const,
        hint: 'Por 30 días',
      },
      {
        key: 'pricing.seller_premium_price',
        label: 'Plan Vendedor Premium',
        type: 'currency' as const,
        hint: 'Mensual - hasta 5 publicaciones',
      },
      {
        key: 'pricing.individual_listing_price',
        label: 'Precio publicación individual',
        type: 'currency' as const,
        hint: 'Precio por publicación individual (vendedor no-dealer)',
      },
      // --- Planes Dealer (4 niveles) ---
      {
        key: 'pricing.dealer_libre',
        label: 'Plan LIBRE',
        type: 'currency' as const,
        hint: 'Gratis ($0)',
      },
      {
        key: 'pricing.dealer_visible',
        label: 'Plan VISIBLE',
        type: 'currency' as const,
        hint: 'Mensual',
      },
      {
        key: 'pricing.dealer_pro',
        label: 'Plan PRO',
        type: 'currency' as const,
        hint: 'Mensual',
      },
      {
        key: 'pricing.dealer_elite',
        label: 'Plan ÉLITE',
        type: 'currency' as const,
        hint: 'Mensual',
      },
      // --- Boosts ---
      {
        key: 'pricing.boost_basic_price',
        label: 'Boost Básico',
        type: 'currency' as const,
        hint: 'Precio por boost corto',
      },
      {
        key: 'pricing.boost_basic_days',
        label: 'Boost Básico (días)',
        type: 'number' as const,
        hint: 'Duración en días',
      },
      {
        key: 'pricing.boost_pro_price',
        label: 'Boost Pro',
        type: 'currency' as const,
        hint: 'Precio por boost medio',
      },
      {
        key: 'pricing.boost_pro_days',
        label: 'Boost Pro (días)',
        type: 'number' as const,
        hint: 'Duración en días',
      },
      {
        key: 'pricing.boost_premium_price',
        label: 'Boost Premium',
        type: 'currency' as const,
        hint: 'Precio por boost largo',
      },
      {
        key: 'pricing.boost_premium_days',
        label: 'Boost Premium (días)',
        type: 'number' as const,
        hint: 'Duración en días',
      },
      // --- Duraciones ---
      {
        key: 'pricing.basic_listing_days',
        label: 'Duración publicación gratuita',
        type: 'number' as const,
        hint: 'Días que dura una publicación gratis',
      },
      {
        key: 'pricing.individual_listing_days',
        label: 'Duración publicación individual',
        type: 'number' as const,
        hint: 'Días que dura una publicación pagada',
      },
      // --- Límites por plan (fotos) ---
      {
        key: 'pricing.free_max_photos',
        label: 'Fotos máx. (LIBRE)',
        type: 'number' as const,
        hint: 'Fotos por vehículo plan LIBRE',
      },
      {
        key: 'pricing.visible_max_photos',
        label: 'Fotos máx. VISIBLE',
        type: 'number' as const,
        hint: 'Fotos por vehículo plan VISIBLE',
      },
      {
        key: 'pricing.pro_max_photos',
        label: 'Fotos máx. PRO',
        type: 'number' as const,
        hint: 'Fotos por vehículo plan PRO',
      },
      {
        key: 'pricing.elite_max_photos',
        label: 'Fotos máx. ÉLITE',
        type: 'number' as const,
        hint: 'Fotos por vehículo plan ÉLITE',
      },
      // --- Comisiones e impuestos ---
      {
        key: 'pricing.platform_commission',
        label: 'Comisión plataforma (%)',
        type: 'number' as const,
        hint: 'Porcentaje de comisión sobre ventas',
      },
      { key: 'pricing.itbis_percentage', label: 'ITBIS (%)', type: 'number' as const },
      {
        key: 'pricing.currency',
        label: 'Moneda',
        type: 'select' as const,
        options: [
          { value: 'DOP', label: 'RD$ (Peso Dominicano)' },
          { value: 'USD', label: '$ (Dólar US)' },
        ],
      },
      // --- Early Bird ---
      {
        key: 'pricing.early_bird_discount',
        label: 'Descuento Early Bird (%)',
        type: 'number' as const,
        hint: 'Porcentaje de descuento para early adopters',
      },
      {
        key: 'pricing.early_bird_deadline',
        label: 'Fecha límite Early Bird',
        type: 'text' as const,
        hint: 'Formato: YYYY-MM-DD',
      },
      {
        key: 'pricing.early_bird_free_months',
        label: 'Meses gratis Early Bird',
        type: 'number' as const,
        hint: 'Meses de servicio gratis al registrarse',
      },
    ],
  },
  email: {
    label: 'Email (Resend)',
    icon: 'Mail',
    keys: [
      {
        key: 'email.enabled',
        label: 'Email Habilitado',
        type: 'toggle' as const,
      },
      {
        key: 'email.provider',
        label: 'Proveedor de Email',
        type: 'select' as const,
        options: [
          { value: 'resend', label: 'Resend (Recomendado)' },
          { value: 'sendgrid', label: 'SendGrid' },
          { value: 'smtp', label: 'SMTP Directo' },
        ],
      },
      // --- Resend (proveedor principal) ---
      {
        key: 'email.resend_api_key',
        label: 'Resend API Key',
        type: 'secret' as const,
        hint: 'Clave API de Resend (re_...)',
      },
      {
        key: 'email.resend_from_email',
        label: 'Resend - Email Remitente',
        type: 'text' as const,
        hint: 'Ej: noreply@okla.com.do',
      },
      {
        key: 'email.resend_from_name',
        label: 'Resend - Nombre Remitente',
        type: 'text' as const,
        hint: 'Ej: OKLA',
      },
      // --- SendGrid (alternativo) ---
      {
        key: 'email.sendgrid_api_key',
        label: 'SendGrid API Key',
        type: 'secret' as const,
        hint: 'Clave API de SendGrid (SG...)',
      },
      {
        key: 'email.sendgrid_from_email',
        label: 'SendGrid - Email Remitente',
        type: 'text' as const,
      },
      {
        key: 'email.sendgrid_from_name',
        label: 'SendGrid - Nombre Remitente',
        type: 'text' as const,
      },
      {
        key: 'email.sendgrid_enable_tracking',
        label: 'SendGrid - Tracking de apertura',
        type: 'toggle' as const,
      },
      // --- SMTP Directo (fallback) ---
      {
        key: 'email.smtp_server',
        label: 'Servidor SMTP',
        type: 'text' as const,
        hint: 'Ej: smtp.sendgrid.net',
      },
      {
        key: 'email.smtp_port',
        label: 'Puerto SMTP',
        type: 'number' as const,
        hint: '587 (TLS) o 465 (SSL)',
      },
      { key: 'email.smtp_user', label: 'Usuario SMTP', type: 'text' as const },
      {
        key: 'email.smtp_password',
        label: 'Contraseña SMTP',
        type: 'secret' as const,
        hint: 'Se almacena cifrada',
      },
      {
        key: 'email.smtp_use_tls',
        label: 'Usar TLS/SSL',
        type: 'toggle' as const,
      },
      // --- Configuración general de email ---
      { key: 'email.sender_email', label: 'Email del Remitente (general)', type: 'text' as const },
      { key: 'email.sender_name', label: 'Nombre del Remitente (general)', type: 'text' as const },
      {
        key: 'email.reply_to_email',
        label: 'Email de Respuesta (Reply-To)',
        type: 'text' as const,
        hint: 'Ej: soporte@okla.com.do',
      },
      {
        key: 'email.templates_path',
        label: 'Ruta de Templates',
        type: 'text' as const,
        hint: 'Ruta a los templates de email',
      },
      {
        key: 'email.daily_send_limit',
        label: 'Límite diario de envíos',
        type: 'number' as const,
        hint: 'Máximo emails por día (0 = sin límite)',
      },
    ],
  },
  sms: {
    label: 'SMS (Twilio)',
    icon: 'Smartphone',
    keys: [
      {
        key: 'sms.enabled',
        label: 'SMS Habilitado',
        type: 'toggle' as const,
      },
      {
        key: 'sms.provider',
        label: 'Proveedor SMS',
        type: 'select' as const,
        options: [
          { value: 'twilio', label: 'Twilio' },
          { value: 'mock', label: 'Mock (solo logs)' },
        ],
      },
      {
        key: 'sms.twilio_account_sid',
        label: 'Twilio Account SID',
        type: 'secret' as const,
        hint: 'Comienza con AC...',
      },
      {
        key: 'sms.twilio_auth_token',
        label: 'Twilio Auth Token',
        type: 'secret' as const,
        hint: 'Se almacena cifrado',
      },
      {
        key: 'sms.twilio_from_number',
        label: 'Número Remitente (Twilio)',
        type: 'text' as const,
        hint: 'Formato: +1XXXXXXXXXX',
      },
      {
        key: 'sms.twilio_messaging_service_sid',
        label: 'Messaging Service SID',
        type: 'secret' as const,
        hint: 'Opcional - para alto volumen (MG...)',
      },
      // --- Configuración de envío SMS ---
      {
        key: 'sms.daily_limit_per_user',
        label: 'Límite SMS/día por usuario',
        type: 'number' as const,
        hint: 'Máximo de SMS por usuario al día',
      },
      {
        key: 'sms.daily_global_limit',
        label: 'Límite global SMS/día',
        type: 'number' as const,
        hint: 'Máximo total de SMS al día',
      },
      {
        key: 'sms.default_country_code',
        label: 'Código de País (por defecto)',
        type: 'text' as const,
        hint: 'Ej: +1 para RD/US',
      },
      // --- Tipos de SMS habilitados ---
      {
        key: 'sms.enable_verification_codes',
        label: 'SMS de verificación (OTP)',
        type: 'toggle' as const,
      },
      {
        key: 'sms.enable_payment_alerts',
        label: 'Alertas de pago por SMS',
        type: 'toggle' as const,
      },
      {
        key: 'sms.enable_listing_alerts',
        label: 'Alertas de publicaciones por SMS',
        type: 'toggle' as const,
      },
      {
        key: 'sms.enable_marketing',
        label: 'SMS de Marketing',
        type: 'toggle' as const,
      },
    ],
  },
  push: {
    label: 'Push Notifications (Firebase)',
    icon: 'BellRing',
    keys: [
      {
        key: 'push.enabled',
        label: 'Push Notifications Habilitadas',
        type: 'toggle' as const,
      },
      {
        key: 'push.provider',
        label: 'Proveedor Push',
        type: 'select' as const,
        options: [
          { value: 'firebase', label: 'Firebase Cloud Messaging (FCM)' },
          { value: 'mock', label: 'Mock (solo logs)' },
        ],
      },
      {
        key: 'push.firebase_project_id',
        label: 'Firebase Project ID',
        type: 'text' as const,
        hint: 'ID del proyecto en Firebase Console',
      },
      {
        key: 'push.firebase_service_account_path',
        label: 'Ruta Service Account JSON',
        type: 'text' as const,
        hint: 'Ruta al archivo de credenciales de Firebase',
      },
      {
        key: 'push.firebase_server_key',
        label: 'Firebase Server Key (Legacy)',
        type: 'secret' as const,
        hint: 'Clave del servidor para FCM Legacy API',
      },
      // --- Configuración de Push ---
      {
        key: 'push.default_ttl_seconds',
        label: 'TTL por defecto (segundos)',
        type: 'number' as const,
        hint: 'Tiempo de vida del mensaje push',
      },
      {
        key: 'push.default_priority',
        label: 'Prioridad por defecto',
        type: 'select' as const,
        options: [
          { value: 'normal', label: 'Normal' },
          { value: 'high', label: 'Alta' },
        ],
      },
      // --- Tipos habilitados ---
      {
        key: 'push.enable_new_messages',
        label: 'Push para nuevos mensajes',
        type: 'toggle' as const,
      },
      {
        key: 'push.enable_price_alerts',
        label: 'Push para alertas de precio',
        type: 'toggle' as const,
      },
      {
        key: 'push.enable_listing_updates',
        label: 'Push para actualizaciones de listados',
        type: 'toggle' as const,
      },
      {
        key: 'push.enable_payment_status',
        label: 'Push para estado de pagos',
        type: 'toggle' as const,
      },
    ],
  },
  whatsapp: {
    label: 'WhatsApp Business',
    icon: 'MessageCircle',
    keys: [
      {
        key: 'whatsapp.enabled',
        label: 'WhatsApp Habilitado',
        type: 'toggle' as const,
      },
      {
        key: 'whatsapp.provider',
        label: 'Proveedor WhatsApp',
        type: 'select' as const,
        options: [
          { value: 'twilio', label: 'Twilio WhatsApp API' },
          { value: 'meta', label: 'Meta WhatsApp Business API' },
          { value: 'mock', label: 'Mock (solo logs)' },
        ],
      },
      {
        key: 'whatsapp.business_number',
        label: 'Número WhatsApp Business',
        type: 'text' as const,
        hint: 'Formato: +1XXXXXXXXXX',
      },
      {
        key: 'whatsapp.twilio_whatsapp_number',
        label: 'Twilio WhatsApp Number',
        type: 'text' as const,
        hint: 'Formato: whatsapp:+1XXXXXXXXXX',
      },
      {
        key: 'whatsapp.meta_phone_number_id',
        label: 'Meta Phone Number ID',
        type: 'text' as const,
        hint: 'ID del número en Meta Business',
      },
      {
        key: 'whatsapp.meta_access_token',
        label: 'Meta Access Token',
        type: 'secret' as const,
        hint: 'Token de acceso permanente de Meta',
      },
      {
        key: 'whatsapp.meta_business_account_id',
        label: 'Meta Business Account ID',
        type: 'text' as const,
      },
      // --- Templates y configuración ---
      {
        key: 'whatsapp.welcome_template',
        label: 'Template de Bienvenida',
        type: 'text' as const,
        hint: 'Nombre del template aprobado',
      },
      {
        key: 'whatsapp.verification_template',
        label: 'Template de Verificación',
        type: 'text' as const,
        hint: 'Nombre del template de OTP',
      },
      {
        key: 'whatsapp.payment_template',
        label: 'Template de Pagos',
        type: 'text' as const,
        hint: 'Nombre del template de confirmación de pago',
      },
      // --- Tipos habilitados ---
      {
        key: 'whatsapp.enable_welcome_message',
        label: 'Mensaje de bienvenida',
        type: 'toggle' as const,
      },
      {
        key: 'whatsapp.enable_verification_codes',
        label: 'Códigos de verificación',
        type: 'toggle' as const,
      },
      {
        key: 'whatsapp.enable_payment_confirmations',
        label: 'Confirmaciones de pago',
        type: 'toggle' as const,
      },
      {
        key: 'whatsapp.enable_listing_notifications',
        label: 'Notificaciones de publicaciones',
        type: 'toggle' as const,
      },
    ],
  },
  notifications: {
    label: 'Alertas Admin',
    icon: 'Bell',
    keys: [
      {
        key: 'notifications.new_user_registered',
        label: 'Nuevo usuario registrado',
        type: 'toggle' as const,
      },
      {
        key: 'notifications.new_listing_pending',
        label: 'Nueva publicación pendiente',
        type: 'toggle' as const,
      },
      {
        key: 'notifications.new_dealer_registered',
        label: 'Nuevo dealer registrado',
        type: 'toggle' as const,
      },
      { key: 'notifications.user_report', label: 'Reporte de usuario', type: 'toggle' as const },
      { key: 'notifications.payment_failed', label: 'Pago fallido', type: 'toggle' as const },
      { key: 'notifications.daily_summary', label: 'Resumen diario', type: 'toggle' as const },
      {
        key: 'notifications.kyc_pending_review',
        label: 'KYC pendiente de revisión',
        type: 'toggle' as const,
      },
      {
        key: 'notifications.system_errors',
        label: 'Errores del sistema (DLQ)',
        type: 'toggle' as const,
      },
      // --- Canales para alertas admin ---
      {
        key: 'notifications.admin_channel',
        label: 'Canal de alertas admin',
        type: 'select' as const,
        options: [
          { value: 'email', label: 'Solo Email' },
          { value: 'email+sms', label: 'Email + SMS' },
          { value: 'email+push', label: 'Email + Push' },
          { value: 'all', label: 'Todos los canales' },
        ],
      },
      {
        key: 'notifications.admin_email',
        label: 'Email para alertas admin',
        type: 'text' as const,
        hint: 'Email donde se envían las alertas del sistema',
      },
      {
        key: 'notifications.admin_phone',
        label: 'Teléfono para alertas admin',
        type: 'text' as const,
        hint: 'Número para SMS de alertas críticas',
      },
      // --- Teams / Slack ---
      {
        key: 'notifications.teams_webhook_url',
        label: 'Microsoft Teams Webhook URL',
        type: 'secret' as const,
        hint: 'URL del webhook para alertas en Teams',
      },
      {
        key: 'notifications.slack_webhook_url',
        label: 'Slack Webhook URL',
        type: 'secret' as const,
        hint: 'URL del webhook para alertas en Slack',
      },
    ],
  },
  security: {
    label: 'Seguridad',
    icon: 'Shield',
    keys: [
      {
        key: 'security.max_login_attempts',
        label: 'Intentos de login máximos',
        type: 'number' as const,
      },
      {
        key: 'security.lockout_duration_minutes',
        label: 'Tiempo de bloqueo (minutos)',
        type: 'number' as const,
      },
      {
        key: 'security.session_expiration_hours',
        label: 'Expiración de sesión (horas)',
        type: 'number' as const,
      },
      {
        key: 'security.min_password_length',
        label: 'Largo mínimo de contraseña',
        type: 'number' as const,
      },
      {
        key: 'security.jwt_expires_minutes',
        label: 'Expiración JWT (minutos)',
        type: 'number' as const,
      },
      {
        key: 'security.refresh_token_days',
        label: 'Vida del Refresh Token (días)',
        type: 'number' as const,
      },
      {
        key: 'security.require_email_verification',
        label: 'Requerir verificación de email',
        type: 'toggle' as const,
      },
      { key: 'security.allow_2fa', label: 'Permitir 2FA', type: 'toggle' as const },
      { key: 'security.force_https', label: 'Forzar HTTPS', type: 'toggle' as const },
      // Rate Limiting (administrado vía Gateway)
      {
        key: 'security.ratelimit_enabled',
        label: 'Rate limiting habilitado',
        type: 'toggle' as const,
      },
      {
        key: 'security.ratelimit_requests_per_minute',
        label: 'Requests por minuto (global)',
        type: 'number' as const,
        hint: 'Límite global de peticiones por minuto por IP/usuario',
      },
      {
        key: 'security.ratelimit_login_attempts_per_hour',
        label: 'Intentos de login por hora (HTTP)',
        type: 'number' as const,
        hint: 'Límite HTTP de intentos de login por hora (diferente al bloqueo de cuenta)',
      },
    ],
  },
  vehicles: {
    label: 'Vehículos',
    icon: 'Car',
    keys: [
      {
        key: 'vehicles.max_images_per_listing',
        label: 'Máx. imágenes por publicación',
        type: 'number' as const,
      },
      {
        key: 'vehicles.listing_expiration_days',
        label: 'Expiración de publicación (días)',
        type: 'number' as const,
      },
      {
        key: 'vehicles.featured_duration_days',
        label: 'Duración publicación destacada (días)',
        type: 'number' as const,
      },
      { key: 'vehicles.max_price_dop', label: 'Precio máximo (DOP)', type: 'number' as const },
      {
        key: 'vehicles.require_kyc_individual_seller',
        label: 'Requerir KYC para vendedor independiente',
        type: 'toggle' as const,
        hint: 'Si está desactivado, los vendedores individuales pueden publicar vehículos sin verificación KYC',
      },
      {
        key: 'vehicles.require_kyc_dealer',
        label: 'Requerir KYC para dealer',
        type: 'toggle' as const,
        hint: 'Si está desactivado, los dealers pueden publicar vehículos sin verificación KYC',
      },
      {
        key: 'vehicles.allow_sale_without_kyc',
        label: 'Permitir venta sin validación de vendedor',
        type: 'toggle' as const,
        hint: 'Permite que se concreten ventas en la plataforma sin que el vendedor (individual o dealer) haya pasado por el proceso de KYC',
      },
      {
        key: 'vehicles.pagination_default',
        label: 'Resultados por página',
        type: 'number' as const,
      },
    ],
  },
  kyc: {
    label: 'Verificación KYC',
    icon: 'UserCheck',
    keys: [
      {
        key: 'kyc.max_verification_attempts',
        label: 'Intentos máximos de verificación',
        type: 'number' as const,
      },
      {
        key: 'kyc.verification_timeout_minutes',
        label: 'Timeout de verificación (min)',
        type: 'number' as const,
      },
      {
        key: 'kyc.document_expiration_days',
        label: 'Validez de documentos (días)',
        type: 'number' as const,
      },
      {
        key: 'kyc.require_liveness_check',
        label: 'Requerir prueba de vida',
        type: 'toggle' as const,
      },
      {
        key: 'kyc.auto_approve_high_confidence',
        label: 'Auto-aprobar alta confianza',
        type: 'toggle' as const,
      },
      {
        key: 'kyc.high_confidence_threshold',
        label: 'Umbral auto-aprobación (0-100)',
        type: 'number' as const,
      },
      {
        key: 'kyc.face_match_threshold',
        label: 'Umbral coincidencia facial (0-100)',
        type: 'number' as const,
      },
      {
        key: 'kyc.bypass_for_individual_seller',
        label: 'Omitir KYC para vendedor independiente',
        type: 'toggle' as const,
        hint: 'Permite que vendedores individuales operen en la plataforma sin completar la verificación de identidad (KYC)',
      },
      {
        key: 'kyc.bypass_for_dealer',
        label: 'Omitir KYC para dealer',
        type: 'toggle' as const,
        hint: 'Permite que dealers operen en la plataforma sin completar la verificación de identidad (KYC)',
      },
    ],
  },
  media: {
    label: 'Media y Archivos',
    icon: 'Image',
    keys: [
      {
        key: 'media.storage_provider',
        label: 'Proveedor de almacenamiento',
        type: 'select' as const,
        options: [
          { value: 'local', label: 'Local' },
          { value: 'S3', label: 'Amazon S3' },
        ],
      },
      { key: 'media.max_upload_size_mb', label: 'Tamaño máximo (MB)', type: 'number' as const },
      { key: 'media.allowed_content_types', label: 'Tipos permitidos', type: 'text' as const },
      { key: 'media.cdn_base_url', label: 'URL base CDN', type: 'text' as const },
    ],
  },
  cache: {
    label: 'Caché',
    icon: 'Database',
    keys: [
      {
        key: 'cache.default_expiration_minutes',
        label: 'Expiración por defecto (min)',
        type: 'number' as const,
      },
      {
        key: 'cache.user_cache_minutes',
        label: 'Cache de usuarios (min)',
        type: 'number' as const,
      },
      {
        key: 'cache.enable_distributed_cache',
        label: 'Cache distribuido (Redis)',
        type: 'toggle' as const,
      },
    ],
  },

  billing: {
    label: 'Pasarelas de Pago',
    icon: 'CreditCard',
    keys: [
      // --- General ---
      {
        key: 'billing.stripe_trial_days',
        label: 'Días de prueba gratis (Dealers)',
        type: 'number' as const,
        hint: 'Período de prueba gratuito para planes de dealer',
      },
      // ─── Azul (Banco Popular RD) ───
      {
        key: 'billing.azul_enabled',
        label: 'Azul habilitado',
        type: 'toggle' as const,
        hint: 'Deshabilitar solo afecta a nuevos usuarios. Usuarios existentes seguirán siendo cobrados normalmente.',
      },
      {
        key: 'billing.azul_environment',
        label: 'Ambiente Azul',
        type: 'select' as const,
        options: [
          { value: 'Test', label: 'Test (Sandbox)' },
          { value: 'Prod', label: 'Producción' },
        ],
      },
      {
        key: 'billing.azul_merchant_name',
        label: 'Nombre comercio Azul',
        type: 'text' as const,
        hint: 'Nombre que aparece en el estado de cuenta del cliente',
      },
      {
        key: 'billing.azul_merchant_id',
        label: 'Azul Merchant ID',
        type: 'secret' as const,
        hint: 'ID del comercio asignado por Azul',
      },
      {
        key: 'billing.azul_api_key',
        label: 'Azul API Key',
        type: 'secret' as const,
        hint: 'Clave API proporcionada por Azul',
      },
      {
        key: 'billing.azul_merchant_type',
        label: 'Tipo de comercio Azul',
        type: 'text' as const,
        hint: 'Ej: eCommerce',
      },
      {
        key: 'billing.azul_currency_code',
        label: 'Moneda Azul',
        type: 'select' as const,
        options: [
          { value: '$', label: 'DOP (Pesos)' },
          { value: 'USD', label: 'USD (Dólares)' },
        ],
        hint: 'Moneda para transacciones Azul',
      },
      // ─── CardNET (Bancaria RD) ───
      {
        key: 'billing.cardnet_enabled',
        label: 'CardNET habilitado',
        type: 'toggle' as const,
        hint: 'Deshabilitar solo afecta a nuevos usuarios. Usuarios existentes seguirán siendo cobrados normalmente.',
      },
      {
        key: 'billing.cardnet_environment',
        label: 'Ambiente CardNET',
        type: 'select' as const,
        options: [
          { value: 'Test', label: 'Test (Sandbox)' },
          { value: 'Prod', label: 'Producción' },
        ],
      },
      {
        key: 'billing.cardnet_api_key',
        label: 'CardNET API Key',
        type: 'secret' as const,
        hint: 'Clave API de CardNET',
      },
      {
        key: 'billing.cardnet_terminal_id',
        label: 'CardNET Terminal ID',
        type: 'secret' as const,
        hint: 'ID del terminal asignado por CardNET',
      },
      {
        key: 'billing.cardnet_merchant_id',
        label: 'CardNET Merchant ID',
        type: 'secret' as const,
        hint: 'ID del comercio en CardNET',
      },
      // ─── PixelPay (Fintech - Comisiones más bajas) ───
      {
        key: 'billing.pixelpay_enabled',
        label: 'PixelPay habilitado',
        type: 'toggle' as const,
        hint: 'Deshabilitar solo afecta a nuevos usuarios. Usuarios existentes seguirán siendo cobrados normalmente.',
      },
      {
        key: 'billing.pixelpay_environment',
        label: 'Ambiente PixelPay',
        type: 'select' as const,
        options: [
          { value: 'Test', label: 'Test (Sandbox)' },
          { value: 'Prod', label: 'Producción' },
        ],
      },
      {
        key: 'billing.pixelpay_public_key',
        label: 'PixelPay Public Key',
        type: 'secret' as const,
        hint: 'Clave pública de PixelPay (pk_...)',
      },
      {
        key: 'billing.pixelpay_secret_key',
        label: 'PixelPay Secret Key',
        type: 'secret' as const,
        hint: 'Clave secreta de PixelPay (sk_...)',
      },
      {
        key: 'billing.pixelpay_webhook_secret',
        label: 'PixelPay Webhook Secret',
        type: 'secret' as const,
        hint: 'Secreto del webhook de PixelPay',
      },
      // ─── Fygaro (Agregador) ───
      {
        key: 'billing.fygaro_enabled',
        label: 'Fygaro habilitado',
        type: 'toggle' as const,
        hint: 'Deshabilitar solo afecta a nuevos usuarios. Usuarios existentes seguirán siendo cobrados normalmente.',
      },
      {
        key: 'billing.fygaro_environment',
        label: 'Ambiente Fygaro',
        type: 'select' as const,
        options: [
          { value: 'Test', label: 'Test (Sandbox)' },
          { value: 'Prod', label: 'Producción' },
        ],
      },
      {
        key: 'billing.fygaro_api_key',
        label: 'Fygaro API Key',
        type: 'secret' as const,
        hint: 'Clave API de Fygaro',
      },
      {
        key: 'billing.fygaro_merchant_id',
        label: 'Fygaro Merchant ID',
        type: 'secret' as const,
        hint: 'ID del comercio en Fygaro',
      },
      {
        key: 'billing.fygaro_webhook_secret',
        label: 'Fygaro Webhook Secret',
        type: 'secret' as const,
        hint: 'Secreto del webhook de Fygaro',
      },
      {
        key: 'billing.fygaro_enable_subscriptions',
        label: 'Fygaro suscripciones',
        type: 'toggle' as const,
        hint: 'Habilitar módulo de suscripciones de Fygaro',
      },
      // ─── Stripe (Internacional + Suscripciones) ───
      {
        key: 'billing.stripe_enabled',
        label: 'Stripe habilitado',
        type: 'toggle' as const,
        hint: 'Deshabilitar solo afecta a nuevos usuarios. Usuarios existentes seguirán siendo cobrados normalmente.',
      },
      {
        key: 'billing.stripe_environment',
        label: 'Ambiente Stripe',
        type: 'select' as const,
        options: [
          { value: 'Test', label: 'Test (Sandbox)' },
          { value: 'Prod', label: 'Producción (Live)' },
        ],
      },
      {
        key: 'billing.stripe_publishable_key',
        label: 'Stripe Publishable Key',
        type: 'secret' as const,
        hint: 'Clave pública de Stripe (pk_test_... o pk_live_...)',
      },
      {
        key: 'billing.stripe_secret_key',
        label: 'Stripe Secret Key',
        type: 'secret' as const,
        hint: 'Clave secreta de Stripe (sk_test_... o sk_live_...)',
      },
      {
        key: 'billing.stripe_webhook_secret',
        label: 'Stripe Webhook Secret',
        type: 'secret' as const,
        hint: 'Secreto del webhook (whsec_...)',
      },
      // ─── PayPal (Internacional) ───
      {
        key: 'billing.paypal_enabled',
        label: 'PayPal habilitado',
        type: 'toggle' as const,
        hint: 'Deshabilitar solo afecta a nuevos usuarios. Usuarios existentes seguirán siendo cobrados normalmente.',
      },
      {
        key: 'billing.paypal_environment',
        label: 'Ambiente PayPal',
        type: 'select' as const,
        options: [
          { value: 'sandbox', label: 'Sandbox' },
          { value: 'live', label: 'Producción (Live)' },
        ],
      },
      {
        key: 'billing.paypal_client_id',
        label: 'PayPal Client ID',
        type: 'secret' as const,
        hint: 'Client ID de la aplicación PayPal',
      },
      {
        key: 'billing.paypal_client_secret',
        label: 'PayPal Client Secret',
        type: 'secret' as const,
        hint: 'Client Secret de la aplicación PayPal',
      },
      {
        key: 'billing.paypal_webhook_id',
        label: 'PayPal Webhook ID',
        type: 'secret' as const,
        hint: 'ID del webhook configurado en PayPal',
      },
      {
        key: 'billing.paypal_webhook_secret',
        label: 'PayPal Webhook Secret',
        type: 'secret' as const,
        hint: 'Secreto del webhook de PayPal',
      },
      // ─── Configuración general de facturación ───
      {
        key: 'billing.invoice_prefix',
        label: 'Prefijo de factura',
        type: 'text' as const,
        hint: 'Ej: OKLA-INV',
      },
      {
        key: 'billing.invoice_ncf_enabled',
        label: 'NCF habilitado (DGII)',
        type: 'toggle' as const,
        hint: 'Habilitar generación de NCF para facturas fiscales RD',
      },
      {
        key: 'billing.auto_retry_failed_payments',
        label: 'Reintentar pagos fallidos',
        type: 'toggle' as const,
        hint: 'Reintentar automáticamente pagos que fallan',
      },
      {
        key: 'billing.max_payment_retries',
        label: 'Máximo reintentos de pago',
        type: 'number' as const,
        hint: 'Cantidad máxima de reintentos antes de cancelar',
      },
      {
        key: 'billing.payment_retry_interval_hours',
        label: 'Intervalo entre reintentos (horas)',
        type: 'number' as const,
        hint: 'Horas entre cada reintento de pago',
      },
    ],
  },
} as const;

export type ConfigCategory = keyof typeof CONFIG_CATEGORIES;

// =============================================================================
// API CLIENT
// =============================================================================

const ENV = 'Development';

export const configurationService = {
  // === Configurations ===

  /** Get all configurations for the current environment */
  getAll: async (environment: string = ENV): Promise<ConfigurationItem[]> => {
    const response = await apiClient.get<ConfigurationItem[]>('/api/admin/configurations', {
      params: { environment },
    });
    return response.data;
  },

  /** Get configurations by category prefix */
  getByCategory: async (
    category: string,
    environment: string = ENV
  ): Promise<ConfigurationItem[]> => {
    const response = await apiClient.get<ConfigurationItem[]>(
      `/api/configurations/category/${category}`,
      {
        params: { environment },
      }
    );
    return response.data;
  },

  /** Get a single configuration by key */
  getByKey: async (key: string, environment: string = ENV): Promise<ConfigurationItem | null> => {
    try {
      const response = await apiClient.get<ConfigurationItem>(`/api/admin/configurations/${key}`, {
        params: { environment },
      });
      return response.data;
    } catch {
      return null;
    }
  },

  /** Bulk upsert configurations */
  bulkUpsert: async (request: BulkUpsertRequest): Promise<ConfigurationItem[]> => {
    const response = await apiClient.post<ConfigurationItem[]>(
      '/api/admin/configurations/bulk',
      request
    );
    return response.data;
  },

  /** Create a single configuration */
  create: async (data: {
    key: string;
    value: string;
    environment: string;
    createdBy: string;
    description?: string;
  }): Promise<ConfigurationItem> => {
    const response = await apiClient.post<ConfigurationItem>('/api/admin/configurations', data);
    return response.data;
  },

  /** Update a configuration */
  update: async (
    id: string,
    data: {
      id: string;
      value: string;
      updatedBy: string;
      changeReason?: string;
    }
  ): Promise<ConfigurationItem> => {
    const response = await apiClient.put<ConfigurationItem>(
      `/api/admin/configurations/${id}`,
      data
    );
    return response.data;
  },

  /** Delete a configuration */
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/admin/configurations/${id}`);
  },

  /** Get configuration history */
  getHistory: async (configId: string): Promise<ConfigurationHistory[]> => {
    const response = await apiClient.get<ConfigurationHistory[]>(
      `/api/admin/configurations/${configId}/history`
    );
    return response.data;
  },

  // === Feature Flags ===

  /** Get all feature flags */
  getFeatureFlags: async (environment?: string): Promise<FeatureFlag[]> => {
    const response = await apiClient.get<FeatureFlag[]>('/api/featureflags', {
      params: environment ? { environment } : {},
    });
    return response.data;
  },

  /** Create a feature flag */
  createFeatureFlag: async (data: CreateFeatureFlagRequest): Promise<FeatureFlag> => {
    const response = await apiClient.post<FeatureFlag>('/api/featureflags', data);
    return response.data;
  },

  /** Update a feature flag */
  updateFeatureFlag: async (id: string, data: UpdateFeatureFlagRequest): Promise<FeatureFlag> => {
    const response = await apiClient.put<FeatureFlag>(`/api/featureflags/${id}`, data);
    return response.data;
  },

  /** Delete a feature flag */
  deleteFeatureFlag: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/featureflags/${id}`);
  },

  /** Check if a feature is enabled */
  isFeatureEnabled: async (key: string, environment?: string): Promise<boolean> => {
    try {
      const response = await apiClient.get<{ key: string; isEnabled: boolean }>(
        `/api/featureflags/${key}/enabled`,
        {
          params: environment ? { environment } : {},
        }
      );
      return response.data.isEnabled;
    } catch {
      return false;
    }
  },

  // === Secrets (encrypted at rest, masked in responses) ===

  /** Get all secrets — values are ALWAYS masked (e.g. "re_B••••••••") */
  getSecrets: async (environment: string = ENV): Promise<SecretMaskedItem[]> => {
    try {
      const response = await apiClient.get<SecretMaskedItem[]>('/api/secrets', {
        params: { environment },
      });
      return response.data;
    } catch {
      // If secrets endpoint is unavailable, return empty (graceful degradation)
      return [];
    }
  },

  /** Upsert a single secret — value is encrypted server-side before storage */
  upsertSecret: async (request: UpsertSecretRequest): Promise<SecretMaskedItem> => {
    const response = await apiClient.post<SecretMaskedItem>('/api/secrets', request);
    return response.data;
  },

  /** Bulk upsert secrets — only sends actually changed values */
  bulkUpsertSecrets: async (request: BulkUpsertSecretsRequest): Promise<SecretMaskedItem[]> => {
    const response = await apiClient.post<SecretMaskedItem[]>('/api/secrets/bulk', request);
    return response.data;
  },

  /** Delete a secret */
  deleteSecret: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/secrets/${id}`);
  },

  // === Payment Providers ===

  /** Get payment provider configuration (admin only) */
  getPaymentProviders: async (environment: string = ENV): Promise<PaymentProvidersResponse> => {
    const response = await apiClient.get<PaymentProvidersResponse>(
      '/api/public/pricing/providers',
      {
        params: { environment },
      }
    );
    return response.data;
  },
};

// =============================================================================
// PAYMENT PROVIDERS RESPONSE TYPE
// =============================================================================

export interface PaymentProvidersResponse {
  stripeTrialDays: number;
  // Azul (Banco Popular RD)
  azulEnabled: boolean;
  azulEnvironment: string;
  azulMerchantName: string;
  azulMerchantType: string;
  azulCurrencyCode: string;
  // CardNET (Bancaria RD)
  cardnetEnabled: boolean;
  cardnetEnvironment: string;
  // PixelPay (Fintech)
  pixelpayEnabled: boolean;
  pixelpayEnvironment: string;
  // Fygaro (Agregador)
  fygaroEnabled: boolean;
  fygaroEnvironment: string;
  fygaroEnableSubscriptions: boolean;
  // Stripe (Internacional + Suscripciones)
  stripeEnabled: boolean;
  stripeEnvironment: string;
  // PayPal (Internacional)
  paypalEnabled: boolean;
  paypalEnvironment: string;
  // General billing
  invoicePrefix: string;
  invoiceNcfEnabled: boolean;
  autoRetryFailedPayments: boolean;
  maxPaymentRetries: number;
  paymentRetryIntervalHours: number;
}
