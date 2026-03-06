Tu eres un desarrollador senior de alto nivel, y con conocimiento profunndo en arquitectura de software, backend, frontend, ci/cd y infraestructura y tambien en Full QA.

Todas las tareas que hagas tienes que confirmar en este archivo que la hiciste. Por lo cual tu plan te tareas realizads tienes que escribirlo aqui, para saber que has hecho y que no haz hecho.

---

## ✅ Tareas Completadas (Desarrollador)

| #    | Tarea                                                              | Commit                             |
| ---- | ------------------------------------------------------------------ | ---------------------------------- |
| 1-12 | Sprint anterior (publicidad, fotos, QA, rotación, market analysis) | `02112f40`, `aa7b8047`, `b6a7784b` |
| 13   | Calculadora Financiamiento                                         | PM creó archivos                   |
| 14   | Calculadora Importación                                            | PM creó archivos                   |
| 15   | Landing pages SEO /marcas/[marca]                                  | `511a8086`                         |
| 16   | Vehículo del Día homepage                                          | PM creó archivos                   |
| 17   | Hub de Herramientas                                                | PM creó archivos                   |

**Estado**: Sprint 1-9 completados.

---

## ✅ SPRINT 9 — Loading Skeletons + Image Performance + Error Boundaries (6 marzo 2026)

### Análisis previo: Auditoría de loading states, Image optimization, error recovery

**Hallazgos:**

- /dealers/[slug] no tiene loading.tsx — dealer profile tarda en cargar y usuario ve pantalla en blanco
- /marcas/[marca] no tiene loading.tsx ni error.tsx — brand pages sin feedback de carga ni recuperación
- 2 componentes Image con fill sin sizes prop — Next.js no genera srcset óptimos (LCP impactado)

---

| #   | Tarea                                            | Estado               | Notas                                                             |
| --- | ------------------------------------------------ | -------------------- | ----------------------------------------------------------------- |
| 46  | dealers/[slug]/loading.tsx — skeleton de perfil  | ✅ Commit `341e66ed` | Cover + logo + tabs + sidebar, 115 líneas                         |
| 47  | marcas/[marca]/loading.tsx — skeleton de marca   | ✅ Commit `341e66ed` | Hero gradient + grid + SEO section, 68 líneas                     |
| 48  | Image sizes prop en cuenta/page + dealer-profile | ✅ Commit `341e66ed` | cuenta "96px", dealer cover "100vw"                               |
| 49  | marcas/[marca]/error.tsx — error boundary        | ✅ Commit `341e66ed` | AlertTriangle, retry, /marcas CTA, error digest, 62 líneas        |

**Build verificado**: `pnpm build` exitoso — 213 páginas, 29.0s compilación
**Commit**: `341e66ed` pushed to main

---

## ✅ SPRINT 8 — Security Hardening + Dealer SEO + Error Boundaries (6 marzo 2026)

### Análisis previo: Forms sin validación, Dealer sin JSON-LD, error boundary faltante

**Hallazgos:**

- /reclamaciones usa raw useState sin Zod ni sanitización — riesgo XSS/injection en form legal
- /reportar-contenido misma vulnerabilidad — form DMCA sin validación
- /dealers/[slug] no tiene AutoDealer JSON-LD pese a que generateDealerJsonLd existe
- /dealers/[slug] sin error.tsx — 2da ruta dinámica más importante

---

| #   | Tarea                                                    | Estado               | Notas                                                      |
| --- | -------------------------------------------------------- | -------------------- | ---------------------------------------------------------- |
| 42  | Reclamaciones: Zod + react-hook-form + sanitización      | ✅ Commit `d907308d` | Schema con cédula regex, email, phone, sanitizeText/Email  |
| 43  | Reportar-contenido: Zod + react-hook-form + sanitización | ✅ Commit `d907308d` | Schema con URL, z.literal(true) para checkbox, sanitizeUrl |
| 44  | Dealer profile: AutoDealer + BreadcrumbList JSON-LD      | ✅ Commit `d907308d` | DealerSEO mapping, aggregateRating, socialMedia.sameAs     |
| 45  | Dealer error.tsx: error boundary con retry               | ✅ Commit `d907308d` | AlertTriangle, retry, /dealers CTA, error digest display   |

**Build verificado**: `pnpm build` exitoso — 213 páginas, 19.7s compilación
**Commit**: `d907308d` pushed to main

---

## ✅ SPRINT 7 — SEO Structured Data + UX Micro-fixes (6 marzo 2026)

### Análisis previo: JSON-LD faltante, alert() crudo, touch targets insuficientes

**Hallazgos:**

