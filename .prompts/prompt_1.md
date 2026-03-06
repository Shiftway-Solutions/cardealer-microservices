Tu eres un desarrollador senior de alto nivel, y con conocimiento profunndo en arquitectura de software, backend, frontend, ci/cd y infraestructura y tambien en Full QA.

Todas las tareas que hagas tienes que confirmar en este archivo que la hiciste. Por lo cual tu plan te tareas realizads tienes que escribirlo aqui, para saber que has hecho y que no haz hecho.

Tambien tienes como responsabilidad, monitorear el ci/cd, y probar los nuevos feature en producccion haciendo pruebas QA y corriendo los errores encontrados y probar nuevamente en produccion.

---

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

---

## � QA Production Report (2026-03-06)

### Bug Found & Fixed

- **`/marcas/[marca]` and `/herramientas/*` redirecting to login** — These SEO-critical public pages were missing from `publicRoutes` in middleware.ts. Also added `/blog/[slug]` and `/guias/[slug]`.
- **Commit:** `5b521504` — Pushed to main, awaiting CI/CD deploy.

### Pages Tested ✅

| Page                                       | Status   | Notes                                               |
| ------------------------------------------ | -------- | --------------------------------------------------- |
| `/` (homepage)                             | ✅ OK    | Hero, search, vehicles, dealers, categories, footer |
| `/vehiculos`                               | ✅ OK    | 14 vehicles, filters, sponsored section             |
| `/vehiculos/[slug]`                        | ✅ OK    | Images, price, financing estimate, similar vehicles |
| `/blog`                                    | ✅ OK    | 6 articles, categories, subscribe                   |
| `/guias`                                   | ✅ OK    | 6 guides, 5 tips, FAQ                               |
| `/faq`                                     | ✅ OK    | 4 categories, help links                            |
| `/dealers`                                 | ✅ OK    | Plans, testimonials, features                       |
| `/comparar`                                | ✅ OK    | Empty state with CTA                                |
| `/nosotros`                                | ✅ OK    | History, values, team                               |
| `/precios`                                 | ✅ OK    | Price ranges, factors                               |
| `/contacto`                                | ✅ OK    | Contact info, form                                  |
| `/vender`                                  | ✅ OK    | Steps, plans, CTA                                   |
| `/terminos`                                | ✅ OK    | 15 sections, Ley 358-05                             |
| `/login`                                   | ✅ OK    | Google/Apple SSO, email form                        |
| `/registro`                                | ✅ OK    | Buyer/seller toggle, social auth                    |
| `/reclamaciones`                           | ✅ OK    | Correctly requires auth                             |
| `/marcas/toyota`                           | 🔧 Fixed | Was redirecting to login                            |
| `/herramientas/calculadora-financiamiento` | 🔧 Fixed | Was redirecting to login                            |

### Observation

- E2E test vehicles visible in production listings ("E2E mm8mioxc", "E2E Test - DO NOT BUY", "Test Seller 20260223") — consider cleanup.

---

## �📋 Sprints Completados

### Sprint 12 — Security, Accessibility & Design Token Cleanup (2026-03-06) ✅

**Commit:** `6b00b544` | **Build:** 213 páginas, 12.5s

- [x] **Task 58**: csrfFetch en admin/okla-score (POST configs), view360-step (POST upload), push-notifications (POST subscribe/unsubscribe) — 4 endpoints
- [x] **Task 59**: aria-label en 19 icon-only buttons: mensajes (7), leads (4), preview (4), calendario (4)
- [x] **Task 60-61**: Limpieza masiva de ~200+ hex colors → design tokens en 70+ archivos

### Sprint 11 — Security, Forms & Design Tokens (2026-03-06) ✅

**Commit:** `a541e0cb` | **Build:** 213 páginas, 17.3s

- [x] **Task 54**: csrfFetch en vender/leads (POST+PATCH), vender/importar (POST×2), vehiculos-client (POST×2) — 6 endpoints protegidos
- [x] **Task 55**: convert-to-seller refactorizado con Zod schema + react-hook-form + errores por campo
- [x] **Task 56**: Eliminado `as any` en vender/registro (Zod SafeParse). Webcam `as any` mantenido (incompatibilidad de tipos Next.js dynamic)
- [x] **Task 57**: #00A870/#007850 → bg-primary/text-primary en 5 error.tsx (dealers, marcas, blog, guias, checkout)

### Sprints anteriores: 1-10 completados (ver docs/reportes/sprints/)
