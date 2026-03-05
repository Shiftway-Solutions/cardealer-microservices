# ✅ Checklist de Deployment - OKLA Frontend

> **Propósito:** Lista de verificación antes de cada deployment a producción
> **Audiencia:** DevOps, Desarrolladores
> **Última actualización:** Enero 31, 2026

---

## 🚀 PRE-DEPLOYMENT CHECKLIST

### 📋 Antes de Crear PR a Main

```markdown
## Código

- [ ] `pnpm lint` sin errores
- [ ] `pnpm typecheck` sin errores
- [ ] `pnpm build` exitoso
- [ ] `pnpm test` todos pasan
- [ ] No hay `console.log` en código de producción
- [ ] No hay `// TODO` críticos sin resolver
- [ ] No hay secrets hardcodeados

## Git

- [ ] Branch actualizado con main/development
- [ ] Commits siguen convención (feat/fix/docs)
- [ ] PR tiene descripción clara
- [ ] PR linked a issue/ticket
- [ ] Code review solicitado
```

---

### 🔐 Seguridad

```markdown
## Variables de Entorno

- [ ] Todas las variables de producción configuradas en Vercel/hosting
- [ ] NEXT*PUBLIC*\* solo contiene valores públicos
- [ ] API keys no expuestas en cliente
- [ ] Secrets rotados si fueron comprometidos

## Headers de Seguridad

- [ ] CSP (Content Security Policy) configurado
- [ ] X-Frame-Options: DENY
- [ ] X-Content-Type-Options: nosniff
- [ ] Referrer-Policy configurado
- [ ] Permissions-Policy configurado

## Autenticación

- [ ] Tokens JWT tienen expiración correcta
- [ ] Refresh tokens funcionan
- [ ] Logout limpia todos los tokens
- [ ] Rutas protegidas verificadas
```

---

### ⚡ Performance

```markdown
## Core Web Vitals (Lighthouse)

- [ ] LCP (Largest Contentful Paint) < 2.5s
- [ ] FID (First Input Delay) < 100ms
- [ ] CLS (Cumulative Layout Shift) < 0.1
- [ ] Performance Score ≥ 90

## Bundle Size

- [ ] Bundle principal < 200KB gzipped
- [ ] No dependencias duplicadas
- [ ] Tree-shaking funcionando
- [ ] Lazy loading de rutas pesadas

## Imágenes

- [ ] Todas las imágenes usan next/image
- [ ] WebP/AVIF habilitado
- [ ] Lazy loading en imágenes below-fold
- [ ] Placeholder blur configurado
```

---

### ♿ Accesibilidad

```markdown
## WCAG 2.1 AA

- [ ] Lighthouse Accessibility ≥ 90
- [ ] axe-core sin errores críticos
- [ ] Navegación por teclado funciona
- [ ] Skip to content link presente
- [ ] Focus visible en todos los elementos
- [ ] Contraste de colores ≥ 4.5:1
- [ ] Alt text en todas las imágenes
- [ ] Formularios con labels asociados
```

---

### 🔍 SEO

```markdown
## Meta Tags

- [ ] Title único por página
- [ ] Description ≤ 160 caracteres
- [ ] Open Graph tags completos
- [ ] Twitter cards configurados
- [ ] Canonical URLs correctos

## Técnico

- [ ] robots.txt correcto
- [ ] sitemap.xml generado y actualizado
- [ ] JSON-LD structured data válido
- [ ] URLs amigables (sin parámetros innecesarios)
- [ ] Redirects configurados (301 para URLs antiguas)
```

---

### 🧪 Testing

```markdown
## Tests Automatizados

- [ ] Unit tests pasan (Vitest)
- [ ] Integration tests pasan
- [ ] E2E tests pasan (Playwright)
- [ ] Coverage ≥ 80%

## Tests Manuales

- [ ] Flujo de login/registro
- [ ] Flujo de búsqueda de vehículos
- [ ] Flujo de publicación (vendedor)
- [ ] Flujo de checkout (pagos)
- [ ] Responsive en móvil real
- [ ] Cross-browser (Chrome, Firefox, Safari)
```

---

### 🌐 Internacionalización

```markdown
## i18n

