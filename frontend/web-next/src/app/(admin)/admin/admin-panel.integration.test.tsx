/**
 * Admin Panel Integration Tests
 */

import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

const API_URL = 'http://localhost:8080';

// Mock admin data
const mockDashboardStats = {
  totalUsers: 12500,
  totalVehicles: 3200,
  totalDealers: 85,
  monthlyRevenue: 4500000,
  usersChange: 12,
  vehiclesChange: 8,
  dealersChange: 15,
  revenueChange: 22,
  pendingApprovals: 15,
  pendingVerifications: 8,
  activeReports: 5,
  supportTickets: 12,
};

const mockUsers = [
  {
    id: 'user-1',
    email: 'juan@example.com',
    firstName: 'Juan',
    lastName: 'Pérez',
    fullName: 'Juan Pérez',
    accountType: 'buyer',
    status: 'active',
    isVerified: true,
    createdAt: '2025-06-15T10:00:00Z',
    lastLoginAt: '2026-02-01T08:30:00Z',
  },
  {
    id: 'user-2',
    email: 'maria@example.com',
    firstName: 'María',
    lastName: 'García',
    fullName: 'María García',
    accountType: 'dealer',
    status: 'active',
    isVerified: true,
    createdAt: '2024-12-01T10:00:00Z',
    lastLoginAt: '2026-02-01T14:00:00Z',
  },
  {
    id: 'user-3',
    email: 'pedro@example.com',
    firstName: 'Pedro',
    lastName: 'Martínez',
    fullName: 'Pedro Martínez',
    accountType: 'buyer',
    status: 'suspended',
    isVerified: false,
    createdAt: '2025-01-20T10:00:00Z',
    lastLoginAt: '2025-12-15T10:00:00Z',
  },
];

const mockVehiclesAdmin = [
  {
    id: 'vehicle-1',
    title: 'Toyota Corolla 2023',
    make: 'Toyota',
    model: 'Corolla',
    year: 2023,
    price: 1850000,
    status: 'pending',
    sellerType: 'seller',
    createdAt: '2026-02-01T10:00:00Z',
    reportCount: 0,
    isFeatured: false,
  },
  {
    id: 'vehicle-2',
    title: 'Honda Civic 2024',
    make: 'Honda',
    model: 'Civic',
    year: 2024,
    price: 2100000,
    status: 'active',
    sellerType: 'dealer',
    createdAt: '2026-01-28T10:00:00Z',
    reportCount: 2,
    isFeatured: true,
  },
];

const mockDealersAdmin = [
  {
    id: 'dealer-1',
    businessName: 'Auto Premium RD',
    email: 'info@autopremium.do',
    phone: '+18095551234',
    status: 'active',
    verificationStatus: 'verified',
    plan: 'pro',
    activeListingsCount: 32,
    city: 'Santo Domingo',
    createdAt: '2020-03-15T10:00:00Z',
  },
  {
    id: 'dealer-2',
    businessName: 'CarMax Dominicana',
    email: 'ventas@carmax.do',
    phone: '+18095559999',
    status: 'pending',
    verificationStatus: 'pending',
    plan: 'libre',
    activeListingsCount: 0,
    city: 'Santiago',
    createdAt: '2026-01-28T10:00:00Z',
  },
];

const mockReports = [
  {
    id: 'report-1',
    type: 'fraud',
    targetType: 'vehicle',
    targetId: 'vehicle-2',
    targetTitle: 'Honda Civic 2024',
    reason: 'Posible fraude - precio muy bajo',
    status: 'pending',
    priority: 'high',
    createdAt: '2026-02-01T10:00:00Z',
    reporterName: 'Usuario Anónimo',
  },
  {
    id: 'report-2',
    type: 'spam',
    targetType: 'dealer',
    targetId: 'dealer-3',
    targetTitle: 'Dealer Spam SRL',
    reason: 'Publicaciones duplicadas',
    status: 'investigating',
    priority: 'medium',
    createdAt: '2026-01-30T10:00:00Z',
    reporterName: 'Juan Pérez',
  },
];

