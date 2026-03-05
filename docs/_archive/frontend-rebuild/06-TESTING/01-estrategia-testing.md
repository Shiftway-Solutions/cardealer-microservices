# 🧪 Estrategia de Testing

> **Tiempo estimado:** 60 minutos
> **Prerrequisitos:** Proyecto configurado, Vitest instalado

---

## 📋 OBJETIVO

Implementar estrategia de testing completa:

- Tests unitarios para hooks y utilidades
- Tests de integración para componentes
- Tests E2E para flujos críticos
- Mocking de API con MSW
- Coverage > 80%

---

## 🎯 PIRÁMIDE DE TESTS

```
                    ┌──────────┐
                    │   E2E    │  5-10 tests
                    │ Playwright│  Flujos críticos
                    └────┬─────┘
                         │
                  ┌──────┴──────┐
                  │ Integration │  20-30 tests
                  │ Components  │  Interacciones
                  └──────┬──────┘
                         │
            ┌────────────┴────────────┐
            │        Unit Tests       │  50-100 tests
            │  Hooks, Utils, Services │  Funciones aisladas
            └─────────────────────────┘
```

---

## 🔧 PASO 1: Configuración de MSW (Mock Service Worker)

### Handlers base

```typescript
// filepath: __tests__/mocks/handlers.ts
import { http, HttpResponse, delay } from "msw";

const API_URL = "https://api.okla.com.do";

// Mock data
export const mockVehicles = [
  {
    id: "1",
    slug: "toyota-camry-2024",
    title: "Toyota Camry SE 2024",
    price: 1850000,
    year: 2024,
    make: "Toyota",
    model: "Camry",
    mileage: 15000,
    city: "Santo Domingo",
    condition: "used",
    primaryImage: "/images/camry.jpg",
    sellerType: "dealer",
    isVerified: true,
  },
  {
    id: "2",
    slug: "honda-accord-2023",
    title: "Honda Accord Sport 2023",
    price: 1650000,
    year: 2023,
    make: "Honda",
    model: "Accord",
    mileage: 25000,
    city: "Santiago",
    condition: "used",
    primaryImage: "/images/accord.jpg",
    sellerType: "individual",
    isVerified: false,
  },
];

export const mockMakes = [
  { id: "1", name: "Toyota", slug: "toyota", vehicleCount: 150 },
  { id: "2", name: "Honda", slug: "honda", vehicleCount: 120 },
  { id: "3", name: "Hyundai", slug: "hyundai", vehicleCount: 90 },
];

export const mockModels = {
  "1": [
    { id: "1", makeId: "1", name: "Camry", slug: "camry", vehicleCount: 45 },
    {
      id: "2",
      makeId: "1",
      name: "Corolla",
      slug: "corolla",
      vehicleCount: 60,
    },
    { id: "3", makeId: "1", name: "RAV4", slug: "rav4", vehicleCount: 35 },
  ],
  "2": [
    { id: "4", makeId: "2", name: "Accord", slug: "accord", vehicleCount: 40 },
    { id: "5", makeId: "2", name: "Civic", slug: "civic", vehicleCount: 55 },
    { id: "6", makeId: "2", name: "CR-V", slug: "cr-v", vehicleCount: 25 },
  ],
};

export const handlers = [
  // =====================
  // VEHICLES
  // =====================

  // Search vehicles
  http.get(`${API_URL}/api/vehicles/search`, async ({ request }) => {
    await delay(100); // Simulate network delay

    const url = new URL(request.url);
    const page = parseInt(url.searchParams.get("page") || "1");
    const pageSize = parseInt(url.searchParams.get("pageSize") || "20");
    const query = url.searchParams.get("query") || "";

    let filtered = [...mockVehicles];

    // Filter by query
    if (query) {
      filtered = filtered.filter(
        (v) =>
          v.title.toLowerCase().includes(query.toLowerCase()) ||
          v.make.toLowerCase().includes(query.toLowerCase()) ||
          v.model.toLowerCase().includes(query.toLowerCase()),
      );
    }

    return HttpResponse.json({
      data: filtered.slice((page - 1) * pageSize, page * pageSize),
      pagination: {
        page,
        pageSize,
        totalItems: filtered.length,
        totalPages: Math.ceil(filtered.length / pageSize),
        hasNextPage: page * pageSize < filtered.length,
        hasPreviousPage: page > 1,
      },
    });
  }),

  // Get vehicle by slug
  http.get(`${API_URL}/api/vehicles/:slug`, async ({ params }) => {
    await delay(100);

    const vehicle = mockVehicles.find((v) => v.slug === params.slug);

    if (!vehicle) {
      return HttpResponse.json(
        { message: "Vehicle not found" },
        { status: 404 },
      );
    }

    return HttpResponse.json({
      success: true,
      data: vehicle,
    });
  }),

  // Get featured vehicles
  http.get(`${API_URL}/api/vehicles/featured`, async () => {
    await delay(100);
    return HttpResponse.json({
      success: true,
      data: mockVehicles.slice(0, 6),
    });
  }),

  // Create vehicle
  http.post(`${API_URL}/api/vehicles`, async ({ request }) => {
    await delay(200);

    const body = (await request.json()) as Record<string, unknown>;

    return HttpResponse.json({
      success: true,
      data: {
        id: "new-id",
        slug: "new-vehicle-slug",
        ...body,
      },
    });
  }),

  // =====================
  // CATALOG
  // =====================

  // Get makes
  http.get(`${API_URL}/api/catalog/makes`, async () => {
    await delay(50);
    return HttpResponse.json({
      success: true,
      data: mockMakes,
    });
  }),

  // Get models by make
  http.get(
    `${API_URL}/api/catalog/makes/:makeId/models`,
    async ({ params }) => {
      await delay(50);
      const models = mockModels[params.makeId as keyof typeof mockModels] || [];
      return HttpResponse.json({
        success: true,
        data: models,
      });
    },
  ),

  // =====================
  // FAVORITES
  // =====================

  // Get favorites
  http.get(`${API_URL}/api/favorites`, async () => {
    await delay(100);
    return HttpResponse.json({
      data: [mockVehicles[0]],
      pagination: {
        page: 1,
        pageSize: 20,
        totalItems: 1,
        totalPages: 1,
      },
    });
  }),

  // Check if favorited
  http.get(`${API_URL}/api/favorites/check/:vehicleId`, async ({ params }) => {
    await delay(50);
    return HttpResponse.json({
      success: true,
      data: { isFavorite: params.vehicleId === "1" },
    });
  }),

  // Toggle favorite
  http.post(
    `${API_URL}/api/favorites/toggle/:vehicleId`,
    async ({ params }) => {
      await delay(100);
      return HttpResponse.json({
        success: true,
        data: { isFavorite: params.vehicleId !== "1" },
      });
    },
  ),

  // =====================
  // AUTH
  // =====================

  // Login
  http.post(`${API_URL}/api/auth/login`, async ({ request }) => {
    await delay(200);

    const body = (await request.json()) as { email: string; password: string };

    if (body.email === "test@example.com" && body.password === "password123") {
      return HttpResponse.json({
        success: true,
        data: {
          user: {
            id: "1",
            email: "test@example.com",
            firstName: "Test",
            lastName: "User",
            role: "buyer",
          },
          tokens: {
            accessToken: "mock-access-token",
            refreshToken: "mock-refresh-token",
            expiresIn: 3600,
          },
        },
      });
    }

    return HttpResponse.json(
      {
        success: false,
        message: "Credenciales inválidas",
      },
      { status: 401 },
    );
  }),

  // Register
  http.post(`${API_URL}/api/auth/register`, async ({ request }) => {
    await delay(200);

    const body = (await request.json()) as Record<string, unknown>;

    if (body.email === "existing@example.com") {
      return HttpResponse.json(
        {
          success: false,
          message: "El email ya está registrado",
          errors: { email: ["El email ya existe"] },
        },
        { status: 400 },
      );
    }

    return HttpResponse.json({
      success: true,
      data: {
        user: {
          id: "new-user-id",
          email: body.email,
          firstName: body.firstName,
          lastName: body.lastName,
          role: "buyer",
        },
        tokens: {
          accessToken: "mock-access-token",
          refreshToken: "mock-refresh-token",
          expiresIn: 3600,
        },
      },
    });
  }),
];
```

