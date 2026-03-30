/**
 * Admin KYC Detail Page
 *
 * Individual KYC verification review - connected to backend API
 */

'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { formatDateTime } from '@/lib/utils';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Textarea } from '@/components/ui/textarea';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { KYCStatusBadge } from '@/components/dashboard';
import {
  ArrowLeft,
  User,
  Mail,
  Phone,
  Calendar,
  MapPin,
  FileText,
  CheckCircle,
  XCircle,
  Clock,
  Eye,
  AlertTriangle,
  Shield,
  Loader2,
  Briefcase,
} from 'lucide-react';
import Link from 'next/link';
import { toast } from 'sonner';
import { useAuth } from '@/hooks/use-auth';
import {
  KYCProfile,
  KYCStatus,
  getKYCProfileById,
  approveKYCProfile,
  rejectKYCProfile,
  getDocumentTypeLabel,
  getDocumentFreshUrl,
  getKYCDocuments,
} from '@/services/kyc';

// Map backend status enum to frontend filter keys
const statusToFilterKey = (status: KYCStatus): string => {
  switch (status) {
    case KYCStatus.Pending:
      return 'pending';
    case KYCStatus.InProgress:
      return 'in_progress';
    case KYCStatus.UnderReview:
      return 'in_review';
    case KYCStatus.Approved:
      return 'approved';
    case KYCStatus.Rejected:
      return 'rejected';
    case KYCStatus.DocumentsRequired:
      return 'documents_required';
    default:
      return 'pending';
  }
};

