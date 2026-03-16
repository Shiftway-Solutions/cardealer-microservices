'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';

import {
  User,
  MapPin,
  Camera,
  Check,
  ChevronRight,
  ChevronLeft,
  Shield,
  AlertCircle,
  Clock,
  CheckCircle,
  XCircle,
  Loader2,
} from 'lucide-react';
import { toast } from 'sonner';
import { DocumentCapture, LivenessChallenge, type DocumentSide } from '@/components/kyc';
import {
  kycService,
  KYCStatus,
  DocumentType,
  getStatusLabel,
  isVerifiedForSelling,
  type KYCProfile,
  type CreateKYCProfileRequest,
  type LivenessData,
} from '@/services/kyc';
import { useAuth } from '@/hooks/use-auth';
import { useKYCDraft, type KYCWizardFormData } from '@/hooks/useKYCDraft';
import { sanitizeText, sanitizePhone } from '@/lib/security/sanitize';
import { getCsrfToken } from '@/lib/security/csrf';

// Local type for captured documents
interface CapturedDocuments {
  frontImage?: File;
  backImage?: File;
}

// ============================================================================
// Types
// ============================================================================

interface PersonalInfoForm {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  nationality: string;
  documentNumber: string; // Cédula
  gender: string;
  phoneNumber: string;
  occupation: string;
}

interface AddressForm {
  street: string;
  sector: string;
  city: string;
  province: string;
  postalCode: string;
  country: string;
}

type Step = 'info' | 'address' | 'documents' | 'review';

// ============================================================================
// Constants
// ============================================================================

const STEPS: { id: Step; label: string; icon: React.ReactNode }[] = [
  { id: 'info', label: 'Información Personal', icon: <User className="h-5 w-5" /> },
  { id: 'address', label: 'Dirección', icon: <MapPin className="h-5 w-5" /> },
  { id: 'documents', label: 'Documentos y Selfie', icon: <Camera className="h-5 w-5" /> },
  { id: 'review', label: 'Revisión', icon: <Check className="h-5 w-5" /> },
];

const PROVINCES_RD = [
  'Azua',
  'Bahoruco',
  'Barahona',
  'Dajabón',
  'Distrito Nacional',
  'Duarte',
  'El Seibo',
  'Elías Piña',
  'Espaillat',
  'Hato Mayor',
  'Hermanas Mirabal',
  'Independencia',
  'La Altagracia',
  'La Romana',
  'La Vega',
  'María Trinidad Sánchez',
  'Monseñor Nouel',
  'Monte Cristi',
  'Monte Plata',
  'Pedernales',
  'Peravia',
  'Puerto Plata',
  'Samaná',
  'San Cristóbal',
  'San José de Ocoa',
  'San Juan',
  'San Pedro de Macorís',
  'Sánchez Ramírez',
  'Santiago',
  'Santiago Rodríguez',
  'Santo Domingo',
  'Valverde',
];

// ============================================================================
// Main Component
// ============================================================================

