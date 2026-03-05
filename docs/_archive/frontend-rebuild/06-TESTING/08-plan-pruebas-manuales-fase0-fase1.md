# 🧪 Plan de Pruebas Manuales - Fase 0 y Fase 1

> **Fecha:** Febrero 2, 2026  
> **Propósito:** Validar todos los microservicios de Fase 0 y Fase 1 desde el frontend  
> **Frontend URL:** http://localhost:3001  
> **Gateway URL:** http://localhost:18443

---

## 📋 AUDITORÍA DE SERVICIOS

### ✅ Fase 0 - Infraestructura + Core (14 servicios)

| # | Servicio | Puerto | Estado | Health Check |
|---|----------|--------|--------|--------------|
| 1 | postgres_db | 5433 | ✅ Healthy | N/A (DB) |
| 2 | redis | 6379 | ✅ Healthy | N/A (Cache) |
| 3 | rabbitmq | 5672/15672 | ✅ Healthy | http://localhost:15672 |
| 4 | consul | 8500 | ✅ Healthy | http://localhost:8500 |
| 5 | seq | 5341 | ✅ Healthy | http://localhost:5341 |
| 6 | jaeger | 16686 | ✅ Healthy | http://localhost:16686 |
| 7 | gateway | 18443 | ✅ Healthy | http://localhost:18443/health |
| 8 | authservice | 15001 | ✅ Healthy | http://localhost:15001/health |
| 9 | userservice | 15002 | ✅ Healthy | http://localhost:15002/health |
| 10 | contactservice | 15003 | ✅ Healthy | http://localhost:15003/health |
| 11 | notificationservice | 15005 | ✅ Healthy | http://localhost:15005/health |
| 12 | vehiclessaleservice | 15010 | ✅ Healthy | http://localhost:15010/health |
| 13 | mediaservice | 15020 | ✅ Healthy | http://localhost:15020/health |
| 14 | frontend-next | 3001 | ✅ Healthy | http://localhost:3001 |

### ✅ Fase 1 - Críticos (4 servicios)

| # | Servicio | Puerto | Estado | Health Check |
|---|----------|--------|--------|--------------|
| 1 | errorservice | 5080 | ✅ Healthy | http://localhost:5080/health |
| 2 | roleservice | 15101 | ✅ Healthy | http://localhost:15101/health |
| 3 | billingservice | 15107 | ✅ Healthy | http://localhost:15107/health |
| 4 | searchservice | 15093 | ✅ Healthy | http://localhost:15093/health |

---

## 🎯 PLAN DE PRUEBAS MANUALES

### Pre-requisitos
1. Docker Desktop corriendo con todos los contenedores healthy
2. Browser (Chrome/Firefox recomendado)
3. DevTools abierto (F12) → pestaña Network para ver llamadas API
4. Terminal disponible para verificar logs

### URLs de Acceso
- **Frontend:** http://localhost:3001
- **API Gateway:** http://localhost:18443
- **Swagger Gateway:** http://localhost:18443/swagger
- **Logs (Seq):** http://localhost:5341
- **Tracing (Jaeger):** http://localhost:16686
- **RabbitMQ:** http://localhost:15672 (guest/guest)
- **Consul:** http://localhost:8500

---

## 📝 ORDEN DE PRUEBAS (Secuencia Lógica)

### ETAPA 1: Verificación de Infraestructura (5 min)
### ETAPA 2: Páginas Públicas sin Auth (10 min)
### ETAPA 3: Registro y Login (10 min)
### ETAPA 4: Flujos de Usuario Autenticado (15 min)
### ETAPA 5: Flujos de Vendedor (15 min)
### ETAPA 6: Verificación de Servicios Fase 1 (10 min)

---

## ETAPA 1: Verificación de Infraestructura

### 1.1 Health Checks Directos
```bash
# Ejecutar en terminal
curl http://localhost:18443/health
curl http://localhost:15001/health
curl http://localhost:15010/health
curl http://localhost:15020/health
curl http://localhost:5080/health
```

**Resultado Esperado:** Todos responden "Healthy" o status 200

### 1.2 Verificar UIs de Observabilidad

| # | Acción | URL | Resultado Esperado |
|---|--------|-----|-------------------|
| 1 | Abrir Seq | http://localhost:5341 | Dashboard de logs visible |
| 2 | Abrir Jaeger | http://localhost:16686 | UI de tracing funcional |
| 3 | Abrir RabbitMQ | http://localhost:15672 | Login con guest/guest OK |
| 4 | Abrir Consul | http://localhost:8500 | Lista de servicios registrados |

---

## ETAPA 2: Páginas Públicas (Sin Autenticación)

