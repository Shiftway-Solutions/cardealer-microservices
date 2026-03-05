# 🔍 SearchService - Documentación Frontend

> **Servicio:** SearchService  
> **Puerto:** 5070 (dev) / 8080 (k8s)  
> **Estado:** ✅ Implementado  
> **Motor:** Elasticsearch  
> **Última actualización:** Enero 2026

---

## 📋 Descripción

Servicio de búsqueda avanzada basado en Elasticsearch. Proporciona búsqueda full-text, filtros facetados, autocompletado y relevancia personalizada para el marketplace de vehículos.

---

## 🎯 Casos de Uso Frontend

### 1. Búsqueda Principal

```typescript
// Búsqueda desde el search bar
const performSearch = async (term: string, filters: SearchFilters) => {
  const results = await searchService.search({
    query: term,
    filters: {
      makes: filters.makes,
      priceRange: filters.priceRange,
      yearRange: filters.yearRange,
      bodyTypes: filters.bodyTypes,
    },
    from: 0,
    size: 20,
    sort: { field: "createdAt", order: "desc" },
  });

  return results;
};
```

### 2. Autocompletado

```typescript
// Sugerencias mientras el usuario escribe
const AutocompleteSearch = () => {
  const [query, setQuery] = useState('');
  const { data: suggestions } = useAutocomplete(query);

  return (
    <Combobox value={query} onChange={setQuery}>
      <Combobox.Input onChange={e => setQuery(e.target.value)} />
      <Combobox.Options>
        {suggestions?.map(s => (
          <Combobox.Option key={s.id} value={s.text}>
            {s.text} <span className="text-gray-400">({s.count})</span>
          </Combobox.Option>
        ))}
      </Combobox.Options>
    </Combobox>
  );
};
```

### 3. Filtros Facetados

```typescript
// Obtener conteos para filtros
const FacetedFilters = ({ currentFilters }: { currentFilters: SearchFilters }) => {
  const { data: facets } = useSearchFacets(currentFilters);

  return (
    <div className="space-y-4">
      <FacetGroup title="Marca" facets={facets?.makes} />
      <FacetGroup title="Tipo" facets={facets?.bodyTypes} />
      <FacetGroup title="Año" facets={facets?.years} />
    </div>
  );
};
```

---

## 📡 API Endpoints

### Search

| Método | Endpoint                               | Descripción                |
| ------ | -------------------------------------- | -------------------------- |
| `POST` | `/api/search/query`                    | Ejecutar búsqueda          |
| `GET`  | `/api/search/indices`                  | Listar índices disponibles |
| `GET`  | `/api/search/{indexName}/{documentId}` | Obtener documento          |

### Index

| Método   | Endpoint                  | Descripción            |
| -------- | ------------------------- | ---------------------- |
| `POST`   | `/api/index/vehicle`      | Indexar vehículo       |
| `DELETE` | `/api/index/vehicle/{id}` | Eliminar del índice    |
| `POST`   | `/api/index/reindex`      | Reindexar todo (Admin) |

### Stats

| Método | Endpoint                      | Descripción             |
| ------ | ----------------------------- | ----------------------- |
| `GET`  | `/api/stats/cluster`          | Estado del cluster ES   |
| `GET`  | `/api/stats/indices`          | Estadísticas de índices |
| `GET`  | `/api/stats/search-analytics` | Métricas de búsqueda    |

---

## 🔧 Cliente TypeScript

