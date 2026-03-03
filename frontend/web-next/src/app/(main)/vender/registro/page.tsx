/**
 * Seller Registration Wizard Page
 *
 * 3-step onboarding flow for new sellers:
 * 1. Create Account (skipped if already authenticated)
 * 2. Seller Profile Setup
 * 3. Publish First Vehicle
 *
 * Route: /vender/registro
 *
 * Features:
 * - Draft auto-save to localStorage
 * - Skip Step 1 if logged in
 * - Buyer → Seller conversion for existing accounts
 * - Inline validation with Zod schemas
 * - Mobile-first responsive design
 */

'use client';

import * as React from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { Loader2, AlertCircle, ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Alert, AlertDescription } from '@/components/ui/alert';

import { useAuth } from '@/hooks/use-auth';
import { useConvertToSeller, useCreateSellerProfile, useSellerByUserId } from '@/hooks/use-seller';
import { useKYCProfile } from '@/hooks/use-kyc';

import { StepIndicator } from '@/components/seller-wizard/step-indicator';
import { AccountStep } from '@/components/seller-wizard/account-step';
import { ProfileStep } from '@/components/seller-wizard/profile-step';

import type { RegisterRequest } from '@/services/auth';
import * as userService from '@/services/users';

// =============================================================================
// CONSTANTS
// =============================================================================

const DRAFT_STORAGE_KEY = 'okla-seller-wizard-draft';

const STEPS = [
  { id: 'account', label: 'Cuenta', description: 'Crea tu cuenta' },
  { id: 'profile', label: 'Perfil', description: 'Tu perfil de vendedor' },
] as const;

// =============================================================================
// TYPES
// =============================================================================

interface AccountData {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  password: string;
  confirmPassword: string;
  accountType: 'individual' | 'dealer';
  acceptTerms: boolean;
}

interface ProfileData {
  displayName: string;
  businessName: string;
  rnc: string;
  description: string;
  phone: string;
  location: string;
  specialties: string[];
}

interface WizardDraft {
  step: number;
  account: Partial<AccountData>;
  profile: Partial<ProfileData>;
  savedAt: string;
}

// =============================================================================
// INITIAL STATE
// =============================================================================

const initialAccount: AccountData = {
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  password: '',
  confirmPassword: '',
  accountType: 'individual',
  acceptTerms: false,
};

const initialProfile: ProfileData = {
  displayName: '',
  businessName: '',
  rnc: '',
  description: '',
  phone: '',
  location: '',
  specialties: [],
};

// =============================================================================
// DRAFT PERSISTENCE HELPERS
// =============================================================================

function loadDraft(): WizardDraft | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = localStorage.getItem(DRAFT_STORAGE_KEY);
    if (!raw) return null;
    const draft = JSON.parse(raw) as WizardDraft;
    // Expire drafts older than 7 days
    const savedAt = new Date(draft.savedAt);
    const now = new Date();
    const daysDiff = (now.getTime() - savedAt.getTime()) / (1000 * 60 * 60 * 24);
    if (daysDiff > 7) {
      localStorage.removeItem(DRAFT_STORAGE_KEY);
      return null;
    }
    return draft;
  } catch {
    localStorage.removeItem(DRAFT_STORAGE_KEY);
    return null;
  }
}

function saveDraft(step: number, account: AccountData, profile: ProfileData) {
  if (typeof window === 'undefined') return;
  try {
    // Never save passwords
    const safeAccount = { ...account, password: '', confirmPassword: '' };
    const draft: WizardDraft = {
      step,
      account: safeAccount,
      profile,
      savedAt: new Date().toISOString(),
    };
    localStorage.setItem(DRAFT_STORAGE_KEY, JSON.stringify(draft));
  } catch {
    // Ignore storage errors (quota exceeded, private browsing, etc.)
  }
}

function clearDraft() {
  if (typeof window === 'undefined') return;
  localStorage.removeItem(DRAFT_STORAGE_KEY);
}

// =============================================================================
// PAGE COMPONENT
// =============================================================================

