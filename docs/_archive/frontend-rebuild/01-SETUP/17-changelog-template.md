# 📝 Changelog - OKLA Frontend

> **Formato:** [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
> **Versionado:** [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
> **Última actualización:** Enero 31, 2026

---

## Guía de Versionado

```
MAJOR.MINOR.PATCH

MAJOR: Cambios incompatibles con versiones anteriores
MINOR: Nueva funcionalidad compatible con versiones anteriores
PATCH: Correcciones de bugs compatibles con versiones anteriores
```

---

## [Unreleased]

### 🚀 Added

- _Nuevas funcionalidades pendientes de release_

### 🔄 Changed

- _Cambios en funcionalidades existentes_

### 🗑️ Deprecated

- _Funcionalidades que serán eliminadas_

### ❌ Removed

- _Funcionalidades eliminadas_

### 🐛 Fixed

- _Correcciones de bugs_

### 🔒 Security

- _Correcciones de seguridad_

---

## [1.0.0] - 2026-02-01

### 🚀 Added

- Homepage con secciones dinámicas (Carousel, Featured, Categories)
- Búsqueda avanzada de vehículos con filtros
- Página de detalle de vehículo con galería y 360°
- Sistema de autenticación (email, Google, Facebook)
- Dashboard de usuario (buyer)
- Dashboard de vendedor individual
- Dashboard de dealer con CRM básico
- Panel de administración
- Sistema de favoritos
- Comparador de vehículos (hasta 3)
- Alertas de precio
- Búsquedas guardadas
- Sistema de mensajería
- Notificaciones en tiempo real
- Checkout con Stripe y AZUL
- Internacionalización (es-DO, en-US)

### 🔒 Security

- Implementación de NextAuth.js para autenticación
- CSRF protection habilitado
- Rate limiting en formularios
- Sanitización de inputs

---

## Template para Nuevas Entradas

```markdown
## [X.Y.Z] - YYYY-MM-DD

### 🚀 Added

- Nueva funcionalidad X que permite a los usuarios hacer Y

### 🔄 Changed

- Componente X ahora soporta prop Y
- Mejora de performance en página Z (LCP -500ms)

### 🐛 Fixed

- Corregido error donde el formulario no validaba correctamente el RNC
- Solucionado problema de scroll en Safari iOS

### 🔒 Security

- Actualizada dependencia vulnerable (CVE-XXXX-XXXX)
```

---

## Categorías Disponibles

| Emoji | Categoría     | Descripción                           |
| ----- | ------------- | ------------------------------------- |
| 🚀    | Added         | Nuevas funcionalidades                |
| 🔄    | Changed       | Cambios en funcionalidades existentes |
| 🗑️    | Deprecated    | Funcionalidades que serán eliminadas  |
| ❌    | Removed       | Funcionalidades eliminadas            |
| 🐛    | Fixed         | Correcciones de bugs                  |
| 🔒    | Security      | Correcciones de seguridad             |
| ⚡    | Performance   | Mejoras de rendimiento                |
| 📝    | Documentation | Cambios en documentación              |
| ♿    | Accessibility | Mejoras de accesibilidad              |

---

## Convenciones de Commits

Los commits deben seguir [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Tipos

| Tipo       | Descripción               | Ejemplo                                    |
| ---------- | ------------------------- | ------------------------------------------ |
| `feat`     | Nueva funcionalidad       | `feat(search): add price range filter`     |
| `fix`      | Corrección de bug         | `fix(auth): resolve token refresh issue`   |
| `docs`     | Documentación             | `docs(readme): update installation steps`  |
| `style`    | Estilo (no afecta lógica) | `style(button): adjust padding`            |
| `refactor` | Refactorización           | `refactor(api): simplify error handling`   |
| `perf`     | Mejora de performance     | `perf(images): optimize lazy loading`      |
| `test`     | Tests                     | `test(checkout): add e2e for payment flow` |
| `chore`    | Tareas de mantenimiento   | `chore(deps): update dependencies`         |
| `ci`       | CI/CD                     | `ci(github): add storybook deployment`     |

### Scopes Comunes

- `auth` - Autenticación
- `search` - Búsqueda
- `vehicle` - Vehículos
- `dealer` - Dealers
- `admin` - Administración
- `checkout` - Pagos
- `ui` - Componentes UI
- `api` - Integración API
- `i18n` - Internacionalización

---

## Automatización con Release-Please

```yaml
# .github/workflows/release.yml
name: Release

on:
  push:
    branches: [main]

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: google-github-actions/release-please-action@v4
        with:
          release-type: node
          package-name: okla-frontend
```

### Configuración release-please

```json
// release-please-config.json
{
  "packages": {
    ".": {
      "release-type": "node",
      "bump-minor-pre-major": true,
      "bump-patch-for-minor-pre-major": true,
      "changelog-path": "CHANGELOG.md",
      "versioning": "default"
    }
  }
}
```

---

## Ejemplo Completo

```markdown
# Changelog

All notable changes to OKLA Frontend will be documented in this file.

## [Unreleased]

### 🚀 Added

- Dark mode support

---

## [1.2.0] - 2026-03-15

### 🚀 Added

- Filtro de vehículos por provincia
- Notificaciones push (PWA)
- Export de comparación a PDF

### 🔄 Changed

- Rediseño del VehicleCard con nuevo layout
- Mejora en el flujo de publicación (wizard 5 pasos → 3 pasos)

### 🐛 Fixed

- Corregido error de paginación en búsqueda cuando hay filtros activos
- Solucionado problema de cache en favoritos

### ⚡ Performance

- Reducido bundle size en 15% mediante tree-shaking
- Implementado ISR en páginas de vehículos (revalidate: 60)

---

## [1.1.0] - 2026-02-15

### 🚀 Added

- Sistema de reviews para dealers
- Chat en tiempo real (WebSocket)
- Visor 360° de vehículos

### 🔒 Security

- Implementado rate limiting en API calls
- Añadido honeypot en formularios contra spam

---

## [1.0.1] - 2026-02-05

### 🐛 Fixed

- Corregido error de CORS en subida de imágenes
- Solucionado problema de sesión expirada sin notificación

---

## [1.0.0] - 2026-02-01

### 🚀 Added

- Release inicial de OKLA Frontend
- Ver sección completa arriba
```

---

## 📚 Referencias

- [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Release Please](https://github.com/google-github-actions/release-please-action)