export default function KYCDetailPage() {
  const params = useParams();
  const router = useRouter();
  const { user } = useAuth();
  const profileId = params.id as string;

  const [profile, setProfile] = useState<KYCProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [processingAction, setProcessingAction] = useState(false);
  const [rejectReason, setRejectReason] = useState('');
  const [adminNotes, setAdminNotes] = useState('');

  // Fetch profile data
  useEffect(() => {
    async function fetchProfile() {
      try {
        setLoading(true);
        const data = await getKYCProfileById(profileId);
        setProfile(data);
      } catch (error) {
        console.error('Error fetching KYC profile:', error);
        toast.error('Error al cargar el perfil KYC');
      } finally {
        setLoading(false);
      }
    }

    if (profileId) {
      fetchProfile();
    }
  }, [profileId]);

  // Fetch documents separately if not included in profile response
  // This handles cases where the backend API doesn't include documents in the profile response
  useEffect(() => {
    async function fetchDocumentsIfMissing() {
      if (profile && (!profile.documents || profile.documents.length === 0)) {
        try {
          const documents = await getKYCDocuments(profileId);
          if (documents && documents.length > 0) {
            setProfile(prev => (prev ? { ...prev, documents } : null));
          }
        } catch (error) {
          console.warn('Failed to fetch documents separately:', error);
          // Don't show error toast - this is just a fallback
        }
      }
    }

    if (profile) {
      fetchDocumentsIfMissing();
    }
  }, [profile, profileId]);

  // Handle approve
  const handleApprove = async () => {
    if (!profile || !user) return;

    try {
      setProcessingAction(true);
      await approveKYCProfile(
        profile.id,
        user.id,
        `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email,
        adminNotes || undefined
      );
      toast.success('Perfil KYC aprobado exitosamente');
      router.push('/admin/kyc');
    } catch (error) {
      console.error('Error approving profile:', error);
      toast.error('Error al aprobar el perfil');
    } finally {
      setProcessingAction(false);
    }
  };

  // Handle reject
  const handleReject = async () => {
    if (!profile || !user || !rejectReason.trim()) {
      toast.error('Debes proporcionar una razón para el rechazo');
      return;
    }

    try {
      setProcessingAction(true);
      await rejectKYCProfile(
        profile.id,
        user.id,
        `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email,
        rejectReason,
        adminNotes || undefined
      );
      toast.success('Perfil KYC rechazado');
      router.push('/admin/kyc');
    } catch (error) {
      console.error('Error rejecting profile:', error);
      toast.error('Error al rechazar el perfil');
    } finally {
      setProcessingAction(false);
    }
  };

  // UTC-safe date-only formatter: prevents off-by-one day from UTC→local conversion
  const formatDateOnly = (dateString: string | null | undefined): string => {
    if (!dateString) return 'N/A';
    const datePart = dateString.split('T')[0];
    const [year, month, day] = datePart.split('-').map(Number);
    if (!year || !month || !day) return 'N/A';
    return new Date(year, month - 1, day).toLocaleDateString('es-DO', {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  };

  if (loading) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <Loader2 className="text-primary h-8 w-8 animate-spin" />
        <span className="ml-2">Cargando perfil...</span>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Link href="/admin/kyc">
            <Button variant="ghost" size="icon" aria-label="Volver a KYC">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold">Perfil no encontrado</h1>
            <p className="text-muted-foreground">El perfil KYC solicitado no existe.</p>
          </div>
        </div>
      </div>
    );
  }

  const canProcess =
    profile.status === KYCStatus.Pending ||
    profile.status === KYCStatus.UnderReview ||
    profile.status === KYCStatus.InProgress;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link href="/admin/kyc">
            <Button variant="ghost" size="icon" aria-label="Volver a KYC">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold">Verificación KYC</h1>
            <p className="text-muted-foreground">ID: {profile.id.slice(0, 8)}...</p>
          </div>
        </div>
        <KYCStatusBadge status={statusToFilterKey(profile.status)} />
      </div>

      {/* User Info */}
      <Card>
        <CardContent className="py-6">
          <div className="flex items-start gap-6">
            <Avatar className="h-20 w-20">
              <AvatarFallback className="bg-primary text-2xl text-white">
                {profile.fullName
                  ? profile.fullName
                      .split(' ')
                      .map(n => n[0])
                      .slice(0, 2)
                      .join('')
                  : 'KYC'}
              </AvatarFallback>
            </Avatar>
            <div className="flex-1">
              <h2 className="mb-2 text-xl font-bold">{profile.fullName || 'Sin nombre'}</h2>
              <div className="grid grid-cols-2 gap-4 text-sm md:grid-cols-4">
                <div className="text-muted-foreground flex items-center gap-2">
                  <Mail className="h-4 w-4" />
                  <span>{profile.email || 'N/A'}</span>
                </div>
                <div className="text-muted-foreground flex items-center gap-2">
                  <Phone className="h-4 w-4" />
                  <span>{profile.phone || profile.phoneNumber || 'N/A'}</span>
                </div>
                <div className="text-muted-foreground flex items-center gap-2">
                  <Calendar className="h-4 w-4" />
                  <span>{formatDateOnly(profile.dateOfBirth)}</span>
                </div>
                <div className="text-muted-foreground flex items-center gap-2">
                  <Clock className="h-4 w-4" />
                  <span>Solicitud: {formatDateTime(profile.createdAt)}</span>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Personal Information */}
        <div className="space-y-6 lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <User className="h-5 w-5" />
                Información Personal
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-muted-foreground text-sm">Nombre Completo</p>
                  <p className="font-medium">{profile.fullName || 'N/A'}</p>
                </div>
                <div>
                  <p className="text-muted-foreground text-sm">Documento</p>
                  <p className="font-medium">{profile.primaryDocumentNumber || 'N/A'}</p>
                </div>
                <div>
                  <p className="text-muted-foreground text-sm">Nacionalidad</p>
                  <p className="font-medium">{profile.nationality || 'N/A'}</p>
                </div>
                <div>
                  <p className="text-muted-foreground text-sm">Fecha de Nacimiento</p>
                  <p className="font-medium">{formatDateOnly(profile.dateOfBirth)}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <MapPin className="h-5 w-5" />
                Dirección
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="col-span-2">
                  <p className="text-muted-foreground text-sm">Dirección</p>
                  <p className="font-medium">{profile.address || 'N/A'}</p>
                </div>
                <div>
                  <p className="text-muted-foreground text-sm">Ciudad</p>
                  <p className="font-medium">{profile.city || 'N/A'}</p>
                </div>
                <div>
                  <p className="text-muted-foreground text-sm">Provincia</p>
                  <p className="font-medium">{profile.province || 'N/A'}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Briefcase className="h-5 w-5" />
                Información Adicional
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-muted-foreground text-sm">Ocupación</p>
                  <p className="font-medium">{profile.occupation || 'N/A'}</p>
                </div>
                <div>
                  <p className="text-muted-foreground text-sm">Fuente de Fondos</p>
                  <p className="font-medium">{profile.sourceOfFunds || 'N/A'}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Documents */}
          {profile.documents && profile.documents.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <FileText className="h-5 w-5" />
                  Documentos Subidos ({profile.documents.length})
                </CardTitle>
                <CardDescription>Revisa cada documento cuidadosamente</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {profile.documents.map(doc => (
                    <div
                      key={doc.id}
                      className="bg-muted/50 flex items-center justify-between rounded-lg border p-4"
                    >
                      <div className="flex items-center gap-3">
                        <FileText className="text-muted-foreground h-8 w-8" />
                        <div>
                          <p className="font-medium">
                            {doc.typeName || getDocumentTypeLabel(doc.type)}
                          </p>
                          <p className="text-muted-foreground text-sm">{doc.documentName}</p>
                          <p className="text-muted-foreground text-xs">
                            {new Date(doc.uploadedAt).toLocaleDateString('es-DO')} -{' '}
                            {(doc.fileSize / 1024).toFixed(1)} KB
                          </p>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <Badge variant="outline">{doc.statusName}</Badge>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={async () => {
                            try {
                              const freshUrlData = await getDocumentFreshUrl(doc.id);
                              if (freshUrlData?.url) {
                                window.open(freshUrlData.url, '_blank', 'noopener,noreferrer');
                              } else {
                                toast.error('No se pudo obtener la URL del documento');
                              }
                            } catch {
                              toast.error('Error al cargar el documento');
                            }
                          }}
                        >
                          <Eye className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Risk Assessment */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Shield className="h-5 w-5" />
                Evaluación de Riesgo
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">Nivel de Riesgo</span>
                <Badge
                  variant="outline"
                  className={
                    profile.riskLevel <= 2
                      ? 'border-green-500 text-green-700'
                      : profile.riskLevel <= 3
                        ? 'border-yellow-500 text-yellow-700'
                        : 'border-red-500 text-red-700'
                  }
                >
                  {profile.riskLevelName}
                </Badge>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">Puntuación</span>
                <span className="font-medium">{profile.riskScore}/100</span>
              </div>
              {profile.riskFactors && (
                <div>
                  <p className="text-muted-foreground mb-2 text-sm">Factores de Riesgo</p>
                  <p className="text-sm">{profile.riskFactors.join(', ') || 'Ninguno'}</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* PEP Warning */}
          {profile.isPEP && (
            <Card className="border-red-200 bg-red-50">
              <CardContent className="pt-6">
                <div className="flex items-center gap-2 text-red-700">
                  <AlertTriangle className="h-5 w-5" />
                  <span className="font-medium">Persona Políticamente Expuesta (PEP)</span>
                </div>
                {profile.pepPosition && (
                  <p className="mt-2 text-sm text-red-600">Cargo: {profile.pepPosition}</p>
                )}
              </CardContent>
            </Card>
          )}

          {/* Verification Status */}
          <Card>
            <CardHeader>
              <CardTitle>Estado de Verificación</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground text-sm">Identidad Verificada</span>
                {profile.isIdentityVerified ? (
                  <CheckCircle className="h-5 w-5 text-green-600" />
                ) : (
                  <XCircle className="text-muted-foreground h-5 w-5" />
                )}
              </div>
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground text-sm">Dirección Verificada</span>
                {profile.isAddressVerified ? (
                  <CheckCircle className="h-5 w-5 text-green-600" />
                ) : (
                  <XCircle className="text-muted-foreground h-5 w-5" />
                )}
              </div>
            </CardContent>
          </Card>

          {/* Already processed info */}
          {profile.status === KYCStatus.Approved && profile.approvedAt && (
            <Card className="border-green-200 bg-green-50">
              <CardContent className="pt-6">
                <div className="flex items-center gap-2 text-green-700">
                  <CheckCircle className="h-5 w-5" />
                  <span className="font-medium">Aprobado</span>
                </div>
                <p className="mt-2 text-sm text-green-600">
                  Por {profile.approvedByName || 'Sistema'} el{' '}
                  {new Date(profile.approvedAt).toLocaleDateString('es-DO')}
                </p>
              </CardContent>
            </Card>
          )}

          {profile.status === KYCStatus.Rejected && profile.rejectedAt && (
            <Card className="border-red-200 bg-red-50">
              <CardContent className="pt-6">
                <div className="flex items-center gap-2 text-red-700">
                  <XCircle className="h-5 w-5" />
                  <span className="font-medium">Rechazado</span>
                </div>
                <p className="mt-2 text-sm text-red-600">
                  Por {profile.rejectedByName || 'Sistema'} el{' '}
                  {new Date(profile.rejectedAt).toLocaleDateString('es-DO')}
                </p>
                {profile.rejectionReason && (
                  <p className="mt-1 text-sm text-red-600">Razón: {profile.rejectionReason}</p>
                )}
              </CardContent>
            </Card>
          )}

          {/* Actions */}
          {canProcess && (
            <Card>
              <CardHeader>
                <CardTitle>Acciones</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <p className="text-muted-foreground mb-2 text-sm">Notas del Administrador</p>
                  <Textarea
                    placeholder="Agregar notas internas..."
                    value={adminNotes}
                    onChange={e => setAdminNotes(e.target.value)}
                    rows={3}
                  />
                </div>
                <div>
                  <p className="text-muted-foreground mb-2 text-sm">
                    Razón de Rechazo (requerido para rechazar)
                  </p>
                  <Textarea
                    placeholder="Especificar razón del rechazo..."
                    value={rejectReason}
                    onChange={e => setRejectReason(e.target.value)}
                    rows={3}
                  />
                </div>
                <div className="flex flex-col gap-2">
                  <Button
                    className="bg-primary hover:bg-primary/90 w-full"
                    onClick={handleApprove}
                    disabled={processingAction}
                  >
                    {processingAction ? (
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    ) : (
                      <CheckCircle className="mr-2 h-4 w-4" />
                    )}
                    Aprobar
                  </Button>
                  <Button
                    variant="destructive"
                    className="w-full"
                    onClick={handleReject}
                    disabled={processingAction || !rejectReason.trim()}
                  >
                    {processingAction ? (
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    ) : (
                      <XCircle className="mr-2 h-4 w-4" />
                    )}
                    Rechazar
                  </Button>
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
