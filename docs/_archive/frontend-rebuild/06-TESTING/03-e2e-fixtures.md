# 🧪 E2E Test Fixtures & Factories

> **Tiempo estimado:** 30 minutos
> **Prerrequisitos:** Playwright configurado
> **Última actualización:** Enero 2026

---

## 📋 OBJETIVO

Crear datos de prueba consistentes y reutilizables para tests E2E con:

- **Factories** para generar datos mock
- **Fixtures** de Playwright
- **Database seeding** para tests
- **API mocking** con datos realistas

---

## 🎯 ESTRUCTURA DE FIXTURES

```
tests/
├── fixtures/
│   ├── index.ts              # Export principal
│   ├── auth.fixture.ts       # Usuario autenticado
│   ├── factories/
│   │   ├── user.factory.ts
│   │   ├── vehicle.factory.ts
│   │   ├── dealer.factory.ts
│   │   └── index.ts
│   └── data/
│       ├── vehicles.json
│       ├── dealers.json
│       └── users.json
├── mocks/
│   ├── handlers.ts           # MSW handlers
│   └── server.ts
└── e2e/
    └── *.spec.ts
```

---

## 🔧 PASO 1: Factory Base

```typescript
// filepath: tests/fixtures/factories/base.factory.ts
import { faker } from "@faker-js/faker/locale/es_MX";

// Configurar seed para reproducibilidad
faker.seed(12345);

// Tipo para funciones factory
type FactoryFn<T> = (overrides?: Partial<T>) => T;

// Crear factory con overrides
export function createFactory<T>(defaults: () => T): FactoryFn<T> {
  return (overrides?: Partial<T>): T => ({
    ...defaults(),
    ...overrides,
  });
}

// Crear múltiples items
export function createMany<T>(
  factory: FactoryFn<T>,
  count: number,
  overrides?: Partial<T>[],
): T[] {
  return Array.from({ length: count }, (_, index) =>
    factory(overrides?.[index]),
  );
}

// Generar ID único
export function generateId(): string {
  return faker.string.uuid();
}

// Generar slug desde texto
export function generateSlug(text: string): string {
  return text
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/(^-|-$)/g, "");
}
```

---

## 🔧 PASO 2: User Factory

```typescript
// filepath: tests/fixtures/factories/user.factory.ts
import { faker } from "@faker-js/faker/locale/es_MX";
import { createFactory, generateId } from "./base.factory";

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone: string;
  avatar?: string;
  role: "buyer" | "seller" | "dealer" | "admin";
  accountType: "individual" | "dealer";
  isVerified: boolean;
  createdAt: string;
}

export const createUser = createFactory<User>(() => {
  const firstName = faker.person.firstName();
  const lastName = faker.person.lastName();

  return {
    id: generateId(),
    email: faker.internet.email({ firstName, lastName }).toLowerCase(),
    firstName,
    lastName,
    fullName: `${firstName} ${lastName}`,
    phone: `+1-809-${faker.string.numeric(3)}-${faker.string.numeric(4)}`,
    avatar: faker.image.avatar(),
    role: "buyer",
    accountType: "individual",
    isVerified: true,
    createdAt: faker.date.past({ years: 1 }).toISOString(),
  };
});

// Variantes específicas
export const createBuyer = (overrides?: Partial<User>) =>
  createUser({ role: "buyer", accountType: "individual", ...overrides });

export const createSeller = (overrides?: Partial<User>) =>
  createUser({ role: "seller", accountType: "individual", ...overrides });

export const createDealerUser = (overrides?: Partial<User>) =>
  createUser({ role: "dealer", accountType: "dealer", ...overrides });

export const createAdmin = (overrides?: Partial<User>) =>
  createUser({ role: "admin", accountType: "individual", ...overrides });
```

---

## 🔧 PASO 3: Vehicle Factory