### Server setup

```typescript
// filepath: __tests__/mocks/server.ts
import { setupServer } from "msw/node";
import { handlers } from "./handlers";

export const server = setupServer(...handlers);
```

### Integrar en setup

```typescript
// filepath: __tests__/setup.ts
import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterAll, afterEach, beforeAll, vi } from "vitest";
import { server } from "./mocks/server";

// Start server before all tests
beforeAll(() => server.listen({ onUnhandledRequest: "warn" }));

// Reset handlers after each test
afterEach(() => {
  cleanup();
  server.resetHandlers();
});

// Close server after all tests
afterAll(() => server.close());

// Mock next/navigation
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: vi.fn(),
    replace: vi.fn(),
    prefetch: vi.fn(),
    back: vi.fn(),
  }),
  usePathname: () => "/",
  useSearchParams: () => new URLSearchParams(),
}));

// Mock next-auth
vi.mock("next-auth/react", () => ({
  useSession: () => ({
    data: null,
    status: "unauthenticated",
  }),
  signIn: vi.fn(),
  signOut: vi.fn(),
  getSession: vi.fn().mockResolvedValue(null),
}));

// Mock next/image
vi.mock("next/image", () => ({
  default: ({ src, alt, ...props }: { src: string; alt: string }) => (
    // eslint-disable-next-line @next/next/no-img-element
    <img src={src} alt={alt} {...props} />
  ),
}));
```

