---
title: "70 - User Security & Privacy Pages"
priority: P0
estimated_time: ""
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 🔐 70 - User Security & Privacy Pages

**Objetivo:** Configuración de seguridad (2FA, sesiones, OAuth) y centro de privacidad con derechos ARCO (Ley 172-13 RD).

**Prioridad:** P0 (Crítica)  
**Complejidad:** 🔴 Alta  
**Dependencias:** SecuritySessionService, PrivacyService, KYCService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [SecuritySettingsPage](#-securitysettingspage)
3. [PrivacyCenterPage](#-privacycenterpage)
4. [Servicios](#-servicios)

---

## 🏗️ ARQUITECTURA

```
pages/user/
├── SecuritySettingsPage.tsx    # Configuración de seguridad (2084 líneas)
└── PrivacyCenterPage.tsx       # Centro de privacidad ARCO (458 líneas)

services/
├── securitySessionService.ts   # Gestión de sesiones activas
├── privacyService.ts           # Preferencias de privacidad
└── kycService.ts               # Estado de verificación KYC
```

---

## 📊 TIPOS

```typescript
// src/types/security.ts

// Two-Factor Authentication Types
export enum TwoFactorType {
  Authenticator = 1,
  SMS = 2,
  Email = 3,
}

export interface TwoFactorMethod {
  type: TwoFactorType;
  enabled: boolean;
  verifiedAt?: string;
  phoneNumber?: string; // For SMS
  email?: string; // For Email
}

export interface ActiveSession {
  id: string;
  deviceName: string;
  deviceType: "desktop" | "mobile" | "tablet";
  browser: string;
  operatingSystem: string;
  ipAddress: string;
  location: string;
  lastActive: string;
  isCurrent: boolean;
}

export interface LinkedAccount {
  provider: "google" | "facebook" | "apple";
  email: string;
  linkedAt: string;
  avatarUrl?: string;
}

// KYC Status
export enum KYCStatus {
  Pending = 1,
  InProgress = 2,
  DocsRequired = 3,
  UnderReview = 4,
  Approved = 5,
  Rejected = 6,
  Expired = 7,
  Suspended = 8,
}

export interface KYCProfile {
  id: string;
  userId: string;
  status: KYCStatus;
  submittedAt?: string;
  approvedAt?: string;
  rejectionReason?: string;
}
```

```typescript
// src/types/privacy.ts

// ARCO Rights (Dominican Law 172-13)
export type ARCORight =
  | "access"
  | "rectification"
  | "cancellation"
  | "opposition";

export interface ARCORightInfo {
  id: ARCORight;
  titleKey: string;
  descriptionKey: string;
  icon: string;
}

export interface PrivacyPreferences {
  marketingEmails: boolean;
  productUpdates: boolean;
  smsNotifications: boolean;
  shareDataWithPartners: boolean;
  personalizedAds: boolean;
}

export interface PrivacyRequest {
  id: string;
  type: ARCORight;
  status: "pending" | "processing" | "completed" | "rejected";
  createdAt: string;
  completedAt?: string;
  notes?: string;
}
```

---

## 🔒 SECURITYSETTINGSPAGE

**Ruta:** `/settings/security`

### Estructura Principal

```typescript
// src/pages/user/SecuritySettingsPage.tsx
import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import MainLayout from '@/layouts/MainLayout';
import {
  FiShield, FiSmartphone, FiMail, FiKey,
  FiMonitor, FiLogOut, FiAlertTriangle,
  FiCheck, FiX, FiChevronRight
} from 'react-icons/fi';
import { securitySessionService } from '@/services/securitySessionService';
import { kycService, KYCStatus } from '@/services/kycService';

// Sections
import TwoFactorSection from './sections/TwoFactorSection';
import ActiveSessionsSection from './sections/ActiveSessionsSection';
import LinkedAccountsSection from './sections/LinkedAccountsSection';
import PhoneVerificationSection from './sections/PhoneVerificationSection';

export default function SecuritySettingsPage() {
  const { t } = useTranslation('security');
  const [activeTab, setActiveTab] = useState<'2fa' | 'sessions' | 'accounts' | 'phone'>('2fa');

  // State for each section
  const [twoFactorMethods, setTwoFactorMethods] = useState<TwoFactorMethod[]>([]);
  const [sessions, setSessions] = useState<ActiveSession[]>([]);
  const [linkedAccounts, setLinkedAccounts] = useState<LinkedAccount[]>([]);
  const [kycProfile, setKycProfile] = useState<KYCProfile | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadSecurityData();
  }, []);

  const loadSecurityData = async () => {
    try {
      setLoading(true);
      const [methods, sessionList, accounts, kyc] = await Promise.all([
        securitySessionService.getTwoFactorMethods(),
        securitySessionService.getActiveSessions(),
        securitySessionService.getLinkedAccounts(),
        kycService.getCurrentUserProfile(),
      ]);

      setTwoFactorMethods(methods);
      setSessions(sessionList);
      setLinkedAccounts(accounts);
      setKycProfile(kyc);
    } catch (error) {
      console.error('Error loading security data:', error);
    } finally {
      setLoading(false);
    }
  };

  const tabs = [
    { id: '2fa', label: 'Autenticación 2FA', icon: <FiShield /> },
    { id: 'sessions', label: 'Sesiones activas', icon: <FiMonitor /> },
    { id: 'accounts', label: 'Cuentas vinculadas', icon: <FiKey /> },
    { id: 'phone', label: 'Verificar teléfono', icon: <FiSmartphone /> },
  ];

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="max-w-4xl mx-auto px-4">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900">
              {t('security.title', 'Seguridad de la cuenta')}
            </h1>
            <p className="text-gray-600 mt-2">
              {t('security.subtitle', 'Gestiona la seguridad y acceso a tu cuenta')}
            </p>
          </div>

          {/* KYC Status Banner */}
          {kycProfile && kycProfile.status !== KYCStatus.Approved && (
            <KYCStatusBanner status={kycProfile.status} />
          )}

          {/* Tabs */}
          <div className="bg-white rounded-xl shadow-sm">
            <div className="border-b">
              <nav className="flex overflow-x-auto">
                {tabs.map((tab) => (
                  <button
                    key={tab.id}
                    onClick={() => setActiveTab(tab.id as typeof activeTab)}
                    className={`flex items-center gap-2 px-6 py-4 text-sm font-medium whitespace-nowrap border-b-2 transition-colors ${
                      activeTab === tab.id
                        ? 'border-blue-500 text-blue-600'
                        : 'border-transparent text-gray-500 hover:text-gray-700'
                    }`}
                  >
                    {tab.icon}
                    {tab.label}
                  </button>
                ))}
              </nav>
            </div>

            {/* Tab Content */}
            <div className="p-6">
              {loading ? (
                <LoadingSkeleton />
              ) : (
                <>
                  {activeTab === '2fa' && (
                    <TwoFactorSection
                      methods={twoFactorMethods}
                      onUpdate={loadSecurityData}
                    />
                  )}
                  {activeTab === 'sessions' && (
                    <ActiveSessionsSection
                      sessions={sessions}
                      onRevoke={loadSecurityData}
                    />
                  )}
                  {activeTab === 'accounts' && (
                    <LinkedAccountsSection
                      accounts={linkedAccounts}
                      onUpdate={loadSecurityData}
                    />
                  )}
                  {activeTab === 'phone' && (
                    <PhoneVerificationSection onVerified={loadSecurityData} />
                  )}
                </>
              )}
            </div>
          </div>
        </div>
      </div>
    </MainLayout>
  );
}
```

### TwoFactorSection

```typescript
// src/pages/user/sections/TwoFactorSection.tsx
interface TwoFactorSectionProps {
  methods: TwoFactorMethod[];
  onUpdate: () => void;
}

export default function TwoFactorSection({ methods, onUpdate }: TwoFactorSectionProps) {
  const [showSetupModal, setShowSetupModal] = useState(false);
  const [selectedMethod, setSelectedMethod] = useState<TwoFactorType | null>(null);

  const methodCards = [
    {
      type: TwoFactorType.Authenticator,
      title: 'App Autenticador',
      description: 'Usa Google Authenticator o Authy',
      icon: <FiSmartphone size={24} />,
      recommended: true,
    },
    {
      type: TwoFactorType.SMS,
      title: 'SMS',
      description: 'Recibe códigos por mensaje de texto',
      icon: <FiMessageSquare size={24} />,
      recommended: false,
    },
    {
      type: TwoFactorType.Email,
      title: 'Correo electrónico',
      description: 'Recibe códigos en tu email',
      icon: <FiMail size={24} />,
      recommended: false,
    },
  ];

  const isMethodEnabled = (type: TwoFactorType) => {
    return methods.find((m) => m.type === type)?.enabled || false;
  };

  const handleToggle = async (type: TwoFactorType) => {
    if (isMethodEnabled(type)) {
      // Disable
      await securitySessionService.disableTwoFactor(type);
      onUpdate();
    } else {
      // Show setup modal
      setSelectedMethod(type);
      setShowSetupModal(true);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3 mb-6">
        <div className="p-2 bg-green-100 rounded-lg">
          <FiShield className="text-green-600" size={24} />
        </div>
        <div>
          <h3 className="text-lg font-semibold">Autenticación de dos factores</h3>
          <p className="text-sm text-gray-500">
            Añade una capa extra de seguridad a tu cuenta
          </p>
        </div>
      </div>

      <div className="space-y-4">
        {methodCards.map((card) => (
          <div
            key={card.type}
            className="border rounded-lg p-4 flex items-center justify-between hover:bg-gray-50"
          >
            <div className="flex items-center gap-4">
              <div className="p-3 bg-gray-100 rounded-lg">{card.icon}</div>
              <div>
                <div className="flex items-center gap-2">
                  <h4 className="font-medium">{card.title}</h4>
                  {card.recommended && (
                    <span className="text-xs bg-blue-100 text-blue-600 px-2 py-0.5 rounded">
                      Recomendado
                    </span>
                  )}
                </div>
                <p className="text-sm text-gray-500">{card.description}</p>
              </div>
            </div>

            <button
              onClick={() => handleToggle(card.type)}
              className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                isMethodEnabled(card.type)
                  ? 'bg-green-100 text-green-700 hover:bg-green-200'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              {isMethodEnabled(card.type) ? 'Activado' : 'Configurar'}
            </button>
          </div>
        ))}
      </div>

      {/* Setup Modal */}
      {showSetupModal && selectedMethod && (
        <TwoFactorSetupModal
          type={selectedMethod}
          onClose={() => setShowSetupModal(false)}
          onSuccess={() => {
            setShowSetupModal(false);
            onUpdate();
          }}
        />
      )}
    </div>
  );
}
```

### ActiveSessionsSection

```typescript
// src/pages/user/sections/ActiveSessionsSection.tsx
interface ActiveSessionsSectionProps {
  sessions: ActiveSession[];
  onRevoke: () => void;
}

export default function ActiveSessionsSection({ sessions, onRevoke }: ActiveSessionsSectionProps) {
  const [revoking, setRevoking] = useState<string | null>(null);

  const handleRevokeSession = async (sessionId: string) => {
    setRevoking(sessionId);
    try {
      await securitySessionService.revokeSession(sessionId);
      onRevoke();
    } catch (error) {
      console.error('Error revoking session:', error);
    } finally {
      setRevoking(null);
    }
  };

  const handleRevokeAll = async () => {
    if (!confirm('¿Cerrar todas las demás sesiones?')) return;
    try {
      await securitySessionService.revokeAllSessions();
      onRevoke();
    } catch (error) {
      console.error('Error revoking all sessions:', error);
    }
  };

  const getDeviceIcon = (type: ActiveSession['deviceType']) => {
    switch (type) {
      case 'mobile':
        return <FiSmartphone size={20} />;
      case 'tablet':
        return <FiTablet size={20} />;
      default:
        return <FiMonitor size={20} />;
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-blue-100 rounded-lg">
            <FiMonitor className="text-blue-600" size={24} />
          </div>
          <div>
            <h3 className="text-lg font-semibold">Sesiones activas</h3>
            <p className="text-sm text-gray-500">
              Dispositivos donde tu cuenta está iniciada
            </p>
          </div>
        </div>

        {sessions.length > 1 && (
          <button
            onClick={handleRevokeAll}
            className="text-red-600 hover:text-red-700 text-sm font-medium"
          >
            Cerrar todas las demás
          </button>
        )}
      </div>

      <div className="space-y-3">
        {sessions.map((session) => (
          <div
            key={session.id}
            className={`border rounded-lg p-4 ${
              session.isCurrent ? 'border-green-300 bg-green-50' : ''
            }`}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <div className="p-2 bg-gray-100 rounded-lg">
                  {getDeviceIcon(session.deviceType)}
                </div>
                <div>
                  <div className="flex items-center gap-2">
                    <h4 className="font-medium">{session.deviceName}</h4>
                    {session.isCurrent && (
                      <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded">
                        Sesión actual
                      </span>
                    )}
                  </div>
                  <p className="text-sm text-gray-500">
                    {session.browser} • {session.operatingSystem}
                  </p>
                  <p className="text-xs text-gray-400">
                    {session.location} • {session.ipAddress} • Última actividad:{' '}
                    {new Date(session.lastActive).toLocaleDateString()}
                  </p>
                </div>
              </div>

              {!session.isCurrent && (
                <button
                  onClick={() => handleRevokeSession(session.id)}
                  disabled={revoking === session.id}
                  className="p-2 text-red-600 hover:bg-red-50 rounded-lg"
                >
                  {revoking === session.id ? (
                    <FiLoader className="animate-spin" />
                  ) : (
                    <FiLogOut size={18} />
                  )}
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## 🛡️ PRIVACYCENTERPAGE

**Ruta:** `/privacy` o `/settings/privacy`

```typescript
// src/pages/user/PrivacyCenterPage.tsx
import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import MainLayout from '@/layouts/MainLayout';
import {
  FiShield, FiEye, FiEdit3, FiTrash2, FiSlash,
  FiMail, FiBell, FiUsers, FiDownload, FiClock
} from 'react-icons/fi';
import { privacyService } from '@/services/privacyService';
import type { PrivacyPreferences, PrivacyRequest, ARCORight } from '@/types/privacy';

// ARCO Rights (Dominican Law 172-13)
const ARCO_RIGHTS: ARCORightInfo[] = [
  {
    id: 'access',
    titleKey: 'privacy.arco.access.title',
    descriptionKey: 'privacy.arco.access.description',
    icon: 'FiEye',
  },
  {
    id: 'rectification',
    titleKey: 'privacy.arco.rectification.title',
    descriptionKey: 'privacy.arco.rectification.description',
    icon: 'FiEdit3',
  },
  {
    id: 'cancellation',
    titleKey: 'privacy.arco.cancellation.title',
    descriptionKey: 'privacy.arco.cancellation.description',
    icon: 'FiTrash2',
  },
  {
    id: 'opposition',
    titleKey: 'privacy.arco.opposition.title',
    descriptionKey: 'privacy.arco.opposition.description',
    icon: 'FiSlash',
  },
];

export default function PrivacyCenterPage() {
  const { t } = useTranslation('privacy');
  const [preferences, setPreferences] = useState<PrivacyPreferences | null>(null);
  const [requests, setRequests] = useState<PrivacyRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    loadPrivacyData();
  }, []);

  const loadPrivacyData = async () => {
    try {
      setLoading(true);
      const [prefs, reqs] = await Promise.all([
        privacyService.getPreferences(),
        privacyService.getRequestHistory(),
      ]);
      setPreferences(prefs);
      setRequests(reqs);
    } catch (error) {
      console.error('Error loading privacy data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handlePreferenceChange = async (key: keyof PrivacyPreferences, value: boolean) => {
    if (!preferences) return;

    setSaving(true);
    try {
      const updated = { ...preferences, [key]: value };
      await privacyService.updatePreferences(updated);
      setPreferences(updated);
    } catch (error) {
      console.error('Error updating preferences:', error);
    } finally {
      setSaving(false);
    }
  };

  const handleARCORequest = async (right: ARCORight) => {
    try {
      await privacyService.createRequest(right);
      loadPrivacyData(); // Refresh requests
    } catch (error) {
      console.error('Error creating ARCO request:', error);
    }
  };

  const getIconComponent = (iconName: string) => {
    const icons: Record<string, React.ReactNode> = {
      FiEye: <FiEye size={24} />,
      FiEdit3: <FiEdit3 size={24} />,
      FiTrash2: <FiTrash2 size={24} />,
      FiSlash: <FiSlash size={24} />,
    };
    return icons[iconName] || <FiShield size={24} />;
  };

  if (loading) {
    return <LoadingSkeleton />;
  }

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="max-w-4xl mx-auto px-4">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900">
              {t('privacy.title', 'Centro de Privacidad')}
            </h1>
            <p className="text-gray-600 mt-2">
              {t('privacy.subtitle', 'Gestiona tus datos y preferencias de privacidad')}
            </p>
          </div>

          {/* Legal Notice */}
          <div className="bg-blue-50 border border-blue-200 rounded-xl p-6 mb-8">
            <div className="flex items-start gap-4">
              <div className="p-2 bg-blue-100 rounded-lg">
                <FiShield className="text-blue-600" size={24} />
              </div>
              <div>
                <h3 className="font-semibold text-blue-900">
                  Ley 172-13 - Protección de Datos Personales
                </h3>
                <p className="text-sm text-blue-700 mt-1">
                  En cumplimiento con la Ley 172-13 de República Dominicana,
                  tienes derechos ARCO sobre tus datos personales.
                </p>
              </div>
            </div>
          </div>

          {/* ARCO Rights Section */}
          <div className="bg-white rounded-xl shadow-sm mb-8">
            <div className="p-6 border-b">
              <h2 className="text-lg font-semibold">Tus Derechos ARCO</h2>
              <p className="text-sm text-gray-500">
                Acceso, Rectificación, Cancelación y Oposición
              </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 p-6">
              {ARCO_RIGHTS.map((right) => (
                <div
                  key={right.id}
                  className="border rounded-lg p-4 hover:bg-gray-50 transition-colors"
                >
                  <div className="flex items-start gap-4">
                    <div className="p-2 bg-gray-100 rounded-lg">
                      {getIconComponent(right.icon)}
                    </div>
                    <div className="flex-1">
                      <h4 className="font-medium">
                        {t(right.titleKey, right.id.charAt(0).toUpperCase() + right.id.slice(1))}
                      </h4>
                      <p className="text-sm text-gray-500 mt-1">
                        {t(right.descriptionKey)}
                      </p>
                      <button
                        onClick={() => handleARCORequest(right.id)}
                        className="mt-3 text-sm text-blue-600 hover:text-blue-700 font-medium"
                      >
                        Solicitar →
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Communication Preferences */}
          <div className="bg-white rounded-xl shadow-sm mb-8">
            <div className="p-6 border-b">
              <h2 className="text-lg font-semibold">Preferencias de Comunicación</h2>
            </div>

            <div className="p-6 space-y-4">
              {preferences && (
                <>
                  <PreferenceToggle
                    icon={<FiMail />}
                    label="Emails de marketing"
                    description="Recibe ofertas y promociones"
                    checked={preferences.marketingEmails}
                    onChange={(v) => handlePreferenceChange('marketingEmails', v)}
                    disabled={saving}
                  />
                  <PreferenceToggle
                    icon={<FiBell />}
                    label="Actualizaciones del producto"
                    description="Nuevas funcionalidades y mejoras"
                    checked={preferences.productUpdates}
                    onChange={(v) => handlePreferenceChange('productUpdates', v)}
                    disabled={saving}
                  />
                  <PreferenceToggle
                    icon={<FiMessageSquare />}
                    label="Notificaciones SMS"
                    description="Alertas importantes por mensaje de texto"
                    checked={preferences.smsNotifications}
                    onChange={(v) => handlePreferenceChange('smsNotifications', v)}
                    disabled={saving}
                  />
                  <PreferenceToggle
                    icon={<FiUsers />}
                    label="Compartir con socios"
                    description="Permite compartir datos con socios comerciales"
                    checked={preferences.shareDataWithPartners}
                    onChange={(v) => handlePreferenceChange('shareDataWithPartners', v)}
                    disabled={saving}
                  />
                </>
              )}
            </div>
          </div>

          {/* Download Data */}
          <div className="bg-white rounded-xl shadow-sm mb-8">
            <div className="p-6 border-b">
              <h2 className="text-lg font-semibold">Descargar mis datos</h2>
            </div>
            <div className="p-6">
              <p className="text-gray-600 mb-4">
                Descarga una copia de todos tus datos personales en formato JSON.
              </p>
              <button
                onClick={() => privacyService.downloadData()}
                className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
              >
                <FiDownload size={18} />
                Descargar mis datos
              </button>
            </div>
          </div>

          {/* Request History */}
          {requests.length > 0 && (
            <div className="bg-white rounded-xl shadow-sm">
              <div className="p-6 border-b">
                <h2 className="text-lg font-semibold">Historial de Solicitudes</h2>
              </div>
              <div className="divide-y">
                {requests.map((request) => (
                  <div key={request.id} className="p-4 flex items-center justify-between">
                    <div className="flex items-center gap-4">
                      <div className="p-2 bg-gray-100 rounded-lg">
                        <FiClock size={18} />
                      </div>
                      <div>
                        <p className="font-medium capitalize">{request.type}</p>
                        <p className="text-sm text-gray-500">
                          {new Date(request.createdAt).toLocaleDateString()}
                        </p>
                      </div>
                    </div>
                    <StatusBadge status={request.status} />
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </MainLayout>
  );
}

// Helper Components
function PreferenceToggle({ icon, label, description, checked, onChange, disabled }) {
  return (
    <div className="flex items-center justify-between py-3 border-b last:border-0">
      <div className="flex items-center gap-4">
        <div className="p-2 bg-gray-100 rounded-lg text-gray-600">{icon}</div>
        <div>
          <p className="font-medium">{label}</p>
          <p className="text-sm text-gray-500">{description}</p>
        </div>
      </div>
      <label className="relative inline-flex items-center cursor-pointer">
        <input
          type="checkbox"
          checked={checked}
          onChange={(e) => onChange(e.target.checked)}
          disabled={disabled}
          className="sr-only peer"
        />
        <div className="w-11 h-6 bg-gray-200 peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
      </label>
    </div>
  );
}

function StatusBadge({ status }: { status: PrivacyRequest['status'] }) {
  const colors = {
    pending: 'bg-yellow-100 text-yellow-700',
    processing: 'bg-blue-100 text-blue-700',
    completed: 'bg-green-100 text-green-700',
    rejected: 'bg-red-100 text-red-700',
  };

  return (
    <span className={`px-3 py-1 rounded-full text-xs font-medium ${colors[status]}`}>
      {status}
    </span>
  );
}
```

---

## 🔧 SERVICIOS

### securitySessionService

```typescript
// src/services/securitySessionService.ts
import api from "./api";
import type {
  TwoFactorMethod,
  TwoFactorType,
  ActiveSession,
  LinkedAccount,
} from "@/types/security";

export const securitySessionService = {
  // Two-Factor Authentication
  getTwoFactorMethods: async (): Promise<TwoFactorMethod[]> => {
    const response = await api.get("/api/auth/2fa/methods");
    return response.data;
  },

  enableTwoFactor: async (
    type: TwoFactorType,
    verificationCode: string,
  ): Promise<void> => {
    await api.post("/api/auth/2fa/enable", { type, verificationCode });
  },

  disableTwoFactor: async (type: TwoFactorType): Promise<void> => {
    await api.post("/api/auth/2fa/disable", { type });
  },

  getAuthenticatorSetup: async (): Promise<{
    qrCode: string;
    secret: string;
  }> => {
    const response = await api.get("/api/auth/2fa/authenticator/setup");
    return response.data;
  },

  // Active Sessions
  getActiveSessions: async (): Promise<ActiveSession[]> => {
    const response = await api.get("/api/auth/sessions");
    return response.data;
  },

  revokeSession: async (sessionId: string): Promise<void> => {
    await api.delete(`/api/auth/sessions/${sessionId}`);
  },

  revokeAllSessions: async (): Promise<void> => {
    await api.post("/api/auth/sessions/revoke-all");
  },

  // Linked Accounts (OAuth)
  getLinkedAccounts: async (): Promise<LinkedAccount[]> => {
    const response = await api.get("/api/auth/linked-accounts");
    return response.data;
  },

  linkAccount: async (provider: string): Promise<string> => {
    const response = await api.post(`/api/auth/link/${provider}`);
    return response.data.redirectUrl;
  },

  unlinkAccount: async (provider: string): Promise<void> => {
    await api.delete(`/api/auth/linked-accounts/${provider}`);
  },

  // Phone Verification
  sendPhoneVerification: async (phoneNumber: string): Promise<void> => {
    await api.post("/api/auth/phone/send-code", { phoneNumber });
  },

  verifyPhone: async (phoneNumber: string, code: string): Promise<void> => {
    await api.post("/api/auth/phone/verify", { phoneNumber, code });
  },
};
```

### privacyService

```typescript
// src/services/privacyService.ts
import api from "./api";
import type {
  PrivacyPreferences,
  PrivacyRequest,
  ARCORight,
} from "@/types/privacy";

export const privacyService = {
  // Preferences
  getPreferences: async (): Promise<PrivacyPreferences> => {
    const response = await api.get("/api/users/privacy/preferences");
    return response.data;
  },

  updatePreferences: async (preferences: PrivacyPreferences): Promise<void> => {
    await api.put("/api/users/privacy/preferences", preferences);
  },

  // ARCO Requests (Ley 172-13)
  createRequest: async (type: ARCORight): Promise<PrivacyRequest> => {
    const response = await api.post("/api/users/privacy/requests", { type });
    return response.data;
  },

  getRequestHistory: async (): Promise<PrivacyRequest[]> => {
    const response = await api.get("/api/users/privacy/requests");
    return response.data;
  },

  // Data Download
  downloadData: async (): Promise<void> => {
    const response = await api.get("/api/users/data/download", {
      responseType: "blob",
    });

    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", "my-data.json");
    document.body.appendChild(link);
    link.click();
    link.remove();
  },

  // Delete Account
  requestAccountDeletion: async (reason?: string): Promise<void> => {
    await api.post("/api/users/account/delete-request", { reason });
  },
};
```

---

## ✅ VALIDACIÓN

### SecuritySettingsPage

- [ ] 4 tabs funcionan (2FA, Sessions, Accounts, Phone)
- [ ] KYC banner muestra si no verificado
- [ ] 2FA: 3 métodos (Authenticator, SMS, Email)
- [ ] 2FA: Toggle activa/desactiva
- [ ] Sessions: Lista de dispositivos activos
- [ ] Sessions: Botón cerrar sesión individual
- [ ] Sessions: Botón cerrar todas las demás
- [ ] OAuth: Lista de cuentas vinculadas
- [ ] OAuth: Botón desvincular
- [ ] Phone: Flujo de verificación con código

### PrivacyCenterPage

- [ ] Banner de Ley 172-13 visible
- [ ] 4 derechos ARCO clickeables
- [ ] Botón "Solicitar" crea request
- [ ] Toggles de preferencias funcionan
- [ ] Botón "Descargar datos" descarga JSON
- [ ] Historial de solicitudes visible
- [ ] Status badges con colores correctos

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/user-security-privacy.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("User Security & Privacy Settings", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test.describe("Security Settings", () => {
    test("debe mostrar configuración de seguridad", async ({ page }) => {
      await page.goto("/settings/security");

      await expect(
        page.getByRole("heading", { name: /seguridad/i }),
      ).toBeVisible();
      await expect(page.getByText(/contraseña/i)).toBeVisible();
      await expect(page.getByText(/2fa|autenticación/i)).toBeVisible();
    });

    test("debe cambiar contraseña", async ({ page }) => {
      await page.goto("/settings/security");

      await page.getByRole("button", { name: /cambiar contraseña/i }).click();
      await page.fill('input[name="currentPassword"]', "OldPass123!");
      await page.fill('input[name="newPassword"]', "NewSecure456!");
      await page.fill('input[name="confirmPassword"]', "NewSecure456!");
      await page.click('button[type="submit"]');

      await expect(page.getByText(/contraseña actualizada/i)).toBeVisible();
    });

    test("debe configurar 2FA", async ({ page }) => {
      await page.goto("/settings/security");

      await page.getByRole("button", { name: /habilitar 2fa/i }).click();
      await expect(page.getByTestId("qr-code")).toBeVisible();
    });

    test("debe mostrar sesiones activas", async ({ page }) => {
      await page.goto("/settings/security");

      await expect(page.getByTestId("active-sessions")).toBeVisible();
    });
  });

  test.describe("Privacy Settings", () => {
    test("debe mostrar centro de privacidad", async ({ page }) => {
      await page.goto("/settings/privacy");

      await expect(
        page.getByRole("heading", { name: /privacidad/i }),
      ).toBeVisible();
    });

    test("debe gestionar preferencias de comunicación", async ({ page }) => {
      await page.goto("/settings/privacy");

      await page.getByRole("switch", { name: /emails promocionales/i }).click();
      await expect(page.getByText(/preferencias guardadas/i)).toBeVisible();
    });

    test("debe ver historial de solicitudes de datos", async ({ page }) => {
      await page.goto("/settings/privacy");

      await expect(page.getByTestId("data-requests-history")).toBeVisible();
    });
  });
});
```

---

## 🔗 RUTAS

```typescript
// src/App.tsx
<Route path="/settings/security" element={<ProtectedRoute><SecuritySettingsPage /></ProtectedRoute>} />
<Route path="/settings/privacy" element={<ProtectedRoute><PrivacyCenterPage /></ProtectedRoute>} />
<Route path="/privacy" element={<Navigate to="/settings/privacy" replace />} />
```

---

_Última actualización: Enero 2026_