```typescript
// filepath: tests/fixtures/factories/vehicle.factory.ts
import { faker } from "@faker-js/faker/locale/es_MX";
import { createFactory, generateId, generateSlug } from "./base.factory";

// Datos realistas de vehículos
const MAKES_MODELS = {
  Toyota: ["Corolla", "Camry", "RAV4", "Hilux", "Land Cruiser", "Yaris"],
  Honda: ["Civic", "Accord", "CR-V", "HR-V", "Pilot"],
  Hyundai: ["Elantra", "Tucson", "Santa Fe", "Sonata", "Kona"],
  Kia: ["Forte", "Sportage", "Sorento", "Seltos", "K5"],
  Nissan: ["Sentra", "Altima", "Rogue", "Pathfinder", "Frontier"],
  Ford: ["Focus", "Fusion", "Explorer", "Ranger", "F-150"],
  Chevrolet: ["Cruze", "Malibu", "Equinox", "Traverse", "Silverado"],
  Mercedes: ["C-Class", "E-Class", "GLC", "GLE", "S-Class"],
  BMW: ["3 Series", "5 Series", "X3", "X5", "X7"],
  Audi: ["A4", "A6", "Q5", "Q7", "e-tron"],
};

const BODY_TYPES = ["Sedan", "SUV", "Pickup", "Hatchback", "Coupe", "Van"];
const FUEL_TYPES = ["Gasolina", "Diesel", "Híbrido", "Eléctrico"];
const TRANSMISSIONS = ["Automática", "Manual", "CVT"];
const COLORS = ["Blanco", "Negro", "Gris", "Plata", "Rojo", "Azul", "Verde"];
const CITIES_RD = [
  "Santo Domingo",
  "Santiago",
  "La Romana",
  "San Pedro de Macorís",
  "Punta Cana",
];

export interface Vehicle {
  id: string;
  slug: string;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  condition: "new" | "used";
  bodyType: string;
  fuelType: string;
  transmission: string;
  exteriorColor: string;
  interiorColor: string;
  engineSize?: string;
  horsepower?: number;
  doors: number;
  seats: number;
  vin: string;
  description: string;
  features: string[];
  images: Array<{ url: string; alt: string; isPrimary: boolean }>;
  city: string;
  status: "Active" | "Pending" | "Sold" | "Inactive";
  sellerId: string;
  sellerType: "individual" | "dealer";
  sellerName: string;
  viewCount: number;
  favoriteCount: number;
  createdAt: string;
  updatedAt: string;
}

export const createVehicle = createFactory<Vehicle>(() => {
  const makes = Object.keys(MAKES_MODELS);
  const make = faker.helpers.arrayElement(makes);
  const model = faker.helpers.arrayElement(
    MAKES_MODELS[make as keyof typeof MAKES_MODELS],
  );
  const year = faker.number.int({ min: 2015, max: 2025 });
  const condition =
    year >= 2024 ? "new" : faker.helpers.arrayElement(["new", "used"]);

  const basePrice =
    condition === "new"
      ? faker.number.int({ min: 1200000, max: 6000000 })
      : faker.number.int({ min: 400000, max: 3500000 });

  const price = Math.round(basePrice / 10000) * 10000; // Redondear a 10,000

  const id = generateId();
  const title = `${year} ${make} ${model}`;

  return {
    id,
    slug: generateSlug(`${title}-${id.slice(0, 8)}`),
    make,
    model,
    year,
    price,
    mileage:
      condition === "new" ? 0 : faker.number.int({ min: 5000, max: 180000 }),
    condition,
    bodyType: faker.helpers.arrayElement(BODY_TYPES),
    fuelType: faker.helpers.arrayElement(FUEL_TYPES),
    transmission: faker.helpers.arrayElement(TRANSMISSIONS),
    exteriorColor: faker.helpers.arrayElement(COLORS),
    interiorColor: faker.helpers.arrayElement(["Negro", "Beige", "Gris"]),
    engineSize: `${faker.helpers.arrayElement([1.6, 1.8, 2.0, 2.4, 2.5, 3.0, 3.5])}L`,
    horsepower: faker.number.int({ min: 120, max: 400 }),
    doors: faker.helpers.arrayElement([2, 4, 5]),
    seats: faker.helpers.arrayElement([2, 4, 5, 7, 8]),
    vin: faker.vehicle.vin(),
    description: faker.lorem.paragraphs(2),
    features: faker.helpers.arrayElements(
      [
        "Cámara de reversa",
        "Sensores de parqueo",
        "Bluetooth",
        "Apple CarPlay",
        "Android Auto",
        "Techo panorámico",
        "Asientos de cuero",
        "Asientos calefaccionados",
        "Sistema de navegación",
        "Cruise control adaptativo",
        "Alerta de colisión",
        "Monitoreo de punto ciego",
      ],
      { min: 4, max: 8 },
    ),
    images: Array.from(
      { length: faker.number.int({ min: 3, max: 8 }) },
      (_, i) => ({
        url: `https://picsum.photos/seed/${id}-${i}/800/600`,
        alt: `${title} - Imagen ${i + 1}`,
        isPrimary: i === 0,
      }),
    ),
    city: faker.helpers.arrayElement(CITIES_RD),
    status: "Active",
    sellerId: generateId(),
    sellerType: faker.helpers.arrayElement(["individual", "dealer"]),
    sellerName: faker.company.name(),
    viewCount: faker.number.int({ min: 0, max: 5000 }),
    favoriteCount: faker.number.int({ min: 0, max: 200 }),
    createdAt: faker.date.recent({ days: 30 }).toISOString(),
    updatedAt: faker.date.recent({ days: 7 }).toISOString(),
  };
});