---

## 🔧 PASO 2: Test Utilities

### Custom render

```typescript
// filepath: __tests__/utils/test-utils.tsx
import * as React from "react";
import { render, RenderOptions } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import userEvent from "@testing-library/user-event";

// Create a new QueryClient for each test
function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
        staleTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  });
}

interface WrapperProps {
  children: React.ReactNode;
}

function AllTheProviders({ children }: WrapperProps) {
  const queryClient = createTestQueryClient();

  return (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
}

function customRender(
  ui: React.ReactElement,
  options?: Omit<RenderOptions, "wrapper">
) {
  return {
    user: userEvent.setup(),
    ...render(ui, { wrapper: AllTheProviders, ...options }),
  };
}

// Re-export everything
export * from "@testing-library/react";
export { customRender as render };
export { userEvent };
```

### Test data factories

```typescript
// filepath: __tests__/utils/factories.ts
import type { Vehicle, VehicleSummary, User, Dealer } from "@/types";

let vehicleIdCounter = 1;
let userIdCounter = 1;

export function createVehicle(overrides?: Partial<Vehicle>): Vehicle {
  const id = `vehicle-${vehicleIdCounter++}`;

  return {
    id,
    slug: `test-vehicle-${id}`,
    title: "Test Vehicle 2024",
    description: "A great test vehicle",
    price: 1500000,
    originalPrice: undefined,
    priceNegotiable: false,
    make: "Toyota",
    makeId: "1",
    model: "Camry",
    modelId: "1",
    trim: "SE",
    year: 2024,
    vin: undefined,
    condition: "used",
    mileage: 20000,
    mileageUnit: "km",
    fuelType: "gasoline",
    transmission: "automatic",
    drivetrain: "fwd",
    engineSize: 2.5,
    horsepower: 203,
    cylinders: 4,
    exteriorColor: "Blanco",
    interiorColor: "Negro",
    bodyType: "sedan",
    doors: 4,
    seats: 5,
    features: ["Bluetooth", "Backup Camera"],
    safetyFeatures: ["ABS", "Airbags"],
    images: [
      {
        id: "img-1",
        url: "/images/test.jpg",
        thumbnailUrl: "/images/test-thumb.jpg",
        alt: "Test Vehicle",
        isPrimary: true,
        sortOrder: 0,
      },
    ],
    videoUrl: undefined,
    city: "Santo Domingo",
    province: "Distrito Nacional",
    status: "active",
    views: 100,
    favorites: 5,
    inquiries: 3,
    sellerId: "seller-1",
    sellerType: "individual",
    dealerId: undefined,
    publishedAt: new Date().toISOString(),
    expiresAt: undefined,
    soldAt: undefined,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    ...overrides,
  };
}

export function createVehicleSummary(
  overrides?: Partial<VehicleSummary>,
): VehicleSummary {
  const id = `vehicle-${vehicleIdCounter++}`;

  return {
    id,
    slug: `test-vehicle-${id}`,
    title: "Test Vehicle 2024",
    price: 1500000,
    year: 2024,
    make: "Toyota",
    model: "Camry",
    mileage: 20000,
    city: "Santo Domingo",
    condition: "used",
    primaryImage: "/images/test.jpg",
    sellerType: "individual",
    isVerified: false,
    ...overrides,
  };
}

export function createUser(overrides?: Partial<User>): User {
  const id = `user-${userIdCounter++}`;

  return {
    id,
    email: `user-${id}@example.com`,
    phone: "8091234567",
    firstName: "Test",
    lastName: "User",
    fullName: "Test User",
    avatarUrl: undefined,
    role: "buyer",
    accountStatus: "active",
    isEmailVerified: true,
    isPhoneVerified: false,
    dealerId: undefined,
    preferredLanguage: "es",
    notificationPreferences: {
      email: true,
      sms: false,
      push: true,
    },
    lastLoginAt: new Date().toISOString(),
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    ...overrides,
  };
}

export function createPaginatedResponse<T>(
  data: T[],
  overrides?: {
    page?: number;
    pageSize?: number;
    totalItems?: number;
  },
) {
  const page = overrides?.page ?? 1;
  const pageSize = overrides?.pageSize ?? 20;
  const totalItems = overrides?.totalItems ?? data.length;
  const totalPages = Math.ceil(totalItems / pageSize);

  return {
    data,
    pagination: {
      page,
      pageSize,
      totalItems,
      totalPages,
      hasNextPage: page < totalPages,
      hasPreviousPage: page > 1,
    },
  };
}
```