export default function SellerRegistrationPage() {
  const router = useRouter();
  const { user, isAuthenticated, isLoading: authLoading, login, register } = useAuth();

  // ── Determine if user is logged in (needed by hooks below) ──
  const isLoggedIn = isAuthenticated && !!user;
  const effectiveStartStep = isLoggedIn ? 1 : 0;

  // ── Mutations ──
  const convertToSeller = useConvertToSeller();
  const createSellerProfile = useCreateSellerProfile();

  // ── Existing seller profile guard: redirect if user already has a profile ──
  const existingSellerQuery = useSellerByUserId(isLoggedIn ? user?.id : undefined);
  const { data: kycProfile } = useKYCProfile();

  // ── Redirect to /cuenta if seller profile already completed ──
  React.useEffect(() => {
    if (existingSellerQuery.data && !existingSellerQuery.isLoading) {
      router.replace('/cuenta');
    }
  }, [existingSellerQuery.data, existingSellerQuery.isLoading, router]);

  // ── Wizard State ──
  const [currentStep, setCurrentStep] = React.useState(0);
  const [accountData, setAccountData] = React.useState<AccountData>(initialAccount);
  const [profileData, setProfileData] = React.useState<ProfileData>(initialProfile);
  const [globalError, setGlobalError] = React.useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = React.useState(false);
  const [, setSellerProfileId] = React.useState<string | null>(null);
  const [draftLoaded, setDraftLoaded] = React.useState(false);

  // ── Load draft on mount ──
  React.useEffect(() => {
    const draft = loadDraft();
    if (draft) {
      if (draft.account) setAccountData(prev => ({ ...prev, ...draft.account }));
      if (draft.profile) setProfileData(prev => ({ ...prev, ...draft.profile }));

      // If user is logged in, ensure we start at at least step 1
      const startStep = isLoggedIn ? Math.max(draft.step, 1) : draft.step;
      setCurrentStep(startStep);

      toast.info('Borrador recuperado', {
        description: 'Continuamos donde lo dejaste.',
        action: {
          label: 'Empezar de nuevo',
          onClick: () => {
            clearDraft();
            setCurrentStep(effectiveStartStep);
            setAccountData(initialAccount);
            setProfileData(initialProfile);
          },
        },
      });
    } else if (isLoggedIn) {
      setCurrentStep(1);
    }
    setDraftLoaded(true);
  }, [isLoggedIn]); // eslint-disable-line react-hooks/exhaustive-deps

  // ── Auto-save draft on data changes ──
  React.useEffect(() => {
    if (!draftLoaded) return;
    saveDraft(currentStep, accountData, profileData);
  }, [currentStep, accountData, profileData, draftLoaded]);

  // ── Pre-fill profile if user data available (including KYC phone fallback) ──
  React.useEffect(() => {
    if (isLoggedIn && user && !profileData.displayName) {
      setProfileData(prev => ({
        ...prev,
        displayName: prev.displayName || `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim(),
        // Fallback chain: draft phone → user profile phone → KYC phone
        phone: prev.phone || user.phone || kycProfile?.phone || '',
      }));
    }
  }, [isLoggedIn, user, kycProfile]); // eslint-disable-line react-hooks/exhaustive-deps

  // ─────────────────────────────────────────────────────────────────────────────
  // STEP HANDLERS
  // ─────────────────────────────────────────────────────────────────────────────

  /**
   * Step 1: Register the user account
   */
  const handleAccountSubmit = async () => {
    setGlobalError(null);
    setIsSubmitting(true);
    try {
      const registerData: RegisterRequest = {
        firstName: accountData.firstName,
        lastName: accountData.lastName,
        email: accountData.email,
        phone: accountData.phone || undefined,
        password: accountData.password,
        acceptTerms: true,
      };
      await register(registerData);

      // After registration, automatically log in the user with the credentials they just provided
      // This allows them to continue to the seller profile step without verification
      try {
        // Use the login function from useAuth hook
        await login({
          email: accountData.email,
          password: accountData.password,
        });

        toast.success('¡Cuenta creada y sesión iniciada!', {
          description: 'Continuaremos con tu perfil de vendedor.',
        });
        // Advance to Step 2 — user is now authenticated
        setCurrentStep(1);
      } catch (loginErr) {
        // If auto-login fails, just notify user to verify email and log in manually
        console.warn('Auto-login failed after registration', loginErr);
        toast.success('¡Cuenta creada!', {
          description: 'Inicia sesión para continuar con tu perfil de vendedor.',
        });
        // Redirect to login page with redirect parameter
        router.push(
          `/login?email=${encodeURIComponent(accountData.email)}&redirect=${encodeURIComponent('/vender/registro')}`
        );
      }
    } catch (err) {
      const error = err as { message?: string };
      setGlobalError(error.message || 'Error al crear la cuenta. Intenta de nuevo.');
    } finally {
      setIsSubmitting(false);
    }
  };

  /**
   * Step 2: Create / convert seller profile
   */
  const handleProfileSubmit = async () => {
    setGlobalError(null);
    setIsSubmitting(true);
    // Use a local flag instead of reading `globalError` state (stale closure bug)
    let submissionFailed = false;
    try {
      // Validate based on account type (check both wizard state and actual user accountType)
      const { sellerProfileSchema, sellerProfileDealerSchema } =
        await import('@/lib/validations/seller-onboarding');
      const effectiveIsDealer =
        accountData.accountType === 'dealer' || user?.accountType === 'dealer';
      const schema = effectiveIsDealer ? sellerProfileDealerSchema : sellerProfileSchema;
      const validation = schema.safeParse(profileData);

      if (!validation.success) {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const issues = (validation as any).error?.issues ?? (validation as any).error?.errors ?? [];
        const errors = (issues as Array<{ message: string }>).map(e => e.message).join(', ');
        setGlobalError(`Validación: ${errors}`);
        return;
      }

      if (isLoggedIn && user) {
        // Existing user → convert to seller
        // isDealer: check both wizard state AND actual user accountType for logged-in users
        const isDealer = accountData.accountType === 'dealer' || user.accountType === 'dealer';

        try {
          // ConvertBuyerToSellerRequest ONLY accepts these fields — no phone, location etc.
          // The handler pulls FullName, Email, Phone, Address from the user's profile automatically.
          const conversionPayload = {
            // businessName is required by the frontend type but ignored by backend (uses user profile)
            businessName: isDealer
              ? profileData.businessName || profileData.displayName
              : profileData.displayName || user.firstName || '',
            bio: profileData.description || undefined,
            acceptTerms: true,
            acceptsOffers: true,
            showPhone: !!profileData.phone,
            showLocation: !!profileData.location,
            preferredContactMethod: 'whatsapp',
          };

          const result = await convertToSeller.mutateAsync({
            data: conversionPayload,
            idempotencyKey: `convert-seller-${user.id}-${Date.now()}`,
          });
          setSellerProfileId(result.sellerProfileId);
        } catch (err: unknown) {
          // Enhanced error handling for conversion failures
          const error = err as { message?: string; response?: { status: number; config?: { url?: string } }; status?: number; code?: string; requiresKyc?: boolean; redirectUrl?: string };

          // Check for 401 Unauthorized (auth token issue)
          if (error?.response?.status === 401 || error?.status === 401) {
            console.error('🔐 Auth token issue - status 401. User:', user.id);
            setGlobalError(
              'Tu sesión ha expirado. Por favor, vuelve a iniciar sesión y intenta de nuevo.'
            );
            // Optionally trigger logout
            return;
          }

          // Check for 404 (endpoint not found)
          if (error?.response?.status === 404 || error?.status === 404) {
            console.error('🚫 Endpoint not found - status 404. URL:', error?.config?.url);
            setGlobalError(
              'El servicio de conversión no está disponible temporalmente. Por favor, intenta en unos momentos.'
            );
            return;
          }

          // Generic error message
          const message =
            error?.message || error?.response?.data?.detail || 'Error al convertirse a vendedor.';
          console.error('❌ Profile submit error:', { error, userID: user.id });
          setGlobalError(message);
          submissionFailed = true;
        }
      } else {
        // New user just registered → create seller profile
        const profilePayload =
          accountData.accountType === 'dealer'
            ? {
                userId: user?.id || '',
                displayName: profileData.displayName,
                businessName: profileData.businessName || profileData.displayName,
                rnc: profileData.rnc,
                description: profileData.description || undefined,
                phone: profileData.phone || undefined,
                location: profileData.location || undefined,
                specialties:
                  profileData.specialties.length > 0 ? profileData.specialties : undefined,
              }
            : {
                userId: user?.id || '',
                displayName: profileData.displayName,
                businessName: profileData.displayName, // For individual sellers, use displayName
                description: profileData.description || undefined,
                phone: profileData.phone || undefined,
                location: profileData.location || undefined,
                specialties:
                  profileData.specialties.length > 0 ? profileData.specialties : undefined,
              };

        const profile = await createSellerProfile.mutateAsync(profilePayload);
        setSellerProfileId(profile.id);
      }

      // Save phone to UserService if provided (non-fatal — phone can be updated later)
      if (profileData.phone) {
        try {
          await userService.updateProfile({ phone: profileData.phone });
        } catch {
          // Non-fatal: phone will show empty in /cuenta/perfil but can be added manually
        }
      }

      // Redirect to portal — user must verify identity before publishing
      if (!submissionFailed) {
        clearDraft();
        toast.success('¡Perfil de vendedor creado!', {
          description: 'Verifica tu identidad para comenzar a publicar vehículos.',
        });
        router.push('/cuenta?registro=completado');
      }
    } catch (err) {
      const error = err as { message?: string; status?: number };
      console.error('❌ handleProfileSubmit caught error:', error);
      setGlobalError(error.message || 'Error al crear el perfil de vendedor.');
      submissionFailed = true;
    } finally {
      setIsSubmitting(false);
    }
  };

  // ─────────────────────────────────────────────────────────────────────────────
  // NAVIGATION
  // ─────────────────────────────────────────────────────────────────────────────

  const handleBack = () => {
    setGlobalError(null);
    if (currentStep > effectiveStartStep) {
      setCurrentStep(currentStep - 1);
    }
  };

  // ─────────────────────────────────────────────────────────────────────────────
  // RENDER
  // ─────────────────────────────────────────────────────────────────────────────

  // Loading state while auth initializes or checking for existing seller profile
  if (authLoading || !draftLoaded || (isLoggedIn && existingSellerQuery.isLoading)) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="text-center">
          <Loader2 className="mx-auto h-8 w-8 animate-spin text-[#00A870]" />
          <p className="mt-4 text-gray-500">Cargando...</p>
        </div>
      </div>
    );
  }

  // Don't render form while redirect is in progress (existing seller profile found)
  if (isLoggedIn && existingSellerQuery.data) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="text-center">
          <Loader2 className="mx-auto h-8 w-8 animate-spin text-[#00A870]" />
          <p className="mt-4 text-gray-500">Redirigiendo...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="border-b bg-white">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center gap-4">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => router.push('/vender')}
              className="text-gray-500 hover:text-gray-700"
            >
              <ArrowLeft className="mr-1 h-4 w-4" />
              Volver
            </Button>
            <div className="h-6 w-px bg-gray-200" />
            <h1 className="text-lg font-semibold text-gray-900">
              {isLoggedIn ? 'Comienza a vender' : 'Crea tu cuenta de vendedor'}
            </h1>
          </div>
        </div>
      </div>

      {/* Step Indicator */}
      <div className="bg-white pb-2">
        <div className="container mx-auto px-4">
          <StepIndicator
            steps={STEPS.map(s => ({ label: s.label, description: s.description }))}
            currentStep={currentStep}
          />
        </div>
      </div>

      {/* Global Error */}
      {globalError && (
        <div className="container mx-auto px-4 pt-4">
          <Alert variant="destructive" className="mx-auto max-w-3xl">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>{globalError}</AlertDescription>
          </Alert>
        </div>
      )}

      {/* Step Content */}
      <div className="container mx-auto px-4 py-6">
        <div className="mx-auto max-w-3xl">
          {/* Step 1: Account Registration */}
          {currentStep === 0 && !isLoggedIn && (
            <AccountStep
              data={accountData}
              onChange={partial => setAccountData(prev => ({ ...prev, ...partial }))}
              onSubmit={handleAccountSubmit}
              isLoading={isSubmitting}
              error={globalError}
            />
          )}

          {/* Step 2: Seller Profile */}
          {currentStep === 1 && (
            <ProfileStep
              data={profileData}
              onChange={partial => setProfileData(prev => ({ ...prev, ...partial }))}
              isDealer={accountData.accountType === 'dealer' || user?.accountType === 'dealer'}
              onSubmit={handleProfileSubmit}
              onBack={!isLoggedIn ? handleBack : () => {}}
              isLoading={isSubmitting}
              error={globalError}
            />
          )}
        </div>
      </div>
    </div>
  );
}