### 2.1 Homepage
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Navegar a http://localhost:3001 | frontend-next | Página carga sin errores |
| 2 | Ver secciones de vehículos | vehiclessaleservice | Carrusel/grid con vehículos |
| 3 | Ver marcas destacadas | vehiclessaleservice | Logos de marcas cargados |
| 4 | Verificar Network tab | gateway | Llamadas a /api/vehicles/featured, /api/homepagesections |

**Endpoints verificados:**
- `GET /api/homepagesections/homepage`
- `GET /api/vehicles/featured`
- `GET /api/catalog/makes`

### 2.2 Búsqueda de Vehículos
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Click en "Buscar" o navegar a /search | frontend-next | Página de búsqueda carga |
| 2 | Ver filtros (Marca, Modelo, Año, Precio) | vehiclessaleservice | Filtros se cargan con opciones |
| 3 | Seleccionar marca "Toyota" | vehiclessaleservice | Modelos se actualizan |
| 4 | Aplicar filtro de precio ($10K-$30K) | searchservice | Resultados filtrados |
| 5 | Verificar paginación | vehiclessaleservice | Cambiar página funciona |

**Endpoints verificados:**
- `GET /api/vehicles/search?make=toyota&minPrice=10000&maxPrice=30000`
- `GET /api/catalog/makes`
- `GET /api/catalog/models/{makeId}`

### 2.3 Detalle de Vehículo
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Click en cualquier vehículo | vehiclessaleservice | Página de detalle carga |
| 2 | Ver galería de imágenes | mediaservice | Imágenes cargan correctamente |
| 3 | Ver información del vendedor | userservice | Datos del vendedor visibles |
| 4 | Ver botón "Contactar" | contactservice | Botón presente y habilitado |
| 5 | Ver vehículos similares | vehiclessaleservice | Sección de recomendados |

**Endpoints verificados:**
- `GET /api/vehicles/{id}` o `GET /api/vehicles/slug/{slug}`
- `GET /api/vehicles/{id}/media`
- `GET /api/vehicles/similar/{id}`

### 2.4 Help Center (si existe)
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Navegar a /ayuda | frontend-next | Página de ayuda carga |
| 2 | Ver categorías de FAQ | notificationservice | Categorías listadas |

---

## ETAPA 3: Registro y Login

### 3.1 Registro de Usuario Nuevo
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Navegar a /register | frontend-next | Formulario de registro |
| 2 | Llenar datos de prueba: | authservice | - |
| | - Email: test-manual@okla.com | | |
| | - Password: Test123!@# | | |
| | - Nombre: Usuario Prueba | | |
| 3 | Click "Registrarse" | authservice | Registro exitoso |
| 4 | Verificar en Seq logs | errorservice | Log de registro |
| 5 | Verificar en Network | gateway | POST /api/auth/register → 201 |

**Endpoints verificados:**
- `POST /api/auth/register`

### 3.2 Login
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Navegar a /login | frontend-next | Formulario de login |
| 2 | Ingresar credenciales: | authservice | - |
| | - Email: test-manual@okla.com | | |
| | - Password: Test123!@# | | |
| 3 | Click "Iniciar Sesión" | authservice | Login exitoso, redirect |
| 4 | Verificar token en localStorage | authservice | Token JWT presente |
| 5 | Verificar Navbar cambia | frontend-next | Muestra nombre de usuario |

**Endpoints verificados:**
- `POST /api/auth/login`
- `GET /api/auth/me` (validar token)

### 3.3 Logout
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Click en menú usuario | frontend-next | Dropdown aparece |
| 2 | Click "Cerrar Sesión" | authservice | Logout exitoso |
| 3 | Verificar token removido | authservice | localStorage limpio |
| 4 | Navbar vuelve a estado público | frontend-next | Botones Login/Register |

**Endpoints verificados:**
- `POST /api/auth/logout`

---

## ETAPA 4: Flujos de Usuario Autenticado

> **Pre-requisito:** Estar logueado con test-manual@okla.com

### 4.1 Perfil de Usuario
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Navegar a /perfil o /settings | userservice | Página de perfil carga |
| 2 | Ver datos del usuario | userservice | Nombre, email visibles |
| 3 | Editar nombre | userservice | PUT exitoso |
| 4 | Subir avatar | mediaservice | Imagen se sube a S3 |
| 5 | Guardar cambios | userservice | Actualización confirmada |

**Endpoints verificados:**
- `GET /api/users/me`
- `PUT /api/users/me`
- `POST /api/media/upload/avatar`