---

## 🔧 PASO 3: Tests de Componentes

### Test de VehicleCard

```typescript
// filepath: __tests__/components/vehicles/VehicleCard.test.tsx
import { describe, it, expect, vi } from "vitest";
import { screen } from "@testing-library/react";
import { render } from "@tests/utils/test-utils";
import { VehicleCard, VehicleCardSkeleton } from "@/components/vehicles/VehicleCard";
import { createVehicleSummary } from "@tests/utils/factories";

describe("VehicleCard", () => {
  const mockVehicle = createVehicleSummary({
    title: "Toyota Camry SE 2024",
    price: 1850000,
    year: 2024,
    make: "Toyota",
    model: "Camry",
    mileage: 15000,
    city: "Santo Domingo",
    condition: "used",
    isVerified: true,
  });

  it("renders vehicle information correctly", () => {
    render(<VehicleCard vehicle={mockVehicle} />);

    expect(screen.getByText("2024 Toyota Camry")).toBeInTheDocument();
    expect(screen.getByText("1,850,000")).toBeInTheDocument();
    expect(screen.getByText("15,000 km")).toBeInTheDocument();
    expect(screen.getByText("Santo Domingo")).toBeInTheDocument();
  });

  it("shows verified badge when dealer is verified", () => {
    render(
      <VehicleCard
        vehicle={{ ...mockVehicle, sellerType: "dealer", isVerified: true }}
      />
    );

    expect(screen.getByText("Verificado")).toBeInTheDocument();
  });

  it("does not show verified badge for individual sellers", () => {
    render(
      <VehicleCard
        vehicle={{ ...mockVehicle, sellerType: "individual" }}
      />
    );

    expect(screen.queryByText("Verificado")).not.toBeInTheDocument();
  });

  it("shows 'Nuevo' badge for new condition", () => {
    render(<VehicleCard vehicle={{ ...mockVehicle, condition: "new" }} />);

    expect(screen.getByText("Nuevo")).toBeInTheDocument();
  });

  it("calls onFavoriteClick when favorite button is clicked", async () => {
    const handleFavorite = vi.fn();
    const { user } = render(
      <VehicleCard vehicle={mockVehicle} onFavoriteClick={handleFavorite} />
    );

    const favoriteButton = screen.getByRole("button", {
      name: /agregar a favoritos/i,
    });
    await user.click(favoriteButton);

    expect(handleFavorite).toHaveBeenCalledWith(mockVehicle.id);
  });

  it("shows filled heart when favorited", () => {
    render(<VehicleCard vehicle={mockVehicle} isFavorited />);

    expect(
      screen.getByRole("button", { name: /quitar de favoritos/i })
    ).toBeInTheDocument();
  });

  it("links to vehicle detail page", () => {
    render(<VehicleCard vehicle={mockVehicle} />);

    const links = screen.getAllByRole("link");
    expect(links[0]).toHaveAttribute(
      "href",
      `/vehiculos/${mockVehicle.slug}`
    );
  });

  it("applies custom className", () => {
    const { container } = render(
      <VehicleCard vehicle={mockVehicle} className="custom-class" />
    );

    expect(container.firstChild).toHaveClass("custom-class");
  });
});

describe("VehicleCardSkeleton", () => {
  it("renders loading skeleton", () => {
    const { container } = render(<VehicleCardSkeleton />);

    const skeletons = container.querySelectorAll(".animate-pulse");
    expect(skeletons.length).toBeGreaterThan(0);
  });
});
```