```typescript
// services/searchService.ts

import { apiClient } from "./apiClient";

// Tipos
interface SearchQuery {
  query?: string;
  filters?: SearchFilters;
  from?: number;
  size?: number;
  sort?: SortOptions;
}

interface SearchFilters {
  makes?: string[];
  models?: string[];
  yearRange?: { min: number; max: number };
  priceRange?: { min: number; max: number };
  mileageRange?: { min: number; max: number };
  bodyTypes?: string[];
  fuelTypes?: string[];
  transmissions?: string[];
  colors?: string[];
  features?: string[];
  location?: {
    lat: number;
    lng: number;
    radius: number; // km
  };
  sellerType?: "Individual" | "Dealer";
  status?: "Active" | "Sold";
}

interface SortOptions {
  field: "price" | "year" | "mileage" | "createdAt" | "_score";
  order: "asc" | "desc";
}

interface SearchResult {
  hits: VehicleHit[];
  total: number;
  took: number; // ms
  aggregations?: SearchAggregations;
}

interface VehicleHit {
  id: string;
  score: number;
  source: {
    id: string;
    title: string;
    make: string;
    model: string;
    year: number;
    price: number;
    mileage: number;
    imageUrl: string;
    slug: string;
    location: string;
    sellerType: string;
    createdAt: string;
  };
  highlight?: {
    title?: string[];
    description?: string[];
  };
}

interface SearchAggregations {
  makes: FacetBucket[];
  models: FacetBucket[];
  bodyTypes: FacetBucket[];
  years: FacetBucket[];
  priceRanges: FacetBucket[];
  locations: FacetBucket[];
}

interface FacetBucket {
  key: string;
  count: number;
}

interface AutocompleteSuggestion {
  id: string;
  text: string;
  type: "vehicle" | "make" | "model" | "location";
  count?: number;
}

export const searchService = {
  // Búsqueda principal
  async search(query: SearchQuery): Promise<SearchResult> {
    const response = await apiClient.post("/api/search/query", query);
    return response.data;
  },

  // Autocompletado
  async autocomplete(
    term: string,
    limit: number = 10,
  ): Promise<AutocompleteSuggestion[]> {
    const response = await apiClient.get("/api/search/autocomplete", {
      params: { term, limit },
    });
    return response.data;
  },

  // Obtener facetas para filtros
  async getFacets(filters: SearchFilters): Promise<SearchAggregations> {
    const result = await this.search({
      filters,
      size: 0, // No queremos resultados, solo facetas
    });
    return result.aggregations!;
  },

  // Búsqueda similar (More Like This)
  async findSimilar(
    vehicleId: string,
    limit: number = 6,
  ): Promise<VehicleHit[]> {
    const response = await apiClient.get(`/api/search/similar/${vehicleId}`, {
      params: { limit },
    });
    return response.data;
  },

  // Estadísticas de búsqueda
  async getSearchAnalytics(): Promise<{
    totalSearches: number;
    avgResponseTime: number;
    topQueries: { query: string; count: number }[];
  }> {
    const response = await apiClient.get("/api/stats/search-analytics");
    return response.data;
  },

  // Helper: Construir query string para URL
  buildQueryString(filters: SearchFilters): string {
    const params = new URLSearchParams();

    if (filters.makes?.length) params.set("makes", filters.makes.join(","));
    if (filters.priceRange?.min)
      params.set("priceMin", String(filters.priceRange.min));
    if (filters.priceRange?.max)
      params.set("priceMax", String(filters.priceRange.max));
    if (filters.yearRange?.min)
      params.set("yearMin", String(filters.yearRange.min));
    if (filters.yearRange?.max)
      params.set("yearMax", String(filters.yearRange.max));
    if (filters.bodyTypes?.length)
      params.set("bodyTypes", filters.bodyTypes.join(","));

    return params.toString();
  },

  // Helper: Parsear query string
  parseQueryString(queryString: string): SearchFilters {
    const params = new URLSearchParams(queryString);
    const filters: SearchFilters = {};

    if (params.has("makes")) filters.makes = params.get("makes")!.split(",");
    if (params.has("priceMin") || params.has("priceMax")) {
      filters.priceRange = {
        min: Number(params.get("priceMin")) || 0,
        max: Number(params.get("priceMax")) || Infinity,
      };
    }
    if (params.has("bodyTypes"))
      filters.bodyTypes = params.get("bodyTypes")!.split(",");

    return filters;
  },
};
```

---

## 🪝 Hooks de React

```typescript
// hooks/useSearch.ts

import { useQuery, useInfiniteQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import {
  searchService,
  SearchFilters,
  SearchQuery,
} from "../services/searchService";

export function useVehicleSearch(query: SearchQuery) {
  return useQuery({
    queryKey: ["search", query],
    queryFn: () => searchService.search(query),
    keepPreviousData: true,
  });
}

// Búsqueda infinita (scroll infinito)
export function useInfiniteSearch(filters: SearchFilters) {
  return useInfiniteQuery({
    queryKey: ["infinite-search", filters],
    queryFn: ({ pageParam = 0 }) =>
      searchService.search({
        filters,
        from: pageParam,
        size: 20,
      }),
    getNextPageParam: (lastPage, allPages) => {
      const totalLoaded = allPages.reduce((sum, p) => sum + p.hits.length, 0);
      return totalLoaded < lastPage.total ? totalLoaded : undefined;
    },
  });
}

// Autocompletado con debounce
export function useAutocomplete(term: string) {
  return useQuery({
    queryKey: ["autocomplete", term],
    queryFn: () => searchService.autocomplete(term),
    enabled: term.length >= 2,
  });
}

// Facetas para filtros
export function useSearchFacets(filters: SearchFilters) {
  return useQuery({
    queryKey: ["facets", filters],
    queryFn: () => searchService.getFacets(filters),
    staleTime: 30000,
  });
}

// Hook para sincronizar filtros con URL
export function useSearchFilters() {
  const [searchParams, setSearchParams] = useSearchParams();

  const filters = useMemo(
    () => searchService.parseQueryString(searchParams.toString()),
    [searchParams],
  );

  const setFilters = useCallback(
    (newFilters: SearchFilters) => {
      setSearchParams(searchService.buildQueryString(newFilters));
    },
    [setSearchParams],
  );

  return { filters, setFilters };
}
```

---

## 🧩 Componentes de Ejemplo

### Search Page

