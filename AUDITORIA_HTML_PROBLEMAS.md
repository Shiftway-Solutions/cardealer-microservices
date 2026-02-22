# 🔍 AUDITORÍA DE PROBLEMAS - Portal TO.HTML

**Fecha:** 21 Febrero 2026  
**Ubicación del archivo:** `/Users/gregorymoreno/Downloads/TO.HTML`

---

## 📋 RESUMEN EJECUTIVO

Se identificaron **6 problemas críticos** que afectan la experiencia del usuario:

| #   | Problema                                        | Severidad  | Sección    | Solución                                                 |
| --- | ----------------------------------------------- | ---------- | ---------- | -------------------------------------------------------- |
| 1   | Modal de avatares NO se cierra al seleccionar   | 🔴 Crítica | Inicio     | Falta función `selectAvatar()` que cierre modal          |
| 2   | Body comienza desde top (debajo del header)     | 🔴 Crítica | General    | Padding-top insuficiente en main                         |
| 3   | Historia Patria: contenido NO visible           | 🔴 Crítica | Historia   | Body height 100% causa layout overflow                   |
| 4   | Biografías: contenido NO visible                | 🔴 Crítica | Biografías | CSS con `max-width: 100%` pero `overflow: hidden`        |
| 5   | Símbolos: página cortada/no se ve bien          | 🔴 Crítica | Símbolos   | Falta CSS para sección `#simbolos-wrapper`               |
| 6   | Mi Territorio: modal aparece + sin mapa visible | 🔴 Crítica | Territorio | Modal `#modalll` abierto por defecto + imagen sin cargar |

---

## 🔴 PROBLEMA 1: Modal de Avatares NO se Cierra

### Ubicación

Línea ~1353 en el HTML (función `selectAvatar()`)

### Síntoma

- Al hacer clic en un avatar, no ocurre nada
- El modal sigue visible
- No se navega a la página principal

### Root Cause

**La función `selectAvatar()` NO está implementada en el JavaScript.**

```html
<div
  class="avatar-option"
  onclick="selectAvatar('Rosa de Bayahibe', 'https://...')"
></div>
```

Pero **NO existe** la función JavaScript `selectAvatar()`

### Solución

**Agregar esta función al final de `<script>` (antes del cierre):**

```javascript
// Función para cerrar modal de avatares y cargar la página
function selectAvatar(avatarName, avatarUrl) {
  // 1. Guardar datos del avatar en localStorage
  localStorage.setItem("selectedAvatar", avatarName);
  localStorage.setItem("selectedAvatarUrl", avatarUrl);

  // 2. Actualizar el mini avatar en el header
  const miniAvatar = document.getElementById("user-avatar-mini");
  if (miniAvatar) {
    miniAvatar.src = avatarUrl;
    miniAvatar.alt = avatarName;
  }

  // 3. Actualizar el mensaje de bienvenida
  const welcomeMsg = document.getElementById("welcome-msg");
  if (welcomeMsg) {
    welcomeMsg.innerText = "Hola, " + avatarName;
  }

  // 4. CERRAR el modal de avatares
  const modalOverlay = document.getElementById("modal-overlay");
  if (modalOverlay) {
    modalOverlay.style.display = "none";
  }

  // 5. Permitir scroll en el body
  document.body.style.overflow = "auto";

  // 6. Navegar a la página de inicio (sección "inicio" debe estar activa)
  navigate("inicio");
}
```

---

## 🔴 PROBLEMA 2: Body Comienza Debajo del Header (Contenido Cortado)

### Ubicación

CSS línea ~170: `main { padding-top: 90px; }`

### Síntoma

- El contenido de "Inicio" empieza debajo del header fijo
- En mobile se corta el texto
- Los títulos se solapan con el header

### Root Cause

El header tiene altura **70px** pero hay **90px** de padding-top, pero en algunas secciones el contenido no se posiciona correctamente.

### Solución