### Test de SearchFilters

```typescript
// filepath: __tests__/components/search/SearchFilters.test.tsx
import { describe, it, expect, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { render } from "@tests/utils/test-utils";
import { SearchFilters } from "@/components/search/SearchFilters";

describe("SearchFilters", () => {
  const defaultFilters = {
    query: "",
    makeId: "",
    modelId: "",
    yearMin: undefined,
    yearMax: undefined,
    priceMin: undefined,
    priceMax: undefined,
  };

  it("renders all filter fields", async () => {
    render(
      <SearchFilters
        filters={defaultFilters}
        onFiltersChange={vi.fn()}
      />
    );

    await waitFor(() => {
      expect(screen.getByPlaceholderText(/buscar/i)).toBeInTheDocument();
    });

    // Verify filter labels exist
    expect(screen.getByText(/marca/i)).toBeInTheDocument();
    expect(screen.getByText(/año/i)).toBeInTheDocument();
    expect(screen.getByText(/precio/i)).toBeInTheDocument();
  });

  it("calls onFiltersChange when filters are updated", async () => {
    const handleChange = vi.fn();
    const { user } = render(
      <SearchFilters
        filters={defaultFilters}
        onFiltersChange={handleChange}
      />
    );

    const searchInput = screen.getByPlaceholderText(/buscar/i);
    await user.type(searchInput, "Toyota");

    await waitFor(() => {
      expect(handleChange).toHaveBeenCalled();
    });
  });

  it("shows clear filters button when filters are active", async () => {
    render(
      <SearchFilters
        filters={{ ...defaultFilters, query: "Toyota" }}
        onFiltersChange={vi.fn()}
      />
    );

    expect(screen.getByText(/limpiar filtros/i)).toBeInTheDocument();
  });

  it("clears all filters when clear button is clicked", async () => {
    const handleChange = vi.fn();
    const { user } = render(
      <SearchFilters
        filters={{ ...defaultFilters, query: "Toyota", priceMin: 500000 }}
        onFiltersChange={handleChange}
      />
    );

    await user.click(screen.getByText(/limpiar filtros/i));

    expect(handleChange).toHaveBeenCalledWith(expect.objectContaining({
      query: "",
      priceMin: undefined,
    }));
  });
});
```

---

## 🔧 PASO 4: Tests de Hooks