// Variantes específicas
export const createNewVehicle = (overrides?: Partial<Vehicle>) =>
  createVehicle({ condition: "new", mileage: 0, year: 2025, ...overrides });

export const createUsedVehicle = (overrides?: Partial<Vehicle>) =>
  createVehicle({ condition: "used", ...overrides });

export const createLuxuryVehicle = (overrides?: Partial<Vehicle>) =>
  createVehicle({
    make: faker.helpers.arrayElement(["Mercedes", "BMW", "Audi"]),
    price: faker.number.int({ min: 3000000, max: 15000000 }),
    ...overrides,
  });

export const createSUV = (overrides?: Partial<Vehicle>) =>
  createVehicle({ bodyType: "SUV", ...overrides });

export const createSedan = (overrides?: Partial<Vehicle>) =>
  createVehicle({ bodyType: "Sedan", ...overrides });
```

---

## 🔧 PASO 4: Dealer Factory

```typescript
// filepath: tests/fixtures/factories/dealer.factory.ts
import { faker } from "@faker-js/faker/locale/es_MX";
import { createFactory, generateId, generateSlug } from "./base.factory";

export interface Dealer {
  id: string;
  slug: string;
  businessName: string;
  rnc: string;
  legalName: string;
  type: "Independent" | "Chain" | "MultipleStore" | "Franchise";
  status: "Pending" | "Active" | "Suspended";
  verificationStatus: "NotVerified" | "Verified" | "Rejected";
  plan: "Starter" | "Pro" | "Enterprise";
  email: string;
  phone: string;
  website?: string;
  address: string;
  city: string;
  province: string;
  logo?: string;
  description: string;
  rating: number;
  reviewCount: number;
  vehicleCount: number;
  maxActiveListings: number;
  isSubscriptionActive: boolean;
  foundedYear: number;
  employeeCount: number;
  createdAt: string;
}

const PROVINCES_RD = [
  "Distrito Nacional",
  "Santo Domingo",
  "Santiago",
  "La Altagracia",
  "San Cristóbal",
  "La Vega",
  "Puerto Plata",
];

