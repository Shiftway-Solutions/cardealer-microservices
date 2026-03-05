# 📐 Decisiones de Arquitectura (ADR) - OKLA Frontend

> **Propósito:** Documentar las decisiones técnicas clave y su justificación
> **Audiencia:** Desarrolladores, IA que implementa el código
> **Última actualización:** Enero 31, 2026

---

## 📋 ÍNDICE DE DECISIONES

| #       | Decisión                              | Estado      | Fecha    |
| ------- | ------------------------------------- | ----------- | -------- |
| ADR-001 | Next.js 14+ como framework            | ✅ Aceptada | Ene 2026 |
| ADR-002 | App Router vs Pages Router            | ✅ Aceptada | Ene 2026 |
| ADR-003 | Zustand para estado global            | ✅ Aceptada | Ene 2026 |
| ADR-004 | TanStack Query para server state      | ✅ Aceptada | Ene 2026 |
| ADR-005 | shadcn/ui como sistema de componentes | ✅ Aceptada | Ene 2026 |
| ADR-006 | Tailwind CSS para estilos             | ✅ Aceptada | Ene 2026 |
| ADR-007 | pnpm como package manager             | ✅ Aceptada | Ene 2026 |
| ADR-008 | Playwright para E2E testing           | ✅ Aceptada | Ene 2026 |
| ADR-009 | NextAuth.js para autenticación        | ✅ Aceptada | Ene 2026 |
| ADR-010 | Axios vs Fetch nativo                 | ✅ Aceptada | Ene 2026 |

---

## ADR-001: Next.js 14+ como Framework

### Contexto

Necesitamos elegir un framework React para construir el frontend de OKLA, un marketplace de vehículos que requiere:

- SEO excelente (páginas de vehículos deben ser indexables)
- Performance óptimo (Core Web Vitals)
- SSR/SSG para páginas públicas
- Buen DX para desarrollo rápido

### Opciones Consideradas

| Framework      | SSR | SSG | SEO | DX  | Ecosistema |
| -------------- | --- | --- | --- | --- | ---------- |
| **Next.js 14** | ✅  | ✅  | ✅  | ✅  | ✅         |
| Vite + React   | ❌  | ❌  | ⚠️  | ✅  | ✅         |
| Remix          | ✅  | ⚠️  | ✅  | ✅  | ⚠️         |
| Astro          | ⚠️  | ✅  | ✅  | ⚠️  | ⚠️         |
| Gatsby         | ⚠️  | ✅  | ✅  | ⚠️  | ⚠️         |

### Decisión

**Usar Next.js 14+ con App Router.**

### Justificación

1. **SEO Crítico:** Las páginas de vehículos (`/vehiculos/[slug]`) deben ser completamente indexables por Google. Next.js SSR/SSG garantiza esto.

2. **Performance:**
   - Server Components reducen JavaScript enviado al cliente
   - Streaming SSR mejora TTFB
   - Image Optimization built-in
   - Font Optimization built-in

3. **Mercado RD:**
   - Usuarios con conexiones variables (3G/4G)
   - Dispositivos de gama media
   - SSR reduce trabajo del cliente

4. **Ecosistema:**
   - Vercel deployment optimizado
   - Documentación excelente
   - Comunidad grande (troubleshooting fácil)
   - Compatible con todas las librerías React

5. **Migración desde Vite:**
   - El frontend actual es Vite + React
   - Next.js permite reusar componentes React existentes
   - Migración incremental posible

### Consecuencias

**Positivas:**

- SEO excelente out-of-the-box
- Performance superior para usuarios RD
- API Routes para BFF patterns

**Negativas:**

- Curva de aprendizaje para developers acostumbrados a SPA
- Complejidad adicional vs Vite puro
- Vendor lock-in con Vercel (mitigable con Docker)

### Alternativas Rechazadas

- **Vite puro:** No tiene SSR built-in. Requiere SEO hacks (prerendering).
- **Remix:** Menor ecosistema, menos recursos en español.
- **Gatsby:** Orientado a sitios estáticos, overkill para app dinámica.

---

## ADR-002: App Router vs Pages Router

### Contexto

Next.js 13+ introdujo App Router como nuevo paradigma. Debemos decidir cuál usar.

### Decisión

**Usar App Router (src/app/).**

### Justificación

1. **Server Components:** Reducen bundle size significativamente
2. **Layouts anidados:** Mejor DX para layouts compartidos
3. **Streaming:** Mejor UX con Suspense boundaries
4. **Futuro:** Pages Router está en modo mantenimiento
5. **Parallel Routes:** Útil para modales y dashboards

### Consecuencias

- Código más moderno y mantenible
- Algunos third-party packages aún no 100% compatibles
- Más opciones puede confundir a developers nuevos

---

## ADR-003: Zustand para Estado Global

### Contexto

Necesitamos manejar estado global del cliente (no server state):

- Usuario autenticado
- Favoritos (optimistic updates)
- UI state (modales, sidebars)
- Preferencias

