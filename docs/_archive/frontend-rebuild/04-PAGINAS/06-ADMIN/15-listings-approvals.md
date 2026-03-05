---
title: "61 - Listings & Approvals Management"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["VehiclesSaleService", "AdminService", "MediaService"]
status: complete
last_updated: "2026-01-30"
---

# 📋 61 - Listings & Approvals Management

**Objetivo:** Gestión de publicaciones de vehículos y flujo de aprobación/rechazo para moderadores.

**Prioridad:** P0 (Crítica)  
**Complejidad:** 🟡 Media  
**Dependencias:** VehiclesSaleService, AdminService, MediaService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [AdminListingsPage](#-adminlistingspage)
3. [PendingApprovalsPage](#-pendingapprovalspage)
4. [Componentes Compartidos](#-componentes-compartidos)
5. [Servicios API](#-servicios-api)

---

## 🏗️ ARQUITECTURA

```
pages/admin/
├── AdminListingsPage.tsx           # Todas las publicaciones (329 líneas)
├── PendingApprovalsPage.tsx        # Cola de aprobación (455 líneas)
└── components/
    ├── ListingsTable.tsx           # Tabla de listings
    ├── VehiclePreviewModal.tsx     # Modal preview vehículo
    ├── ApprovalCard.tsx            # Card de aprobación
    └── RejectReasonModal.tsx       # Modal razón de rechazo
```

---

## 📊 ADMINLISTINGSPAGE

**Ruta:** `/admin/listings`

```typescript
// src/pages/admin/AdminListingsPage.tsx
import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { LocalizedContent } from '@/components/common';
import {
  FiEye,
  FiTrash2,
  FiCheck,
  FiX,
  FiSearch,
  FiFilter,
} from 'react-icons/fi';
import {
  getAllVehicles,
  approveVehicle,
  rejectVehicle,
  deleteVehicle,
  type Vehicle,
  type VehicleFilters,
} from '@/services/vehicleService';
import { generateVehicleUrl } from '@/utils/seoSlug';

type StatusFilter = 'all' | 'pending' | 'approved' | 'rejected' | 'sold';

interface Vehicle {
  id: string;
  title: string; // LocalizedContent
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  status: 'pending' | 'approved' | 'rejected' | 'sold';
  images: string[];
  sellerId: string;
  sellerName: string;
  createdAt: string;
  slug: string;
}

export default function AdminListingsPage() {
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [showFilters, setShowFilters] = useState(false);

  useEffect(() => {
    loadVehicles();
  }, [currentPage, statusFilter]);

  const loadVehicles = async () => {
    try {
      setIsLoading(true);
      const filters: VehicleFilters = {};
      if (searchQuery) filters.search = searchQuery;

      const response = await getAllVehicles(filters, currentPage, 20);

      // Filter by status on client side if needed
      let filteredVehicles = response.vehicles;
      if (statusFilter !== 'all') {
        filteredVehicles = response.vehicles.filter((v) => v.status === statusFilter);
      }

      setVehicles(filteredVehicles);
      setTotalPages(response.totalPages);
    } catch (error) {
      console.error('Error loading vehicles:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleApprove = async (id: string) => {
    if (!confirm('¿Aprobar esta publicación?')) return;

    try {
      await approveVehicle(id);
      loadVehicles();
    } catch {
      alert('Error al aprobar la publicación');
    }
  };

  const handleReject = async (id: string) => {
    const reason = prompt('Razón del rechazo (opcional):');

    try {
      await rejectVehicle(id, reason || undefined);
      loadVehicles();
    } catch {
      alert('Error al rechazar la publicación');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('¿Eliminar esta publicación permanentemente? Esta acción no se puede deshacer.')) {
      return;
    }

    try {
      await deleteVehicle(id);
      loadVehicles();
    } catch {
      alert('Error al eliminar la publicación');
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setCurrentPage(1);
    loadVehicles();
  };

  const getStatusBadge = (status: Vehicle['status']) => {
    const styles = {
      pending: 'bg-yellow-100 text-yellow-800',
      approved: 'bg-green-100 text-green-800',
      rejected: 'bg-red-100 text-red-800',
      sold: 'bg-blue-100 text-blue-800',
    };

    const labels = {
      pending: 'Pendiente',
      approved: 'Aprobado',
      rejected: 'Rechazado',
      sold: 'Vendido',
    };

    return (
      <span className={`px-2 py-1 text-xs font-medium rounded-full ${styles[status]}`}>
        {labels[status]}
      </span>
    );
  };

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('es-DO', {
      style: 'currency',
      currency: 'DOP',
      minimumFractionDigits: 0,
    }).format(price);
  };

  return (
    <div className="p-6">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Todas las Publicaciones</h1>
        <p className="text-gray-600">Gestionar todas las publicaciones de vehículos</p>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-card p-4 mb-6">
        <div className="flex flex-col md:flex-row gap-4">
          {/* Search */}
          <form onSubmit={handleSearch} className="flex-1">
            <div className="relative">
              <FiSearch className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Buscar publicaciones..."
                className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
              />
            </div>
          </form>

          {/* Status Filter */}
          <select
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value as StatusFilter);
              setCurrentPage(1);
            }}
            className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
          >
            <option value="all">Todos los estados</option>
            <option value="pending">Pendientes</option>
            <option value="approved">Aprobados</option>
            <option value="rejected">Rechazados</option>
            <option value="sold">Vendidos</option>
          </select>

          {/* Advanced Filters Toggle */}
          <button
            onClick={() => setShowFilters(!showFilters)}
            className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors flex items-center gap-2"
          >
            <FiFilter size={18} />
            Filtros
          </button>
        </div>

        {showFilters && (
          <div className="mt-4 pt-4 border-t border-gray-200 grid md:grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Marca</label>
              <input
                type="text"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Modelo</label>
              <input
                type="text"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Año</label>
              <input
                type="number"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg"
              />
            </div>
          </div>
        )}
      </div>

      {/* Stats Cards */}
      <div className="grid md:grid-cols-4 gap-4 mb-6">
        {[
          { label: 'Total', value: vehicles.length, color: 'blue' },
          { label: 'Pendientes', value: vehicles.filter((v) => v.status === 'pending').length, color: 'yellow' },
          { label: 'Aprobados', value: vehicles.filter((v) => v.status === 'approved').length, color: 'green' },
          { label: 'Vendidos', value: vehicles.filter((v) => v.status === 'sold').length, color: 'purple' },
        ].map((stat) => (
          <div key={stat.label} className="bg-white rounded-xl shadow-card p-4">
            <p className="text-sm text-gray-600 mb-1">{stat.label}</p>
            <p className="text-2xl font-bold text-gray-900">{stat.value}</p>
          </div>
        ))}
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl shadow-card overflow-hidden">
        {isLoading ? (
          <div className="p-12 text-center">
            <div className="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full mx-auto" />
            <p className="text-gray-600 mt-4">Cargando publicaciones...</p>
          </div>
        ) : vehicles.length === 0 ? (
          <div className="p-12 text-center">
            <p className="text-gray-600">No se encontraron publicaciones</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Vehículo
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Precio
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Estado
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Vendedor
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Fecha
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Acciones
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {vehicles.map((vehicle) => (
                  <tr key={vehicle.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        <img
                          src={vehicle.images[0]}
                          alt={vehicle.title}
                          className="w-16 h-12 object-cover rounded-lg"
                        />
                        <div>
                          <p className="font-medium text-gray-900">
                            <LocalizedContent content={vehicle.title} showBadge={false} />
                          </p>
                          <p className="text-sm text-gray-500">
                            {vehicle.year} • {vehicle.mileage.toLocaleString()} km
                          </p>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 font-semibold text-gray-900">
                      {formatPrice(vehicle.price)}
                    </td>
                    <td className="px-6 py-4">{getStatusBadge(vehicle.status)}</td>
                    <td className="px-6 py-4 text-gray-600">{vehicle.sellerName}</td>
                    <td className="px-6 py-4 text-sm text-gray-500">
                      {new Date(vehicle.createdAt).toLocaleDateString('es-DO')}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2">
                        <Link
                          to={generateVehicleUrl(vehicle)}
                          target="_blank"
                          className="p-2 text-blue-600 hover:bg-blue-50 rounded-lg"
                          title="Ver"
                        >
                          <FiEye />
                        </Link>
                        {vehicle.status === 'pending' && (
                          <>
                            <button
                              onClick={() => handleApprove(vehicle.id)}
                              className="p-2 text-green-600 hover:bg-green-50 rounded-lg"
                              title="Aprobar"
                            >
                              <FiCheck />
                            </button>
                            <button
                              onClick={() => handleReject(vehicle.id)}
                              className="p-2 text-red-600 hover:bg-red-50 rounded-lg"
                              title="Rechazar"
                            >
                              <FiX />
                            </button>
                          </>
                        )}
                        <button
                          onClick={() => handleDelete(vehicle.id)}
                          className="p-2 text-red-600 hover:bg-red-50 rounded-lg"
                          title="Eliminar"
                        >
                          <FiTrash2 />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 mt-6">
          <button
            onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
            disabled={currentPage === 1}
            className="px-4 py-2 border border-gray-300 rounded-lg disabled:opacity-50"
          >
            Anterior
          </button>
          <span className="text-gray-600">
            Página {currentPage} de {totalPages}
          </span>
          <button
            onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
            disabled={currentPage === totalPages}
            className="px-4 py-2 border border-gray-300 rounded-lg disabled:opacity-50"
          >
            Siguiente
          </button>
        </div>
      )}
    </div>
  );
}
```

---

## ✅ PENDINGAPPROVALSPAGE

**Ruta:** `/admin/pending-approvals`

```typescript
// src/pages/admin/PendingApprovalsPage.tsx
import { useState, useCallback } from 'react';
import { usePendingApprovalsPage } from '@/hooks';
import { LocalizedContent } from '@/components/common';
import type { PendingVehicle } from '@/types/admin';
import {
  FiCheck,
  FiX,
  FiEye,
  FiUser,
  FiCalendar,
  FiRefreshCw,
  FiChevronLeft,
  FiChevronRight,
} from 'react-icons/fi';

interface PendingVehicle {
  id: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  images: string[];
  description: string;
  sellerId: string;
  sellerName: string;
  sellerEmail: string;
  submittedAt: string;
  condition: 'new' | 'used' | 'certified';
  location: string;
}

const PendingApprovalsPage = () => {
  const {
    vehicles,
    total,
    isLoading,
    refetch,
    approveVehicle,
    rejectVehicle,
  } = usePendingApprovalsPage();

  const [selectedVehicle, setSelectedVehicle] = useState<PendingVehicle | null>(null);
  const [rejectReason, setRejectReason] = useState('');
  const [isProcessing, setIsProcessing] = useState(false);
  const [showDetailsModal, setShowDetailsModal] = useState(false);
  const [detailsVehicle, setDetailsVehicle] = useState<PendingVehicle | null>(null);
  const [currentImageIndex, setCurrentImageIndex] = useState(0);

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('es-DO', {
      style: 'currency',
      currency: 'DOP',
      minimumFractionDigits: 0,
    }).format(price);
  };

  const formatMileage = (mileage: number) => {
    return new Intl.NumberFormat('es-DO').format(mileage) + ' km';
  };

  const formatTimeAgo = (timestamp: string) => {
    const now = new Date();
    const date = new Date(timestamp);
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    if (seconds < 60) return 'ahora mismo';
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `hace ${minutes}m`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `hace ${hours}h`;
    const days = Math.floor(hours / 24);
    return `hace ${days}d`;
  };

  const handleViewDetails = (vehicle: PendingVehicle) => {
    setDetailsVehicle(vehicle);
    setShowDetailsModal(true);
    setCurrentImageIndex(0);
  };

  const handleCloseModal = () => {
    setShowDetailsModal(false);
    setDetailsVehicle(null);
    setRejectReason('');
    setCurrentImageIndex(0);
  };

  const handleApprove = useCallback(
    async (vehicleId: string) => {
      setIsProcessing(true);

      try {
        await approveVehicle.mutateAsync(vehicleId);
        refetch();
      } catch (error) {
        console.error('Error approving vehicle:', error);
      }

      setSelectedVehicle(null);
      setShowDetailsModal(false);
      setIsProcessing(false);
    },
    [approveVehicle, refetch]
  );

  const handleReject = useCallback(
    async (vehicleId: string) => {
      if (!rejectReason.trim()) {
        alert('Por favor proporciona una razón para el rechazo');
        return;
      }

      setIsProcessing(true);

      try {
        await rejectVehicle.mutateAsync({ vehicleId, reason: rejectReason });
        refetch();
      } catch (error) {
        console.error('Error rejecting vehicle:', error);
      }

      setSelectedVehicle(null);
      setRejectReason('');
      setShowDetailsModal(false);
      setIsProcessing(false);
    },
    [rejectVehicle, rejectReason, refetch]
  );

  return (
    <div>
      {/* Header */}
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Aprobaciones Pendientes</h1>
          <p className="text-gray-600 mt-1">
            {total} publicación{total !== 1 ? 'es' : ''} esperando revisión
          </p>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isLoading}
          className="flex items-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors disabled:opacity-50"
        >
          <FiRefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} />
          Actualizar
        </button>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="text-center py-8 text-gray-500">Cargando aprobaciones pendientes...</div>
      )}

      {/* Empty State */}
      {!isLoading && vehicles.length === 0 ? (
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
          <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <FiCheck className="w-8 h-8 text-green-600" />
          </div>
          <h3 className="text-lg font-semibold text-gray-900 mb-2">¡Todo al día!</h3>
          <p className="text-gray-600">No hay publicaciones pendientes de revisión.</p>
        </div>
      ) : (
        <div className="space-y-6">
          {vehicles.map((vehicle) => (
            <ApprovalCard
              key={vehicle.id}
              vehicle={vehicle}
              onViewDetails={() => handleViewDetails(vehicle)}
              onApprove={() => handleApprove(vehicle.id)}
              onReject={() => setSelectedVehicle(vehicle)}
              isProcessing={isProcessing}
              formatPrice={formatPrice}
              formatMileage={formatMileage}
              formatTimeAgo={formatTimeAgo}
            />
          ))}
        </div>
      )}

      {/* Details Modal */}
      {showDetailsModal && detailsVehicle && (
        <VehicleDetailsModal
          vehicle={detailsVehicle}
          currentImageIndex={currentImageIndex}
          setCurrentImageIndex={setCurrentImageIndex}
          onClose={handleCloseModal}
          onApprove={() => handleApprove(detailsVehicle.id)}
          onReject={() => setSelectedVehicle(detailsVehicle)}
          isProcessing={isProcessing}
          formatPrice={formatPrice}
          formatMileage={formatMileage}
        />
      )}

      {/* Reject Reason Modal */}
      {selectedVehicle && (
        <RejectReasonModal
          vehicleTitle={selectedVehicle.title}
          rejectReason={rejectReason}
          setRejectReason={setRejectReason}
          onSubmit={() => handleReject(selectedVehicle.id)}
          onClose={() => {
            setSelectedVehicle(null);
            setRejectReason('');
          }}
          isProcessing={isProcessing}
        />
      )}
    </div>
  );
};

// ApprovalCard Component
function ApprovalCard({
  vehicle,
  onViewDetails,
  onApprove,
  onReject,
  isProcessing,
  formatPrice,
  formatMileage,
  formatTimeAgo,
}: {
  vehicle: PendingVehicle;
  onViewDetails: () => void;
  onApprove: () => void;
  onReject: () => void;
  isProcessing: boolean;
  formatPrice: (price: number) => string;
  formatMileage: (mileage: number) => string;
  formatTimeAgo: (timestamp: string) => string;
}) {
  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      <div className="p-6">
        <div className="flex gap-6">
          {/* Vehicle Image */}
          <img
            src={vehicle.images[0]}
            alt={vehicle.title}
            className="w-48 h-36 rounded-lg object-cover flex-shrink-0"
          />

          {/* Vehicle Info */}
          <div className="flex-1">
            <div className="flex items-start justify-between mb-3">
              <div>
                <h3 className="text-xl font-semibold text-gray-900 mb-1">
                  <LocalizedContent content={vehicle.title} showBadge={false} />
                </h3>
                <p className="text-2xl font-bold text-primary">{formatPrice(vehicle.price)}</p>
              </div>
              <span className="px-3 py-1 bg-orange-100 text-orange-800 text-sm font-medium rounded-full">
                Pendiente de Revisión
              </span>
            </div>

            <div className="grid grid-cols-2 gap-4 mb-4">
              <div>
                <p className="text-sm text-gray-600">Kilometraje</p>
                <p className="font-medium text-gray-900">{formatMileage(vehicle.mileage)}</p>
              </div>
              <div>
                <p className="text-sm text-gray-600">Año</p>
                <p className="font-medium text-gray-900">{vehicle.year}</p>
              </div>
            </div>

            <div className="flex items-center gap-4 text-sm text-gray-600 mb-4">
              <div className="flex items-center gap-2">
                <FiUser className="w-4 h-4" />
                <span>{vehicle.sellerName}</span>
              </div>
              <div className="flex items-center gap-2">
                <FiCalendar className="w-4 h-4" />
                <span>Enviado {formatTimeAgo(vehicle.submittedAt)}</span>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="flex gap-3">
              <button
                onClick={onViewDetails}
                className="flex items-center gap-2 px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50 transition-colors"
              >
                <FiEye className="w-4 h-4" />
                Ver Detalles
              </button>
              <button
                onClick={onApprove}
                disabled={isProcessing}
                className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:opacity-50"
              >
                <FiCheck className="w-4 h-4" />
                Aprobar
              </button>
              <button
                onClick={onReject}
                disabled={isProcessing}
                className="flex items-center gap-2 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50"
              >
                <FiX className="w-4 h-4" />
                Rechazar
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// RejectReasonModal
function RejectReasonModal({
  vehicleTitle,
  rejectReason,
  setRejectReason,
  onSubmit,
  onClose,
  isProcessing,
}: {
  vehicleTitle: string;
  rejectReason: string;
  setRejectReason: (reason: string) => void;
  onSubmit: () => void;
  onClose: () => void;
  isProcessing: boolean;
}) {
  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl shadow-xl max-w-md w-full mx-4 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-2">Rechazar Publicación</h3>
        <p className="text-sm text-gray-600 mb-4">
          <LocalizedContent content={vehicleTitle} showBadge={false} />
        </p>

        <div className="mb-4">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Razón del rechazo *
          </label>
          <textarea
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
            rows={4}
            placeholder="Describe la razón del rechazo..."
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-red-500 focus:border-transparent"
          />
        </div>

        <div className="flex gap-3 justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50"
          >
            Cancelar
          </button>
          <button
            onClick={onSubmit}
            disabled={!rejectReason.trim() || isProcessing}
            className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50"
          >
            {isProcessing ? 'Procesando...' : 'Confirmar Rechazo'}
          </button>
        </div>
      </div>
    </div>
  );
}

export default PendingApprovalsPage;
```

---

## 🔌 SERVICIOS API

```typescript
// src/hooks/usePendingApprovalsPage.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { adminService } from "@/services/adminService";

export function usePendingApprovalsPage() {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ["admin", "pending-approvals"],
    queryFn: () => adminService.getPendingVehicles(),
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  const approveVehicle = useMutation({
    mutationFn: (vehicleId: string) => adminService.approveVehicle(vehicleId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["admin", "pending-approvals"],
      });
      queryClient.invalidateQueries({ queryKey: ["admin", "dashboard"] });
    },
  });

  const rejectVehicle = useMutation({
    mutationFn: ({
      vehicleId,
      reason,
    }: {
      vehicleId: string;
      reason: string;
    }) => adminService.rejectVehicle(vehicleId, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["admin", "pending-approvals"],
      });
      queryClient.invalidateQueries({ queryKey: ["admin", "dashboard"] });
    },
  });

  return {
    vehicles: query.data?.items || [],
    total: query.data?.totalCount || 0,
    isLoading: query.isLoading,
    refetch: query.refetch,
    approveVehicle,
    rejectVehicle,
  };
}

// src/services/vehicleService.ts (admin methods)
export const approveVehicle = async (id: string): Promise<void> => {
  await api.post(`/api/admin/vehicles/${id}/approve`);
};

export const rejectVehicle = async (
  id: string,
  reason?: string,
): Promise<void> => {
  await api.post(`/api/admin/vehicles/${id}/reject`, { reason });
};

export const deleteVehicle = async (id: string): Promise<void> => {
  await api.delete(`/api/admin/vehicles/${id}`);
};
```

---

## ✅ VALIDACIÓN

- [ ] Tabla de listings con paginación
- [ ] Búsqueda por texto
- [ ] Filtros por status
- [ ] Filtros avanzados colapsables
- [ ] Stats cards con conteos
- [ ] Preview de imagen en tabla
- [ ] Botones aprobar/rechazar/eliminar
- [ ] Modal de detalles con carousel
- [ ] Modal de razón de rechazo obligatorio
- [ ] Empty state cuando no hay pendientes
- [ ] Refresh automático cada 30s
- [ ] LocalizedContent para títulos bilingües

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/listings-approvals.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Listings Approvals", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar cola de aprobaciones", async ({ page }) => {
    await page.goto("/admin/listings/approvals");

    await expect(page.getByTestId("approvals-queue")).toBeVisible();
  });

  test("debe aprobar listing", async ({ page }) => {
    await page.goto("/admin/listings/approvals");

    await page
      .getByTestId("listing-card")
      .first()
      .getByRole("button", { name: /aprobar/i })
      .click();
    await expect(page.getByText(/listing aprobado/i)).toBeVisible();
  });

  test("debe rechazar listing con razón obligatoria", async ({ page }) => {
    await page.goto("/admin/listings/approvals");

    await page
      .getByTestId("listing-card")
      .first()
      .getByRole("button", { name: /rechazar/i })
      .click();
    await expect(page.getByRole("dialog")).toBeVisible();

    await page.getByRole("combobox", { name: /razón/i }).click();
    await page.getByRole("option", { name: /fotos inapropiadas/i }).click();
    await page.getByRole("button", { name: /confirmar/i }).click();

    await expect(page.getByText(/listing rechazado/i)).toBeVisible();
  });

  test("debe ver detalles con carousel", async ({ page }) => {
    await page.goto("/admin/listings/approvals");

    await page.getByTestId("listing-card").first().click();
    await expect(page.getByTestId("image-carousel")).toBeVisible();
  });

  test("debe mostrar empty state sin pendientes", async ({ page }) => {
    await page.goto("/admin/listings/approvals?status=approved");

    // Assuming no approvals needed scenario
    await expect(page.getByTestId("empty-state")).toBeVisible();
  });
});
```

---

_Última actualización: Enero 2026_