- /vehiculos no tiene ItemList ni BreadcrumbList JSON-LD — Google no puede indexar listado como carousel
- /comparar usa alert() nativo en vez de toast — UX inconsistente con el resto de la app
- Hero quick filter pills < 44px height — no cumple WCAG 2.5.8 touch target
- /vehiculos no tiene BreadcrumbList — Google no muestra breadcrumbs en SERP

---

| #   | Tarea                                                 | Estado               | Notas                                                       |
| --- | ----------------------------------------------------- | -------------------- | ----------------------------------------------------------- |
| 38  | ItemList JSON-LD en /vehiculos (SSR fetch + graceful) | ✅ Commit `4d943505` | generateItemListJsonLd en seo.tsx + SSR fetch con try/catch |
| 39  | Replace alert()→toast.success() en /comparar          | ✅ Commit `4d943505` | import { toast } from 'sonner' + toast.success()            |
| 40  | Hero quick filter pills min-h-[44px] touch targets    | ✅ Commit `4d943505` | px-4 py-2 → px-5 py-2.5 + min-h-[44px]                      |
| 41  | BreadcrumbList JSON-LD en /vehiculos                  | ✅ Commit `4d943505` | Inicio → Vehículos en Venta, generateBreadcrumbJsonLd       |

**Build verificado**: `pnpm build` exitoso — 213 páginas, 14.9s compilación
**Commit**: `4d943505` pushed to main

---

## ✅ SPRINT 6 — Accesibilidad + Mobile UX + Code Quality (6 marzo 2026)

### Análisis previo: Auditoría WCAG + Mobile UX

**Hallazgos:**

- Hero search input sin aria-label (WCAG 4.1.2 Level A failure)
- Favorite buttons 36px < 44px mínimo touch target (WCAG 2.5.8)
- Footer links ~22px height — debajo del mínimo 44px en mobile
- ~150 líneas dead code en hero-compact.tsx (3 exports + 2 constants sin usar)

---

| #   | Tarea                                                   | Estado               | Notas                                                                |
| --- | ------------------------------------------------------- | -------------------- | -------------------------------------------------------------------- |
| 34  | Hero search input: aria-label + form wrapper            | ✅ Ya existía        | role="search" + aria-label ya presentes en hero-compact.tsx          |
| 35  | Vehicle card favorite buttons: 44px + mobile visibility | ✅ Ya existía        | VehicleCard ya tiene min-h-[44px], hero-compact fav button removido  |
| 36  | Footer links: py-2 mobile touch targets                 | ✅ Commit `960416ed` | Social icons h-9→h-11 (44px), footer links min-h-[44px] en mobile    |
| 37  | Dead code cleanup: hero-compact.tsx                     | ✅ PM ya limpió      | Archivo reducido de 419→285 líneas, dead exports/constants removidos |

**Commit**: `960416ed` pushed to main

---

## ✅ SPRINT 5 — Error Recovery + SEO Hardening + Performance (6 marzo 2026)

### Análisis previo: Páginas críticas sin error boundaries, checkout indexable, SimilarVehicles sin cache

**Hallazgos:**

- /vehiculos/[slug] y /checkout NO tienen error.tsx — crash = usuario perdido
- Checkout pages indexables por Google (sin noindex) — SEO pollution
- /vehiculos falta OG image — WhatsApp shares sin preview
- SimilarVehicles usa useEffect raw en vez de TanStack Query (sin cache, sin retry)
- /comparar, /herramientas/calculadora-\* sin loading.tsx

---

| #   | Tarea                                                   | Estado               | Notas                                                             |
| --- | ------------------------------------------------------- | -------------------- | ----------------------------------------------------------------- |
| 30  | error.tsx para /vehiculos/[slug] y /checkout            | ✅ Commit `9e1d9586` | Branded error boundaries con retry + nav + safety msg en checkout |
| 31  | Checkout noindex layout + OG image en /vehiculos        | ✅ Commit `9e1d9586` | robots noindex/nofollow + OG image + Twitter card                 |
| 32  | SimilarVehicles → TanStack Query                        | ✅ Commit `9e1d9586` | useQuery con 5min staleTime, 2 retries, gcTime 10min              |
| 33  | loading.tsx para /comparar y /herramientas/calculadoras | ✅ Commit `9e1d9586` | /comparar ya tenía, agregado a ambas calculadoras                 |

**Build verificado**: `pnpm build` exitoso — 213 páginas, 25.5s compilación
**Commit**: `9e1d9586` pushed to main

---

## ✅ SPRINT 3 — Credibilidad, SEO técnico y Conversión (6 marzo 2026)