export const createDealer = createFactory<Dealer>(() => {
  const businessName = faker.company.name() + " Auto";
  const id = generateId();
  const plan = faker.helpers.arrayElement([
    "Starter",
    "Pro",
    "Enterprise",
  ]) as Dealer["plan"];

  const maxListings = {
    Starter: 15,
    Pro: 50,
    Enterprise: 9999,
  };

  return {
    id,
    slug: generateSlug(`${businessName}-${id.slice(0, 8)}`),
    businessName,
    rnc: faker.string.numeric(9),
    legalName: `${businessName}, S.R.L.`,
    type: faker.helpers.arrayElement([
      "Independent",
      "Chain",
      "MultipleStore",
      "Franchise",
    ]),
    status: "Active",
    verificationStatus: "Verified",
    plan,
    email: faker.internet.email().toLowerCase(),
    phone: `+1-809-${faker.string.numeric(3)}-${faker.string.numeric(4)}`,
    website: `https://${generateSlug(businessName)}.com`,
    address: faker.location.streetAddress(),
    city: faker.helpers.arrayElement([
      "Santo Domingo",
      "Santiago",
      "La Romana",
    ]),
    province: faker.helpers.arrayElement(PROVINCES_RD),
    logo: faker.image.urlLoremFlickr({ category: "business" }),
    description: faker.company.catchPhrase() + ". " + faker.lorem.paragraph(),
    rating: parseFloat(
      faker.number.float({ min: 3.5, max: 5.0, fractionDigits: 1 }).toFixed(1),
    ),
    reviewCount: faker.number.int({ min: 5, max: 200 }),
    vehicleCount: faker.number.int({ min: 1, max: maxListings[plan] }),
    maxActiveListings: maxListings[plan],
    isSubscriptionActive: true,
    foundedYear: faker.number.int({ min: 1990, max: 2023 }),
    employeeCount: faker.number.int({ min: 2, max: 50 }),
    createdAt: faker.date.past({ years: 2 }).toISOString(),
  };
});

// Variantes
export const createStarterDealer = (overrides?: Partial<Dealer>) =>
  createDealer({ plan: "Starter", maxActiveListings: 15, ...overrides });

export const createProDealer = (overrides?: Partial<Dealer>) =>
  createDealer({ plan: "Pro", maxActiveListings: 50, ...overrides });

export const createEnterpriseDealer = (overrides?: Partial<Dealer>) =>
  createDealer({ plan: "Enterprise", maxActiveListings: 9999, ...overrides });
```

---

## 🔧 PASO 5: Export de Factories

```typescript
// filepath: tests/fixtures/factories/index.ts
export * from "./base.factory";
export * from "./user.factory";
export * from "./vehicle.factory";
export * from "./dealer.factory";

// Re-export createMany helper con tipos
import { createMany } from "./base.factory";
import { createVehicle, Vehicle } from "./vehicle.factory";
import { createUser, User } from "./user.factory";
import { createDealer, Dealer } from "./dealer.factory";

export const createVehicles = (count: number, overrides?: Partial<Vehicle>[]) =>
  createMany(createVehicle, count, overrides);

export const createUsers = (count: number, overrides?: Partial<User>[]) =>
  createMany(createUser, count, overrides);

export const createDealers = (count: number, overrides?: Partial<Dealer>[]) =>
  createMany(createDealer, count, overrides);
```

---

## 🔧 PASO 6: Playwright Fixtures

```typescript
// filepath: tests/fixtures/index.ts
import { test as base, Page } from "@playwright/test";
import {
  createUser,
  createVehicle,
  createDealer,
  User,
  Vehicle,
  Dealer,
} from "./factories";

// Tipos para fixtures
interface TestFixtures {
  // Data fixtures
  testUser: User;
  testVehicle: Vehicle;
  testDealer: Dealer;
  testVehicles: Vehicle[];

  // Page fixtures
  authenticatedPage: Page;
  dealerPage: Page;
  adminPage: Page;
}