```typescript
// filepath: __tests__/hooks/use-vehicles.test.tsx
import { describe, it, expect, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useVehicleSearch, useFeaturedVehicles, useVehicle } from "@/lib/hooks";

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
}

describe("useVehicleSearch", () => {
  it("fetches vehicles with default params", async () => {
    const { result } = renderHook(
      () => useVehicleSearch({ page: 1, pageSize: 20 }),
      { wrapper: createWrapper() }
    );

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(result.current.data?.data).toBeDefined();
    expect(result.current.data?.pagination).toBeDefined();
  });

  it("filters by query", async () => {
    const { result } = renderHook(
      () => useVehicleSearch({ page: 1, pageSize: 20, query: "Toyota" }),
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    // Should only return Toyota vehicles
    result.current.data?.data.forEach((vehicle) => {
      expect(vehicle.make).toBe("Toyota");
    });
  });
});

describe("useFeaturedVehicles", () => {
  it("fetches featured vehicles", async () => {
    const { result } = renderHook(() => useFeaturedVehicles(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(result.current.data?.data).toHaveLength(2); // From mock
  });
});

describe("useVehicle", () => {
  it("fetches vehicle by slug", async () => {
    const { result } = renderHook(
      () => useVehicle("toyota-camry-2024"),
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(result.current.data?.data.slug).toBe("toyota-camry-2024");
  });

  it("returns 404 for non-existent vehicle", async () => {
    const { result } = renderHook(
      () => useVehicle("non-existent-vehicle"),
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });
  });

  it("is disabled when slug is empty", () => {
    const { result } = renderHook(
      () => useVehicle(""),
      { wrapper: createWrapper() }
    );

    expect(result.current.fetchStatus).toBe("idle");
  });
});
```

---

## 🔧 PASO 5: Tests E2E con Playwright

### Configuración

```typescript
// filepath: playwright.config.ts
import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./__tests__/e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [["html", { outputFolder: "playwright-report" }], ["list"]],
  use: {
    baseURL: "http://localhost:3000",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: "on-first-retry",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
    {
      name: "Mobile Safari",
      use: { ...devices["iPhone 13"] },
    },
  ],
  webServer: {
    command: "pnpm dev",
    url: "http://localhost:3000",
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },
});
```

### Tests E2E

```typescript
// filepath: __tests__/e2e/search.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Vehicle Search", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/vehiculos");
  });

  test("should display search results", async ({ page }) => {
    // Wait for vehicles to load
    await expect(page.getByRole("article").first()).toBeVisible();

    // Should show multiple vehicles
    const vehicles = page.getByRole("article");
    await expect(vehicles).toHaveCount(6); // Default page size
  });

  test("should filter by search query", async ({ page }) => {
    // Type in search
    await page.getByPlaceholder(/buscar/i).fill("Toyota");

    // Wait for results to update
    await page.waitForResponse((response) =>
      response.url().includes("/api/vehicles/search"),
    );

    // All results should be Toyota
    const titles = page.locator("article h3");
    for (const title of await titles.all()) {
      await expect(title).toContainText("Toyota");
    }
  });

  test("should navigate to vehicle detail", async ({ page }) => {
    // Click on first vehicle
    await page.getByRole("article").first().click();

    // Should be on detail page
    await expect(page).toHaveURL(/\/vehiculos\/.+/);

    // Should show vehicle details
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    await expect(page.getByText("RD$")).toBeVisible();
  });

  test("should add to favorites (requires login)", async ({ page }) => {
    // Click favorite button
    await page
      .getByRole("button", { name: /agregar a favoritos/i })
      .first()
      .click();

    // Should show login modal or redirect
    await expect(
      page.getByRole("dialog").or(page.locator('[href*="login"]')),
    ).toBeVisible();
  });
});

test.describe("Vehicle Detail", () => {
  test("should display vehicle information", async ({ page }) => {
    await page.goto("/vehiculos/toyota-camry-2024");

    // Basic info
    await expect(page.getByRole("heading", { level: 1 })).toContainText(
      /Toyota Camry/i,
    );

    // Price
    await expect(page.getByText("RD$")).toBeVisible();

    // Specs
    await expect(page.getByText(/km/)).toBeVisible();

    // Contact button
    await expect(
      page.getByRole("button", { name: /contactar/i }),
    ).toBeVisible();
  });

  test("should show image gallery", async ({ page }) => {
    await page.goto("/vehiculos/toyota-camry-2024");

    // Main image
    const mainImage = page.locator('[data-testid="main-image"]');
    await expect(mainImage).toBeVisible();

    // Thumbnails
    const thumbnails = page.locator('[data-testid="thumbnail"]');
    await expect(thumbnails.first()).toBeVisible();
  });

  test("should show similar vehicles", async ({ page }) => {
    await page.goto("/vehiculos/toyota-camry-2024");

    // Scroll to similar section
    await page.getByText(/vehículos similares/i).scrollIntoViewIfNeeded();

    // Should show similar vehicles
    await expect(
      page.locator('[data-testid="similar-vehicles"] article'),
    ).toHaveCount(4);
  });
});
```

