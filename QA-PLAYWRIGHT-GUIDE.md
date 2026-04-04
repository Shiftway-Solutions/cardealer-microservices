# 🎬 Playwright QA - Chrome Screenshots Guide

## Instalación ✅ COMPLETADA

Se han instalado e configurado todos los componentes necesarios:

- ✅ `@playwright/test` - Framework E2E testing
- ✅ Navegadores en Chrome (Chromium) - Descargados
- ✅ Configuración `playwright.config.ts` - Optimizada
- ✅ Tests de ejemplo en `e2e/`
- ✅ Scripts de QA en `scripts/qa-screenshots.sh`

## Uso Rápido

### 1️⃣ Ejecutar tests con screenshots automáticos

```bash
# Todos los tests
npx playwright test

# Tests específicos
npx playwright test e2e/login.spec.ts

# En modo "headed" (ver el navegador ejecutándose)
npx playwright test --headed

# Solo Chrome
npx playwright test --project=chromium

# Chrome Mobile
npx playwright test --project=chromium-mobile
```

### 2️⃣ Usar script helper

```bash
# Todos los tests
./scripts/qa-screenshots.sh

# Test específico
./scripts/qa-screenshots.sh login
```

### 3️⃣ Ver reportes interactivos

```bash
# HTML Report (muy bonito)
npx playwright show-report

# JSON report
cat test-results/results.json | jq
```

### 4️⃣ Screenshots por demanda

Dentro de un test:

```typescript
// Screenshot automático en caso de fallo
use: {
  screenshot: "only-on-failure";
}

// O manual
await page.screenshot({
  path: "screenshots/my-page.png",
  fullPage: true, // Si false, solo viewport actual
});

// Screenshot de un elemento específico
await page.locator("#user-menu").screenshot({
  path: "screenshots/user-menu.png",
});
```

## Carpetas Generadas

```
- e2e/                     # Tests de ejemplo
- screenshots/             # Capturas de pantalla
- playwright-report/       # Reporte HTML interactivo
- test-results/           # Resultados en JSON/JUnit
```

## Configuración Actual

| Setting                   | Valor                     |
| ------------------------- | ------------------------- |
| **Navegadores**           | Chrome (Chromium), Mobile |
| **Screenshot on failure** | ✅ Automático             |
| **Video recording**       | ✅ Solo en fallos         |
| **Trace capturing**       | ✅ Primer retry           |
| **Parallelización**       | 1 worker (QA estable)     |
| **Base URL**              | `http://localhost:3000`   |
| **Timeout acciones**      | 10s por acción            |

## Scripts npm disponibles

Agrega esto a `package.json` para accesos rápidos:

```json
{
  "scripts": {
    "test:e2e": "playwright test",
    "test:e2e:headed": "playwright test --headed",
    "test:e2e:chrome": "playwright test --project=chromium",
    "test:e2e:debug": "playwright test --debug",
    "test:report": "playwright show-report"
  }
}
```

## Ejemplos de Pruebas

### Login

```bash
npx playwright test e2e/login.spec.ts --headed
```

### Búsqueda Marketplace

```bash
npx playwright test e2e/login.spec.ts -g "Search Vehicles"
```

## Debug Mode (Debugger Integrado)

```bash
# Abre inspector visual de Playwright
npx playwright test --debug

# O con un test específico
npx playwright test e2e/login.spec.ts --debug
```

## Limitaciones & Tips

⚠️ **No funciona sin navegador**

- Requiere que localhost:3000 esté corriendo
- Si no está corriendo, el script lo intenta iniciar automáticamente

✅ **Best Practices**

- Un test = una funcionalidad
- Use `expect()` para validaciones
- Screenshots en puntos clave (antes/después de acción)
- Mantén IDs estables en HTML: `data-testid="elemento"`

📊 **Performance**

- Primer test es lento (carga navegador)
- Los siguientes son rápidos (~10s por test)
- Para CI/CD es ideal

---

**¿Necesitas más?** Revisa la documentación oficial:
https://playwright.dev/docs/intro