// Extender test con fixtures
export const test = base.extend<TestFixtures>({
  // Data fixtures
  testUser: async ({}, use) => {
    const user = createUser();
    await use(user);
  },

  testVehicle: async ({}, use) => {
    const vehicle = createVehicle();
    await use(vehicle);
  },

  testDealer: async ({}, use) => {
    const dealer = createDealer();
    await use(dealer);
  },

  testVehicles: async ({}, use) => {
    const vehicles = Array.from({ length: 10 }, () => createVehicle());
    await use(vehicles);
  },

  // Página autenticada como usuario normal
  authenticatedPage: async ({ page, testUser }, use) => {
    // Mock del login
    await page.route("**/api/auth/session", (route) => {
      route.fulfill({
        status: 200,
        body: JSON.stringify({
          user: testUser,
          expires: new Date(Date.now() + 86400000).toISOString(),
        }),
      });
    });

    // Establecer cookie de sesión mock
    await page.context().addCookies([
      {
        name: "next-auth.session-token",
        value: "mock-session-token",
        domain: "localhost",
        path: "/",
      },
    ]);

    await use(page);
  },

  // Página autenticada como dealer
  dealerPage: async ({ page }, use) => {
    const dealerUser = createUser({ role: "dealer", accountType: "dealer" });

    await page.route("**/api/auth/session", (route) => {
      route.fulfill({
        status: 200,
        body: JSON.stringify({
          user: dealerUser,
          expires: new Date(Date.now() + 86400000).toISOString(),
        }),
      });
    });

    await page.context().addCookies([
      {
        name: "next-auth.session-token",
        value: "mock-dealer-session-token",
        domain: "localhost",
        path: "/",
      },
    ]);

    await use(page);
  },

  // Página autenticada como admin
  adminPage: async ({ page }, use) => {
    const adminUser = createUser({ role: "admin" });

    await page.route("**/api/auth/session", (route) => {
      route.fulfill({
        status: 200,
        body: JSON.stringify({
          user: adminUser,
          expires: new Date(Date.now() + 86400000).toISOString(),
        }),
      });
    });

    await page.context().addCookies([
      {
        name: "next-auth.session-token",
        value: "mock-admin-session-token",
        domain: "localhost",
        path: "/",
      },
    ]);

    await use(page);
  },
});

export { expect } from "@playwright/test";
```

---

## 🔧 PASO 7: API Mocking con MSW

```typescript
// filepath: tests/mocks/handlers.ts
import { http, HttpResponse } from "msw";
import {
  createVehicles,
  createDealers,
  createUser,
} from "../fixtures/factories";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:18443";

