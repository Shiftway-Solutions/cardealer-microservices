/**
 * Admin KYC Queue Page - Split View (Master-Detail Pattern)
 *
 * Professional layout used by Stripe, Persona, Intercom:
 * - Left panel: Compact list of KYC requests
 * - Right panel: Full detail view of selected request
 * - No popups - everything in one view
 *
 * Sub-components extracted to ./components.tsx for code splitting.
 * Helpers and constants extracted to ./helpers.ts.
 */

'use client';

import { useState, useEffect, useCallback } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { ScrollArea } from '@/components/ui/scroll-area';
import { KYCStatusBadge } from '@/components/dashboard';
import { cn } from '@/lib/utils';
import {
  Search,
  Clock,
  CheckCircle,
  XCircle,
  Eye,
  User,
  FileText,
  AlertTriangle,
  Calendar,
  Loader2,
  Mail,
  Phone,
  MapPin,
  Shield,
  ExternalLink,
  Image as ImageIcon,
  ZoomIn,
  Maximize2,
  Download,
} from 'lucide-react';
import { useAuth } from '@/hooks/use-auth';
import { toast } from 'sonner';
import {
  KYCStatus,
  type KYCProfileSummary,
  type KYCProfile,
  type KYCStatistics,
  getAdminKYCProfiles,
  getKYCStatistics,
  getKYCProfileById,
  approveKYCProfile,
  rejectKYCProfile,
  getDocumentFreshUrl,
  extractBiometricScores,
  type BiometricScores,
} from '@/services/kyc';

