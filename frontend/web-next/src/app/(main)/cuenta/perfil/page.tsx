/**
 * Profile Page — Role-Aware
 *
 * Shows only the fields relevant to each user type, matching exactly
 * what was captured during their registration flow:
 *
 * BUYER        → Personal info (name, email, phone) + bio + personal location
 * SELLER       → Personal info + Seller business card (businessName, description,
 *                location, specialties) ← sourced from SellerService
 * DEALER       → Personal info (contact rep) + Dealer business card (businessName,
 *                rnc, address, city/province, phones, website, social media)
 *                ← sourced from DealerManagementService
 * DEALER_EMPLOYEE → Personal info only (same as buyer)
 *
 * Data sources:
 *   Personal   → UserService  PUT /api/users/me
 *   Seller biz → SellerService PUT /api/sellers/{id}
 *   Dealer biz → DealerService PUT /api/dealers/{id}
 */

'use client';

import * as React from 'react';
import { useRouter } from 'next/navigation';
import {
  User,
  Mail,
  Phone,
  MapPin,
  Camera,
  Loader2,
  Check,
  X,
  Trash2,
  Building2,
  Globe,
  Hash,
  Briefcase,
  Instagram,
  Facebook,
  MessageCircle,
  Info,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { userService, type UpdateProfileRequest } from '@/services/users';
import { useAuth } from '@/hooks/use-auth';
import { useSellerByUserId, useUpdateSellerProfile } from '@/hooks/use-seller';
import { useCurrentDealer, useUpdateDealer } from '@/hooks/use-dealers';
import { sanitizeText, sanitizePhone, sanitizeUrl } from '@/lib/security/sanitize';
import { cn } from '@/lib/utils';
import { SELLER_SPECIALTIES } from '@/lib/validations/seller-onboarding';

// ─── Dominican Republic Provinces ────────────────────────────────────────────

const RD_PROVINCES = [
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

// ─── Types ────────────────────────────────────────────────────────────────────

interface PersonalForm {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  /** Only for buyer / dealer_employee */
  bio: string;
  city: string;
  province: string;
  avatarUrl: string | null;
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
}

interface SellerBizForm {
  fullName: string;
  bio: string;
  city: string;
  specialties: string[];
}

interface DealerBizForm {
  businessName: string;
  legalName: string;
  rnc: string;
  phone: string;
  mobilePhone: string;
  website: string;
  address: string;
  city: string;
  province: string;
  description: string;
  facebookUrl: string;
  instagramUrl: string;
  whatsAppNumber: string;
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

const ROLE_LABELS: Record<string, string> = {
  buyer: 'Comprador',
  seller: 'Vendedor',
  dealer: 'Dealer',
  dealer_employee: 'Empleado',
  admin: 'Administrador',
  platform_employee: 'Staff OKLA',
  guest: 'Invitado',
};

// ─── Page Component ───────────────────────────────────────────────────────────

export default function ProfilePage() {
  const router = useRouter();
  const { user: authUser } = useAuth();
  const accountType = authUser?.accountType ?? 'buyer';

  // ── Personal section state ──────────────────────────────────────────────────
  const [isLoading, setIsLoading] = React.useState(true);
  const [personalSaving, setPersonalSaving] = React.useState(false);
  const [personalSuccess, setPersonalSuccess] = React.useState(false);
  const [personalError, setPersonalError] = React.useState<string | null>(null);
  const [userServiceAvailable, setUserServiceAvailable] = React.useState(true);

  const [personal, setPersonal] = React.useState<PersonalForm>({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    bio: '',
    city: '',
    province: '',
    avatarUrl: null,
    isEmailVerified: false,
    isPhoneVerified: false,
  });

  // ── Avatar ──────────────────────────────────────────────────────────────────
  const [isUploadingAvatar, setIsUploadingAvatar] = React.useState(false);
  const fileInputRef = React.useRef<HTMLInputElement>(null);

  // ── Seller section state (seller only) ─────────────────────────────────────
  const sellerQuery = useSellerByUserId(accountType === 'seller' ? authUser?.id : undefined);
  const updateSellerMutation = useUpdateSellerProfile();
  const [sellerForm, setSellerForm] = React.useState<SellerBizForm>({
    fullName: '',
    bio: '',
    city: '',
    specialties: [],
  });
  const [sellerSaving, setSellerSaving] = React.useState(false);
  const [sellerSuccess, setSellerSuccess] = React.useState(false);
  const [sellerError, setSellerError] = React.useState<string | null>(null);

  const toggleSpecialty = (specialty: string) => {
    setSellerForm(prev => ({
      ...prev,
      specialties: prev.specialties.includes(specialty)
        ? prev.specialties.filter(s => s !== specialty)
        : [...prev.specialties, specialty],
    }));
  };

  // ── Dealer section state (dealer only) ─────────────────────────────────────
  // useCurrentDealer() is internally enabled only when accountType === 'dealer'
  const dealerQuery = useCurrentDealer();
  const updateDealerMutation = useUpdateDealer();
  const [dealerForm, setDealerForm] = React.useState<DealerBizForm>({
    businessName: '',
    legalName: '',
    rnc: '',
    phone: '',
    mobilePhone: '',
    website: '',
    address: '',
    city: '',
    province: '',
    description: '',
    facebookUrl: '',
    instagramUrl: '',
    whatsAppNumber: '',
  });
  const [dealerSaving, setDealerSaving] = React.useState(false);
  const [dealerSuccess, setDealerSuccess] = React.useState(false);
  const [dealerError, setDealerError] = React.useState<string | null>(null);

  // ── Load personal profile on mount ─────────────────────────────────────────
  React.useEffect(() => {
    async function load() {
      try {
        const data = await userService.getCurrentProfile();
        setPersonal({
          firstName: data.firstName,
          lastName: data.lastName,
          email: data.email,
          phone: data.phone || '',
          bio: data.bio || '',
          city: data.city || '',
          province: data.province || '',
          avatarUrl: data.avatarUrl || null,
          isEmailVerified: data.isEmailVerified,
          isPhoneVerified: data.isPhoneVerified,
        });
        setUserServiceAvailable(true);
      } catch {
        // Fallback to JWT data when UserService profile is still being synced
        if (authUser) {
          setPersonal(prev => ({
            ...prev,
            firstName: authUser.firstName || '',
            lastName: authUser.lastName || '',
            email: authUser.email || '',
            avatarUrl: authUser.avatarUrl || null,
            isEmailVerified: authUser.isEmailVerified || false,
          }));
          setUserServiceAvailable(false);
        } else {
          setPersonalError('Error al cargar el perfil');
        }
      } finally {
        setIsLoading(false);
      }
    }
    load();
  }, [authUser]);

  // ── Sync seller form when query resolves ───────────────────────────────────
  React.useEffect(() => {
    if (sellerQuery.data) {
      const s = sellerQuery.data;
      setSellerForm({
        fullName: s.fullName || '',
        bio: s.bio || '',
        city: s.city || '',
        specialties: s.specialties || [],
      });
    }
  }, [sellerQuery.data]);

  // ── Sync dealer form when query resolves ───────────────────────────────────
  React.useEffect(() => {
    if (dealerQuery.data) {
      const d = dealerQuery.data;
      setDealerForm({
        businessName: d.businessName || '',
        legalName: d.legalName || '',
        rnc: d.rnc || '',
        phone: d.phone || '',
        mobilePhone: d.mobilePhone || '',
        website: d.website || '',
        address: d.address || '',
        city: d.city || '',
        province: d.province || '',
        description: d.description || '',
        facebookUrl: d.facebookUrl || '',
        instagramUrl: d.instagramUrl || '',
        whatsAppNumber: d.whatsAppNumber || '',
      });
    }
  }, [dealerQuery.data]);

  // ── Save personal info ─────────────────────────────────────────────────────
  const handlePersonalSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setPersonalError(null);
    setPersonalSuccess(false);

    if (!userServiceAvailable) {
      setPersonalError('Tu perfil aún no está disponible. Recarga la página en unos momentos.');
      return;
    }

    setPersonalSaving(true);
    try {
      const payload: UpdateProfileRequest = {
        firstName: sanitizeText(personal.firstName.trim(), { maxLength: 50 }),
        lastName: sanitizeText(personal.lastName.trim(), { maxLength: 50 }),
        phone: personal.phone ? sanitizePhone(personal.phone) : undefined,
      };

      // Bio + personal location — only for buyer / dealer_employee.
      // Sellers use their own "location" field in the seller business section.
      // Dealers use the business address in the dealer section.
      if (accountType === 'buyer' || accountType === 'dealer_employee') {
        payload.bio = personal.bio ? sanitizeText(personal.bio, { maxLength: 500 }) : undefined;
        payload.city = personal.city
          ? sanitizeText(personal.city.trim(), { maxLength: 100 })
          : undefined;
        payload.province = personal.province
          ? sanitizeText(personal.province.trim(), { maxLength: 100 })
          : undefined;
      }

      await userService.updateProfile(payload);
      setPersonalSuccess(true);
      router.refresh();
      setTimeout(() => setPersonalSuccess(false), 3000);
    } catch (err) {
      const error = err as { message?: string };
      setPersonalError(error.message || 'Error al guardar los datos personales');
    } finally {
      setPersonalSaving(false);
    }
  };

  // ── Save seller business ───────────────────────────────────────────────────
  const handleSellerSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSellerError(null);
    setSellerSuccess(false);

    if (!sellerQuery.data?.id) {
      setSellerError('Perfil de vendedor no encontrado. Contacta soporte si el problema persiste.');
      return;
    }

    setSellerSaving(true);
    try {
      await updateSellerMutation.mutateAsync({
        sellerId: sellerQuery.data.id,
        data: {
          fullName: sellerForm.fullName.trim()
            ? sanitizeText(sellerForm.fullName.trim(), { maxLength: 100 })
            : undefined,
          bio: sellerForm.bio ? sanitizeText(sellerForm.bio, { maxLength: 1000 }) : undefined,
          city: sellerForm.city
            ? sanitizeText(sellerForm.city.trim(), { maxLength: 100 })
            : undefined,
          specialties: sellerForm.specialties.length > 0 ? sellerForm.specialties : undefined,
        },
      });
      setSellerSuccess(true);
      setTimeout(() => setSellerSuccess(false), 3000);
    } catch (err) {
      const error = err as { message?: string };
      setSellerError(error.message || 'Error al guardar el perfil de vendedor');
    } finally {
      setSellerSaving(false);
    }
  };

  // ── Save dealer business ────────────────────────────────────────────────────
  const handleDealerSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setDealerError(null);
    setDealerSuccess(false);

    if (!dealerQuery.data?.id) {
      setDealerError('Perfil del negocio no encontrado. Contacta soporte si el problema persiste.');
      return;
    }

    setDealerSaving(true);
    try {
      await updateDealerMutation.mutateAsync({
        id: dealerQuery.data.id,
        data: {
          businessName: sanitizeText(dealerForm.businessName.trim(), { maxLength: 200 }),
          legalName: dealerForm.legalName
            ? sanitizeText(dealerForm.legalName.trim(), { maxLength: 200 })
            : undefined,
          phone: dealerForm.phone ? sanitizePhone(dealerForm.phone) : undefined,
          mobilePhone: dealerForm.mobilePhone ? sanitizePhone(dealerForm.mobilePhone) : undefined,
          website: dealerForm.website ? sanitizeUrl(dealerForm.website.trim()) : undefined,
          address: dealerForm.address
            ? sanitizeText(dealerForm.address.trim(), { maxLength: 300 })
            : undefined,
          city: dealerForm.city
            ? sanitizeText(dealerForm.city.trim(), { maxLength: 100 })
            : undefined,
          province: dealerForm.province
            ? sanitizeText(dealerForm.province.trim(), { maxLength: 100 })
            : undefined,
          description: dealerForm.description
            ? sanitizeText(dealerForm.description, { maxLength: 1000 })
            : undefined,
          facebookUrl: dealerForm.facebookUrl
            ? sanitizeUrl(dealerForm.facebookUrl.trim())
            : undefined,
          instagramUrl: dealerForm.instagramUrl
            ? sanitizeUrl(dealerForm.instagramUrl.trim())
            : undefined,
          whatsAppNumber: dealerForm.whatsAppNumber
            ? sanitizePhone(dealerForm.whatsAppNumber)
            : undefined,
        },
      });
      setDealerSuccess(true);
      setTimeout(() => setDealerSuccess(false), 3000);
    } catch (err) {
      const error = err as { message?: string };
      setDealerError(error.message || 'Error al guardar la información del negocio');
    } finally {
      setDealerSaving(false);
    }
  };

  // ── Avatar handlers ────────────────────────────────────────────────────────
  const handleAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      setPersonalError('Solo se permiten imágenes');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      setPersonalError('La imagen no puede superar 5MB');
      return;
    }
    setIsUploadingAvatar(true);
    setPersonalError(null);
    try {
      const { avatarUrl } = await userService.uploadAvatar(file);
      setPersonal(prev => ({ ...prev, avatarUrl }));
      router.refresh();
    } catch {
      setPersonalError('Error al subir la imagen');
    } finally {
      setIsUploadingAvatar(false);
    }
  };

  const handleDeleteAvatar = async () => {
    if (!confirm('¿Estás seguro de eliminar tu foto de perfil?')) return;
    setIsUploadingAvatar(true);
    try {
      await userService.deleteAvatar();
      setPersonal(prev => ({ ...prev, avatarUrl: null }));
      router.refresh();
    } catch {
      setPersonalError('Error al eliminar la imagen');
    } finally {
      setIsUploadingAvatar(false);
    }
  };

  // ── Loading skeleton ───────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="text-primary h-8 w-8 animate-spin" />
      </div>
    );
  }

  // ── Render ─────────────────────────────────────────────────────────────────
  return (
    <div className="space-y-6">
      {/* Page header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Mi Perfil</h1>
          <p className="text-muted-foreground text-sm">
            {accountType === 'dealer'
              ? 'Tus datos personales y la información de tu negocio'
              : accountType === 'seller'
                ? 'Tus datos personales y tu perfil de vendedor'
                : 'Actualiza tu información personal'}
          </p>
        </div>
        <Badge variant="secondary">{ROLE_LABELS[accountType] ?? accountType}</Badge>
      </div>

      {/* Sync pending banner */}
      {!userServiceAvailable && (
        <div className="flex items-center gap-2 rounded-lg border border-yellow-200 bg-yellow-50 p-4 text-sm text-yellow-800">
          <Loader2 className="h-4 w-4 shrink-0 animate-spin" />
          Tu perfil está siendo sincronizado. Los datos mostrados provienen de tu cuenta de acceso.
          Recarga en unos momentos para editarlos.
        </div>
      )}

      {/* ════════════════════════════════════════════════════════════
          AVATAR
          ════════════════════════════════════════════════════════════ */}
      <Card>
        <CardHeader>
          <CardTitle>Foto de Perfil</CardTitle>
          <CardDescription>Visible en tus publicaciones y perfil público</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-6">
            <div className="relative">
              <div className="bg-muted flex h-24 w-24 items-center justify-center overflow-hidden rounded-full">
                {isUploadingAvatar ? (
                  <Loader2 className="text-muted-foreground h-8 w-8 animate-spin" />
                ) : personal.avatarUrl ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={personal.avatarUrl}
                    alt="Avatar"
                    className="h-full w-full object-cover"
                  />
                ) : (
                  <User className="text-muted-foreground h-12 w-12" />
                )}
              </div>
              <button
                type="button"
                onClick={() => fileInputRef.current?.click()}
                disabled={isUploadingAvatar}
                className="bg-primary hover:bg-primary/90 absolute right-0 bottom-0 flex h-8 w-8 items-center justify-center rounded-full text-white transition-colors"
              >
                <Camera className="h-4 w-4" />
              </button>
            </div>
            <div className="space-y-2">
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => fileInputRef.current?.click()}
                  disabled={isUploadingAvatar}
                >
                  Cambiar foto
                </Button>
                {personal.avatarUrl && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={handleDeleteAvatar}
                    disabled={isUploadingAvatar}
                    className="text-red-600 hover:bg-red-50 hover:text-red-700"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                )}
              </div>
              <p className="text-muted-foreground text-sm">JPG, PNG. Máximo 5MB.</p>
            </div>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              onChange={handleAvatarChange}
              className="hidden"
            />
          </div>
        </CardContent>
      </Card>

      {/* ════════════════════════════════════════════════════════════
          PERSONAL INFO — all users
          ════════════════════════════════════════════════════════════ */}
      <form onSubmit={handlePersonalSubmit}>
        <Card>
          <CardHeader>
            <CardTitle>Información Personal</CardTitle>
            <CardDescription>
              {accountType === 'dealer'
                ? 'Tus datos como representante del negocio'
                : accountType === 'dealer_employee'
                  ? 'Tus datos personales como empleado'
                  : 'Tus datos de contacto registrados'}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {personalSuccess && (
              <div className="flex items-center gap-2 rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700">
                <Check className="h-4 w-4" />
                Datos personales actualizados correctamente
              </div>
            )}
            {personalError && (
              <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                <X className="h-4 w-4" />
                {personalError}
              </div>
            )}

            {/* Name */}
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="firstName">Nombre</Label>
                <Input
                  id="firstName"
                  value={personal.firstName}
                  onChange={e => setPersonal(p => ({ ...p, firstName: e.target.value }))}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lastName">Apellido</Label>
                <Input
                  id="lastName"
                  value={personal.lastName}
                  onChange={e => setPersonal(p => ({ ...p, lastName: e.target.value }))}
                  required
                />
              </div>
            </div>

            {/* Email — read-only */}
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <div className="relative">
                <Mail className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                <Input
                  id="email"
                  type="email"
                  value={personal.email}
                  disabled
                  className="pl-10 opacity-70"
                />
                {personal.isEmailVerified && (
                  <span className="absolute top-1/2 right-3 flex -translate-y-1/2 items-center gap-1 text-xs text-green-600">
                    <Check className="h-3 w-3" />
                    Verificado
                  </span>
                )}
              </div>
              <p className="text-muted-foreground text-xs">
                El email no puede ser cambiado desde aquí
              </p>
            </div>

            {/* Personal phone */}
            <div className="space-y-2">
              <Label htmlFor="phone">
                {accountType === 'dealer' ? 'Teléfono personal (representante)' : 'Teléfono'}
              </Label>
              <div className="relative">
                <Phone className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                <Input
                  id="phone"
                  type="tel"
                  placeholder="809-555-0123"
                  value={personal.phone}
                  onChange={e => setPersonal(p => ({ ...p, phone: e.target.value }))}
                  className="pl-10"
                />
                {personal.phone && personal.isPhoneVerified && (
                  <span className="absolute top-1/2 right-3 flex -translate-y-1/2 items-center gap-1 text-xs text-green-600">
                    <Check className="h-3 w-3" />
                    Verificado
                  </span>
                )}
              </div>
            </div>

            {/* Bio + personal location — ONLY buyer / dealer_employee */}
            {(accountType === 'buyer' || accountType === 'dealer_employee') && (
              <>
                <div className="space-y-2">
                  <Label htmlFor="bio">Sobre ti</Label>
                  <textarea
                    id="bio"
                    placeholder="Cuéntanos sobre ti..."
                    value={personal.bio}
                    onChange={e => setPersonal(p => ({ ...p, bio: e.target.value }))}
                    rows={3}
                    maxLength={500}
                    className="focus:ring-primary/20 focus:border-primary border-border w-full resize-none rounded-lg border px-3 py-2 text-sm focus:ring-2 focus:outline-none"
                  />
                  <p className="text-muted-foreground text-right text-xs">
                    {personal.bio.length}/500
                  </p>
                </div>

                <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="city">Ciudad</Label>
                    <div className="relative">
                      <MapPin className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                      <Input
                        id="city"
                        placeholder="Santo Domingo"
                        value={personal.city}
                        onChange={e => setPersonal(p => ({ ...p, city: e.target.value }))}
                        className="pl-10"
                      />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="province">Provincia</Label>
                    <select
                      id="province"
                      value={personal.province}
                      onChange={e => setPersonal(p => ({ ...p, province: e.target.value }))}
                      className="border-border focus:ring-primary/20 focus:border-primary h-10 w-full rounded-lg border px-3 text-sm focus:ring-2 focus:outline-none"
                    >
                      <option value="">Seleccionar provincia</option>
                      {RD_PROVINCES.map(prov => (
                        <option key={prov} value={prov}>
                          {prov}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
              </>
            )}

            <div className="flex justify-end pt-2">
              <Button type="submit" disabled={personalSaving || !userServiceAvailable}>
                {personalSaving ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Guardando...
                  </>
                ) : (
                  'Guardar datos personales'
                )}
              </Button>
            </div>
          </CardContent>
        </Card>
      </form>

      {/* ════════════════════════════════════════════════════════════
          SELLER BUSINESS SECTION — seller only
          ════════════════════════════════════════════════════════════ */}
      {accountType === 'seller' && (
        <form onSubmit={handleSellerSubmit}>
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Briefcase className="h-5 w-5" />
                Perfil de Vendedor
              </CardTitle>
              <CardDescription>
                Información de tu actividad como vendedor. Visible públicamente en tus anuncios.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {sellerQuery.isLoading && (
                <div className="text-muted-foreground flex items-center gap-2 py-4 text-sm">
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Cargando perfil de vendedor...
                </div>
              )}

              {sellerQuery.isError && (
                <div className="flex items-center gap-2 rounded-lg border border-yellow-200 bg-yellow-50 p-3 text-sm text-yellow-800">
                  <Info className="h-4 w-4 shrink-0" />
                  Perfil de vendedor pendiente de configuración.{' '}
                  <a href="/vender/registro" className="font-medium underline">
                    Completar ahora
                  </a>
                </div>
              )}

              {sellerSuccess && (
                <div className="flex items-center gap-2 rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700">
                  <Check className="h-4 w-4" />
                  Perfil de vendedor actualizado correctamente
                </div>
              )}

              {sellerError && (
                <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                  <X className="h-4 w-4" />
                  {sellerError}
                </div>
              )}

              {!sellerQuery.isLoading && !sellerQuery.isError && (
                <>
                  <div className="space-y-2">
                    <Label htmlFor="sellerFullName">Nombre del vendedor / negocio</Label>
                    <div className="relative">
                      <Building2 className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                      <Input
                        id="sellerFullName"
                        placeholder="Ej: Juan Pérez / JP Autos"
                        value={sellerForm.fullName}
                        onChange={e => setSellerForm(s => ({ ...s, fullName: e.target.value }))}
                        className="pl-10"
                      />
                    </div>
                    <p className="text-muted-foreground text-xs">
                      Así aparecerás en tus anuncios y perfil público
                    </p>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="sellerDesc">Descripción</Label>
                    <textarea
                      id="sellerDesc"
                      placeholder="Describe tu actividad como vendedor, qué tipos de vehículos ofreces..."
                      value={sellerForm.bio}
                      onChange={e => setSellerForm(s => ({ ...s, bio: e.target.value }))}
                      rows={3}
                      maxLength={1000}
                      className="focus:ring-primary/20 focus:border-primary border-border w-full resize-none rounded-lg border px-3 py-2 text-sm focus:ring-2 focus:outline-none"
                    />
                    <p className="text-muted-foreground text-right text-xs">
                      {sellerForm.bio.length}/1000
                    </p>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="sellerCity">Ubicación</Label>
                    <div className="relative">
                      <MapPin className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                      <Input
                        id="sellerCity"
                        placeholder="Ej: Santo Domingo, Distrito Nacional"
                        value={sellerForm.city}
                        onChange={e => setSellerForm(s => ({ ...s, city: e.target.value }))}
                        className="pl-10"
                      />
                    </div>
                  </div>

                  {/* Specialties badges */}
                  <div className="space-y-3">
                    <Label>Especialidades <span className="text-muted-foreground font-normal">(opcional)</span></Label>
                    <p className="text-muted-foreground text-xs">
                      Toca las especialidades en las que te enfocas
                    </p>
                    <div className="flex flex-wrap gap-2">
                      {SELLER_SPECIALTIES.map(specialty => {
                        const isSelected = sellerForm.specialties.includes(specialty);
                        return (
                          <Badge
                            key={specialty}
                            variant={isSelected ? 'default' : 'outline'}
                            className={cn(
                              'cursor-pointer transition-colors',
                              isSelected
                                ? 'bg-[#00A870] hover:bg-[#009663]'
                                : 'hover:border-[#00A870] hover:text-[#00A870]'
                            )}
                            onClick={() => toggleSpecialty(specialty)}
                          >
                            {specialty}
                          </Badge>
                        );
                      })}
                    </div>
                  </div>

                  <div className="flex justify-end pt-2">
                    <Button type="submit" disabled={sellerSaving || !sellerQuery.data?.id}>
                      {sellerSaving ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Guardando...
                        </>
                      ) : (
                        'Guardar perfil de vendedor'
                      )}
                    </Button>
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        </form>
      )}

      {/* ════════════════════════════════════════════════════════════
          DEALER BUSINESS SECTION — dealer only
          ════════════════════════════════════════════════════════════ */}
      {accountType === 'dealer' && (
        <form onSubmit={handleDealerSubmit}>
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Building2 className="h-5 w-5" />
                Información del Negocio
              </CardTitle>
              <CardDescription>
                Datos de tu concesionario. Visibles en tu perfil público de dealer.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {dealerQuery.isLoading && (
                <div className="text-muted-foreground flex items-center gap-2 py-4 text-sm">
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Cargando información del negocio...
                </div>
              )}

              {!dealerQuery.isLoading && !dealerQuery.data && (
                <div className="flex items-center gap-2 rounded-lg border border-yellow-200 bg-yellow-50 p-3 text-sm text-yellow-800">
                  <Info className="h-4 w-4 shrink-0" />
                  El perfil del negocio aún no está configurado. Completa los campos y guarda para
                  activarlo.
                </div>
              )}

              {dealerSuccess && (
                <div className="flex items-center gap-2 rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700">
                  <Check className="h-4 w-4" />
                  Información del negocio actualizada correctamente
                </div>
              )}

              {dealerError && (
                <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                  <X className="h-4 w-4" />
                  {dealerError}
                </div>
              )}

              {!dealerQuery.isLoading && (
                <>
                  {/* Business name + legal name */}
                  <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="dealerBizName">
                        Nombre del negocio <span className="text-red-500">*</span>
                      </Label>
                      <div className="relative">
                        <Building2 className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                        <Input
                          id="dealerBizName"
                          placeholder="Ej: Auto Premium RD"
                          value={dealerForm.businessName}
                          onChange={e =>
                            setDealerForm(d => ({ ...d, businessName: e.target.value }))
                          }
                          className="pl-10"
                          required
                        />
                      </div>
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="dealerLegalName">Razón social</Label>
                      <Input
                        id="dealerLegalName"
                        placeholder="Nombre legal registrado (opcional)"
                        value={dealerForm.legalName}
                        onChange={e => setDealerForm(d => ({ ...d, legalName: e.target.value }))}
                      />
                    </div>
                  </div>

                  {/* RNC + website */}
                  <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="dealerRnc">RNC</Label>
                      <div className="relative">
                        <Hash className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                        <Input
                          id="dealerRnc"
                          placeholder="000-00000-0"
                          value={dealerForm.rnc}
                          onChange={e => setDealerForm(d => ({ ...d, rnc: e.target.value }))}
                          className="pl-10"
                        />
                      </div>
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="dealerWebsite">Sitio web</Label>
                      <div className="relative">
                        <Globe className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                        <Input
                          id="dealerWebsite"
                          type="url"
                          placeholder="https://tudealer.com"
                          value={dealerForm.website}
                          onChange={e => setDealerForm(d => ({ ...d, website: e.target.value }))}
                          className="pl-10"
                        />
                      </div>
                    </div>
                  </div>

                  {/* Business phones */}
                  <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="dealerPhone">Teléfono del negocio</Label>
                      <div className="relative">
                        <Phone className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                        <Input
                          id="dealerPhone"
                          type="tel"
                          placeholder="809-555-0123"
                          value={dealerForm.phone}
                          onChange={e => setDealerForm(d => ({ ...d, phone: e.target.value }))}
                          className="pl-10"
                        />
                      </div>
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="dealerMobilePhone">Teléfono móvil</Label>
                      <div className="relative">
                        <Phone className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                        <Input
                          id="dealerMobilePhone"
                          type="tel"
                          placeholder="849-555-0123"
                          value={dealerForm.mobilePhone}
                          onChange={e =>
                            setDealerForm(d => ({ ...d, mobilePhone: e.target.value }))
                          }
                          className="pl-10"
                        />
                      </div>
                    </div>
                  </div>

                  {/* Description */}
                  <div className="space-y-2">
                    <Label htmlFor="dealerDesc">Descripción del negocio</Label>
                    <textarea
                      id="dealerDesc"
                      placeholder="Describe tu concesionario, marcas, servicios..."
                      value={dealerForm.description}
                      onChange={e => setDealerForm(d => ({ ...d, description: e.target.value }))}
                      rows={3}
                      maxLength={1000}
                      className="focus:ring-primary/20 focus:border-primary border-border w-full resize-none rounded-lg border px-3 py-2 text-sm focus:ring-2 focus:outline-none"
                    />
                    <p className="text-muted-foreground text-right text-xs">
                      {dealerForm.description.length}/1000
                    </p>
                  </div>

                  {/* Business address */}
                  <div className="space-y-2">
                    <Label htmlFor="dealerAddress">Dirección del negocio</Label>
                    <div className="relative">
                      <MapPin className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                      <Input
                        id="dealerAddress"
                        placeholder="Av. Principal #123, Local 4"
                        value={dealerForm.address}
                        onChange={e => setDealerForm(d => ({ ...d, address: e.target.value }))}
                        className="pl-10"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="dealerCity">Ciudad</Label>
                      <Input
                        id="dealerCity"
                        placeholder="Santo Domingo"
                        value={dealerForm.city}
                        onChange={e => setDealerForm(d => ({ ...d, city: e.target.value }))}
                      />
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="dealerProvince">Provincia</Label>
                      <select
                        id="dealerProvince"
                        value={dealerForm.province}
                        onChange={e => setDealerForm(d => ({ ...d, province: e.target.value }))}
                        className="border-border focus:ring-primary/20 focus:border-primary h-10 w-full rounded-lg border px-3 text-sm focus:ring-2 focus:outline-none"
                      >
                        <option value="">Seleccionar provincia</option>
                        {RD_PROVINCES.map(prov => (
                          <option key={prov} value={prov}>
                            {prov}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>

                  {/* Social media */}
                  <div className="space-y-2">
                    <Label>Redes sociales</Label>
                    <div className="space-y-2">
                      <div className="relative">
                        <Facebook className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                        <Input
                          type="url"
                          placeholder="https://facebook.com/tunegocio"
                          value={dealerForm.facebookUrl}
                          onChange={e =>
                            setDealerForm(d => ({ ...d, facebookUrl: e.target.value }))
                          }
                          className="pl-10"
                        />
                      </div>

                      <div className="relative">
                        <Instagram className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                        <Input
                          type="url"
                          placeholder="https://instagram.com/tunegocio"
                          value={dealerForm.instagramUrl}
                          onChange={e =>
                            setDealerForm(d => ({ ...d, instagramUrl: e.target.value }))
                          }
                          className="pl-10"
                        />
                      </div>

                      <div className="relative">
                        <MessageCircle className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                        <Input
                          type="tel"
                          placeholder="WhatsApp: 809-555-0123"
                          value={dealerForm.whatsAppNumber}
                          onChange={e =>
                            setDealerForm(d => ({ ...d, whatsAppNumber: e.target.value }))
                          }
                          className="pl-10"
                        />
                      </div>
                    </div>
                  </div>

                  <div className="flex justify-end pt-2">
                    <Button type="submit" disabled={dealerSaving || !dealerQuery.data?.id}>
                      {dealerSaving ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Guardando...
                        </>
                      ) : (
                        'Guardar información del negocio'
                      )}
                    </Button>
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        </form>
      )}
    </div>
  );
}