### Opciones Consideradas

| Librería      | Bundle Size | Boilerplate | DevTools | Learning Curve |
| ------------- | ----------- | ----------- | -------- | -------------- |
| **Zustand**   | 1.1 KB      | Mínimo      | ✅       | Baja           |
| Redux Toolkit | 10+ KB      | Medio       | ✅       | Media          |
| Jotai         | 2 KB        | Mínimo      | ⚠️       | Baja           |
| Recoil        | 20+ KB      | Medio       | ⚠️       | Media          |
| Context API   | 0 KB        | Alto        | ❌       | Baja           |

### Decisión

**Usar Zustand para estado cliente.**

### Justificación

1. **Tamaño:** 1.1 KB gzipped (Redux Toolkit es 10x más grande)
2. **API Simple:**
   ```typescript
   // Redux: actions, reducers, slices, selectors...
   // Zustand: un hook y listo
   const useStore = create((set) => ({
     count: 0,
     increment: () => set((s) => ({ count: s.count + 1 })),
   }));
   ```
3. **No Providers:** Funciona sin Context wrapper
4. **TypeScript:** Excelente inferencia de tipos
5. **DevTools:** Compatible con Redux DevTools
6. **Persistencia:** `zustand/persist` para localStorage
7. **SSR Ready:** Compatible con Next.js App Router

### Consecuencias

**Positivas:**

- Código más limpio y menos boilerplate
- Bundle más pequeño
- Fácil de aprender

**Negativas:**

- Menos estructura para equipos grandes
- Menos middleware ecosystem que Redux

---

## ADR-004: TanStack Query para Server State

### Contexto

Necesitamos manejar datos del servidor:

- Fetch de vehículos
- Cache
- Revalidation
- Optimistic updates
- Pagination

### Decisión

**Usar TanStack Query (React Query) v5.**

### Justificación

1. **Separación de Concerns:** Server state ≠ Client state
2. **Cache Inteligente:** Stale-while-revalidate built-in
3. **Deduplication:** Múltiples componentes, un solo request
4. **Background Updates:** Datos siempre frescos
5. **DevTools:** Excelente debugging
6. **Mutations:** Optimistic updates fáciles

```typescript
// Sin TanStack Query
const [data, setData] = useState(null);
const [loading, setLoading] = useState(true);
const [error, setError] = useState(null);

useEffect(() => {
  fetchVehicles()
    .then(setData)
    .catch(setError)
    .finally(() => setLoading(false));
}, []);

// Con TanStack Query
const { data, isLoading, error } = useQuery({
  queryKey: ["vehicles"],
  queryFn: fetchVehicles,
});
```

### Consecuencias

- Menos código para data fetching
- Mejor UX con cache
- Curva de aprendizaje inicial

---

## ADR-005: shadcn/ui como Sistema de Componentes

### Contexto

Necesitamos componentes UI consistentes y accesibles.

### Opciones Consideradas

| Librería       | Customizable | A11y | Bundle    | Copy-paste |
| -------------- | ------------ | ---- | --------- | ---------- |
| **shadcn/ui**  | ✅ Total     | ✅   | 0 KB base | ✅         |
| Material UI    | ⚠️ Difícil   | ✅   | ~90 KB    | ❌         |
| Chakra UI      | ✅           | ✅   | ~40 KB    | ❌         |
| Radix + Custom | ✅           | ✅   | Variable  | ✅         |
| Headless UI    | ✅           | ✅   | ~10 KB    | ❌         |

### Decisión

**Usar shadcn/ui.**

### Justificación

1. **Ownership:** Código copiado a tu proyecto, no dependencia
2. **Customización:** 100% control sobre estilos
3. **Accesibilidad:** Basado en Radix primitives (WCAG AA)
4. **Tailwind Native:** Consistente con nuestro stack
5. **Tree-shakeable:** Solo incluyes lo que usas
6. **Sin Breaking Changes:** Tu código, tus versiones

```bash
# No es npm install, es copiar componentes
npx shadcn-ui@latest add button
# Genera: src/components/ui/button.tsx
# Lo modificas como quieras
```

### Consecuencias

**Positivas:**

- Control total sobre UI
- Sin dependencias pesadas
- Diseño consistente

**Negativas:**

- Más archivos en el proyecto
- Actualizaciones manuales

---

## ADR-006: Tailwind CSS para Estilos

### Contexto

Necesitamos sistema de estilos escalable.

### Decisión

**Usar Tailwind CSS v3.4+.**

### Justificación

1. **Utility-First:** Prototipado rápido
2. **Purging:** Solo CSS usado en bundle final
3. **Consistency:** Design tokens vía config
4. **Performance:** CSS más pequeño que frameworks tradicionales
5. **DX:** Autocompletado en VSCode
6. **Responsive:** Breakpoints fáciles (`md:`, `lg:`)

### Alternativas Rechazadas