- [ ] Textos en español (es-DO) completos
- [ ] Textos en inglés (en-US) completos
- [ ] Formato de fechas correcto (DD/MM/YYYY para RD)
- [ ] Formato de moneda correcto (RD$ y USD)
- [ ] Zona horaria correcta (America/Santo_Domingo)
```

---

## 📦 DEPLOYMENT CHECKLIST

### Durante el Deployment

```markdown
## Vercel/Hosting

- [ ] Build exitoso en preview
- [ ] Preview URL probada
- [ ] Variables de entorno de producción verificadas
- [ ] Dominio configurado correctamente
- [ ] SSL/TLS activo

## Database/Backend

- [ ] API backend está disponible
- [ ] Health check del gateway responde
- [ ] Migraciones de base de datos aplicadas
- [ ] Cache invalidado si necesario
```

---

## 🔄 POST-DEPLOYMENT CHECKLIST

### Inmediatamente Después

```markdown
## Smoke Tests (5 minutos)

- [ ] Homepage carga correctamente
- [ ] Login funciona
- [ ] Búsqueda de vehículos funciona
- [ ] Página de detalle de vehículo carga
- [ ] No errores en console del navegador
- [ ] No errores 500 en Network tab

## Monitoreo

- [ ] Sentry no reporta nuevos errores
- [ ] Analytics registrando eventos
- [ ] Uptime monitor activo
- [ ] Alertas configuradas
```

### Primeras 24 Horas

```markdown
## Observabilidad

- [ ] Error rate < 1%
- [ ] Latency p99 < 3s
- [ ] No memory leaks detectados
- [ ] CPU/Memory estable

## Feedback

- [ ] Revisar reportes de usuarios
- [ ] Monitorear canales de soporte
- [ ] Revisar métricas de conversión
```

---

## 🚨 ROLLBACK PLAN

### Criterios de Rollback

```markdown
## Automático (si configurado)

- Error rate > 5% por 5 minutos
- Latency p99 > 5s por 5 minutos
- Health check falla 3 veces consecutivas

## Manual

- Feature crítico roto (pagos, login)
- Data corruption detectada
- Vulnerabilidad de seguridad encontrada
```

### Proceso de Rollback

```bash
# Vercel - Rollback inmediato
vercel rollback

# O usando deployment específico
vercel rollback [deployment-url]

# Verificar rollback
curl -I https://okla.com.do/health
```

---

## 📊 MÉTRICAS A MONITOREAR

| Métrica     | Target  | Crítico |
| ----------- | ------- | ------- |
| Error Rate  | < 0.5%  | > 2%    |
| Latency p50 | < 500ms | > 1s    |
| Latency p99 | < 2s    | > 5s    |
| Uptime      | 99.9%   | < 99%   |
| LCP         | < 2.5s  | > 4s    |
| CLS         | < 0.1   | > 0.25  |

---

## 🔧 HERRAMIENTAS

| Herramienta     | Propósito         | URL                    |
| --------------- | ----------------- | ---------------------- |
| Vercel          | Hosting & Deploy  | vercel.com/okla        |
| Sentry          | Error Tracking    | sentry.io/okla         |
| Datadog/Grafana | Monitoring        | monitoring.okla.com.do |
| Lighthouse CI   | Performance       | En GitHub Actions      |
| Uptime Robot    | Uptime Monitoring | uptimerobot.com        |

---

## 📝 TEMPLATE DE RELEASE NOTES

```markdown
## [v1.X.X] - YYYY-MM-DD

### 🚀 Nuevas Funcionalidades

- Feature 1 description
- Feature 2 description

### 🐛 Correcciones

- Bug fix 1
- Bug fix 2

### ⚡ Mejoras de Performance

- Optimization 1

### 🔒 Seguridad

- Security fix (if any)

### ⚠️ Breaking Changes

- None / List breaking changes

### 📝 Notas de Migración

- Migration steps if needed
```

---

## 📚 REFERENCIAS

- [Vercel Deployment Docs](https://vercel.com/docs/deployments)
- [Next.js Production Checklist](https://nextjs.org/docs/pages/building-your-application/deploying/production-checklist)
- [OWASP Security Checklist](https://owasp.org/www-project-web-security-testing-guide/)