```tsx
// pages/SearchPage.tsx

import {
  useSearchFilters,
  useVehicleSearch,
  useSearchFacets,
} from "../hooks/useSearch";

export function SearchPage() {
  const { filters, setFilters } = useSearchFilters();
  const [query, setQuery] = useState("");

  const { data: results, isLoading } = useVehicleSearch({
    query,
    filters,
    from: 0,
    size: 20,
    sort: { field: "createdAt", order: "desc" },
  });

  const { data: facets } = useSearchFacets(filters);

  return (
    <div className="flex gap-6">
      {/* Sidebar con filtros */}
      <aside className="w-64 space-y-6">
        <SearchInput value={query} onChange={setQuery} />

        <FacetGroup
          title="Marca"
          facets={facets?.makes || []}
          selected={filters.makes || []}
          onChange={(makes) => setFilters({ ...filters, makes })}
        />

        <PriceRangeSlider
          value={filters.priceRange}
          onChange={(priceRange) => setFilters({ ...filters, priceRange })}
        />

        <FacetGroup
          title="Tipo de Carrocería"
          facets={facets?.bodyTypes || []}
          selected={filters.bodyTypes || []}
          onChange={(bodyTypes) => setFilters({ ...filters, bodyTypes })}
        />
      </aside>

      {/* Resultados */}
      <main className="flex-1">
        <div className="flex justify-between items-center mb-4">
          <p className="text-gray-600">
            {results?.total} vehículos encontrados en {results?.took}ms
          </p>
          <SortSelector />
        </div>

        {isLoading ? (
          <VehicleGridSkeleton count={12} />
        ) : (
          <VehicleGrid
            vehicles={results?.hits.map((h) => h.source) || []}
            highlights={results?.hits.reduce(
              (acc, h) => {
                if (h.highlight) acc[h.id] = h.highlight;
                return acc;
              },
              {} as Record<string, any>,
            )}
          />
        )}
      </main>
    </div>
  );
}
```

### Facet Group

```tsx
// components/FacetGroup.tsx

interface FacetGroupProps {
  title: string;
  facets: FacetBucket[];
  selected: string[];
  onChange: (selected: string[]) => void;
  maxVisible?: number;
}

export function FacetGroup({
  title,
  facets,
  selected,
  onChange,
  maxVisible = 5,
}: FacetGroupProps) {
  const [showAll, setShowAll] = useState(false);

  const visibleFacets = showAll ? facets : facets.slice(0, maxVisible);
  const hasMore = facets.length > maxVisible;

  const toggleFacet = (key: string) => {
    if (selected.includes(key)) {
      onChange(selected.filter((s) => s !== key));
    } else {
      onChange([...selected, key]);
    }
  };

  return (
    <div>
      <h3 className="font-medium mb-2">{title}</h3>
      <ul className="space-y-1">
        {visibleFacets.map((facet) => (
          <li key={facet.key}>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={selected.includes(facet.key)}
                onChange={() => toggleFacet(facet.key)}
                className="rounded"
              />
              <span className="flex-1">{facet.key}</span>
              <span className="text-gray-400 text-sm">({facet.count})</span>
            </label>
          </li>
        ))}
      </ul>

      {hasMore && (
        <button
          onClick={() => setShowAll(!showAll)}
          className="text-sm text-blue-600 mt-2"
        >
          {showAll ? "Ver menos" : `Ver ${facets.length - maxVisible} más`}
        </button>
      )}
    </div>
  );
}
```

---

## 🧪 Testing

### Vitest Unit Test

```typescript
// __tests__/searchService.test.ts
import { describe, it, expect, vi } from "vitest";
import { searchService } from "../services/searchService";

describe("searchService", () => {
  it("should build query string correctly", () => {
    const filters = {
      makes: ["Toyota", "Honda"],
      priceRange: { min: 500000, max: 1500000 },
      bodyTypes: ["SUV"],
    };

    const qs = searchService.buildQueryString(filters);

    expect(qs).toContain("makes=Toyota,Honda");
    expect(qs).toContain("priceMin=500000");
    expect(qs).toContain("priceMax=1500000");
    expect(qs).toContain("bodyTypes=SUV");
  });

  it("should parse query string correctly", () => {
    const qs = "makes=Toyota,Honda&priceMin=500000&bodyTypes=SUV,Sedan";

    const filters = searchService.parseQueryString(qs);

    expect(filters.makes).toEqual(["Toyota", "Honda"]);
    expect(filters.priceRange?.min).toBe(500000);
    expect(filters.bodyTypes).toEqual(["SUV", "Sedan"]);
  });
});
```

---

## 📊 Características de Elasticsearch

| Feature                | Uso                               |
| ---------------------- | --------------------------------- |
| **Full-Text Search**   | Búsqueda en título, descripción   |
| **Faceted Navigation** | Conteos de filtros en tiempo real |
| **Highlighting**       | Resaltar coincidencias            |
| **Autocomplete**       | Sugerencias mientras se escribe   |
| **Geo Search**         | Búsqueda por ubicación/radio      |
| **More Like This**     | Vehículos similares               |

---

## 🔗 Referencias

- [VehiclesSaleService](../05-API-INTEGRATION/01-vehicles-api.md)
- [Elasticsearch Docs](https://www.elastic.co/guide/en/elasticsearch/reference/current/)

---

_Elasticsearch proporciona búsquedas sub-segundo incluso con millones de documentos._