- **CSS Modules:** Más código, menos consistencia
- **Styled Components:** Runtime CSS, peor performance
- **Sass:** Más código, sin design tokens built-in

---

## ADR-007: pnpm como Package Manager

### Contexto

Elegir package manager para el monorepo.

### Decisión

**Usar pnpm.**

### Justificación

| Feature       | npm        | yarn  | pnpm        |
| ------------- | ---------- | ----- | ----------- |
| Disk usage    | ❌ Duplica | ⚠️    | ✅ Symlinks |
| Install speed | Lento      | Medio | ✅ Rápido   |
| Monorepo      | ⚠️         | ✅    | ✅          |
| Strict        | ❌         | ❌    | ✅          |

1. **Disk Space:** Usa symlinks, ahorra GB en node_modules
2. **Speed:** 2-3x más rápido que npm
3. **Strictness:** No phantom dependencies
4. **Monorepo:** Workspaces nativos

---

## ADR-008: Playwright para E2E Testing

### Contexto

Necesitamos testing E2E para flujos críticos.

### Opciones

| Tool           | Speed | API | Browsers | Flakiness |
| -------------- | ----- | --- | -------- | --------- |
| **Playwright** | ✅    | ✅  | ✅ All   | ✅ Low    |
| Cypress        | ⚠️    | ✅  | ⚠️       | ⚠️        |
| Selenium       | ❌    | ❌  | ✅       | ❌        |

### Decisión

**Usar Playwright.**

### Justificación

1. **Multi-browser:** Chrome, Firefox, Safari, Mobile
2. **Auto-waiting:** Menos flaky tests
3. **API Testing:** Built-in support
4. **Trace Viewer:** Debugging excelente
5. **Parallel:** Tests corren en paralelo
6. **Codegen:** Graba acciones y genera código

---

## ADR-009: NextAuth.js para Autenticación

### Contexto

Implementar autenticación con:

- Email/password
- OAuth (Google, Facebook)
- JWT tokens
- Refresh tokens

### Decisión

**Usar NextAuth.js (Auth.js) v5.**

### Justificación

1. **Next.js Native:** Integración perfecta
2. **Providers:** 50+ OAuth providers listos
3. **Credentials:** Email/password soportado
4. **JWT:** Built-in con encryption
5. **Session Management:** Automático
6. **TypeScript:** Tipos excelentes

### Alternativas Rechazadas

- **Auth0:** Vendor lock-in, costoso a escala
- **Firebase Auth:** Lock-in con Google
- **Custom JWT:** Más trabajo, más bugs potenciales

---

## ADR-010: Axios vs Fetch Nativo

### Contexto

Elegir cliente HTTP para comunicación con backend.

### Decisión

**Usar Axios.**

### Justificación

1. **Interceptors:** Request/response modification
2. **Error Handling:** Mejor que fetch
3. **Request Cancellation:** Nativo
4. **Timeout:** Configuración fácil
5. **Progress:** Upload progress tracking
6. **Transform:** Request/response transforms

```typescript
// Fetch: Handling manual
const res = await fetch("/api/...");
if (!res.ok) throw new Error(res.statusText);
const data = await res.json();

// Axios: Automático
const { data } = await axios.get("/api/...");
// Error handling via interceptor
```

### Consecuencias

- +15 KB al bundle (justificado por DX)
- API consistente en toda la app
- Interceptors centralizados

---

## 📊 RESUMEN DE STACK

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         STACK TECNOLÓGICO OKLA                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Framework:        Next.js 14+ (App Router)                                 │
│  Language:         TypeScript 5.x (strict mode)                             │
│  Styling:          Tailwind CSS 3.4+                                        │
│  Components:       shadcn/ui (Radix primitives)                             │
│  State (Client):   Zustand                                                  │
│  State (Server):   TanStack Query v5                                        │
│  Forms:            React Hook Form + Zod                                    │
│  Auth:             NextAuth.js v5                                           │
│  HTTP Client:      Axios                                                    │
│  Testing:          Vitest (unit) + Playwright (E2E)                         │
│  Package Manager:  pnpm                                                     │
│  Animations:       Framer Motion                                            │
│  Icons:            Lucide React                                             │
│  Date:             date-fns                                                 │
│  Linting:          ESLint + Prettier                                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 PROCESO DE ACTUALIZACIÓN

Cuando se tome una nueva decisión arquitectónica:

1. Crear nueva sección ADR-XXX en este documento
2. Incluir: Contexto, Opciones, Decisión, Justificación, Consecuencias
3. Actualizar tabla de índice
4. Comunicar al equipo
5. Actualizar código/docs afectados

---

## 📚 REFERENCIAS

- [Architectural Decision Records](https://adr.github.io/)
- [Next.js Documentation](https://nextjs.org/docs)
- [Zustand Documentation](https://docs.pmnd.rs/zustand)
- [TanStack Query](https://tanstack.com/query)
- [shadcn/ui](https://ui.shadcn.com/)
