# ⚡ QUICK START — QA Frontend Destacados (5 min setup)

## 🎯 TL;DR — Si tienes prisa

```bash
# 1. Setup
cd ~/Developer/Web/Backend/cardealer-microservices/frontend/web-next
git checkout main && git pull origin main
pnpm install && pnpm dev

# 2. Abre http://localhost:3000 en Chrome
# 3. Presiona F12 → Console → copia/pega esto:

console.table([
  { check: '✅ ⭐ Destacados visible?', status: 'VER PANTALLA' },
  { check: '✅ 💎 Premium visible?', status: 'VER PANTALLA' },
  { check: '✅ Badges correctos?', status: 'VER PANTALLA' },
  { check: '✅ Precios formateados?', status: 'VER PANTALLA' },
  { check: '✅ Sin errores de red?', status: 'VER NETWORK TAB' },
  { check: '✅ Responsive (móvil/tablet)?', status: 'F12 → CTRL+SHIFT+M' },
]);

# 4. Network tab → Busca:
#    ✅ /api/advertising/rotation/FeaturedSpot (200)
#    ✅ /api/advertising/rotation/PremiumSpot (200)
#    ✅ /api/advertising/tracking/impression (200 o 204)

# 5. Haz clic en una tarjeta:
#    ✅ Navega a /vehiculos/{slug}
#    ✅ Tracking/click se registra

# 6. Si TODO OK → Abre PROMPT_QA_FRONTEND_DESTACADOS.md para auditoría completa
```

---

## 📊 Matriz de verificación (30 segundos)

| Elemento              | Esperado                   | Estado  |
| --------------------- | -------------------------- | ------- |
| Sección ⭐ Destacados | Visible                    | ✅ / ❌ |
| Sección 💎 Premium    | Visible                    | ✅ / ❌ |
| Tarjeta: Imagen       | 200x125px o 🚗             | ✅ / ❌ |
| Tarjeta: Título       | "Toyota Corolla..."        | ✅ / ❌ |
| Tarjeta: Precio       | "RD$900,000"               | ✅ / ❌ |
| Tarjeta: Ubicación    | "📍 Santo Domingo"         | ✅ / ❌ |
| Badge ⭐              | Solo si isFeatured=true    | ✅ / ❌ |
| Badge 💎              | Solo si isPremium=true     | ✅ / ❌ |
| Click tarjeta         | Navega a /vehiculos/{slug} | ✅ / ❌ |
| Console               | Sin Uncaught errors        | ✅ / ❌ |
| Network calls         | 200 OK                     | ✅ / ❌ |
| Responsive móvil      | 2 cols, sin overflow       | ✅ / ❌ |
| Responsive tablet     | 3 cols                     | ✅ / ❌ |
| Responsive desktop    | 4 cols                     | ✅ / ❌ |

**Todo ✅?** → Auditoría exitosa  
**Algún ❌?** → Ir a PROMPT_QA_FRONTEND_DESTACADOS.md para debug detallado

---

## 🔧 Debugging rápido

### "No veo las secciones"

```javascript
// En Console:
fetch("/api/advertising/rotation/FeaturedSpot")
  .then((r) => r.json())
  .then((d) => console.log(d));
```

Si retorna error 404 o 500 → Problema backend (revisar logs del servicio)  
Si retorna datos → Problema en componente frontend

### "Los badges están mal"

```javascript
// En Console:
// Abre DevTools → Elements → busca <div> con clase "badge"
// Verifica si tiene texto "💎 Premium" o "⭐ Destacado"
```

Si no está → Problema en lógica de renderizado (ver PASO 2 del prompt)

### "Precio no formatea"

```javascript
// En Console:
// Haz clic derecho en el elemento <p> del precio
// Inspect → ve el HTML
// Debería ser: "RD$900,000" no "900000"
```

### "Errores de red"

```javascript
// En Console:
// Network tab → filtra "XHR"
// Click en cada petición → Response tab
// Si error 500 → Backend issue
// Si error 401 → Token/Auth issue
// Si error 404 → URL incorrecta
```

---

## 📱 Test responsive (1 minuto)