### 4.2 Favoritos
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Navegar a un vehículo | vehiclessaleservice | Detalle carga |
| 2 | Click en ❤️ (agregar favorito) | vehiclessaleservice | Vehículo agregado |
| 3 | Navegar a /favoritos | vehiclessaleservice | Lista de favoritos |
| 4 | Verificar vehículo aparece | vehiclessaleservice | Vehículo en lista |
| 5 | Quitar de favoritos | vehiclessaleservice | DELETE exitoso |

**Endpoints verificados:**
- `POST /api/favorites`
- `GET /api/favorites`
- `DELETE /api/favorites/{vehicleId}`

### 4.3 Comparador
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Agregar 2-3 vehículos a comparar | vehiclessaleservice | Badge de comparación +N |
| 2 | Navegar a /comparar | vehiclessaleservice | Tabla comparativa |
| 3 | Ver specs lado a lado | vehiclessaleservice | Datos correctos |
| 4 | Quitar un vehículo | vehiclessaleservice | Se remueve de tabla |

**Endpoints verificados:**
- `GET /api/comparisons`
- `POST /api/comparisons`
- `DELETE /api/comparisons/{id}/vehicles/{vehicleId}`

### 4.4 Alertas de Precio
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Navegar a /alertas | billingservice | Página de alertas |
| 2 | Crear nueva alerta de precio | billingservice | Formulario funciona |
| 3 | Configurar criterios | vehiclessaleservice | Filtros funcionan |
| 4 | Guardar alerta | billingservice | Alerta creada |
| 5 | Ver lista de alertas | billingservice | Alerta en lista |

**Endpoints verificados:**
- `GET /api/price-alerts`
- `POST /api/price-alerts`

### 4.5 Contactar Vendedor
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Ir a detalle de vehículo | vehiclessaleservice | Página carga |
| 2 | Click "Contactar Vendedor" | contactservice | Modal/formulario aparece |
| 3 | Escribir mensaje | contactservice | Textarea funciona |
| 4 | Enviar mensaje | contactservice | Mensaje enviado OK |
| 5 | Verificar en /mensajes | notificationservice | Mensaje en threads |

**Endpoints verificados:**
- `POST /api/inquiries`
- `GET /api/messages/threads`

### 4.6 Notificaciones
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Click en 🔔 (bell icon) | notificationservice | Dropdown de notificaciones |
| 2 | Ver lista de notificaciones | notificationservice | Notificaciones listadas |
| 3 | Marcar como leída | notificationservice | Status cambia |
| 4 | Navegar a /notificaciones | notificationservice | Página completa |

**Endpoints verificados:**
- `GET /api/notifications`
- `PUT /api/notifications/{id}/read`
- `GET /api/notifications/unread-count`

---

## ETAPA 5: Flujos de Vendedor

### 5.1 Publicar Vehículo
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Click "Vender" o navegar a /publicar | frontend-next | Formulario de publicación |
| 2 | **Paso 1:** Seleccionar marca | vehiclessaleservice | Lista de marcas carga |
| 3 | Seleccionar modelo | vehiclessaleservice | Modelos filtrados |
| 4 | Seleccionar año | vehiclessaleservice | Años disponibles |
| 5 | **Paso 2:** Detalles del vehículo | vehiclessaleservice | - |
| | - Kilometraje: 50000 | | |
| | - Precio: $25,000 | | |
| | - Condición: Usado | | |
| | - Transmisión: Automática | | |
| 6 | **Paso 3:** Subir fotos (3-5) | mediaservice | Upload funciona |
| 7 | Verificar preview de imágenes | mediaservice | Thumbnails visibles |
| 8 | **Paso 4:** Descripción | vehiclessaleservice | Textarea funciona |
| 9 | Click "Publicar" | vehiclessaleservice | Vehículo creado |
| 10 | Verificar redirect a detalle | vehiclessaleservice | Nuevo vehículo visible |

**Endpoints verificados:**
- `GET /api/catalog/makes`
- `GET /api/catalog/models/{makeId}`
- `POST /api/media/upload`
- `POST /api/vehicles`
- `POST /api/vehicles/draft` (si guarda borrador)

### 5.2 Dashboard de Vendedor
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Navegar a /vendedor/dashboard | vehiclessaleservice | Dashboard carga |
| 2 | Ver lista de mis vehículos | vehiclessaleservice | Vehículos listados |
| 3 | Ver estadísticas (vistas, contactos) | vehiclessaleservice | Métricas visibles |
| 4 | Editar un vehículo | vehiclessaleservice | Formulario de edición |
| 5 | Pausar/Activar listing | vehiclessaleservice | Status cambia |

**Endpoints verificados:**
- `GET /api/vehicles/my`
- `GET /api/vehicles/my/stats`
- `PUT /api/vehicles/{id}`
- `PUT /api/vehicles/{id}/status`

---

## ETAPA 6: Verificación de Servicios Fase 1

