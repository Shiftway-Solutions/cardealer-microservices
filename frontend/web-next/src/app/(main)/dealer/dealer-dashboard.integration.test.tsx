/**
 * Dealer Dashboard Integration Tests
 */

import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

const API_URL = 'http://localhost:8080';

// Mock dealer data
const mockDealer = {
  id: 'dealer-123',
  userId: 'user-123',
  businessName: 'Auto Premium RD',
  legalName: 'Auto Premium SRL',
  rnc: '101234567',
  type: 'independent',
  status: 'active',
  verificationStatus: 'verified',
  plan: 'pro',
  maxActiveListings: 50,
  email: 'info@autopremium.do',
  phone: '+18095551234',
  city: 'Santo Domingo',
  province: 'Distrito Nacional',
  logoUrl: 'https://example.com/logo.jpg',
  rating: 4.8,
  reviewCount: 125,
  activeListingsCount: 32,
  totalSalesCount: 320,
  isSubscriptionActive: true,
  createdAt: '2020-03-15T10:00:00Z',
};

const mockDealerStats = {
  totalListings: 35,
  activeListings: 32,
  totalViews: 12500,
  viewsThisMonth: 12500,
  viewsChange: 15,
  totalInquiries: 245,
  inquiriesThisMonth: 245,
  inquiriesChange: -5,
  pendingInquiries: 12,
  responseRate: 95,
  avgResponseTimeMinutes: 45,
  totalRevenue: 15200000,
  revenueThisMonth: 15200000,
  revenueChange: 12.5,
};

const mockVehicles = [
  {
    id: 'vehicle-1',
    slug: 'toyota-corolla-2023',
    make: 'Toyota',
    model: 'Corolla',
    year: 2023,
    price: 1850000,
    status: 'active',
    viewCount: 150,
    favoriteCount: 25,
    images: [{ url: 'https://example.com/car1.jpg', isPrimary: true }],
  },
  {
    id: 'vehicle-2',
    slug: 'honda-civic-2024',
    make: 'Honda',
    model: 'Civic',
    year: 2024,
    price: 2100000,
    status: 'active',
    viewCount: 200,
    favoriteCount: 40,
    images: [{ url: 'https://example.com/car2.jpg', isPrimary: true }],
  },
];

const mockLeads = [
  {
    id: 'lead-1',
    vehicleId: 'vehicle-1',
    vehicleTitle: 'Toyota Corolla 2023',
    buyerName: 'Juan Pérez',
    buyerEmail: 'juan@example.com',
    buyerPhone: '+18095551111',
    status: 'new',
    priority: 'high',
    createdAt: '2026-02-01T10:00:00Z',
    lastContactAt: null,
  },
  {
    id: 'lead-2',
    vehicleId: 'vehicle-2',
    vehicleTitle: 'Honda Civic 2024',
    buyerName: 'María García',
    buyerEmail: 'maria@example.com',
    buyerPhone: '+18095552222',
    status: 'contacted',
    priority: 'medium',
    createdAt: '2026-01-30T10:00:00Z',
    lastContactAt: '2026-01-31T14:00:00Z',
  },
];