export const handlers = [
  // Vehicles
  http.get(`${API_URL}/api/vehicles`, ({ request }) => {
    const url = new URL(request.url);
    const page = parseInt(url.searchParams.get("page") || "1");
    const pageSize = parseInt(url.searchParams.get("pageSize") || "20");

    const vehicles = createVehicles(pageSize);

    return HttpResponse.json({
      data: vehicles,
      pagination: {
        page,
        pageSize,
        totalItems: 150,
        totalPages: Math.ceil(150 / pageSize),
        hasNextPage: page < Math.ceil(150 / pageSize),
        hasPreviousPage: page > 1,
      },
    });
  }),

  http.get(`${API_URL}/api/vehicles/:slug`, ({ params }) => {
    const { slug } = params;
    const vehicle = createVehicles(1)[0];
    vehicle.slug = slug as string;

    return HttpResponse.json(vehicle);
  }),

  // Search
  http.get(`${API_URL}/api/vehicles/search`, ({ request }) => {
    const url = new URL(request.url);
    const make = url.searchParams.get("make");
    const minPrice = url.searchParams.get("minPrice");
    const maxPrice = url.searchParams.get("maxPrice");

    let vehicles = createVehicles(20);

    // Aplicar filtros mock
    if (make) {
      vehicles = vehicles.filter(
        (v) => v.make.toLowerCase() === make.toLowerCase(),
      );
    }
    if (minPrice) {
      vehicles = vehicles.filter((v) => v.price >= parseInt(minPrice));
    }
    if (maxPrice) {
      vehicles = vehicles.filter((v) => v.price <= parseInt(maxPrice));
    }

    return HttpResponse.json({
      data: vehicles,
      pagination: {
        page: 1,
        pageSize: 20,
        totalItems: vehicles.length,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
      facets: {
        makes: [
          { value: "Toyota", count: 5 },
          { value: "Honda", count: 4 },
          { value: "Hyundai", count: 3 },
        ],
        priceRanges: [
          { min: 0, max: 1000000, count: 8 },
          { min: 1000000, max: 2000000, count: 6 },
          { min: 2000000, max: 5000000, count: 4 },
        ],
      },
    });
  }),

  // Dealers
  http.get(`${API_URL}/api/dealers`, () => {
    const dealers = createDealers(10);
    return HttpResponse.json({
      data: dealers,
      pagination: {
        page: 1,
        pageSize: 20,
        totalItems: 10,
        totalPages: 1,
      },
    });
  }),

  http.get(`${API_URL}/api/dealers/:id`, ({ params }) => {
    const dealer = createDealers(1)[0];
    dealer.id = params.id as string;
    return HttpResponse.json(dealer);
  }),

  // Auth
  http.post(`${API_URL}/api/auth/login`, async ({ request }) => {
    const body = (await request.json()) as { email: string; password: string };

    if (body.email === "test@example.com" && body.password === "password123") {
      const user = createUser({ email: body.email });
      return HttpResponse.json({
        token: "mock-jwt-token",
        refreshToken: "mock-refresh-token",
        user,
      });
    }

    return HttpResponse.json(
      { message: "Invalid credentials" },
      { status: 401 },
    );
  }),

  // Favorites
  http.get(`${API_URL}/api/favorites`, () => {
    const vehicles = createVehicles(5);
    return HttpResponse.json({
      data: vehicles.map((v) => ({
        vehicleId: v.id,
        vehicle: v,
        note: null,
        notifyPriceChanges: false,
        createdAt: new Date().toISOString(),
      })),
    });
  }),

  http.post(`${API_URL}/api/favorites`, async ({ request }) => {
    const body = (await request.json()) as { vehicleId: string };
    return HttpResponse.json(
      {
        vehicleId: body.vehicleId,
        createdAt: new Date().toISOString(),
      },
      { status: 201 },
    );
  }),

  // Homepage sections
  http.get(`${API_URL}/api/homepagesections/homepage`, () => {
    return HttpResponse.json([
      {
        id: "1",
        name: "Carousel Principal",
        slug: "carousel",
        vehicles: createVehicles(5),
      },
      {
        id: "2",
        name: "Destacados",
        slug: "destacados",
        vehicles: createVehicles(9),
      },
      {
        id: "3",
        name: "Sedanes",
        slug: "sedanes",
        vehicles: createVehicles(10).map((v) => ({ ...v, bodyType: "Sedan" })),
      },
      {
        id: "4",
        name: "SUVs",
        slug: "suvs",
        vehicles: createVehicles(10).map((v) => ({ ...v, bodyType: "SUV" })),
      },
    ]);
  }),
];
```

---

## 🔧 PASO 8: Setup de MSW

```typescript
// filepath: tests/mocks/server.ts
import { setupServer } from "msw/node";
import { handlers } from "./handlers";

export const server = setupServer(...handlers);

// Setup en playwright.config.ts
// beforeAll(() => server.listen())
// afterEach(() => server.resetHandlers())
// afterAll(() => server.close())
```

---

## 🔧 PASO 9: Datos Estáticos JSON

```json
// filepath: tests/fixtures/data/vehicles.json
{
  "featured": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "slug": "toyota-camry-2024-premium",
      "make": "Toyota",
      "model": "Camry",
      "year": 2024,
      "price": 2450000,
      "condition": "new"
    },
    {
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "slug": "honda-cr-v-2023-touring",
      "make": "Honda",
      "model": "CR-V",
      "year": 2023,
      "price": 2100000,
      "condition": "used"
    }
  ],
  "searchResults": {
    "toyota": [
      {
        "id": "toyota-001",
        "make": "Toyota",
        "model": "Corolla",
        "year": 2023,
        "price": 1350000
      },
      {
        "id": "toyota-002",
        "make": "Toyota",
        "model": "RAV4",
        "year": 2024,
        "price": 2200000
      }
    ]
  }
}
```

---

## 🔧 PASO 10: Uso en Tests E2E

```typescript
// filepath: tests/e2e/vehicles.spec.ts
import { test, expect } from "../fixtures";