```typescript
// filepath: __tests__/e2e/auth.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Authentication", () => {
  test("should login successfully", async ({ page }) => {
    await page.goto("/login");

    // Fill form
    await page.getByLabel("Email").fill("test@example.com");
    await page.getByLabel("Contraseña").fill("password123");

    // Submit
    await page.getByRole("button", { name: /iniciar sesión/i }).click();

    // Should redirect to home or dashboard
    await expect(page).not.toHaveURL(/login/);
  });

  test("should show error for invalid credentials", async ({ page }) => {
    await page.goto("/login");

    // Fill form with wrong credentials
    await page.getByLabel("Email").fill("wrong@example.com");
    await page.getByLabel("Contraseña").fill("wrongpassword");

    // Submit
    await page.getByRole("button", { name: /iniciar sesión/i }).click();

    // Should show error
    await expect(page.getByText(/credenciales inválidas/i)).toBeVisible();
  });

  test("should navigate to register", async ({ page }) => {
    await page.goto("/login");

    // Click register link
    await page.getByRole("link", { name: /crear cuenta/i }).click();

    // Should be on register page
    await expect(page).toHaveURL(/registro/);
  });
});
```

---

## 🔧 PASO 6: Coverage y CI

### Configurar thresholds

```typescript
// En vitest.config.ts - coverage section
coverage: {
  provider: "v8",
  reporter: ["text", "json", "html", "lcov"],
  exclude: [
    "node_modules/",
    ".next/",
    "__tests__/",
    "**/*.d.ts",
    "**/*.config.*",
    "**/types/",
  ],
  thresholds: {
    global: {
      branches: 70,
      functions: 80,
      lines: 80,
      statements: 80,
    },
  },
},
```

### Script de CI

```yaml
# filepath: .github/workflows/test.yml
name: Tests

on:
  push:
    branches: [main, development]
  pull_request:
    branches: [main, development]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: pnpm/action-setup@v2
        with:
          version: 8

      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: pnpm

      - run: pnpm install

      - name: Run unit tests
        run: pnpm test:coverage

      - name: Upload coverage
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage/lcov.info

  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: pnpm/action-setup@v2
        with:
          version: 8

      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: pnpm

      - run: pnpm install

      - name: Install Playwright
        run: pnpm exec playwright install --with-deps

      - name: Run E2E tests
        run: pnpm test:e2e

      - name: Upload report
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: playwright-report
          path: playwright-report/
```

---

## ✅ VALIDACIÓN

### Ejecutar todos los tests

```bash
# Unit tests
pnpm test

# Unit tests con coverage
pnpm test:coverage

# E2E tests
pnpm test:e2e

# E2E tests con UI
pnpm test:e2e:ui
```

### Verificar coverage

```bash
# Ver coverage report
open coverage/index.html

# Debe mostrar:
# - Statements: > 80%
# - Branches: > 70%
# - Functions: > 80%
# - Lines: > 80%
```

---

## 📊 RESUMEN

| Tipo de Test             | Cantidad Estimada | Herramienta              |
| ------------------------ | ----------------- | ------------------------ |
| Unit (Utils)             | 20-30             | Vitest                   |
| Unit (Hooks)             | 30-40             | Vitest + MSW             |
| Integration (Components) | 40-50             | Vitest + Testing Library |
| E2E (Flows)              | 10-15             | Playwright               |
| **Total**                | **100-135**       |                          |

| Métrica            | Target |
| ------------------ | ------ |
| Coverage           | > 80%  |
| Tests passing      | 100%   |
| E2E critical paths | 100%   |

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/07-BACKEND-SUPPORT/01-supportservice.md`