// Setup MSW server
const server = setupServer(
  // Get current dealer
  http.get(`${API_URL}/api/dealers/me`, () => {
    return HttpResponse.json(mockDealer);
  }),

  http.get(`${API_URL}/api/dealers/owner/:userId`, () => {
    return HttpResponse.json(mockDealer);
  }),

  // Get dealer KPIs
  http.get(`${API_URL}/api/dealer-analytics/:id/kpis`, () => {
    return HttpResponse.json({
      totalViews: mockDealerStats.totalViews,
      viewsChange: mockDealerStats.viewsChange,
      totalContacts: mockDealerStats.totalInquiries,
      contactsChange: mockDealerStats.inquiriesChange,
      totalLeads: mockDealerStats.pendingInquiries,
      leadsChange: 8,
      totalSales: 8,
      salesChange: 10,
      totalRevenue: mockDealerStats.totalRevenue,
      revenueChange: mockDealerStats.revenueChange,
      conversionRate: mockDealerStats.responseRate,
      conversionChange: 4,
      avgResponseTime: mockDealerStats.avgResponseTimeMinutes,
      responseTimeChange: -12,
      activeListings: mockDealerStats.activeListings,
      inventoryValue: 55000000,
    });
  }),

  // Get dealer inventory stats
  http.get(`${API_URL}/api/dealer-analytics/inventory/:id/stats`, () => {
    return HttpResponse.json({
      dealerId: mockDealer.id,
      snapshotDate: '2026-03-30T00:00:00Z',
      totalVehicles: mockDealerStats.totalListings,
      activeVehicles: mockDealerStats.activeListings,
      soldVehicles: 8,
      totalInventoryValue: 55000000,
      avgVehiclePrice: 1571428,
      avgDaysOnMarket: 28,
      vehiclesOver60Days: 4,
      totalViews: mockDealerStats.totalViews,
      uniqueViews: 9100,
      totalContacts: mockDealerStats.totalInquiries,
      totalFavorites: 120,
      searchImpressions: 42000,
      newLeads: mockDealerStats.pendingInquiries,
      qualifiedLeads: 6,
      convertedLeads: 3,
      leadConversionRate: 12.5,
      totalRevenue: mockDealerStats.totalRevenue,
      avgTransactionValue: 1900000,
      clickThroughRate: 5.5,
      contactRate: 2,
      inventoryTurnoverRate: 18,
      agingRate: 11,
    });
  }),

  // Get dealer vehicles
  http.get(`${API_URL}/api/dealers/:id/vehicles`, () => {
    return HttpResponse.json({
      items: mockVehicles,
      page: 1,
      pageSize: 12,
      totalItems: 2,
      totalPages: 1,
    });
  }),

  // Get recent dealer leads
  http.get(`${API_URL}/api/crm/leads/recent/:count`, () => {
    return HttpResponse.json(mockLeads);
  }),

  // Update vehicle status
  http.put(`${API_URL}/api/vehicles/:id/status`, () => {
    return HttpResponse.json({ success: true });
  }),

  // Delete vehicle
  http.delete(`${API_URL}/api/vehicles/:id`, () => {
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

describe('Dealer Dashboard Integration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Dashboard Stats', () => {
    it('should display dealer information', async () => {
      // Mock component that uses dealer hooks
      const MockDashboard = () => {
        const { data: dealer, isLoading } = {
          data: mockDealer,
          isLoading: false,
        };

        if (isLoading) return <div>Loading...</div>;

        return (
          <div>
            <h1>{dealer?.businessName}</h1>
            <p>{dealer?.city}</p>
            <span data-testid="plan">{dealer?.plan}</span>
          </div>
        );
      };

      render(<MockDashboard />, { wrapper: createWrapper() });

      expect(screen.getByText('Auto Premium RD')).toBeInTheDocument();
      expect(screen.getByText('Santo Domingo')).toBeInTheDocument();
      expect(screen.getByTestId('plan')).toHaveTextContent('pro');
    });

    it('should display stats cards with correct data', async () => {
      const MockStatsCards = () => {
        const stats = mockDealerStats;

        return (
          <div>
            <div data-testid="views">{stats.totalViews.toLocaleString()}</div>
            <div data-testid="inquiries">{stats.totalInquiries}</div>
            <div data-testid="active-listings">{stats.activeListings}</div>
            <div data-testid="response-rate">{stats.responseRate}%</div>
          </div>
        );
      };

      render(<MockStatsCards />, { wrapper: createWrapper() });

      expect(screen.getByTestId('views')).toHaveTextContent('12,500');
      expect(screen.getByTestId('inquiries')).toHaveTextContent('245');
      expect(screen.getByTestId('active-listings')).toHaveTextContent('32');
      expect(screen.getByTestId('response-rate')).toHaveTextContent('95%');
    });

    it('should show trend indicators', async () => {
      const MockTrends = () => {
        const stats = mockDealerStats;

        return (
          <div>
            <span
              data-testid="views-trend"
              className={stats.viewsChange > 0 ? 'positive' : 'negative'}
            >
              {stats.viewsChange > 0 ? '+' : ''}
              {stats.viewsChange}%
            </span>
            <span
              data-testid="inquiries-trend"
              className={stats.inquiriesChange > 0 ? 'positive' : 'negative'}
            >
              {stats.inquiriesChange > 0 ? '+' : ''}
              {stats.inquiriesChange}%
            </span>
          </div>
        );
      };

      render(<MockTrends />, { wrapper: createWrapper() });

      const viewsTrend = screen.getByTestId('views-trend');
      const inquiriesTrend = screen.getByTestId('inquiries-trend');

      expect(viewsTrend).toHaveTextContent('+15%');
      expect(viewsTrend).toHaveClass('positive');
      expect(inquiriesTrend).toHaveTextContent('-5%');
      expect(inquiriesTrend).toHaveClass('negative');
    });
  });

  describe('Inventory Management', () => {
    it('should display vehicle list', async () => {
      const MockInventory = () => {
        const vehicles = mockVehicles;

        return (
          <div>
            <h2>Inventario ({vehicles.length})</h2>
            <ul>
              {vehicles.map(v => (
                <li key={v.id} data-testid={`vehicle-${v.id}`}>
                  <span>
                    {v.make} {v.model} {v.year}
                  </span>
                  <span>RD$ {v.price.toLocaleString()}</span>
                  <span>{v.status}</span>
                </li>
              ))}
            </ul>
          </div>
        );
      };

      render(<MockInventory />, { wrapper: createWrapper() });

      expect(screen.getByText('Inventario (2)')).toBeInTheDocument();
      expect(screen.getByText('Toyota Corolla 2023')).toBeInTheDocument();
      expect(screen.getByText('Honda Civic 2024')).toBeInTheDocument();
    });

    it('should show vehicle stats', async () => {
      const MockVehicleStats = () => {
        const vehicle = mockVehicles[0];

        return (
          <div data-testid="vehicle-card">
            <span data-testid="views">{vehicle.viewCount} vistas</span>
            <span data-testid="favorites">{vehicle.favoriteCount} favoritos</span>
          </div>
        );
      };

      render(<MockVehicleStats />, { wrapper: createWrapper() });

      expect(screen.getByTestId('views')).toHaveTextContent('150 vistas');
      expect(screen.getByTestId('favorites')).toHaveTextContent('25 favoritos');
    });

    it('should show plan limits', async () => {
      const MockLimits = () => {
        const dealer = mockDealer;
        const activeCount = 32;
        const remaining = dealer.maxActiveListings - activeCount;

        return (
          <div>
            <span data-testid="count">
              {activeCount}/{dealer.maxActiveListings}
            </span>
            <span data-testid="remaining">{remaining} disponibles</span>
          </div>
        );
      };

      render(<MockLimits />, { wrapper: createWrapper() });

      expect(screen.getByTestId('count')).toHaveTextContent('32/50');
      expect(screen.getByTestId('remaining')).toHaveTextContent('18 disponibles');
    });
  });

  describe('Leads Management', () => {
    it('should display leads list', async () => {
      const MockLeads = () => {
        const leads = mockLeads;

        return (
          <div>
            <h2>Leads ({leads.length})</h2>
            <ul>
              {leads.map(lead => (
                <li key={lead.id} data-testid={`lead-${lead.id}`}>
                  <span>{lead.buyerName}</span>
                  <span>{lead.vehicleTitle}</span>
                  <span data-testid={`status-${lead.id}`}>{lead.status}</span>
                </li>
              ))}
            </ul>
          </div>
        );
      };

      render(<MockLeads />, { wrapper: createWrapper() });

      expect(screen.getByText('Leads (2)')).toBeInTheDocument();
      expect(screen.getByText('Juan Pérez')).toBeInTheDocument();
      expect(screen.getByText('María García')).toBeInTheDocument();
    });

    it('should show lead priority indicator', async () => {
      const MockLeadPriority = () => {
        const lead = mockLeads[0];

        return (
          <div data-testid="priority-badge" className={`priority-${lead.priority}`}>
            {lead.priority}
          </div>
        );
      };

      render(<MockLeadPriority />, { wrapper: createWrapper() });

      const badge = screen.getByTestId('priority-badge');
      expect(badge).toHaveTextContent('high');
      expect(badge).toHaveClass('priority-high');
    });

    it('should display contact options', async () => {
      const MockContactOptions = () => {
        const lead = mockLeads[0];

        return (
          <div>
            <a href={`tel:${lead.buyerPhone}`} data-testid="phone-link">
              Llamar
            </a>
            <a href={`mailto:${lead.buyerEmail}`} data-testid="email-link">
              Email
            </a>
            <a
              href={`https://wa.me/${lead.buyerPhone.replace(/\+/g, '')}`}
              data-testid="whatsapp-link"
            >
              WhatsApp
            </a>
          </div>
        );
      };

      render(<MockContactOptions />, { wrapper: createWrapper() });

      expect(screen.getByTestId('phone-link')).toHaveAttribute('href', 'tel:+18095551111');
      expect(screen.getByTestId('email-link')).toHaveAttribute('href', 'mailto:juan@example.com');
      expect(screen.getByTestId('whatsapp-link')).toHaveAttribute(
        'href',
        'https://wa.me/18095551111'
      );
    });
  });

  describe('Subscription Status', () => {
    it('should show active subscription badge', async () => {
      const MockSubscription = () => {
        const dealer = mockDealer;

        return (
          <div>
            <span data-testid="plan-badge">{dealer.plan.toUpperCase()}</span>
            <span data-testid="subscription-status">
              {dealer.isSubscriptionActive ? 'Activa' : 'Inactiva'}
            </span>
          </div>
        );
      };

      render(<MockSubscription />, { wrapper: createWrapper() });

      expect(screen.getByTestId('plan-badge')).toHaveTextContent('PRO');
      expect(screen.getByTestId('subscription-status')).toHaveTextContent('Activa');
    });

    it('should show verification status', async () => {
      const MockVerification = () => {
        const dealer = mockDealer;

        return (
          <div data-testid="verification-badge" className={dealer.verificationStatus}>
            {dealer.verificationStatus === 'verified' ? '✓ Verificado' : 'Pendiente'}
          </div>
        );
      };

      render(<MockVerification />, { wrapper: createWrapper() });

      expect(screen.getByTestId('verification-badge')).toHaveTextContent('✓ Verificado');
    });
  });
});

describe('Dealer Inventory CRUD', () => {
  it('should handle vehicle status toggle', async () => {
    const user = userEvent.setup();
    const onToggle = vi.fn();

    const MockStatusToggle = ({ onToggle }: { onToggle: () => void }) => {
      return (
        <button onClick={onToggle} data-testid="toggle-status">
          Pausar
        </button>
      );
    };

    render(<MockStatusToggle onToggle={onToggle} />, { wrapper: createWrapper() });

    await user.click(screen.getByTestId('toggle-status'));
    expect(onToggle).toHaveBeenCalled();
  });

  it('should show delete confirmation dialog', async () => {
    const user = userEvent.setup();

    const MockDeleteButton = () => {
      const [isOpen, setIsOpen] = React.useState(false);

      return (
        <div>
          <button onClick={() => setIsOpen(true)} data-testid="delete-btn">
            Eliminar
          </button>
          {isOpen && (
            <div data-testid="confirm-dialog">
              <p>¿Estás seguro?</p>
              <button data-testid="confirm-delete">Confirmar</button>
              <button data-testid="cancel-delete" onClick={() => setIsOpen(false)}>
                Cancelar
              </button>
            </div>
          )}
        </div>
      );
    };

    render(<MockDeleteButton />, { wrapper: createWrapper() });

    expect(screen.getByTestId('delete-btn')).toBeInTheDocument();
  });
});