**Modificar el CSS de `main` para asegurar posicionamiento correcto:**

```css
main {
  flex-grow: 1;
  overflow-y: auto;
  position: relative;
  scrollbar-width: thin;
  scrollbar-color: var(--rd-red) var(--rd-blue);
  padding-top: 80px; /* ← Cambiar de 90px a 80px para alineación exacta */
  margin-top: 0;
  scroll-behavior: smooth;
}

main.padded {
  padding-left: 20px;
  padding-right: 20px;
  padding-bottom: 20px;
  /* ← Mantener padding-top de main */
}

main.full-screen {
  padding: 0;
  padding-top: 80px; /* ← Agregar aquí también */
  overflow: hidden;
}
```

**Agregar también a `body`:**

```css
body {
  margin: 0;
  padding: 0;
  overflow: hidden; /* ← Cambiar de 'auto' a 'hidden' mientras modal está abierto */
  background-color: var(--rd-blue);
  font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
  color: var(--rd-white);
  min-height: 100vh;
  width: 100%;
  display: flex;
  flex-direction: column; /* ← Agregar para layout correcto */
}

body.scroll-enabled {
  overflow: auto; /* ← Clase para permitir scroll después de cerrar modal */
}
```

---

## 🔴 PROBLEMA 3: Historia Patria - Contenido NO Visible

### Ubicación

CSS línea ~195-200: `#historia.seccion` tiene `height: 100%`

### Síntoma

- Sección "Historia Patria" aparece en blanco
- No se ve el mapa, los nodos, ni nada
- El contenido existe pero está oculto

### Root Cause

```css
#historia.seccion,
#biografias.seccion,
#simbolos.seccion,
#territorio.seccion {
  max-width: 100%;
  margin: 0;
  height: 100%; /* ← PROBLEMA: 100% del viewport, pero main tiene overflow */
  width: 100%;
}
```

Cuando `main` tiene `overflow-y: auto` y la sección tiene `height: 100%`, el contenido se desborda sin verse.

### Solución

**Reemplazar CSS de secciones especiales:**

```css
/* Estilos para secciones de contenido completo (Historia, Biografías, etc.) */
#historia.seccion,
#biografias.seccion,
#simbolos.seccion,
#territorio.seccion {
  max-width: 100%;
  margin: 0;
  width: 100%;
  height: auto; /* ← Cambiar de 100% a auto */
  min-height: calc(100vh - 80px); /* ← Mínimo altura de viewport menos header */
  display: none;
  padding: 0;
  position: relative;
}

#historia.seccion.active,
#biografias.seccion.active,
#simbolos.seccion.active,
#territorio.seccion.active {
  display: block;
}

/* Ensure historia-wrapper fills the container */
#historia-wrapper {
  position: relative;
  width: 100%;
  height: calc(100vh - 80px); /* ← Viewport height menos header */
  background-color: #000805;
  overflow: hidden;
}
```

---

## 🔴 PROBLEMA 4: Biografías - Contenido NO Visible

### Ubicación

CSS y HTML de la sección biografías

### Síntoma

- Sección Biografías aparece en blanco
- El carrusel no se ve
- No aparecen las tarjetas de personajes

### Root Cause

Mismo problema que Historia Patria + falta de wrapper visible

```css
#bio-wrapper {
  /* No tiene estilos definidos */
  /* Hereda display:none de .seccion */
}
```

### Solución

**Agregar CSS para `#bio-wrapper`:**