### 6.1 ErrorService
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Provocar un error (URL inválida) | errorservice | Error logueado |
| 2 | Revisar Seq (localhost:5341) | errorservice | Error visible en logs |
| 3 | Verificar error ID en respuesta | errorservice | ID de tracking |

**Verificación directa:**
```bash
curl http://localhost:5080/api/errors | jq
```

### 6.2 RoleService
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Login como admin (si existe) | roleservice | Roles cargados |
| 2 | Verificar permisos en respuesta | roleservice | Claims en token |
| 3 | Acceder a ruta protegida | roleservice | Autorización funciona |

**Verificación directa:**
```bash
curl http://localhost:15101/api/roles | jq
curl http://localhost:15101/api/permissions | jq
```

### 6.3 BillingService
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Navegar a /planes o /pricing | billingservice | Planes visibles |
| 2 | Ver precios de suscripción | billingservice | Precios correctos |
| 3 | Click en "Suscribirse" (test) | billingservice | Checkout flow |

**Verificación directa:**
```bash
curl http://localhost:15107/api/billing/plans | jq
curl http://localhost:15107/api/billing/earlybird/status | jq
```

### 6.4 SearchService
| # | Acción | Servicio Probado | Resultado Esperado |
|---|--------|------------------|-------------------|
| 1 | Usar barra de búsqueda global | searchservice | Sugerencias aparecen |
| 2 | Buscar "Toyota Corolla" | searchservice | Resultados relevantes |
| 3 | Verificar autocomplete | searchservice | Suggestions dinámicas |

**Verificación directa:**
```bash
curl "http://localhost:15093/api/search?q=toyota" | jq
curl "http://localhost:15093/api/search/suggestions?q=cor" | jq
```

---

## 📊 CHECKLIST DE VERIFICACIÓN FINAL

### Servicios Fase 0
- [ ] postgres_db - Conexiones funcionan
- [ ] redis - Cache funciona
- [ ] rabbitmq - Queue procesando
- [ ] consul - Servicios registrados
- [ ] seq - Logs visibles
- [ ] jaeger - Traces capturados
- [ ] gateway - Routing funciona
- [ ] authservice - Login/Register OK
- [ ] userservice - Perfiles OK
- [ ] contactservice - Mensajes OK
- [ ] notificationservice - Notificaciones OK
- [ ] vehiclessaleservice - CRUD vehículos OK
- [ ] mediaservice - Upload imágenes OK
- [ ] frontend-next - UI funcional

### Servicios Fase 1
- [ ] errorservice - Logging de errores OK
- [ ] roleservice - Permisos funcionan
- [ ] billingservice - Planes/Pagos OK
- [ ] searchservice - Búsqueda funciona

### Flujos End-to-End
- [ ] Usuario puede registrarse
- [ ] Usuario puede loguearse
- [ ] Usuario puede ver vehículos
- [ ] Usuario puede buscar y filtrar
- [ ] Usuario puede agregar favoritos
- [ ] Usuario puede comparar vehículos
- [ ] Usuario puede contactar vendedor
- [ ] Vendedor puede publicar vehículo
- [ ] Vendedor puede gestionar listings
- [ ] Notificaciones funcionan
- [ ] Alertas de precio funcionan

---

## 🔧 TROUBLESHOOTING

### Si algo falla:

1. **Verificar logs del servicio:**
```bash
docker logs <nombre_servicio> --tail 50
```

2. **Verificar Seq para errores:**
http://localhost:5341

3. **Verificar Jaeger para traces:**
http://localhost:16686

4. **Reiniciar servicio problemático:**
```bash
docker restart <nombre_servicio>
```

5. **Verificar conectividad:**
```bash
docker exec gateway wget -qO- http://<servicio>:80/health
```

---

## 📝 NOTAS DE PRUEBA

### Usuario de Prueba Creado
- **Email:** test-manual@okla.com
- **Password:** Test123!@#
- **Rol:** Usuario regular

### Datos de Vehículo de Prueba
- **Marca:** Toyota
- **Modelo:** Corolla
- **Año:** 2022
- **Precio:** $25,000
- **Kilometraje:** 50,000

---

## ✅ RESULTADO DE AUDITORÍA

| Fase | Total Servicios | Healthy | Unhealthy | % Operativo |
|------|-----------------|---------|-----------|-------------|
| Fase 0 | 14 | 14 | 0 | 100% |
| Fase 1 | 4 | 4 | 0 | 100% |
| **TOTAL** | **18** | **18** | **0** | **100%** |

**Estado:** ✅ TODOS LOS SERVICIOS OPERATIVOS

---

_Documento generado: Febrero 2, 2026_  
_Versión: 1.0_
