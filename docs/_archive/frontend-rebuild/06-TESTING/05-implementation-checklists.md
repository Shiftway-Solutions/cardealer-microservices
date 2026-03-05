# ✅ Checklists de Implementación

> **Propósito:** Listas de verificación para cada tipo de componente
> **Audiencia:** Desarrolladores, IA que implementa
> **Última actualización:** Enero 31, 2026

---

## 📋 ÍNDICE

1. [Checklist por Tipo de Componente](#-checklist-por-tipo-de-componente)
2. [Checklist por Página](#-checklist-por-página)
3. [Checklist de Quality Assurance](#-checklist-de-quality-assurance)
4. [Checklist Pre-Deploy](#-checklist-pre-deploy)

---

## 🧩 CHECKLIST POR TIPO DE COMPONENTE

### UI Component (Atómico)

Usar para: Button, Input, Badge, Avatar, Icon, etc.

```markdown
## [ComponentName]

### Requisitos Funcionales

- [ ] Props tipadas con TypeScript
- [ ] Props documentadas con JSDoc
- [ ] Variantes implementadas (variant, size, color)
- [ ] Estado disabled funcional
- [ ] Estado loading funcional (si aplica)
- [ ] Children/Slot support (si aplica)

### Accesibilidad (A11y)

- [ ] role ARIA correcto
- [ ] aria-label donde aplique
- [ ] aria-describedby para errores
- [ ] Focusable con Tab
- [ ] Focus visible (ring)
- [ ] Contraste de colores ≥ 4.5:1
- [ ] Funciona con screen reader

### Responsive

- [ ] Funciona en mobile (320px)
- [ ] Funciona en tablet (768px)
- [ ] Funciona en desktop (1024px+)
- [ ] Touch targets ≥ 44px en móvil

### Testing

- [ ] Unit test: renders without crashing
- [ ] Unit test: props funcionan
- [ ] Unit test: eventos disparan
- [ ] Unit test: estados visuales
- [ ] Storybook story creada

### Código

- [ ] ESLint sin errores
- [ ] Prettier formateado
- [ ] No any types
- [ ] Componente exportado en index.ts
```

---

### Feature Component (Compuesto)

Usar para: VehicleCard, SearchFilters, MessageThread, etc.

```markdown
## [ComponentName]

### Requisitos Funcionales

- [ ] Props tipadas con interfaces
- [ ] Manejo de estado (local o global)
- [ ] Conectado a API (useQuery/useMutation)
- [ ] Loading state UI
- [ ] Error state UI
- [ ] Empty state UI
- [ ] Success feedback

### Accesibilidad

- [ ] Navegación por teclado completa
- [ ] Focus management correcto
- [ ] Anuncios para screen readers
- [ ] Regiones ARIA definidas

### Responsive

- [ ] Mobile-first CSS
- [ ] Breakpoints sm/md/lg/xl
- [ ] Imágenes responsive (srcset o next/image)
- [ ] Scroll horizontal controlado

### Performance

- [ ] Imágenes optimizadas
- [ ] Lazy loading donde aplique
- [ ] Skeleton loaders
- [ ] Debounce en inputs de búsqueda
- [ ] Virtualización si lista larga

### Testing

- [ ] Unit tests para lógica
- [ ] Integration test con API mock
- [ ] E2E test para flujo crítico
- [ ] Storybook con todos los estados

### Código

- [ ] Custom hooks extraídos
- [ ] Separación de concerns
- [ ] Memoización donde necesario
- [ ] Error boundaries si aplica
```

---

### Form Component

Usar para: LoginForm, RegisterForm, VehicleForm, etc.

```markdown
## [FormName]

### Requisitos Funcionales

- [ ] React Hook Form integrado
- [ ] Zod schema de validación
- [ ] Todos los campos requeridos marcados
- [ ] Validación en tiempo real
- [ ] Validación al submit
- [ ] Error messages en español
- [ ] Success message/redirect

### Campos

- [ ] Labels asociados a inputs
- [ ] Placeholders informativos
- [ ] Helper text donde necesario
- [ ] Contador de caracteres (si max)
- [ ] Máscaras de input (teléfono, RNC)

### Accesibilidad

- [ ] Labels con htmlFor
- [ ] aria-invalid en errores
- [ ] aria-describedby para mensajes
- [ ] Tab order lógico
- [ ] Submit con Enter funciona

### UX

- [ ] Loading state en submit
- [ ] Disable form durante submit
- [ ] Retry en error de red
- [ ] Dirty state tracking
- [ ] Unsaved changes warning

### Testing

- [ ] Test: campos requeridos
- [ ] Test: validaciones
- [ ] Test: submit success
- [ ] Test: submit error
- [ ] Test: keyboard navigation
```

---

### Modal/Dialog Component

Usar para: ConfirmDialog, ImageGallery, FilterSheet, etc.

```markdown
## [ModalName]

### Requisitos Funcionales

- [ ] Open/Close controlado o uncontrolled
- [ ] onOpenChange callback
- [ ] Backdrop click cierra (o no, según UX)
- [ ] ESC key cierra
- [ ] Animación de entrada/salida

### Accesibilidad

- [ ] role="dialog"
- [ ] aria-modal="true"
- [ ] aria-labelledby (título)
- [ ] aria-describedby (contenido)
- [ ] Focus trap activado
- [ ] Focus returns al cerrar
- [ ] Inert en contenido detrás

### Responsive

- [ ] Full screen en mobile
- [ ] Centrado en desktop
- [ ] Max-height con scroll
- [ ] Safe area padding en iOS

### Testing

- [ ] Test: abre correctamente
- [ ] Test: cierra con X
- [ ] Test: cierra con ESC
- [ ] Test: focus trap
- [ ] Test: backdrop click
```

---

### Layout Component

Usar para: Navbar, Footer, Sidebar, Container, etc.

```markdown
## [LayoutName]

### Requisitos Funcionales

- [ ] Responsive breakpoints
- [ ] Navegación principal accesible
- [ ] Mobile menu funcional
- [ ] Active state en links
- [ ] User menu (si auth)

### SEO

- [ ] Semantic HTML (header, nav, main, footer)
- [ ] Skip to content link
- [ ] Breadcrumbs si aplica

### Accesibilidad

- [ ] Landmarks ARIA
- [ ] Navegación por teclado
- [ ] Menu toggle accesible
- [ ] Focus visible en todos los links

### Performance

- [ ] Navbar sticky con will-change
- [ ] Lazy load de avatar
- [ ] Prefetch de rutas principales

### Testing

- [ ] Test: navegación funciona
- [ ] Test: responsive breakpoints
- [ ] Test: mobile menu toggle
- [ ] E2E: navegación entre páginas
```

---

## 📄 CHECKLIST POR PÁGINA

### Página Pública (Landing, Vehicle Detail, etc.)

```markdown
## [PageName]

### SEO

- [ ] Metadata (title, description)
- [ ] Open Graph tags
- [ ] Twitter cards
- [ ] Canonical URL
- [ ] JSON-LD structured data
- [ ] Alt text en imágenes
- [ ] H1 único y descriptivo
- [ ] Heading hierarchy (H1 > H2 > H3)

### Performance

- [ ] Core Web Vitals target:
  - [ ] LCP < 2.5s
  - [ ] FID < 100ms
  - [ ] CLS < 0.1
- [ ] Server-side rendering (SSR/SSG)
- [ ] Images en next/image
- [ ] Fonts preloaded
- [ ] Critical CSS inlined

### Accesibilidad

- [ ] Page title descriptivo
- [ ] Main landmark único
- [ ] Skip to content funciona
- [ ] Focus management en navegación
- [ ] Anuncios de carga

### Responsive

- [ ] Mobile: 320px - 767px
- [ ] Tablet: 768px - 1023px
- [ ] Desktop: 1024px+
- [ ] Text legible sin zoom
- [ ] No horizontal scroll

### Testing

- [ ] Lighthouse score ≥ 90 (todas las categorías)
- [ ] E2E: flujo principal funciona
- [ ] Cross-browser: Chrome, Firefox, Safari
- [ ] Mobile testing en device real
```

---

### Página Privada (Dashboard, Profile, etc.)

```markdown
## [PageName]

### Auth

- [ ] ProtectedRoute wrapper
- [ ] Redirect a login si no auth
- [ ] Redirect a home post-login
- [ ] Session refresh automático

### Data Fetching

- [ ] useQuery con queryKey único
- [ ] staleTime/gcTime configurados
- [ ] Loading skeleton
- [ ] Error boundary
- [ ] Empty state
- [ ] Retry automático en error

### Optimistic Updates

- [ ] useMutation configurado
- [ ] onMutate: optimistic update
- [ ] onError: rollback
- [ ] onSuccess: invalidate queries
- [ ] Toast de confirmación

### UX

- [ ] Breadcrumbs de navegación
- [ ] Page title dinámico
- [ ] Loading states granulares
- [ ] Confirmación antes de acciones destructivas

### Testing

- [ ] Test: requiere autenticación
- [ ] Test: carga datos correctamente
- [ ] Test: mutations funcionan
- [ ] Test: error handling
```

---

### Página de Formulario (Create, Edit)

```markdown
## [FormPageName]

### Form State

- [ ] React Hook Form setup
- [ ] Zod schema completo
- [ ] Default values cargados
- [ ] Reset form funciona
- [ ] Dirty checking activo

### Validation

- [ ] Client-side validation
- [ ] Server-side validation handling
- [ ] Error display por campo
- [ ] Summary de errores (opcional)

### Submit Flow

- [ ] Loading indicator
- [ ] Disable submit durante carga
- [ ] Success redirect
- [ ] Error toast/alert
- [ ] Retry disponible

### UX

- [ ] Autosave (opcional)
- [ ] Leave confirmation si dirty
- [ ] Progress indicator (multi-step)
- [ ] Cancel/Discard action
- [ ] Preview antes de submit

### Testing

- [ ] Test: validación de campos
- [ ] Test: submit exitoso
- [ ] Test: submit con error
- [ ] Test: cancel/discard
- [ ] E2E: flujo completo
```

---

## 🔍 CHECKLIST DE QUALITY ASSURANCE

### Antes de cada PR

```markdown
## PR Checklist

### Código

- [ ] Lint sin errores (`pnpm lint`)
- [ ] TypeScript sin errores (`pnpm typecheck`)
- [ ] Tests pasan (`pnpm test`)
- [ ] Build exitoso (`pnpm build`)

### Funcionalidad

- [ ] Feature funciona según especificación
- [ ] Edge cases considerados
- [ ] Error handling completo
- [ ] No regresiones en features existentes

### Accesibilidad

- [ ] axe-core sin errores críticos
- [ ] Navegación por teclado probada
- [ ] Screen reader probado (VoiceOver/NVDA)
- [ ] Color contrast verificado

### Responsive

- [ ] Probado en iPhone SE (375px)
- [ ] Probado en iPad (768px)
- [ ] Probado en Desktop (1920px)

### Performance

- [ ] No memory leaks
- [ ] No re-renders innecesarios
- [ ] Bundle size no aumentó drásticamente

### Documentación

- [ ] README actualizado (si cambio significativo)
- [ ] Storybook actualizado
- [ ] Comentarios en código complejo
```

---

### Review de Diseño

```markdown
## Design Review Checklist

### Fidelidad al Diseño

- [ ] Spacing correcto (8px grid)
- [ ] Typography correcta (font, size, weight)
- [ ] Colors del design system
- [ ] Border radius consistente
- [ ] Shadows correctas

### Interacciones

- [ ] Hover states
- [ ] Active states
- [ ] Focus states
- [ ] Loading states
- [ ] Error states
- [ ] Empty states
- [ ] Disabled states

### Animaciones

- [ ] Transiciones suaves (150-300ms)
- [ ] No jank visible
- [ ] Respeta prefers-reduced-motion

### Consistencia

- [ ] Componentes del design system usados
- [ ] No colores hardcoded
- [ ] No spacing hardcoded
- [ ] Patrones UI consistentes
```

---

## 🚀 CHECKLIST PRE-DEPLOY

### Staging Deploy

```markdown
## Staging Checklist

### Build

- [ ] `pnpm build` exitoso
- [ ] No warnings en console
- [ ] Environment variables configuradas
- [ ] Secrets no expuestos

### Smoke Tests

- [ ] Homepage carga
- [ ] Login funciona
- [ ] Página crítica funciona (Búsqueda)
- [ ] API conecta correctamente
- [ ] Images cargan

### Performance

- [ ] Lighthouse Performance ≥ 85
- [ ] Lighthouse Accessibility ≥ 90
- [ ] Lighthouse Best Practices ≥ 90
- [ ] Lighthouse SEO ≥ 90

### Monitoring

- [ ] Error tracking configurado (Sentry)
- [ ] Analytics configurado
- [ ] RUM configurado (si aplica)
```

---

### Production Deploy

```markdown
## Production Checklist

### Pre-Deploy

- [ ] Changelog actualizado
- [ ] Version bump en package.json
- [ ] QA sign-off obtenido
- [ ] Rollback plan documentado
- [ ] Comunicación a stakeholders

### Deploy

- [ ] Feature flags configurados
- [ ] Canary deploy (si aplica)
- [ ] Zero-downtime verificado
- [ ] Database migrations ejecutadas

### Post-Deploy

- [ ] Smoke tests en producción
- [ ] Monitoring dashboards revisados
- [ ] Error rates normales
- [ ] Performance metrics normales
- [ ] User feedback channels monitoreados

### Rollback Criteria

- [ ] Error rate > 5%
- [ ] Latency p99 > 3s
- [ ] Core feature broken
- [ ] Payment flow broken
```

---

## 📊 TEMPLATE: Nuevo Componente

Copiar esto al crear un nuevo componente:

```markdown
# [ComponentName]

## Descripción

[Breve descripción del componente]

## Ubicación

`src/components/[category]/[ComponentName].tsx`

## Props

| Prop | Tipo | Default | Descripción |
| ---- | ---- | ------- | ----------- |
| ...  | ...  | ...     | ...         |

## Checklist de Implementación

### Funcionalidad

- [ ] Props tipadas
- [ ] Estados implementados
- [ ] Eventos configurados

### A11y

- [ ] ARIA roles
- [ ] Focus management
- [ ] Screen reader tested

### Responsive

- [ ] Mobile OK
- [ ] Tablet OK
- [ ] Desktop OK

### Testing

- [ ] Unit tests
- [ ] Storybook

## Notas de Implementación

[Cualquier nota relevante]
```

---

## 📚 REFERENCIAS

- [Web Content Accessibility Guidelines (WCAG)](https://www.w3.org/WAI/WCAG21/quickref/)
- [Core Web Vitals](https://web.dev/vitals/)
- [Testing Library Best Practices](https://testing-library.com/docs/guiding-principles)
- [Component Story Format](https://storybook.js.org/docs/react/api/csf)