test.describe("Vehicle Search", () => {
  test("should display vehicles on search page", async ({
    page,
    testVehicles,
  }) => {
    // Mock API response
    await page.route("**/api/vehicles/search*", (route) => {
      route.fulfill({
        status: 200,
        body: JSON.stringify({
          data: testVehicles,
          pagination: {
            page: 1,
            pageSize: 20,
            totalItems: testVehicles.length,
            totalPages: 1,
          },
        }),
      });
    });

    await page.goto("/buscar");

    // Verificar que se muestran los vehículos
    const vehicleCards = page.locator('[data-testid="vehicle-card"]');
    await expect(vehicleCards).toHaveCount(testVehicles.length);
  });

  test("should filter by make", async ({ page }) => {
    await page.goto("/buscar?make=Toyota");

    // Verificar que los resultados son de Toyota
    const makeLabels = page.locator('[data-testid="vehicle-make"]');
    const count = await makeLabels.count();

    for (let i = 0; i < count; i++) {
      await expect(makeLabels.nth(i)).toContainText("Toyota");
    }
  });
});

test.describe("Favorites", () => {
  test("should add vehicle to favorites when authenticated", async ({
    authenticatedPage,
    testVehicle,
  }) => {
    // Mock vehicle detail
    await authenticatedPage.route(
      `**/api/vehicles/${testVehicle.slug}`,
      (route) => {
        route.fulfill({
          status: 200,
          body: JSON.stringify(testVehicle),
        });
      },
    );

    // Mock add favorite
    await authenticatedPage.route("**/api/favorites", (route) => {
      if (route.request().method() === "POST") {
        route.fulfill({
          status: 201,
          body: JSON.stringify({ vehicleId: testVehicle.id }),
        });
      }
    });

    await authenticatedPage.goto(`/vehiculos/${testVehicle.slug}`);

    // Click en botón de favoritos
    await authenticatedPage.click('[data-testid="favorite-button"]');

    // Verificar que cambió el estado
    await expect(
      authenticatedPage.locator(
        '[data-testid="favorite-button"][data-favorited="true"]',
      ),
    ).toBeVisible();
  });
});

test.describe("Dealer Dashboard", () => {
  test("should show dealer stats", async ({ dealerPage, testDealer }) => {
    // Mock dealer data
    await dealerPage.route("**/api/dealers/me", (route) => {
      route.fulfill({
        status: 200,
        body: JSON.stringify(testDealer),
      });
    });

    await dealerPage.goto("/dealer/dashboard");

    // Verificar estadísticas
    await expect(
      dealerPage.locator('[data-testid="vehicle-count"]'),
    ).toContainText(String(testDealer.vehicleCount));
    await expect(
      dealerPage.locator('[data-testid="plan-badge"]'),
    ).toContainText(testDealer.plan);
  });
});
```

---

## ✅ Checklist

### Factories

- [ ] Crear base factory con helpers
- [ ] User factory con variantes
- [ ] Vehicle factory con datos realistas
- [ ] Dealer factory

### Fixtures

- [ ] Playwright fixtures para auth
- [ ] Data fixtures para tests
- [ ] Page fixtures pre-configuradas

### Mocking

- [ ] MSW handlers para todas las APIs
- [ ] Datos JSON estáticos para casos específicos
- [ ] Configurar server MSW

### Testing

- [ ] Usar fixtures en tests E2E
- [ ] Tests con datos mock consistentes
- [ ] Verificar flujos completos

---

## 🔗 Referencias

- [Faker.js](https://fakerjs.dev/)
- [Playwright Fixtures](https://playwright.dev/docs/test-fixtures)
- [MSW (Mock Service Worker)](https://mswjs.io/)

---

_Los fixtures garantizan tests reproducibles y mantenibles._