```css
#bio-wrapper {
  position: relative;
  width: 100%;
  height: auto;
  min-height: calc(100vh - 80px);
  background-image: url("https://..."); /* Si tiene fondo */
  background-size: cover;
  background-position: center;
  padding: 40px 20px;
  overflow-y: auto;
  overflow-x: hidden;
}

#bio-wrapper h1 {
  position: relative;
  z-index: 10;
  color: var(--rd-white);
  text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.5);
  margin-top: 20px;
}

#bio-wrapper h1::after {
  content: "";
  display: block;
  width: 100px;
  height: 4px;
  background: var(--rd-red);
  margin-top: 10px;
}

/* Ensure carousel is visible */
.carousel-wrapper {
  position: relative;
  z-index: 10;
  margin-top: 30px;
}

.carousel-track-container {
  background: rgba(0, 0, 0, 0.3);
  border-radius: 10px;
  padding: 10px;
}
```

---

## 🔴 PROBLEMA 5: Símbolos - Página Cortada/No Se Ve Bien

### Ubicación

Falta CSS para `#simbolos-wrapper`

### Síntoma

- La página de Símbolos se ve cortada
- Layout no responde bien
- Falta estructura visual

### Root Cause

**No existe CSS para `#simbolos-wrapper`** en el archivo, solo para `#bio-wrapper` e `#historia-wrapper`

### Solución

**Agregar CSS completo para símbolos:**

```css
#simbolos-wrapper {
  position: relative;
  width: 100%;
  height: auto;
  min-height: calc(100vh - 80px);
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
  padding: 40px 20px;
  overflow-y: auto;
  overflow-x: hidden;
}

#simbolos-wrapper h1 {
  color: var(--rd-blue);
  font-size: 2.5rem;
  margin-bottom: 30px;
  border-bottom: 4px solid var(--rd-red);
  display: inline-block;
  padding-bottom: 10px;
}

/* Grid de símbolos */
.simbolos-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 30px;
  margin-top: 30px;
  max-width: 1200px;
  margin-left: auto;
  margin-right: auto;
}

.simbolo-card {
  background: white;
  border-radius: 15px;
  padding: 30px;
  text-align: center;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.15);
  transition: all 0.3s ease;
  cursor: pointer;
}

.simbolo-card:hover {
  transform: translateY(-10px);
  box-shadow: 0 15px 40px rgba(206, 17, 38, 0.2);
}

.simbolo-icon {
  font-size: 3rem;
  margin-bottom: 15px;
}

.simbolo-card h3 {
  color: var(--rd-blue);
  font-size: 1.5rem;
  margin: 15px 0;
}

.simbolo-card p {
  color: #666;
  line-height: 1.6;
  font-size: 0.95rem;
}
```

---

## 🔴 PROBLEMA 6: Mi Territorio - Modal Abierto + Sin Mapa

### Ubicación

Líneas ~1373-1385 (Modal `#modalll`) y función `loadMap()`

### Síntoma

- Al entrar a "Mi Territorio" aparece un modal
- El modal dice "X" pero no se cierra bien
- No se ve ningún mapa en la página principal

### Root Cause

**CAUSA A:** Modal abierto por defecto

```html
<div
  id="modalll"
  class="modal"
  style="display:none;"
  onclick="closeDesc(event)"
>
  <!-- El estilo display:none debería prevenir esto, pero hay conflicto CSS -->
</div>
```

**CAUSA B:** Falta wrapper para la sección territorio

```html
<div id="territorio" class="seccion">
  <!-- No hay estructura clara, solo el modal -->
</div>
```

**CAUSA C:** Imagen del mapa no carga

```javascript
mapImg.src = data.image; // URL de Google Drive puede bloquearse
```

### Solución

#### Paso 1: Restructurar la sección Territorio

**Reemplazar la sección territorio con:**