```bash
# DevTools → Presiona Cmd+Shift+M (Mac) o Ctrl+Shift+M (Windows)

# Móvil (375px)
Selecciona: iPhone SE
✅ 2 columnas
✅ No overflow

# Tablet (768px)
Selecciona: iPad
✅ 3 columnas
✅ Espaciado OK

# Desktop (1280px+)
Selecciona: Laptop L o quita DevTools
✅ 4 columnas
✅ Hover effects
```

---

## 🎬 Caso de uso: "Necesito verificar en 10 minutos"

**Escenario:** Gerente pide verificación rápida antes de deploy

```bash
1. pnpm dev (si no está corriendo)
2. Abre http://localhost:3000
3. F12 → Console
4. Ejecuta checklist (arriba)
5. F12 → Network
6. Filtra XHR, haz clic en tarjeta
7. Verifica status 200 OK
8. Reporta: "✅ VERDE - Listo para deploy" o "❌ ROJO - Hay bugs"
```

---

## 💡 "¿Cuál es el archivo más importante?"

| Archivo                  | Rol                                |
| ------------------------ | ---------------------------------- |
| `featured-vehicles.tsx`  | Renderiza las tarjetas (UI visual) |
| `use-advertising.ts`     | Hook que obtiene datos + tracking  |
| `advertising.ts`         | Llamadas HTTP a la API             |
| `advertising.ts` (types) | Interfaces TypeScript              |

**Si hay bug en:**

- **Visual (imagen, precio, badge)** → `featured-vehicles.tsx`
- **Datos faltantes** → `use-advertising.ts` o `advertising.ts`
- **Tracking no funciona** → `use-advertising.ts`
- **Errores TypeScript** → `advertising.ts` (types)

---

## 🚀 "Qué hacer si todo funciona"

1. ✅ Documentá con screenshot
2. ✅ Abre el PROMPT_QA_FRONTEND_DESTACADOS.md para auditoría completa
3. ✅ Completa el checklist final
4. ✅ Genera informe REVISION_FRONTEND_DESTACADOS_YYYYMMDD.md
5. ✅ Abre PR (opcional, si no hay bugs)

---

## ⚠️ "¿Y si encuentro un bug?"

**Paso 1:** Documenta con screenshot + descripción

```
BUG: Badges superpuestos en misma tarjeta
SCREENSHOT: 01_bug_badges.png
REPRODUCCIÓN: Cargar homepage con vehículo que tiene isFeatured=true Y isPremium=true
UBICACIÓN: featured-vehicles.tsx línea XX
```

**Paso 2:** Crea rama y fix

```bash
git checkout -b fix/homepage-badge-logic
# ... edita featured-vehicles.tsx
git add frontend/web-next/src/components/advertising/featured-vehicles.tsx
git commit -m "fix(homepage): correct badge XOR logic"
git push origin fix/homepage-badge-logic
```

**Paso 3:** Abre PR con descripción clara

---

## 📞 "¿Preguntas comunes?"

**P: ¿Necesito backend corriendo?**  
R: Idealmente sí, en http://localhost:8080 (o usa mock en use-advertising.ts)

**P: ¿Puedo testear sin backend?**  
R: Sí, crea mock en use-advertising.ts (ver PASO 4 del prompt principal)

**P: ¿Cuánto tarda la auditoría completa?**  
R: 2-3 horas con screenshots y documentación

**P: ¿Necesito axe DevTools?**  
R: Opcional. Solo para accesibilidad profunda.

**P: ¿Debo corregir los bugs o solo reportarlos?**  
R: Si eres dev → Corrígelos. Si eres QA → Reporta en GitHub Issues.

---

## 🎯 Siguiente paso

Corre esto si estás listo para auditoría **completa**:

```bash
open PROMPT_QA_FRONTEND_DESTACADOS.md
# Lee el PASO 0 → PASO 9
# Toma screenshots en cada paso
# Completa el checklist
# Genera informe final
```

---

_⏱️ Tiempo: 5 min setup + 2-3 horas auditoría completa_

_📍 Para dudas → Ver PROMPT_QA_FRONTEND_DESTACADOS.md sección "PREGUNTAS FRECUENTES"_