// Extracted sub-components and helpers
import {
  filterKeyToStatus,
  statusToFilterKey,
  getInitials,
  formatDate,
  FIELD_VALIDATION_KEYS,
  VERIFICATION_ITEMS,
} from './helpers';
import {
  StatCard,
  ProfileListItem,
  EmptyDetailPanel,
  LoadingDetailPanel,
  DocumentImage,
  ValidatableField,
  VerificationChecklist,
  BiometricResultsCard,
  ImageViewer,
} from './components';

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function AdminKycPage() {
  const { user, isLoading: authLoading } = useAuth();

  // Admin role validation
  const isAdmin = user?.accountType === 'admin';

  if (authLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <Loader2 className="text-primary h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user) {
    return (
      <div className="flex h-screen flex-col items-center justify-center p-8">
        <AlertTriangle className="mb-4 h-12 w-12 text-amber-600" />
        <h1 className="text-xl font-bold">Acceso Requerido</h1>
        <p className="text-muted-foreground mt-2">
          Debes iniciar sesión para acceder a esta página
        </p>
      </div>
    );
  }

  if (!isAdmin) {
    return (
      <div className="flex h-screen flex-col items-center justify-center p-8">
        <Shield className="mb-4 h-12 w-12 text-red-600" />
        <h1 className="text-xl font-bold">Acceso Denegado</h1>
        <p className="text-muted-foreground mt-2">
          Solo los administradores pueden acceder al panel de KYC
        </p>
      </div>
    );
  }

  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [selectedProfileId, setSelectedProfileId] = useState<string | null>(null);
  const [selectedProfile, setSelectedProfile] = useState<KYCProfile | null>(null);
  const [profiles, setProfiles] = useState<KYCProfileSummary[]>([]);
  const [statistics, setStatistics] = useState<KYCStatistics | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadingProfile, setLoadingProfile] = useState(false);
  const [processingAction, setProcessingAction] = useState(false);
  const [loadingDocumentId, setLoadingDocumentId] = useState<string | null>(null);
  const [rejectionReason, setRejectionReason] = useState('');
  const [adminNotes, setAdminNotes] = useState('');
  // Verification checklist state
  const [verificationChecks, setVerificationChecks] = useState<Record<string, boolean>>({});
  // Biometric scores from Amazon Rekognition
  const [biometricScores, setBiometricScores] = useState<BiometricScores | null>(null);
  // Image viewer state
  const [viewerOpen, setViewerOpen] = useState(false);
  const [viewerImages, setViewerImages] = useState<
    Array<{ id: string; url: string; name: string }>
  >([]);
  const [viewerIndex, setViewerIndex] = useState(0);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  // Fetch profiles
  const fetchProfiles = useCallback(
    async (silent = false) => {
      try {
        if (!silent) setLoading(true);
        const status = statusFilter === 'all' ? null : filterKeyToStatus(statusFilter);
        const result = await getAdminKYCProfiles({ page, pageSize: 50, status });
        setProfiles(result.items || []);
        setTotalPages(result.totalPages || 1);
      } catch (error: unknown) {
        if (!silent) {
          console.error('Error fetching KYC profiles:', error);
          toast.error('Error al cargar los perfiles KYC');
          setProfiles([]);
        }
      } finally {
        if (!silent) setLoading(false);
      }
    },
    [statusFilter, page]
  );

  // Fetch statistics
  const fetchStatistics = useCallback(async () => {
    try {
      const stats = await getKYCStatistics();
      setStatistics(stats);
    } catch {
      setStatistics({
        totalProfiles: 0,
        pendingProfiles: 0,
        approvedProfiles: 0,
        rejectedProfiles: 0,
        inProgressProfiles: 0,
      });
    }
  }, []);

  useEffect(() => {
    fetchProfiles();
    fetchStatistics();
  }, [fetchProfiles, fetchStatistics]);

  // Silent background polling (professional pattern: Stripe, Vercel, AWS)
  useEffect(() => {
    const profilesInterval = setInterval(() => {
      if (!document.hidden) fetchProfiles(true);
    }, 30_000);

    const statsInterval = setInterval(() => {
      if (!document.hidden) fetchStatistics();
    }, 60_000);

    return () => {
      clearInterval(profilesInterval);
      clearInterval(statsInterval);
    };
  }, [fetchProfiles, fetchStatistics]);

  // Load profile details when selected
  useEffect(() => {
    if (!selectedProfileId) {
      setSelectedProfile(null);
      return;
    }

    const loadProfile = async () => {
      try {
        setLoadingProfile(true);
        const profile = await getKYCProfileById(selectedProfileId);
        setSelectedProfile(profile);
        setAdminNotes('');
        setRejectionReason('');

        // Extract biometric scores from verifications
        const scores = profile.verifications?.length
          ? extractBiometricScores(profile.verifications)
          : null;
        setBiometricScores(scores);

        // Auto-fill checklist items based on biometric scores
        const autoChecks: Record<string, boolean> = {};
        if (scores?.hasResults) {
          if (scores.faceMatch) {
            autoChecks['photoMatchesSelfie'] = scores.faceMatch.passed;
          }
          if (scores.liveness) {
            autoChecks['livenessGenuine'] = scores.liveness.passed;
            autoChecks['livenessComplete'] = scores.liveness.passed;
          }
        }
        setVerificationChecks(autoChecks); // Pre-fill from AI, staff can override
      } catch {
        toast.error('Error al cargar los detalles');
        setSelectedProfile(null);
      } finally {
        setLoadingProfile(false);
      }
    };

    loadProfile();
  }, [selectedProfileId]);

  // Handle open document with fresh URL
  const handleOpenDocument = async (documentId: string) => {
    try {
      setLoadingDocumentId(documentId);

      const response = await getDocumentFreshUrl(documentId);
      if (!response.url) {
        toast.error('URL del documento no disponible');
        return;
      }
      window.open(response.url, '_blank', 'noopener,noreferrer');
    } catch (error: unknown) {
      const err = error as { response?: { status?: number }; message?: string };
      console.error('[KYC] Error getting document URL:', err);

      if (err.response?.status === 404) {
        toast.error('El documento no existe en el almacenamiento');
      } else if (err.response?.status === 403) {
        toast.error('No tienes permiso para acceder a este documento');
      } else {
        toast.error('Error al obtener el documento. Intente nuevamente.');
      }
    } finally {
      setLoadingDocumentId(null);
    }
  };

  // Handle download document
  const handleDownloadDocument = async (documentId: string, documentName: string) => {
    try {
      setLoadingDocumentId(documentId);
      const response = await getDocumentFreshUrl(documentId);

      if (!response.url) {
        toast.error('URL del documento no disponible');
        return;
      }

      // Create a temporary link and trigger download
      const link = document.createElement('a');
      link.href = response.url;
      link.download = documentName || `documento-kyc-${documentId}`;
      link.target = '_blank';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);

      toast.success('Descarga iniciada');
    } catch (error: unknown) {
      const err = error as { response?: { status?: number }; message?: string };
      console.error('[KYC Debug] Error downloading document:', err);

      if (err.response?.status === 404) {
        toast.error('El documento no existe en el almacenamiento');
      } else if (err.response?.status === 403) {
        toast.error('No tienes permiso para descargar este documento');
      } else {
        toast.error('Error al descargar el documento');
      }
    } finally {
      setLoadingDocumentId(null);
    }
  };

  // Handle approve
  const handleApprove = async () => {
    if (!selectedProfile || !user) return;

    // Check if verification checklist is complete
    const checklistComplete =
      Object.keys(verificationChecks).length > 0 &&
      VERIFICATION_ITEMS.every(item => verificationChecks[item.key] === true);

    if (!checklistComplete) {
      toast.error('Debes completar el checklist de verificación antes de aprobar');
      return;
    }

    try {
      setProcessingAction(true);
      await approveKYCProfile(
        selectedProfile.id,
        user.id,
        `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email,
        adminNotes || undefined
      );
      toast.success('Perfil KYC aprobado exitosamente');
      setSelectedProfileId(null);
      setVerificationChecks({});
      fetchProfiles(true);
      fetchStatistics();
    } catch {
      toast.error('Error al aprobar el perfil');
    } finally {
      setProcessingAction(false);
    }
  };

  // Handle reject
  const handleReject = async () => {
    if (!selectedProfile || !user || !rejectionReason.trim()) {
      toast.error('Debes proporcionar una razón para el rechazo');
      return;
    }

    try {
      setProcessingAction(true);
      await rejectKYCProfile(
        selectedProfile.id,
        user.id,
        `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email,
        rejectionReason,
        adminNotes || undefined
      );
      toast.success('Perfil KYC rechazado');
      setSelectedProfileId(null);
      setVerificationChecks({});
      fetchProfiles(true);
      fetchStatistics();
    } catch {
      toast.error('Error al rechazar el perfil');
    } finally {
      setProcessingAction(false);
    }
  };

  // Handle toggle verification check
  const handleToggleCheck = (key: string) => {
    setVerificationChecks(prev => ({ ...prev, [key]: !prev[key] }));
  };

  // Open image viewer with all documents
  const handleOpenViewer = async (startIndex: number) => {
    if (!selectedProfile?.documents) return;

    try {
      // Load fresh URLs for all documents
      const images: Array<{ id: string; url: string; name: string }> = [];

      for (const doc of selectedProfile.documents) {
        if (doc.fileUrl || doc.storageKey) {
          try {
            const response = await getDocumentFreshUrl(doc.id);
            images.push({
              id: doc.id,
              url: response.url,
              name: doc.typeName || doc.documentName || 'Documento',
            });
          } catch (loadError: unknown) {
            // Log detailed error
            const err = loadError as {
              response?: { status?: number; data?: unknown };
              message?: string;
            };
            console.error(`[KYC Viewer Debug] Failed to load document ${doc.id}:`, {
              status: err.response?.status,
              data: err.response?.data,
              message: err.message,
            });
          }
        }
      }

      if (images.length > 0) {
        setViewerImages(images);
        setViewerIndex(Math.min(startIndex, images.length - 1));
        setViewerOpen(true);
      } else {
        toast.error('No se pudieron cargar los documentos');
      }
    } catch (error) {
      console.error('Error loading documents:', error);
      toast.error('Error al cargar los documentos');
    }
  };

  // Filter profiles
  const filteredProfiles = profiles.filter(profile => {
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      return (
        profile.fullName?.toLowerCase().includes(query) ||
        profile.documentNumber?.toLowerCase().includes(query)
      );
    }
    return true;
  });

  const canTakeAction = !!(
    selectedProfile &&
    (selectedProfile.status === KYCStatus.Pending ||
      selectedProfile.status === KYCStatus.UnderReview ||
      selectedProfile.status === KYCStatus.InProgress)
  );

  return (
    <div className="flex h-[calc(100vh-8rem)] flex-col">
      {/* Header */}
      <div className="pb-4">
        <h1 className="text-2xl font-bold">Verificación KYC</h1>
        <p className="text-muted-foreground text-sm">Cola de verificación de identidad</p>
      </div>

      {/* Stats */}
      <div className="mb-4 rounded-xl bg-gray-50/50 p-3">
        <div className="grid grid-cols-4 gap-3">
          <StatCard
            icon={Clock}
            value={statistics?.pendingProfiles ?? 0}
            label="Pendientes"
            color="amber"
            isActive={statusFilter === 'pending'}
            onClick={() => setStatusFilter(statusFilter === 'pending' ? 'all' : 'pending')}
          />
          <StatCard
            icon={Eye}
            value={statistics?.inProgressProfiles ?? 0}
            label="En Progreso"
            color="blue"
            isActive={statusFilter === 'in_progress'}
            onClick={() => setStatusFilter(statusFilter === 'in_progress' ? 'all' : 'in_progress')}
          />
          <StatCard
            icon={CheckCircle}
            value={statistics?.approvedProfiles ?? 0}
            label="Aprobados"
            color="primary"
            isActive={statusFilter === 'approved'}
            onClick={() => setStatusFilter(statusFilter === 'approved' ? 'all' : 'approved')}
          />
          <StatCard
            icon={XCircle}
            value={statistics?.rejectedProfiles ?? 0}
            label="Rechazados"
            color="red"
            isActive={statusFilter === 'rejected'}
            onClick={() => setStatusFilter(statusFilter === 'rejected' ? 'all' : 'rejected')}
          />
        </div>
      </div>

      {/* Main Split View */}
      <div className="flex flex-1 overflow-hidden rounded-xl bg-white shadow-sm">
        {/* Left Panel - List */}
        <div className="flex w-80 flex-shrink-0 flex-col bg-gray-50/70 shadow-[1px_0_0_0_rgba(0,0,0,0.05)]">
          {/* Search */}
          <div className="p-3 pb-2">
            <div className="relative">
              <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
              <Input
                placeholder="Buscar..."
                className="h-9 border-0 bg-white pl-9 shadow-sm"
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
              />
            </div>
          </div>

          {/* List */}
          <ScrollArea className="flex-1">
            {loading ? (
              <div className="flex items-center justify-center py-12">
                <Loader2 className="text-primary h-6 w-6 animate-spin" />
              </div>
            ) : filteredProfiles.length === 0 ? (
              <div className="p-6 text-center">
                <FileText className="text-muted-foreground mx-auto h-8 w-8" />
                <p className="text-muted-foreground mt-2 text-sm">No hay solicitudes</p>
              </div>
            ) : (
              filteredProfiles.map(profile => (
                <ProfileListItem
                  key={profile.id}
                  profile={profile}
                  isSelected={selectedProfileId === profile.id}
                  onClick={() => setSelectedProfileId(profile.id)}
                />
              ))
            )}
          </ScrollArea>

          {/* List Footer */}
          {totalPages > 1 && (
            <div className="flex flex-col gap-2 border-t border-gray-100 bg-white p-3">
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground text-xs">
                  Página {page} de {totalPages}
                </span>
                <span className="text-muted-foreground text-xs">
                  {filteredProfiles.length} perfiles
                </span>
              </div>
              <div className="flex gap-1">
                <Button
                  variant="outline"
                  size="sm"
                  className="flex-1"
                  disabled={page <= 1}
                  onClick={() => setPage(p => Math.max(1, p - 1))}
                >
                  ← Anterior
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  className="flex-1"
                  disabled={page >= totalPages}
                  onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                >
                  Siguiente →
                </Button>
              </div>
            </div>
          )}
        </div>

        {/* Right Panel - Detail */}
        <div className="flex flex-1 flex-col overflow-hidden">
          {loadingProfile ? (
            <LoadingDetailPanel />
          ) : !selectedProfile ? (
            <EmptyDetailPanel />
          ) : (
            <>
              {/* Detail Header */}
              <div className="flex items-center justify-between border-b border-gray-100 bg-gray-50/30 p-4">
                <div className="flex items-center gap-4">
                  <Avatar className="h-14 w-14">
                    <AvatarFallback className="bg-primary/10 text-primary text-lg">
                      {getInitials(selectedProfile.fullName)}
                    </AvatarFallback>
                  </Avatar>
                  <div>
                    <h2 className="text-xl font-semibold">
                      {selectedProfile.fullName || 'Sin nombre'}
                    </h2>
                    <div className="mt-1 flex items-center gap-2">
                      <KYCStatusBadge status={statusToFilterKey(selectedProfile.status)} />
                      {selectedProfile.isPEP && (
                        <Badge className="bg-red-100 text-red-700">
                          <AlertTriangle className="mr-1 h-3 w-3" />
                          PEP
                        </Badge>
                      )}
                      <Badge
                        className={cn(
                          selectedProfile.riskLevel <= 2
                            ? 'bg-green-100 text-green-700'
                            : selectedProfile.riskLevel <= 3
                              ? 'bg-yellow-100 text-yellow-700'
                              : 'bg-red-100 text-red-700'
                        )}
                      >
                        Riesgo: {selectedProfile.riskLevelName || 'N/A'}
                      </Badge>
                    </div>
                  </div>
                </div>
              </div>

              {/* Detail Content - Two Column Layout */}
              <div className="flex flex-1 overflow-hidden">
                {/* Left Column - User Data with Validation */}
                <div className="flex w-1/2 flex-col border-r border-gray-100">
                  <ScrollArea className="flex-1">
                    <div className="space-y-4 p-4">
                      {/* Personal Information with Validation */}
                      <Card className="shadow-sm">
                        <CardHeader className="pb-2">
                          <CardTitle className="flex items-center gap-2 text-base">
                            <User className="h-4 w-4" />
                            Datos Personales
                          </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-1">
                          <ValidatableField
                            icon={User}
                            label="Nombre Completo"
                            value={selectedProfile.fullName}
                            checkKey="nameMatches"
                            checked={verificationChecks['nameMatches']}
                            onToggle={() => handleToggleCheck('nameMatches')}
                            canValidate={canTakeAction}
                          />
                          <ValidatableField
                            icon={FileText}
                            label="Número de Documento"
                            value={selectedProfile.primaryDocumentNumber}
                            checkKey="documentNumberMatches"
                            checked={verificationChecks['documentNumberMatches']}
                            onToggle={() => handleToggleCheck('documentNumberMatches')}
                            canValidate={canTakeAction}
                            mono
                          />
                          <ValidatableField
                            icon={Calendar}
                            label="Fecha de Nacimiento"
                            value={formatDate(selectedProfile.dateOfBirth)}
                            checkKey="dobValid"
                            checked={verificationChecks['dobValid']}
                            onToggle={() => handleToggleCheck('dobValid')}
                            canValidate={canTakeAction}
                          />
                          <ValidatableField
                            icon={Shield}
                            label="Nacionalidad"
                            value={selectedProfile.nationality}
                            checkKey="nationalityValid"
                            checked={verificationChecks['nationalityValid']}
                            onToggle={() => handleToggleCheck('nationalityValid')}
                            canValidate={canTakeAction}
                          />
                        </CardContent>
                      </Card>

                      {/* Contact Information */}
                      <Card className="shadow-sm">
                        <CardHeader className="pb-2">
                          <CardTitle className="flex items-center gap-2 text-base">
                            <Mail className="h-4 w-4" />
                            Contacto
                          </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-1">
                          <ValidatableField
                            icon={Mail}
                            label="Email"
                            value={selectedProfile.email}
                            checkKey="emailValid"
                            checked={verificationChecks['emailValid']}
                            onToggle={() => handleToggleCheck('emailValid')}
                            canValidate={canTakeAction}
                          />
                          <ValidatableField
                            icon={Phone}
                            label="Teléfono"
                            value={selectedProfile.phone || selectedProfile.phoneNumber}
                            checkKey="phoneValid"
                            checked={verificationChecks['phoneValid']}
                            onToggle={() => handleToggleCheck('phoneValid')}
                            canValidate={canTakeAction}
                          />
                          <ValidatableField
                            icon={MapPin}
                            label="Dirección"
                            value={
                              [
                                selectedProfile.address,
                                selectedProfile.city,
                                selectedProfile.province,
                              ]
                                .filter(Boolean)
                                .join(', ') || 'No especificada'
                            }
                            checkKey="addressValid"
                            checked={verificationChecks['addressValid']}
                            onToggle={() => handleToggleCheck('addressValid')}
                            canValidate={canTakeAction}
                          />
                        </CardContent>
                      </Card>

                      {/* Document Validation */}
                      <Card className="shadow-sm">
                        <CardHeader className="pb-2">
                          <CardTitle className="flex items-center gap-2 text-base">
                            <FileText className="h-4 w-4" />
                            Validación de Documentos
                          </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-1">
                          <ValidatableField
                            icon={FileText}
                            label="Documento parece auténtico"
                            value="Sin ediciones visibles"
                            checkKey="documentAuthentic"
                            checked={verificationChecks['documentAuthentic']}
                            onToggle={() => handleToggleCheck('documentAuthentic')}
                            canValidate={canTakeAction}
                          />
                          <ValidatableField
                            icon={Calendar}
                            label="Documento no expirado"
                            value="Verificar fecha de vencimiento"
                            checkKey="documentNotExpired"
                            checked={verificationChecks['documentNotExpired']}
                            onToggle={() => handleToggleCheck('documentNotExpired')}
                            canValidate={canTakeAction}
                          />
                          <ValidatableField
                            icon={Shield}
                            label="Formato válido (cédula RD)"
                            value="Verificar formato oficial"
                            checkKey="documentFormatValid"
                            checked={verificationChecks['documentFormatValid']}
                            onToggle={() => handleToggleCheck('documentFormatValid')}
                            canValidate={canTakeAction}
                          />
                        </CardContent>
                      </Card>

                      {/* Amazon Rekognition Biometric Results */}
                      <BiometricResultsCard
                        scores={
                          biometricScores ?? {
                            faceMatch: null,
                            liveness: null,
                            documentOcr: null,
                            overallScore: null,
                            hasResults: false,
                          }
                        }
                      />

                      {/* Identity Validation */}
                      <Card className="shadow-sm">
                        <CardHeader className="pb-2">
                          <CardTitle className="flex items-center gap-2 text-base">
                            <Eye className="h-4 w-4" />
                            Validación de Identidad
                          </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-1">
                          <ValidatableField
                            icon={User}
                            label="Foto coincide con selfie"
                            value={
                              biometricScores?.faceMatch
                                ? `Score: ${biometricScores.faceMatch.score}% — ${biometricScores.faceMatch.passed ? 'Coincide ✅' : 'No coincide ❌'}`
                                : 'Comparar rostros manualmente'
                            }
                            checkKey="photoMatchesSelfie"
                            checked={verificationChecks['photoMatchesSelfie']}
                            onToggle={() => handleToggleCheck('photoMatchesSelfie')}
                            canValidate={canTakeAction}
                          />
                          <ValidatableField
                            icon={Eye}
                            label="Selfie genuino"
                            value={
                              biometricScores?.liveness
                                ? `Score: ${biometricScores.liveness.score}% — ${biometricScores.liveness.passed ? 'Genuino ✅' : 'Sospechoso ❌'}`
                                : 'No es foto de foto'
                            }
                            checkKey="livenessGenuine"
                            checked={verificationChecks['livenessGenuine']}
                            onToggle={() => handleToggleCheck('livenessGenuine')}
                            canValidate={canTakeAction}
                          />
                          <ValidatableField
                            icon={CheckCircle}
                            label="Prueba de vida completada"
                            value={
                              biometricScores?.liveness
                                ? `Score: ${biometricScores.liveness.score}% — ${biometricScores.liveness.passed ? 'Completada ✅' : 'Fallida ❌'}`
                                : 'Verificar gestos'
                            }
                            checkKey="livenessComplete"
                            checked={verificationChecks['livenessComplete']}
                            onToggle={() => handleToggleCheck('livenessComplete')}
                            canValidate={canTakeAction}
                          />
                        </CardContent>
                      </Card>

                      {/* PEP Warning */}
                      {selectedProfile.isPEP && (
                        <Card className="border-red-200 bg-red-50">
                          <CardContent className="p-4">
                            <div className="flex items-center gap-2 text-red-700">
                              <AlertTriangle className="h-5 w-5" />
                              <span className="font-medium">
                                Persona Políticamente Expuesta (PEP)
                              </span>
                            </div>
                            {selectedProfile.pepPosition && (
                              <p className="mt-2 text-sm text-red-600">
                                Cargo: {selectedProfile.pepPosition}
                              </p>
                            )}
                          </CardContent>
                        </Card>
                      )}

                      {/* Verification Result */}
                      {selectedProfile.status === KYCStatus.Approved && (
                        <Card className="border-green-200 bg-green-50">
                          <CardContent className="p-4">
                            <div className="flex items-center gap-2 text-green-700">
                              <CheckCircle className="h-5 w-5" />
                              <span className="font-medium">Aprobado</span>
                            </div>
                            <p className="mt-2 text-sm text-green-600">
                              Por {selectedProfile.approvedByName || 'Sistema'} el{' '}
                              {formatDate(selectedProfile.approvedAt)}
                            </p>
                          </CardContent>
                        </Card>
                      )}

                      {selectedProfile.status === KYCStatus.Rejected && (
                        <Card className="border-red-200 bg-red-50">
                          <CardContent className="p-4">
                            <div className="flex items-center gap-2 text-red-700">
                              <XCircle className="h-5 w-5" />
                              <span className="font-medium">Rechazado</span>
                            </div>
                            <p className="mt-2 text-sm text-red-600">
                              Por {selectedProfile.rejectedByName || 'Sistema'} el{' '}
                              {formatDate(selectedProfile.rejectedAt)}
                            </p>
                            {selectedProfile.rejectionReason && (
                              <p className="mt-1 text-sm text-red-600">
                                Razón: {selectedProfile.rejectionReason}
                              </p>
                            )}
                          </CardContent>
                        </Card>
                      )}

                      {/* Validation Progress */}
                      {canTakeAction && (
                        <Card className="bg-gray-50 shadow-sm">
                          <CardContent className="p-4">
                            <div className="mb-2 flex items-center justify-between">
                              <span className="text-sm font-medium">Progreso de Validación</span>
                              <span className="text-muted-foreground text-sm">
                                {Object.values(verificationChecks).filter(Boolean).length} /{' '}
                                {FIELD_VALIDATION_KEYS.length}
                              </span>
                            </div>
                            <div className="h-2 w-full rounded-full bg-gray-200">
                              <div
                                className="bg-primary h-2 rounded-full transition-all"
                                style={{
                                  width: `${(Object.values(verificationChecks).filter(Boolean).length / FIELD_VALIDATION_KEYS.length) * 100}%`,
                                }}
                              />
                            </div>
                          </CardContent>
                        </Card>
                      )}
                    </div>
                  </ScrollArea>
                </div>

                {/* Right Column - All Images Visible */}
                <div className="flex w-1/2 flex-col bg-gray-50">
                  <div className="flex items-center justify-between border-b border-gray-100 p-3">
                    <h3 className="flex items-center gap-2 font-medium">
                      <ImageIcon className="h-4 w-4" />
                      Documentos ({selectedProfile.documents?.length || 0})
                    </h3>
                    {selectedProfile.documents && selectedProfile.documents.length > 0 && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleOpenViewer(0)}
                        className="gap-1"
                      >
                        <Maximize2 className="h-3 w-3" />
                        Pantalla Completa
                      </Button>
                    )}
                  </div>
                  <ScrollArea className="flex-1">
                    <div className="p-3">
                      {selectedProfile.documents && selectedProfile.documents.length > 0 ? (
                        <div className="grid grid-cols-1 gap-4">
                          {selectedProfile.documents.map((doc, index) => (
                            <div
                              key={doc.id}
                              className="overflow-hidden rounded-lg bg-white shadow-sm"
                            >
                              {/* Document Header */}
                              <div className="flex items-center justify-between bg-gray-50/80 px-3 py-2">
                                <div className="flex items-center gap-2">
                                  <Badge variant="secondary" className="text-xs">
                                    {doc.typeName || doc.documentName}
                                    {doc.side && ` (${doc.side})`}
                                  </Badge>
                                  <Badge
                                    variant="outline"
                                    className={cn(
                                      'text-xs',
                                      doc.statusName === 'Verified'
                                        ? 'border-primary text-primary'
                                        : doc.statusName === 'Rejected'
                                          ? 'border-red-200 text-red-700'
                                          : 'border-amber-200 text-amber-700'
                                    )}
                                  >
                                    {doc.statusName}
                                  </Badge>
                                </div>
                                <div className="flex gap-1">
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => handleOpenViewer(index)}
                                    title="Ver en pantalla completa con zoom (+/-)"
                                    className="h-7 w-7 p-0 hover:bg-blue-50 hover:text-blue-600"
                                  >
                                    <ZoomIn className="h-3.5 w-3.5" />
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => handleOpenDocument(doc.id)}
                                    disabled={loadingDocumentId === doc.id}
                                    title="Abrir imagen en nueva pestaña"
                                    className="h-7 w-7 p-0 hover:bg-purple-50 hover:text-purple-600"
                                  >
                                    {loadingDocumentId === doc.id ? (
                                      <Loader2 className="h-3.5 w-3.5 animate-spin" />
                                    ) : (
                                      <ExternalLink className="h-3.5 w-3.5" />
                                    )}
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() =>
                                      handleDownloadDocument(
                                        doc.id,
                                        doc.documentName || doc.typeName || 'documento'
                                      )
                                    }
                                    disabled={loadingDocumentId === doc.id}
                                    title="Descargar documento"
                                    className="hover:bg-primary/10 hover:text-primary h-7 w-7 p-0"
                                  >
                                    <Download className="h-3.5 w-3.5" />
                                  </Button>
                                </div>
                              </div>
                              {/* Document Image */}
                              <div className="relative flex min-h-[200px] items-center justify-center bg-gray-100">
                                <DocumentImage
                                  documentId={doc.id}
                                  alt={doc.documentName || doc.typeName || 'Documento'}
                                  className="max-h-[300px] max-w-full cursor-pointer transition-opacity hover:opacity-90"
                                />
                                {/* Clickable overlay to open viewer */}
                                <div
                                  className="absolute inset-0 cursor-pointer"
                                  onClick={() => handleOpenViewer(index)}
                                  title="Clic para ampliar"
                                />
                              </div>
                              {/* Extracted Data */}
                              {(doc.extractedNumber || doc.extractedName) && (
                                <div className="bg-blue-50/80 px-3 py-2 text-xs">
                                  {doc.extractedNumber && (
                                    <p>
                                      <span className="text-muted-foreground">
                                        Número extraído:
                                      </span>{' '}
                                      <span className="font-mono font-medium">
                                        {doc.extractedNumber}
                                      </span>
                                    </p>
                                  )}
                                  {doc.extractedName && (
                                    <p>
                                      <span className="text-muted-foreground">
                                        Nombre extraído:
                                      </span>{' '}
                                      <span className="font-medium">{doc.extractedName}</span>
                                    </p>
                                  )}
                                </div>
                              )}
                            </div>
                          ))}
                        </div>
                      ) : (
                        <div className="flex h-64 flex-col items-center justify-center text-gray-400">
                          <FileText className="mb-2 h-12 w-12" />
                          <span className="text-sm">No hay documentos subidos</span>
                        </div>
                      )}
                    </div>
                  </ScrollArea>
                </div>
              </div>

              {/* Action Footer */}
              {canTakeAction && (
                <div className="border-t border-gray-100 bg-gray-50 p-4">
                  <div className="mb-3 grid gap-3 md:grid-cols-2">
                    <div>
                      <label className="text-muted-foreground mb-1 block text-xs font-medium">
                        Notas del Administrador
                      </label>
                      <Textarea
                        placeholder="Agregar notas internas..."
                        value={adminNotes}
                        onChange={e => setAdminNotes(e.target.value)}
                        className="h-20 resize-none"
                      />
                    </div>
                    <div>
                      <label className="text-muted-foreground mb-1 block text-xs font-medium">
                        Razón de Rechazo (requerido para rechazar)
                      </label>
                      <Textarea
                        placeholder="Especificar razón del rechazo..."
                        value={rejectionReason}
                        onChange={e => setRejectionReason(e.target.value)}
                        className="h-20 resize-none"
                      />
                    </div>
                  </div>

                  {/* Checklist Warning */}
                  {Object.keys(verificationChecks).length > 0 &&
                    !VERIFICATION_ITEMS.every(item => verificationChecks[item.key]) && (
                      <div className="mb-3 flex items-center gap-2 rounded-md bg-amber-50 p-2 text-sm text-amber-700">
                        <AlertTriangle className="h-4 w-4 flex-shrink-0" />
                        <span>
                          Completa el checklist de verificación antes de aprobar (
                          {VERIFICATION_ITEMS.filter(item => verificationChecks[item.key]).length}/
                          {VERIFICATION_ITEMS.length} completados)
                        </span>
                      </div>
                    )}

                  <div className="flex gap-3">
                    <Button
                      className="bg-primary hover:bg-primary/90 flex-1"
                      onClick={handleApprove}
                      disabled={
                        processingAction ||
                        !VERIFICATION_ITEMS.every(item => verificationChecks[item.key])
                      }
                    >
                      {processingAction ? (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      ) : (
                        <CheckCircle className="mr-2 h-4 w-4" />
                      )}
                      Aprobar Verificación
                    </Button>
                    <Button
                      variant="destructive"
                      className="flex-1"
                      onClick={handleReject}
                      disabled={processingAction || !rejectionReason.trim()}
                    >
                      {processingAction ? (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      ) : (
                        <XCircle className="mr-2 h-4 w-4" />
                      )}
                      Rechazar
                    </Button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>

      {/* Image Viewer Modal */}
      {viewerOpen && viewerImages.length > 0 && (
        <ImageViewer
          images={viewerImages}
          currentIndex={viewerIndex}
          onClose={() => {
            setViewerOpen(false);
            setViewerImages([]);
          }}
          onNavigate={setViewerIndex}
        />
      )}
    </div>
  );
}