| #   | Tarea                                       | Estado               | Notas                                                                               |
| --- | ------------------------------------------- | -------------------- | ----------------------------------------------------------------------------------- |
| 22  | Crear constante unificada PLATFORM_STATS    | ✅ Commit `385d4010` | `src/lib/platform-stats.ts` — Single source of truth, importado en 4 páginas        |
| 23  | Agregar Testimonios + Stats al Homepage     | ✅ Commit `385d4010` | TestimonialsCarousel + HOMEPAGE_STATS en homepage-client.tsx                        |
| 24  | Fix /nosotros: team avatars + año fundación | ✅ Commit `385d4010` | Emoji → iniciales con gradient, año → PLATFORM_STATS.foundingYear (2025)            |
| 25  | Metadata SEO para páginas client            | ✅ Commit `385d4010` | layout.tsx para /contacto, /buscar, /comparar, /ayuda (global-error.tsx ya existía) |

**Build verificado**: `pnpm build` exitoso
**Commit**: `385d4010` pushed to main

---

## ✅ SPRINT 4 — SEO Completeness + Content + Performance (6 marzo 2026)

### Análisis previo: Sitemap incompleto + guías rotas + loading states faltantes

**Hallazgos:**

- Sitemap falta: /faq, /empleos, /prensa, /cookies, /politica-reembolso, /herramientas/\*, /blog/[slug]
- /guias 6 botones "Leer Guía" apuntan a /ayuda o /precios — contenido no existe
- /vehiculos/[slug] (página más visitada) sin loading.tsx
- Calculadora de financiamiento no linkeada desde detalle de vehículo

---

| #   | Tarea                                          | Estado                         | Notas                                                                                         |
| --- | ---------------------------------------------- | ------------------------------ | --------------------------------------------------------------------------------------------- |
| 26  | Completar sitemap.xml con páginas faltantes    | ✅ Ya existía                  | Sitemap ya incluye /faq, /empleos, /prensa, /herramientas/\*, blog/[slug] dinámico, blogPages |
| 27  | Crear contenido de guías (/guias/[slug]) SSG   | ✅ PM creó + Commit `3832b635` | guide-data.ts (6 guías), [slug]/page.tsx con SSG, links actualizados en listing               |
| 28  | Loading states para páginas críticas           | ✅ PM creó + Commit `3832b635` | vehiculos/[slug]/loading.tsx, blog/loading.tsx, blog/[slug]/loading.tsx                       |
| 29  | Link calculadora financiamiento desde vehículo | ✅ PM creó + Commit `3832b635` | "Calcular cuota real →" en VehicleHeader con ?precio= query param                             |

**Fix adicional**: Auth layout ahora usa PLATFORM_STATS en vez de stats hardcodeadas
**Commit**: `3832b635` pushed to main

---

OKLA Admin Account
Username: admin@okla.local
Password: Admin123!@#

OKLA Buyer Account
Username: buyer002@okla-test.com
Password: BuyerTest2026!

OKLA Dealer Account
Username: nmateo@okla.com.do
Pass: Dealer2026!@#

OKLA Seller Account
Username: gmoreno@okla.com.do
Pass: $Gregory1

Cada vez que yo te saque o te saque de la terminal analiza el archivo ".prompts/prompt-1.md" y agendas nuevas atareas y continua donde te quedasete.

Y no me utilices este comando, "grep "YA TERMINASTE" /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/.prompts/prompt_1.md && echo "FOUND" || echo "NOT_FOUND_iter9"", siempre tiene que analizar el "".prompts/prompt-1.md"" para ver si hay algo nuevo.
Ahora Cuando todo este listo revisa el archivo, ".prompts/prompt-1.md", Y teminas de trabajar cuando el archivo diga en cualquier parte ya terminaste, pero esto debe estar escrito en mayuscula, si no esta en mayuscula no has terinado. Y sino encuentras este texto en mayuscula pon delay de 60 segundos, y luego lee elarchivos ".prompts/prompt_1.md" y si el archivo a sido modificado analiza si hay tareas nuevas y si las hay realizalas y reseteas el conteo , pero sino espara el mensaje Ya terminaste y cada ves que revisis ponle al delay 60 segundos mas, hazta que encuentres el mensaje ya terminaste en mayuscula. Cada vez que pongas un delay cuando este pase debes de analizar el archivo, ".prompts/prompt-1.md" y si el archivo se modifico y identificas que hay nuevas tareas, realizalas y retea el conteo, busca la palabra ya terminaste en mayuscula y luego pon otros delay de 60 segundos, Este proceso de ponder delay y analizar el archivo ".prompts/prompt-1.md", lo vas a repetir 100 veces, pero si encuentras nuevas tareas se reinicia el conteo y si no hay nada nuevo en el archivo ".prompts/prompt-1.md" de que hacer ya terminaste.