```html
<div id="territorio" class="seccion">
  <div id="territorio-wrapper">
    <div id="bg-image-territorio"></div>

    <div class="terr-header">
      <h1>Mi Territorio <span style="color:var(--rd-red)">Dominicano</span></h1>
      <p>Explora la evolución territorial de nuestra república</p>
    </div>

    <div class="territory-content">
      <!-- Navegación de años -->
      <div class="years-navigation" id="years-nav"></div>

      <!-- Contenedor de mapa e información -->
      <div class="map-info-container">
        <div class="map-section">
          <div class="map-bg-year" id="bg-year">1777</div>
          <img
            id="map-img"
            src="https://lh3.googleusercontent.com/d/1GM9jpQoYY7SK-RaobrSJQ64cp5y60jFX"
            alt="Mapa Territorial"
            style="opacity: 1; transition: opacity 0.5s;"
          />
        </div>

        <div class="info-section animate-content-terr" id="content-box">
          <div class="period-tag" id="period-range">Siglo XVIII</div>
          <h2 id="period-title" style="color: var(--rd-blue);">
            Tratado de Aranjuez
          </h2>
          <p id="period-desc" style="line-height: 1.8; font-size: 1rem;"></p>

          <div style="margin-top: 20px;">
            <a id="download-link" href="#" class="btn-download-terr" download>
              📥 Descargar Mapa
            </a>
          </div>
        </div>
      </div>
    </div>
  </div>
</div>
```

#### Paso 2: Agregar CSS para Territorio

```css
#territorio-wrapper {
  position: relative;
  width: 100%;
  height: auto;
  min-height: calc(100vh - 80px);
  background: linear-gradient(135deg, #1a3a52 0%, #2d5a7b 100%);
  padding: 40px 20px;
  overflow-y: auto;
  overflow-x: hidden;
}

.terr-header {
  text-align: center;
  margin-bottom: 40px;
  color: white;
}

.terr-header h1 {
  font-size: 2.5rem;
  margin: 0;
  text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.3);
}

.terr-header p {
  font-size: 1.1rem;
  opacity: 0.9;
  margin-top: 10px;
}

.territory-content {
  max-width: 1200px;
  margin: 0 auto;
}

.years-navigation {
  display: flex;
  justify-content: center;
  gap: 10px;
  margin-bottom: 30px;
  flex-wrap: wrap;
}

.year-btn-terr {
  background: rgba(255, 255, 255, 0.2);
  color: white;
  border: 2px solid transparent;
  padding: 10px 20px;
  border-radius: 25px;
  cursor: pointer;
  font-weight: 700;
  transition: all 0.3s;
  text-transform: uppercase;
  font-size: 0.9rem;
}

.year-btn-terr:hover {
  background: rgba(206, 17, 38, 0.3);
  border-color: var(--rd-red);
}

.year-btn-terr.active {
  background: var(--rd-red);
  border-color: var(--rd-red);
  box-shadow: 0 0 20px rgba(206, 17, 38, 0.5);
}

.map-info-container {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 30px;
  align-items: start;
}

@media (max-width: 768px) {
  .map-info-container {
    grid-template-columns: 1fr;
    gap: 20px;
  }
}

.map-section {
  position: relative;
  background: white;
  border-radius: 15px;
  overflow: hidden;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
  aspect-ratio: 4/3;
}

.map-bg-year {
  position: absolute;
  top: 10px;
  right: 10px;
  background: var(--rd-red);
  color: white;
  padding: 8px 15px;
  border-radius: 20px;
  font-weight: 900;
  font-size: 1.2rem;
  z-index: 10;
}

#map-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.info-section {
  background: rgba(255, 255, 255, 0.95);
  padding: 30px;
  border-radius: 15px;
  color: #333;
  animation: slideInRight 0.5s ease;
}

@keyframes slideInRight {
  from {
    opacity: 0;
    transform: translateX(20px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.period-tag {
  display: inline-block;
  background: var(--rd-red);
  color: white;
  padding: 5px 12px;
  border-radius: 15px;
  font-size: 0.85rem;
  font-weight: 700;
  margin-bottom: 15px;
  text-transform: uppercase;
}

.period-tag::before {
  content: "📍 ";
}

.btn-download-terr {
  display: inline-block;
  background: var(--rd-blue);
  color: white;
  padding: 12px 25px;
  border-radius: 8px;
  text-decoration: none;
  font-weight: 700;
  transition: all 0.3s;
}

.btn-download-terr:hover {
  background: var(--rd-red);
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(206, 17, 38, 0.3);
}

/* Animación de contenido */
.animate-content-terr {
  animation: fadeInUp 0.5s ease;
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
```