export default function VerificacionPage() {
  const router = useRouter();
  const { user, isAuthenticated, isLoading: authLoading } = useAuth();

  // SECURITY: Initialize CSRF token on component load
  useEffect(() => {
    getCsrfToken();
  }, []);

  // State
  const [currentStep, setCurrentStep] = useState<Step>('info');
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [existingProfile, setExistingProfile] = useState<KYCProfile | null>(null);

  // Form State - Empty initial values (no test data in production)
  const [personalInfo, setPersonalInfo] = useState<PersonalInfoForm>({
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    nationality: 'Dominicana',
    documentNumber: '',
    gender: '',
    phoneNumber: '',
    occupation: '',
  });

  const [address, setAddress] = useState<AddressForm>({
    street: '',
    sector: '',
    city: '',
    province: '',
    postalCode: '',
    country: 'República Dominicana',
  });

  // Document State
  const [capturedDocuments, setCapturedDocuments] = useState<CapturedDocuments>({});
  const [selfieBlob, setSelfieBlob] = useState<Blob | null>(null);
  const [livenessData, setLivenessData] = useState<LivenessData | null>(null);
  const [documentStep, setDocumentStep] = useState<'front' | 'back' | 'liveness' | 'complete'>(
    'front'
  );

  // ============================================================================
  // KYC Draft Autosave Hook
  // ============================================================================

  const handleDraftLoaded = useCallback((data: KYCWizardFormData, step: string) => {
    const hasPersonal =
      !!data.personalInfo &&
      Object.values(data.personalInfo).some(v => v !== undefined && String(v).trim() !== '');
    const hasAddress =
      !!data.address &&
      Object.values(data.address).some(v => v !== undefined && String(v).trim() !== '');

    // Only populate fields and notify the user when the draft actually contains meaningful data
    if (hasPersonal) {
      setPersonalInfo(prev => ({ ...prev, ...data.personalInfo }));
    }
    if (hasAddress) {
      setAddress(prev => ({ ...prev, ...data.address }));
    }

    if (hasPersonal || hasAddress) {
      setCurrentStep(step as Step);
      toast.info('Se restauró tu progreso anterior');
    }
  }, []);

  const { clearDraft } = useKYCDraft({
    userId: user?.id,
    currentStep,
    formData: { personalInfo, address },
    onDraftLoaded: handleDraftLoaded,
    enabled: !isSubmitting && !existingProfile,
  });

  // ============================================================================
  // Effects
  // ============================================================================

  // Check for existing KYC profile
  const checkExistingProfile = useCallback(async () => {
    if (!user?.id) return;

    setIsLoading(true);
    try {
      const profile = await kycService.getProfileByUserId(user.id);

      if (profile) {
        setExistingProfile(profile);

        // If already verified, redirect
        if (isVerifiedForSelling(profile)) {
          toast.success('¡Ya estás verificado!');
          router.push('/cuenta');
          return;
        }

        // If under review, show waiting state
        if (profile.status === KYCStatus.UnderReview) {
          // Stay on page but show waiting state
        }
      }
      // profile is null = user hasn't started KYC yet (this is normal)
    } catch (error) {
      // Unexpected error (network, server error, etc.)
      console.error('Error checking KYC profile:', error);
    } finally {
      setIsLoading(false);
    }
  }, [user?.id, router]);

  // Pre-fill form with user data from auth context
  useEffect(() => {
    if (user && !authLoading) {
      setPersonalInfo(prev => ({
        ...prev,
        firstName: user.firstName || prev.firstName,
        lastName: user.lastName || prev.lastName,
        phoneNumber: user.phone || prev.phoneNumber,
      }));
    }
  }, [user, authLoading]);

  // Pre-fill form with existing KYC profile data (e.g. rejected profile for retry)
  useEffect(() => {
    if (existingProfile && existingProfile.status === KYCStatus.Rejected) {
      setPersonalInfo(prev => ({
        ...prev,
        firstName: existingProfile.firstName || prev.firstName,
        lastName: existingProfile.lastName || prev.lastName,
        dateOfBirth: existingProfile.dateOfBirth || prev.dateOfBirth,
        nationality: existingProfile.nationality || prev.nationality,
        documentNumber: existingProfile.primaryDocumentNumber || prev.documentNumber,
        gender: existingProfile.gender || prev.gender,
        phoneNumber: existingProfile.phone || existingProfile.phoneNumber || prev.phoneNumber,
        occupation: existingProfile.occupation || prev.occupation,
      }));
      setAddress(prev => ({
        ...prev,
        street: existingProfile.address || prev.street,
        sector: existingProfile.sector || prev.sector,
        city: existingProfile.city || prev.city,
        province: existingProfile.province || prev.province,
        postalCode: existingProfile.postalCode || prev.postalCode,
        country: existingProfile.country || prev.country,
      }));
    }
  }, [existingProfile]);

  // Redirect if not authenticated
  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      toast.error('Debes iniciar sesión para verificar tu cuenta');
      router.push('/login?redirect=/cuenta/verificacion');
    }
  }, [authLoading, isAuthenticated, router]);

  // Load existing profile when user is available
  useEffect(() => {
    if (user && !authLoading) {
      checkExistingProfile();
    }
  }, [user, authLoading, checkExistingProfile]);

  // ============================================================================
  // Handlers
  // ============================================================================

  const handlePersonalInfoChange = (field: keyof PersonalInfoForm, value: string) => {
    setPersonalInfo(prev => ({ ...prev, [field]: value }));
  };

  const handleAddressChange = (field: keyof AddressForm, value: string) => {
    setAddress(prev => ({ ...prev, [field]: value }));
  };

  const handleDocumentCapture = async (image: File, side: DocumentSide) => {
    if (side === 'Front') {
      setCapturedDocuments(prev => ({ ...prev, frontImage: image }));
      setDocumentStep('back');
    } else {
      setCapturedDocuments(prev => ({ ...prev, backImage: image }));
      setDocumentStep('liveness');
    }
  };

  const handleLivenessComplete = async (selfie: Blob, data: LivenessData) => {
    setSelfieBlob(selfie);
    setLivenessData(data);
    setDocumentStep('complete');
  };

  const validatePersonalInfo = (): boolean => {
    const { firstName, lastName, dateOfBirth, documentNumber, phoneNumber } = personalInfo;

    if (!firstName || !lastName || !dateOfBirth || !documentNumber || !phoneNumber) {
      toast.error('Por favor completa todos los campos requeridos');
      return false;
    }

    // Validate cédula format (11 digits for RD)
    const cedulaRegex = /^\d{3}-?\d{7}-?\d{1}$/;
    if (!cedulaRegex.test(documentNumber.replace(/-/g, '').padStart(11, '0'))) {
      toast.error('El número de cédula no es válido');
      return false;
    }

    // Validate age (must be 18+)
    const birthDate = new Date(dateOfBirth);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    if (age < 18) {
      toast.error('Debes ser mayor de 18 años');
      return false;
    }

    return true;
  };

  const validateAddress = (): boolean => {
    const { street, city, province } = address;

    if (!street || !city || !province) {
      toast.error('Por favor completa todos los campos de dirección requeridos');
      return false;
    }

    return true;
  };

  const validateDocuments = (): boolean => {
    if (!capturedDocuments.frontImage || !capturedDocuments.backImage) {
      toast.error('Por favor captura ambos lados de tu cédula');
      return false;
    }

    if (!selfieBlob || !livenessData) {
      toast.error('Por favor completa la verificación de identidad');
      return false;
    }

    return true;
  };

  const goToNextStep = () => {
    const stepIndex = STEPS.findIndex(s => s.id === currentStep);

    // Validate current step
    if (currentStep === 'info' && !validatePersonalInfo()) return;
    if (currentStep === 'address' && !validateAddress()) return;
    if (currentStep === 'documents' && !validateDocuments()) return;

    if (stepIndex < STEPS.length - 1) {
      setCurrentStep(STEPS[stepIndex + 1].id);
    }
  };

  const goToPrevStep = () => {
    const stepIndex = STEPS.findIndex(s => s.id === currentStep);
    if (stepIndex > 0) {
      setCurrentStep(STEPS[stepIndex - 1].id);
    }
  };

  const handleSubmit = async () => {
    // =========================================================================
    // SECURITY: Prevent multiple submissions (race condition protection)
    // =========================================================================
    if (isSubmitting) {
      console.warn('Submission already in progress, ignoring duplicate request');
      return;
    }

    // SECURITY: Block if user already has a profile (prevents duplicate profiles)
    if (
      existingProfile &&
      existingProfile.status !== KYCStatus.Rejected &&
      existingProfile.status !== KYCStatus.Expired
    ) {
      toast.error('Ya tienes una verificación en proceso o completada');
      router.push('/cuenta');
      return;
    }

    if (!validateDocuments()) return;
    if (!user?.id) {
      toast.error('Error: No se pudo identificar tu cuenta. Por favor, inicia sesión de nuevo.');
      return;
    }

    setIsSubmitting(true);

    // Track created resources for potential cleanup on error
    let createdProfileId: string | null = null;

    try {
      // 1. Create KYC Profile — SECURITY: Sanitize all user inputs
      const profileRequest: CreateKYCProfileRequest = {
        userId: user.id,
        email: user.email, // Save email so admins can see it in the KYC review panel
        firstName: sanitizeText(personalInfo.firstName.trim(), { maxLength: 50 }),
        lastName: sanitizeText(personalInfo.lastName.trim(), { maxLength: 50 }),
        dateOfBirth: personalInfo.dateOfBirth,
        nationality: sanitizeText(personalInfo.nationality.trim(), { maxLength: 50 }),
        documentNumber: personalInfo.documentNumber.replace(/[^0-9-]/g, ''),
        documentType: DocumentType.Cedula,
        address: sanitizeText(address.street.trim(), {
          maxLength: 300,
        }),
        city: sanitizeText(address.city.trim(), { maxLength: 100 }),
        province: sanitizeText(address.province.trim(), { maxLength: 100 }),
        gender: personalInfo.gender || undefined,
        sector: address.sector
          ? sanitizeText(address.sector.trim(), { maxLength: 100 })
          : undefined,
        postalCode: address.postalCode
          ? sanitizeText(address.postalCode.trim(), { maxLength: 20 })
          : undefined,
        phoneNumber: sanitizePhone(personalInfo.phoneNumber) || personalInfo.phoneNumber,
        sourceOfFunds: 'Ingresos laborales',
        occupation: sanitizeText(personalInfo.occupation.trim(), { maxLength: 100 }),
        expectedMonthlyTransaction: 10000,
      };

      const profile = await kycService.createProfile(profileRequest);
      createdProfileId = profile.id; // Track for potential cleanup

      // 2. Upload cédula front
      if (capturedDocuments.frontImage) {
        await kycService.uploadDocument({
          profileId: profile.id,
          documentType: DocumentType.Cedula,
          file: capturedDocuments.frontImage,
          side: 'Front',
        });
      }

      // 3. Upload cédula back
      if (capturedDocuments.backImage) {
        await kycService.uploadDocument({
          profileId: profile.id,
          documentType: DocumentType.Cedula,
          file: capturedDocuments.backImage,
          side: 'Back',
        });
      }

      // 4. Upload selfie and process identity verification
      if (selfieBlob) {
        // Convert blob to file for upload
        const selfieFile = new File([selfieBlob], 'selfie_verificacion.jpg', {
          type: 'image/jpeg',
        });

        await kycService.uploadDocument({
          profileId: profile.id,
          documentType: DocumentType.Selfie,
          file: selfieFile,
        });

        // 5. Process identity verification (face comparison)
        await kycService.processIdentityVerification({
          profileId: profile.id,
          selfieFile: selfieFile,
          livenessData: livenessData || undefined,
        });
      }

      // 6. Submit for review
      await kycService.submitForReview(profile.id);

      // 7. Clear draft after successful submission
      await clearDraft();

      toast.success('¡Verificación enviada! Te notificaremos cuando sea aprobada.');
      router.push('/cuenta?verification=submitted');
    } catch (error: unknown) {
      console.error('Error submitting KYC:', error);

      // Provide specific error messages based on error type
      let errorMessage = 'Error al enviar la verificación';
      const err = error as { message?: string; name?: string; status?: number };

      if (
        err.message?.includes('already has a KYC profile') ||
        err.message?.includes('duplicate')
      ) {
        errorMessage = 'Ya tienes una verificación en proceso. Revisa tu perfil.';
        // Refresh profile state
        await checkExistingProfile();
      } else if (
        err.message?.includes('Document number') &&
        err.message?.includes('already registered')
      ) {
        errorMessage = 'Este número de documento ya está registrado con otra cuenta.';
      } else if (err.message?.includes('network') || err.name === 'TypeError') {
        errorMessage = 'Error de conexión. Por favor verifica tu internet e intenta de nuevo.';
      } else if (err.status === 401 || err.message?.includes('unauthorized')) {
        errorMessage = 'Tu sesión ha expirado. Por favor inicia sesión de nuevo.';
        router.push('/login?redirect=/cuenta/verificacion');
        return;
      } else if (createdProfileId && err.message) {
        // Profile was created but subsequent step failed
        errorMessage = `Error en la verificación: ${err.message}. Tu perfil fue creado pero los documentos no se procesaron completamente. Por favor contacta soporte.`;
        console.error('Partial submission - Profile ID:', createdProfileId);
      }

      toast.error(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  // ============================================================================
  // Render Helpers
  // ============================================================================

  const renderStatusBadge = (status: KYCStatus) => {
    const config: Record<KYCStatus, { color: string; icon: React.ReactNode }> = {
      [KYCStatus.NotStarted]: {
        color: 'bg-gray-100 text-gray-700',
        icon: <Clock className="h-4 w-4" />,
      },
      [KYCStatus.Pending]: {
        color: 'bg-gray-100 text-gray-700',
        icon: <Clock className="h-4 w-4" />,
      },
      [KYCStatus.InProgress]: {
        color: 'bg-blue-100 text-blue-700',
        icon: <Loader2 className="h-4 w-4 animate-spin" />,
      },
      [KYCStatus.DocumentsRequired]: {
        color: 'bg-yellow-100 text-yellow-700',
        icon: <AlertCircle className="h-4 w-4" />,
      },
      [KYCStatus.UnderReview]: {
        color: 'bg-purple-100 text-purple-700',
        icon: <Clock className="h-4 w-4" />,
      },
      [KYCStatus.Approved]: {
        color: 'bg-green-100 text-green-700',
        icon: <CheckCircle className="h-4 w-4" />,
      },
      [KYCStatus.Rejected]: {
        color: 'bg-red-100 text-red-700',
        icon: <XCircle className="h-4 w-4" />,
      },
      [KYCStatus.Expired]: {
        color: 'bg-orange-100 text-orange-700',
        icon: <AlertCircle className="h-4 w-4" />,
      },
      [KYCStatus.Suspended]: {
        color: 'bg-red-100 text-red-700',
        icon: <XCircle className="h-4 w-4" />,
      },
    };

    const { color, icon } = config[status] || config[KYCStatus.Pending];

    return (
      <span
        className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-sm font-medium ${color}`}
      >
        {icon}
        {getStatusLabel(status)}
      </span>
    );
  };

  // ============================================================================
  // Loading State
  // ============================================================================

  if (isLoading || authLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <Loader2 className="mx-auto h-8 w-8 animate-spin text-blue-600" />
          <p className="mt-2 text-gray-600">Cargando tu información...</p>
        </div>
      </div>
    );
  }

  // ============================================================================
  // Existing Profile Under Review
  // ============================================================================

  if (existingProfile && existingProfile.status === KYCStatus.UnderReview) {
    return (
      <div className="min-h-screen bg-gray-50 py-12">
        <div className="mx-auto max-w-lg px-4">
          <div className="rounded-2xl border border-gray-200 bg-white p-8 text-center shadow-sm">
            <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-purple-100">
              <Clock className="h-8 w-8 text-purple-600" />
            </div>
            <h1 className="mb-2 text-2xl font-bold text-gray-900">Verificación en Revisión</h1>
            <p className="mb-6 text-gray-600">
              Tu solicitud de verificación está siendo revisada por nuestro equipo. Te notificaremos
              por correo electrónico cuando el proceso esté completo.
            </p>
            <div className="mb-6 rounded-lg bg-purple-50 p-4">
              <p className="text-sm text-purple-700">
                Tiempo estimado de revisión: <strong>24-48 horas</strong>
              </p>
            </div>
            {renderStatusBadge(existingProfile.status)}
            <div className="mt-8">
              <button
                onClick={() => router.push('/cuenta')}
                className="font-medium text-blue-600 hover:text-blue-700"
              >
                Volver a mi cuenta
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // ============================================================================
  // Existing Profile Rejected
  // ============================================================================

  if (existingProfile && existingProfile.status === KYCStatus.Rejected) {
    return (
      <div className="min-h-screen bg-gray-50 py-12">
        <div className="mx-auto max-w-lg px-4">
          <div className="rounded-2xl border border-gray-200 bg-white p-8 text-center shadow-sm">
            <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-red-100">
              <XCircle className="h-8 w-8 text-red-600" />
            </div>
            <h1 className="mb-2 text-2xl font-bold text-gray-900">Verificación Rechazada</h1>
            <p className="mb-4 text-gray-600">
              Tu solicitud de verificación fue rechazada por la siguiente razón:
            </p>
            {existingProfile.rejectionReason && (
              <div className="mb-6 rounded-lg bg-red-50 p-4 text-left">
                <p className="text-sm text-red-700">{existingProfile.rejectionReason}</p>
              </div>
            )}
            <button
              onClick={() => {
                // Pre-fill form with existing data
                const nameParts = existingProfile.fullName?.split(' ') || [];
                setPersonalInfo({
                  firstName: existingProfile.firstName || nameParts[0] || '',
                  lastName: existingProfile.lastName || nameParts.slice(1).join(' ') || '',
                  dateOfBirth: existingProfile.dateOfBirth?.split('T')[0] || '',
                  nationality: existingProfile.nationality || 'Dominicana',
                  documentNumber: existingProfile.primaryDocumentNumber || '',
                  gender: existingProfile.gender || '',
                  phoneNumber: existingProfile.phone || existingProfile.phoneNumber || '',
                  occupation: existingProfile.occupation || '',
                });
                setAddress({
                  street: existingProfile.address || '',
                  sector: existingProfile.sector || '',
                  city: existingProfile.city || '',
                  province: existingProfile.province || '',
                  postalCode: existingProfile.postalCode || '',
                  country: existingProfile.country || 'República Dominicana',
                });
                // Reset document capture state for new submission
                setCapturedDocuments({});
                setSelfieBlob(null);
                setLivenessData(null);
                setDocumentStep('front');
                // Clear existing profile to show form
                setExistingProfile(null);
                setCurrentStep('info');
              }}
              className="rounded-lg bg-blue-600 px-6 py-3 font-medium text-white transition-colors hover:bg-blue-700"
            >
              Intentar Nuevamente
            </button>
          </div>
        </div>
      </div>
    );
  }

  // ============================================================================
  // Main Verification Flow
  // ============================================================================

  return (
    <div className="min-h-screen bg-gray-50 py-8 sm:py-12">
      <div className="mx-auto max-w-3xl px-4">
        {/* Header */}
        <div className="mb-8 text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-blue-100">
            <Shield className="h-8 w-8 text-blue-600" />
          </div>
          <h1 className="mb-2 text-2xl font-bold text-gray-900 sm:text-3xl">
            Verificación de Identidad
          </h1>
          <p className="mx-auto max-w-md text-gray-600">
            Para vender en OKLA necesitamos verificar tu identidad. Este proceso es seguro y cumple
            con la Ley 155-17.
          </p>
        </div>

        {/* Progress Steps */}
        <div className="mb-8">
          <div className="relative flex items-center justify-between">
            {/* Progress Line */}
            <div className="absolute top-5 right-0 left-0 -z-10 h-0.5 bg-gray-200" />
            <div
              className="absolute top-5 left-0 -z-10 h-0.5 bg-blue-600 transition-all duration-300"
              style={{
                width: `${(STEPS.findIndex(s => s.id === currentStep) / (STEPS.length - 1)) * 100}%`,
              }}
            />

            {STEPS.map((step, index) => {
              const stepIndex = STEPS.findIndex(s => s.id === currentStep);
              const isCompleted = index < stepIndex;
              const isCurrent = step.id === currentStep;

              return (
                <div key={step.id} className="flex flex-col items-center">
                  <div
                    className={`flex h-10 w-10 items-center justify-center rounded-full transition-all duration-300 ${
                      isCompleted
                        ? 'bg-blue-600 text-white'
                        : isCurrent
                          ? 'bg-blue-600 text-white ring-4 ring-blue-100'
                          : 'bg-gray-200 text-gray-500'
                    } `}
                  >
                    {isCompleted ? <Check className="h-5 w-5" /> : step.icon}
                  </div>
                  <span
                    className={`mt-2 text-center text-xs font-medium sm:text-sm ${isCurrent ? 'text-blue-600' : 'text-gray-500'} `}
                  >
                    {step.label}
                  </span>
                </div>
              );
            })}
          </div>
        </div>

        {/* Form Container */}
        <div className="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm">
          <div key={currentStep} className="animate-fade-in p-6 sm:p-8">
            {/* Step 1: Personal Info */}
            {currentStep === 'info' && (
              <div className="space-y-6">
                <h2 className="text-xl font-semibold text-gray-900">Información Personal</h2>

                {/* Info about pre-filled data */}
                {user && (user.firstName || user.lastName || user.phone) && (
                  <div className="flex items-center gap-2 rounded-lg bg-blue-50 px-4 py-3 text-sm text-blue-700">
                    <CheckCircle className="h-4 w-4 flex-shrink-0" />
                    <span>
                      Hemos pre-llenado algunos campos con la información de tu cuenta. Verifica que
                      estén correctos.
                    </span>
                  </div>
                )}

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">
                      Nombres *
                    </label>
                    <input
                      type="text"
                      value={personalInfo.firstName}
                      onChange={e => handlePersonalInfoChange('firstName', e.target.value)}
                      className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
                      placeholder="Juan Carlos"
                      maxLength={50}
                    />
                  </div>
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">
                      Apellidos *
                    </label>
                    <input
                      type="text"
                      value={personalInfo.lastName}
                      onChange={e => handlePersonalInfoChange('lastName', e.target.value)}
                      className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
                      placeholder="Pérez García"
                      maxLength={50}
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">
                      Número de Cédula *
                    </label>
                    <input
                      type="text"
                      value={personalInfo.documentNumber}
                      onChange={e => handlePersonalInfoChange('documentNumber', e.target.value)}
                      className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
                      placeholder="001-1234567-8"
                      maxLength={13}
                    />
                  </div>
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">
                      Fecha de Nacimiento *
                    </label>
                    <input
                      type="date"
                      value={personalInfo.dateOfBirth}
                      onChange={e => handlePersonalInfoChange('dateOfBirth', e.target.value)}
                      className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">Género</label>
                    <select
                      value={personalInfo.gender}
                      onChange={e => handlePersonalInfoChange('gender', e.target.value)}
                      className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
                    >
                      <option value="">Seleccionar</option>
                      <option value="M">Masculino</option>
                      <option value="F">Femenino</option>
                    </select>
                  </div>
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">
                      Teléfono Celular *
                    </label>
                    <input
                      type="tel"
                      value={personalInfo.phoneNumber}
                      onChange={e => handlePersonalInfoChange('phoneNumber', e.target.value)}
                      className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
                      placeholder="809-555-1234"
                      maxLength={20}
                    />
                  </div>
                </div>

                <div>
                  <label className="mb-1 block text-sm font-medium text-gray-700">Ocupación</label>
                  <input
                    type="text"
                    value={personalInfo.occupation}
                    onChange={e => handlePersonalInfoChange('occupation', e.target.value)}
                    className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
                    placeholder="Ingeniero de Software"
                    maxLength={100}
                  />
                </div>
              </div>
            )}

            {/* Step 2: Address */}
            {currentStep === 'address' && (
              <div className="space-y-6">
                <h2 className="text-xl font-semibold text-gray-900">Dirección de Residencia</h2>

                <div>
                  <label className="mb-1 block text-sm font-medium text-gray-700">
                    Calle y Número *
                  </label>
                  <input
                    type="text"
                    value={address.street}
                    onChange={e => handleAddressChange('street', e.target.value)}
                    className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
                    placeholder="Calle Principal #123"
                    maxLength={200}
                  />
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">Sector</label>
                    <input
                      type="text"
                      value={address.sector}
                      onChange={e => handleAddressChange('sector', e.target.value)}
                      className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
                      placeholder="Piantini"
                      maxLength={100}
                    />
                  </div>
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">Ciudad *</label>
                    <input
                      type="text"
                      value={address.city}
                      onChange={e => handleAddressChange('city', e.target.value)}
                      className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
                      placeholder="Santo Domingo"
                      maxLength={100}
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">
                      Provincia *
                    </label>
                    <select
                      value={address.province}
                      onChange={e => handleAddressChange('province', e.target.value)}
                      className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
                    >
                      <option value="">Seleccionar provincia</option>
                      {PROVINCES_RD.map(province => (
                        <option key={province} value={province}>
                          {province}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">
                      Código Postal
                    </label>
                    <input
                      type="text"
                      value={address.postalCode}
                      onChange={e => handleAddressChange('postalCode', e.target.value)}
                      className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
                      placeholder="10101"
                      maxLength={10}
                    />
                  </div>
                </div>
              </div>
            )}

            {/* Step 3: Documents & Selfie */}
            {currentStep === 'documents' && (
              <div className="space-y-6">
                <h2 className="text-xl font-semibold text-gray-900">Verificación de Documentos</h2>

                {documentStep === 'front' && (
                  <>
                    <p className="text-gray-600">
                      Captura una foto clara del <strong>frente</strong> de tu cédula de identidad.
                    </p>
                    <DocumentCapture
                      side="Front"
                      documentType="Cedula"
                      onCapture={handleDocumentCapture}
                    />
                    <button
                      onClick={() => router.back()}
                      className="mt-4 text-sm text-gray-500 hover:text-gray-700"
                    >
                      Cancelar
                    </button>
                  </>
                )}

                {documentStep === 'back' && (
                  <>
                    <p className="text-gray-600">
                      Ahora captura una foto del <strong>reverso</strong> de tu cédula.
                    </p>
                    <DocumentCapture
                      side="Back"
                      documentType="Cedula"
                      onCapture={handleDocumentCapture}
                    />
                    <button
                      onClick={() => setDocumentStep('front')}
                      className="mt-4 text-sm text-gray-500 hover:text-gray-700"
                    >
                      ← Volver al frente
                    </button>
                  </>
                )}

                {documentStep === 'liveness' && (
                  <>
                    <p className="text-gray-600">
                      Ahora verificaremos tu identidad con una prueba de vida.
                    </p>
                    <LivenessChallenge
                      onComplete={handleLivenessComplete}
                      onCancel={() => setDocumentStep('front')}
                      requiredChallenges={['Blink', 'Smile', 'TurnLeft']}
                    />
                  </>
                )}

                {documentStep === 'complete' && (
                  <div className="py-8 text-center">
                    <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-green-100">
                      <CheckCircle className="h-8 w-8 text-green-600" />
                    </div>
                    <h3 className="mb-2 text-lg font-semibold text-gray-900">
                      ¡Documentos Capturados!
                    </h3>
                    <p className="mb-6 text-gray-600">
                      Tu cédula y verificación de identidad están listos para enviar.
                    </p>
                    <div className="flex items-center justify-center gap-4">
                      <button
                        onClick={() => {
                          setCapturedDocuments({});
                          setSelfieBlob(null);
                          setLivenessData(null);
                          setDocumentStep('front');
                        }}
                        className="px-4 py-2 text-gray-700 hover:text-gray-900"
                      >
                        Volver a capturar
                      </button>
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* Step 4: Review */}
            {currentStep === 'review' && (
              <div className="space-y-6">
                <h2 className="text-xl font-semibold text-gray-900">Revisar Información</h2>

                <p className="text-gray-600">
                  Por favor verifica que toda la información sea correcta antes de enviar.
                </p>

                {/* Personal Info Summary */}
                <div className="space-y-2 rounded-lg bg-gray-50 p-4">
                  <h3 className="flex items-center gap-2 font-medium text-gray-900">
                    <User className="h-5 w-5 text-blue-600" />
                    Información Personal
                  </h3>
                  <div className="grid grid-cols-2 gap-2 text-sm">
                    <div>
                      <span className="text-gray-500">Nombre:</span>
                      <span className="ml-2 text-gray-900">
                        {personalInfo.firstName} {personalInfo.lastName}
                      </span>
                    </div>
                    <div>
                      <span className="text-gray-500">Cédula:</span>
                      <span className="ml-2 text-gray-900">{personalInfo.documentNumber}</span>
                    </div>
                    <div>
                      <span className="text-gray-500">Teléfono:</span>
                      <span className="ml-2 text-gray-900">{personalInfo.phoneNumber}</span>
                    </div>
                    <div>
                      <span className="text-gray-500">Fecha de Nacimiento:</span>
                      <span className="ml-2 text-gray-900">{personalInfo.dateOfBirth}</span>
                    </div>
                  </div>
                </div>

                {/* Address Summary */}
                <div className="space-y-2 rounded-lg bg-gray-50 p-4">
                  <h3 className="flex items-center gap-2 font-medium text-gray-900">
                    <MapPin className="h-5 w-5 text-blue-600" />
                    Dirección
                  </h3>
                  <p className="text-sm text-gray-700">
                    {address.street}
                    {address.sector && `, ${address.sector}`}
                    <br />
                    {address.city}, {address.province}
                    {address.postalCode && ` ${address.postalCode}`}
                  </p>
                </div>

                {/* Documents Summary */}
                <div className="space-y-2 rounded-lg bg-gray-50 p-4">
                  <h3 className="flex items-center gap-2 font-medium text-gray-900">
                    <Camera className="h-5 w-5 text-blue-600" />
                    Documentos
                  </h3>
                  <div className="flex items-center gap-4 text-sm">
                    <span className="flex items-center gap-1 text-green-600">
                      <CheckCircle className="h-4 w-4" />
                      Cédula frontal
                    </span>
                    <span className="flex items-center gap-1 text-green-600">
                      <CheckCircle className="h-4 w-4" />
                      Cédula reverso
                    </span>
                    <span className="flex items-center gap-1 text-green-600">
                      <CheckCircle className="h-4 w-4" />
                      Verificación facial
                    </span>
                  </div>
                </div>

                {/* Terms */}
                <div className="rounded-lg border border-blue-200 bg-blue-50 p-4">
                  <p className="text-sm text-blue-800">
                    <strong>Al enviar esta solicitud:</strong>
                    <br />
                    • Confirmo que toda la información proporcionada es verdadera y precisa
                    <br />
                    • Autorizo a OKLA a verificar mi identidad según la Ley 155-17
                    <br />• Entiendo que proporcionar información falsa puede resultar en suspensión
                    de cuenta
                  </p>
                </div>
              </div>
            )}
          </div>

          {/* Navigation Buttons */}
          <div className="flex items-center justify-between border-t border-gray-200 bg-gray-50 px-6 py-4 sm:px-8">
            {currentStep !== 'info' ? (
              <button
                onClick={goToPrevStep}
                disabled={currentStep === 'documents' && documentStep !== 'front'}
                className="flex items-center gap-2 px-4 py-2 text-gray-700 hover:text-gray-900 disabled:cursor-not-allowed disabled:opacity-50"
              >
                <ChevronLeft className="h-5 w-5" />
                Anterior
              </button>
            ) : (
              <div />
            )}

            {currentStep !== 'review' ? (
              <button
                onClick={goToNextStep}
                disabled={currentStep === 'documents' && documentStep !== 'complete'}
                className="flex items-center gap-2 rounded-lg bg-blue-600 px-6 py-2.5 font-medium text-white transition-colors hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
              >
                Siguiente
                <ChevronRight className="h-5 w-5" />
              </button>
            ) : (
              <button
                onClick={handleSubmit}
                disabled={isSubmitting}
                className="flex items-center gap-2 rounded-lg bg-green-600 px-6 py-2.5 font-medium text-white transition-colors hover:bg-green-700 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="h-5 w-5 animate-spin" />
                    Enviando...
                  </>
                ) : (
                  <>
                    <Check className="h-5 w-5" />
                    Enviar Verificación
                  </>
                )}
              </button>
            )}
          </div>
        </div>

        {/* Security Notice */}
        <div className="mt-6 text-center text-sm text-gray-500">
          <p className="flex items-center justify-center gap-2">
            <Shield className="h-5 w-5 text-green-600" />
            Tus datos están protegidos con conexión segura (HTTPS) y cifrado en tránsito
          </p>
        </div>
      </div>
    </div>
  );
}