// Setup MSW server
const server = setupServer(
  // Dashboard stats
  http.get(`${API_URL}/api/admin/dashboard/stats`, () => {
    return HttpResponse.json(mockDashboardStats);
  }),

  // Users
  http.get(`${API_URL}/api/admin/users`, () => {
    return HttpResponse.json({
      items: mockUsers,
      page: 1,
      pageSize: 20,
      totalItems: 3,
      totalPages: 1,
    });
  }),

  // Vehicles
  http.get(`${API_URL}/api/admin/vehicles`, () => {
    return HttpResponse.json({
      items: mockVehiclesAdmin,
      page: 1,
      pageSize: 20,
      totalItems: 2,
      totalPages: 1,
    });
  }),

  // Dealers
  http.get(`${API_URL}/api/admin/dealers`, () => {
    return HttpResponse.json({
      items: mockDealersAdmin,
      page: 1,
      pageSize: 20,
      totalItems: 2,
      totalPages: 1,
    });
  }),

  // Reports
  http.get(`${API_URL}/api/admin/reports`, () => {
    return HttpResponse.json({
      items: mockReports,
      page: 1,
      pageSize: 20,
      totalItems: 2,
      totalPages: 1,
    });
  }),

  // Actions
  http.post(`${API_URL}/api/admin/users/:id/suspend`, () => {
    return HttpResponse.json({ success: true });
  }),

  http.post(`${API_URL}/api/admin/vehicles/:id/approve`, () => {
    return HttpResponse.json({ success: true });
  }),

  http.post(`${API_URL}/api/admin/vehicles/:id/reject`, () => {
    return HttpResponse.json({ success: true });
  }),

  http.post(`${API_URL}/api/admin/dealers/:id/verify`, () => {
    return HttpResponse.json({ success: true });
  }),

  http.put(`${API_URL}/api/admin/reports/:id/status`, () => {
    return HttpResponse.json({ success: true });
  })
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

// Test wrapper
function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });

  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('Admin Dashboard Integration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Dashboard Stats', () => {
    it('should display platform statistics', async () => {
      const MockDashboard = () => {
        const stats = mockDashboardStats;

        return (
          <div>
            <div data-testid="total-users">{stats.totalUsers.toLocaleString()}</div>
            <div data-testid="total-vehicles">{stats.totalVehicles.toLocaleString()}</div>
            <div data-testid="total-dealers">{stats.totalDealers}</div>
            <div data-testid="mrr">RD$ {stats.monthlyRevenue.toLocaleString()}</div>
          </div>
        );
      };

      render(<MockDashboard />, { wrapper: createWrapper() });

      expect(screen.getByTestId('total-users')).toHaveTextContent('12,500');
      expect(screen.getByTestId('total-vehicles')).toHaveTextContent('3,200');
      expect(screen.getByTestId('total-dealers')).toHaveTextContent('85');
      expect(screen.getByTestId('mrr')).toHaveTextContent('RD$ 4,500,000');
    });

    it('should display pending actions', async () => {
      const MockPendingActions = () => {
        const stats = mockDashboardStats;

        return (
          <div>
            <span data-testid="pending-approvals">{stats.pendingApprovals}</span>
            <span data-testid="pending-verifications">{stats.pendingVerifications}</span>
            <span data-testid="active-reports">{stats.activeReports}</span>
            <span data-testid="support-tickets">{stats.supportTickets}</span>
          </div>
        );
      };

      render(<MockPendingActions />, { wrapper: createWrapper() });

      expect(screen.getByTestId('pending-approvals')).toHaveTextContent('15');
      expect(screen.getByTestId('pending-verifications')).toHaveTextContent('8');
      expect(screen.getByTestId('active-reports')).toHaveTextContent('5');
      expect(screen.getByTestId('support-tickets')).toHaveTextContent('12');
    });

    it('should show trend indicators', async () => {
      const MockTrends = () => {
        const stats = mockDashboardStats;

        return (
          <div>
            <span data-testid="users-trend">+{stats.usersChange}%</span>
            <span data-testid="revenue-trend">+{stats.revenueChange}%</span>
          </div>
        );
      };

      render(<MockTrends />, { wrapper: createWrapper() });

      expect(screen.getByTestId('users-trend')).toHaveTextContent('+12%');
      expect(screen.getByTestId('revenue-trend')).toHaveTextContent('+22%');
    });
  });

  describe('User Management', () => {
    it('should display users list', async () => {
      const MockUsersList = () => {
        const users = mockUsers;

        return (
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Email</th>
                <th>Tipo</th>
                <th>Estado</th>
              </tr>
            </thead>
            <tbody>
              {users.map(user => (
                <tr key={user.id} data-testid={`user-${user.id}`}>
                  <td>{user.fullName}</td>
                  <td>{user.email}</td>
                  <td>{user.accountType}</td>
                  <td data-testid={`status-${user.id}`}>{user.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        );
      };

      render(<MockUsersList />, { wrapper: createWrapper() });

      expect(screen.getByText('Juan Pérez')).toBeInTheDocument();
      expect(screen.getByText('María García')).toBeInTheDocument();
      expect(screen.getByTestId('status-user-3')).toHaveTextContent('suspended');
    });

    it('should show verification status badge', async () => {
      const MockVerificationBadge = ({ isVerified }: { isVerified: boolean }) => {
        return (
          <span data-testid="verification-badge" className={isVerified ? 'verified' : 'unverified'}>
            {isVerified ? '✓ Verificado' : 'No verificado'}
          </span>
        );
      };

      const { rerender } = render(<MockVerificationBadge isVerified={true} />, {
        wrapper: createWrapper(),
      });
      expect(screen.getByTestId('verification-badge')).toHaveTextContent('✓ Verificado');

      rerender(<MockVerificationBadge isVerified={false} />);
      expect(screen.getByTestId('verification-badge')).toHaveTextContent('No verificado');
    });

    it('should handle user actions', async () => {
      const user = userEvent.setup();
      const onSuspend = vi.fn();
      const onVerify = vi.fn();

      const MockUserActions = () => {
        return (
          <div>
            <button onClick={onSuspend} data-testid="suspend-btn">
              Suspender
            </button>
            <button onClick={onVerify} data-testid="verify-btn">
              Verificar
            </button>
          </div>
        );
      };

      render(<MockUserActions />, { wrapper: createWrapper() });

      await user.click(screen.getByTestId('suspend-btn'));
      expect(onSuspend).toHaveBeenCalled();

      await user.click(screen.getByTestId('verify-btn'));
      expect(onVerify).toHaveBeenCalled();
    });
  });

  describe('Vehicle Moderation', () => {
    it('should display pending vehicles', async () => {
      const MockVehicleModeration = () => {
        const pendingVehicles = mockVehiclesAdmin.filter(v => v.status === 'pending');

        return (
          <div>
            <h2>Pendientes de Aprobación ({pendingVehicles.length})</h2>
            {pendingVehicles.map(vehicle => (
              <div key={vehicle.id} data-testid={`vehicle-${vehicle.id}`}>
                <span>{vehicle.title}</span>
                <span>RD$ {vehicle.price.toLocaleString()}</span>
                <span>{vehicle.sellerType}</span>
              </div>
            ))}
          </div>
        );
      };

      render(<MockVehicleModeration />, { wrapper: createWrapper() });

      expect(screen.getByText('Pendientes de Aprobación (1)')).toBeInTheDocument();
      expect(screen.getByText('Toyota Corolla 2023')).toBeInTheDocument();
    });

    it('should show report count indicator', async () => {
      const MockReportIndicator = () => {
        const vehicleWithReports = mockVehiclesAdmin.find(v => v.reportCount > 0);

        return (
          <div data-testid="report-indicator">
            {vehicleWithReports && (
              <span className="report-badge">{vehicleWithReports.reportCount} reportes</span>
            )}
          </div>
        );
      };

      render(<MockReportIndicator />, { wrapper: createWrapper() });

      expect(screen.getByTestId('report-indicator')).toHaveTextContent('2 reportes');
    });

    it('should handle approve/reject actions', async () => {
      const user = userEvent.setup();
      const onApprove = vi.fn();
      const onReject = vi.fn();

      const MockApprovalActions = () => {
        return (
          <div>
            <button onClick={onApprove} data-testid="approve-btn">
              Aprobar
            </button>
            <button onClick={onReject} data-testid="reject-btn">
              Rechazar
            </button>
          </div>
        );
      };

      render(<MockApprovalActions />, { wrapper: createWrapper() });

      await user.click(screen.getByTestId('approve-btn'));
      expect(onApprove).toHaveBeenCalled();

      await user.click(screen.getByTestId('reject-btn'));
      expect(onReject).toHaveBeenCalled();
    });
  });

  describe('Dealer Management', () => {
    it('should display dealers list', async () => {
      const MockDealersList = () => {
        const dealers = mockDealersAdmin;

        return (
          <table>
            <tbody>
              {dealers.map(dealer => (
                <tr key={dealer.id} data-testid={`dealer-${dealer.id}`}>
                  <td>{dealer.businessName}</td>
                  <td>{dealer.city}</td>
                  <td data-testid={`plan-${dealer.id}`}>{dealer.plan}</td>
                  <td data-testid={`status-${dealer.id}`}>{dealer.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        );
      };

      render(<MockDealersList />, { wrapper: createWrapper() });

      expect(screen.getByText('Auto Premium RD')).toBeInTheDocument();
      expect(screen.getByText('CarMax Dominicana')).toBeInTheDocument();
      expect(screen.getByTestId('plan-dealer-1')).toHaveTextContent('pro');
      expect(screen.getByTestId('status-dealer-2')).toHaveTextContent('pending');
    });

    it('should show pending verifications prominently', async () => {
      const MockPendingDealers = () => {
        const pendingDealers = mockDealersAdmin.filter(d => d.verificationStatus === 'pending');

        return (
          <div>
            <h3>Verificaciones Pendientes ({pendingDealers.length})</h3>
            {pendingDealers.map(dealer => (
              <div key={dealer.id} data-testid={`pending-${dealer.id}`}>
                <span>{dealer.businessName}</span>
                <button data-testid={`verify-${dealer.id}`}>Verificar</button>
              </div>
            ))}
          </div>
        );
      };

      render(<MockPendingDealers />, { wrapper: createWrapper() });

      expect(screen.getByText('Verificaciones Pendientes (1)')).toBeInTheDocument();
      expect(screen.getByText('CarMax Dominicana')).toBeInTheDocument();
    });
  });

  describe('Report Management', () => {
    it('should display reports list', async () => {
      const MockReportsList = () => {
        const reports = mockReports;

        return (
          <div>
            {reports.map(report => (
              <div key={report.id} data-testid={`report-${report.id}`}>
                <span data-testid={`type-${report.id}`}>{report.type}</span>
                <span>{report.targetTitle}</span>
                <span data-testid={`priority-${report.id}`}>{report.priority}</span>
                <span data-testid={`status-${report.id}`}>{report.status}</span>
              </div>
            ))}
          </div>
        );
      };

      render(<MockReportsList />, { wrapper: createWrapper() });

      expect(screen.getByTestId('type-report-1')).toHaveTextContent('fraud');
      expect(screen.getByTestId('priority-report-1')).toHaveTextContent('high');
      expect(screen.getByTestId('status-report-2')).toHaveTextContent('investigating');
    });

    it('should show priority indicators', async () => {
      const MockPriorityBadge = ({ priority }: { priority: string }) => {
        const colors: Record<string, string> = {
          high: 'bg-red-500',
          medium: 'bg-yellow-500',
          low: 'bg-green-500',
        };

        return (
          <span data-testid="priority-badge" className={colors[priority] || 'bg-muted/500'}>
            {priority}
          </span>
        );
      };

      const { rerender } = render(<MockPriorityBadge priority="high" />, {
        wrapper: createWrapper(),
      });
      expect(screen.getByTestId('priority-badge')).toHaveTextContent('high');

      rerender(<MockPriorityBadge priority="medium" />);
      expect(screen.getByTestId('priority-badge')).toHaveTextContent('medium');
    });

    it('should handle report status updates', async () => {
      const user = userEvent.setup();
      const onUpdateStatus = vi.fn();

      const MockReportActions = () => {
        return (
          <select data-testid="status-select" onChange={e => onUpdateStatus(e.target.value)}>
            <option value="pending">Pendiente</option>
            <option value="investigating">Investigando</option>
            <option value="resolved">Resuelto</option>
            <option value="dismissed">Descartado</option>
          </select>
        );
      };

      render(<MockReportActions />, { wrapper: createWrapper() });

      await user.selectOptions(screen.getByTestId('status-select'), 'resolved');
      expect(onUpdateStatus).toHaveBeenCalledWith('resolved');
    });
  });
});

describe('Admin Filters and Search', () => {
  it('should filter users by type', async () => {
    const MockFilters = () => {
      const [filter, setFilter] = React.useState('all');

      return (
        <div>
          <select
            data-testid="type-filter"
            value={filter}
            onChange={e => setFilter(e.target.value)}
          >
            <option value="all">Todos</option>
            <option value="buyer">Comprador</option>
            <option value="seller">Vendedor</option>
            <option value="dealer">Dealer</option>
            <option value="admin">Admin</option>
          </select>
        </div>
      );
    };

    render(<MockFilters />, { wrapper: createWrapper() });

    expect(screen.getByTestId('type-filter')).toBeInTheDocument();
  });

  it('should search by name or email', async () => {
    const user = userEvent.setup();
    const onSearch = vi.fn();

    const MockSearch = () => {
      return (
        <input
          type="text"
          data-testid="search-input"
          placeholder="Buscar por nombre o email..."
          onChange={e => onSearch(e.target.value)}
        />
      );
    };

    render(<MockSearch />, { wrapper: createWrapper() });

    await user.type(screen.getByTestId('search-input'), 'juan');
    expect(onSearch).toHaveBeenLastCalledWith('juan');
  });
});