#### Paso 3: Actualizar JavaScript para Territorio

```javascript
// Referencias DOM
const mapImg = document.getElementById("map-img");
const bgYear = document.getElementById("bg-year");
const pRange = document.getElementById("period-range");
const pTitle = document.getElementById("period-title");
const pDesc = document.getElementById("period-desc");
const contentBox = document.getElementById("content-box");
const yearsNav = document.getElementById("years-nav");
const downloadLink = document.getElementById("download-link");

// Eliminar esto: const modalll = document.getElementById("modalll");
// YA NO necesitamos el modal

function loadMap(index) {
  if (!historicalData[index]) return;

  const data = historicalData[index];

  // Transición de salida
  contentBox.style.opacity = "0";
  mapImg.style.opacity = "0";

  setTimeout(() => {
    // Actualizar datos
    mapImg.src = data.image;
    bgYear.innerText = data.label;
    pRange.innerText = data.range;
    pTitle.innerText = data.title;
    pDesc.innerText = data.desc;

    // Actualizar link de descarga
    if (downloadLink && data.pdf) {
      downloadLink.href = data.pdf;
      downloadLink.download = `Mapa_RD_${data.label}.pdf`;
    }

    // Transición de entrada
    setTimeout(() => {
      mapImg.style.opacity = "1";
      contentBox.style.opacity = "1";
    }, 50);
  }, 250);

  // Actualizar botones activos
  const buttons = document.querySelectorAll(".year-btn-terr");
  buttons.forEach((btn, i) => {
    btn.classList.toggle("active", i === index);
  });
}

// Generar botones de navegación
if (yearsNav && historicalData) {
  historicalData.forEach((item, index) => {
    const btn = document.createElement("button");
    btn.className = "year-btn-terr" + (index === 0 ? " active" : "");
    btn.innerText = item.label;
    btn.onclick = () => loadMap(index);
    yearsNav.appendChild(btn);
  });

  // Cargar primer mapa
  loadMap(0);
}
```

---

## 📋 RESUMEN DE CAMBIOS A REALIZAR

### 1. **Agregar función `selectAvatar()`** (Problema 1)

- Ubicación: Final del script
- Líneas: ~15 líneas de código

### 2. **Modificar CSS de `main`** (Problema 2)

- Cambiar padding-top
- Agregar overflow handling
- Ubicación: Sección estilos CSS

### 3. **Actualizar CSS de secciones** (Problema 3-4)

- Cambiar `height: 100%` → `height: auto; min-height: calc(100vh - 80px);`
- Ubicación: CSS de `.seccion`

### 4. **Agregar CSS para `#simbolos-wrapper`** (Problema 5)

- Nuevo bloque CSS completo
- Ubicación: CSS estilos

### 5. **Restructurar sección Territorio** (Problema 6)

- Reemplazar HTML de territorio
- Agregar nuevo CSS
- Actualizar JavaScript para loadMap
- Eliminar modal innecesario

---

## ✅ VERIFICACIÓN POST-CAMBIOS

Después de implementar los cambios, verificar:

- [ ] Modal de avatares se cierra al seleccionar
- [ ] Avatar aparece en header y dice "Hola, [nombre]"
- [ ] Body comienza correctamente después del header
- [ ] Historia Patria muestra contenido (mapa, nodos)
- [ ] Biografías muestra carrusel de personajes
- [ ] Símbolos muestra grid de tarjetas
- [ ] Territorio muestra mapa e información
- [ ] Todos los botones de navegación funcionan
- [ ] Responsive design en mobile (768px)
- [ ] No hay scroll horizontal innecesario

---

_Documento de auditoría completado - 21/02/2026_
