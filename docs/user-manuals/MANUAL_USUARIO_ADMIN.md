# 📖 Manual de Usuario — Administrador de la Plataforma

**Plataforma:** OKLA — Marketplace de Vehículos #1 en República Dominicana  
**Última actualización:** 2026-03-02  
**Tipo de usuario:** Administrador (Admin / Platform Employee)  
**Credenciales de prueba:** admin@okla.local / Admin123!@#

---

## 1. Funcionalidades disponibles

| Funcionalidad                    | Disponible | Notas                                     |
| -------------------------------- | ---------- | ----------------------------------------- |
| Todas las funciones de comprador | ✅ Sí      | Buscar, ver detalles, etc.                |
| Panel de administración completo | ✅ Sí      | Gestión de toda la plataforma             |
| Gestión de usuarios              | ✅ Sí      | CRUD de todos los usuarios                |
| Gestión de vehículos             | ✅ Sí      | Aprobar, rechazar, eliminar               |
| Gestión de dealers               | ✅ Sí      | Crear, editar, suspender                  |
| Moderación de contenido          | ✅ Sí      | Reportes, reseñas                         |
| KYC — Verificación de identidad  | ✅ Sí      | Aprobar/rechazar documentos               |
| Analíticas de la plataforma      | ✅ Sí      | Métricas globales                         |
| Configuración del sistema        | ✅ Sí      | Ajustes de la plataforma                  |
| Gestión de roles y permisos      | ✅ Sí      | RBAC completo                             |
| Configuración de AI/chatbots     | ✅ Sí      | SearchAgent, SupportAgent, ChatbotService |
| Logs de auditoría                | ✅ Sí      | Trazabilidad completa                     |
| Gestión de notificaciones        | ✅ Sí      | Templates, programación                   |
| Mantenimiento del sistema        | ✅ Sí      | Modo mantenimiento, backups               |
| Gestión de precios y planes      | ✅ Sí      | Configurar planes de dealer               |
| Staff / empleados de plataforma  | ✅ Sí      | Gestión del equipo OKLA                   |

---

## 2. Acceder al Panel de Administración

1. Inicia sesión con tu cuenta de administrador.
2. En la barra de navegación, haz clic en **"Admin"** o ve a **okla.com.do/admin**.
3. El panel muestra el dashboard principal con métricas clave.

---

## 3. Dashboard Principal

### Métricas en tiempo real:

- **Usuarios registrados:** Total y nuevos hoy/semana/mes.
- **Vehículos activos:** Total de anuncios publicados.
- **Dealers activos:** Concesionarios con plan activo.
- **Transacciones:** Ingresos por publicaciones y suscripciones.
- **Sesiones activas:** Usuarios conectados ahora.
- **Chatbot:** Sesiones activas de chat IA.

### Gráficos:

- Registros por día/semana/mes.
- Vehículos publicados por período.
- Ingresos por tipo (publicaciones, suscripciones, publicidad).
- Distribución de usuarios por tipo de cuenta.

### Alertas del sistema:

- Verificaciones KYC pendientes.
- Reportes de contenido sin revisar.
- Servicios con problemas de salud.
- Queues de RabbitMQ con backlog.

---

## 4. Gestión de Usuarios

### 4.1 Listar usuarios

1. Ve a **Admin** → **Usuarios**.
2. Busca por nombre, email o ID.
3. Filtra por: Tipo de cuenta, Estado, Verificación KYC.

### 4.2 Ver detalle de usuario

- Información personal (nombre, email, fecha de registro).
- Tipo de cuenta (buyer, seller, dealer, admin).
- Estado de verificación KYC.
- Historial de actividad.
- Anuncios publicados (si seller/dealer).
- Sesiones de chat.
- Notificaciones enviadas.

### 4.3 Acciones sobre usuarios

- **Suspender:** Desactiva temporalmente la cuenta.
- **Banear:** Bloquea permanentemente.
- **Restaurar:** Reactiva una cuenta suspendida.
- **Cambiar tipo:** Actualizar de buyer a seller, etc.
- **Reset de contraseña:** Enviar email de reseteo.
- **Verificar manualmente:** Aprobar KYC sin documentos (casos especiales).

---

## 5. Verificación KYC

### 5.1 Cola de verificación

1. Ve a **Admin** → **KYC** → **Pendientes**.
2. Verás la lista de solicitudes de verificación pendientes.

### 5.2 Revisar una solicitud

1. Haz clic en una solicitud.
2. Revisa:
   - Documento de identidad (frontal y reverso).
   - Selfie del usuario.
   - Coincidencia de datos (nombre, número de cédula).
3. Acciones:
   - **Aprobar:** El usuario recibe la insignia ✓.
   - **Rechazar:** Con motivo (documento ilegible, no coincide, etc.).
   - **Solicitar re-envío:** Pide al usuario nuevos documentos.

### 5.3 Métricas KYC

- Total de solicitudes por estado.
- Tiempo promedio de verificación.
- Tasa de aprobación/rechazo.

---

## 6. Gestión de Vehículos

### 6.1 Listar vehículos

1. Ve a **Admin** → **Vehículos**.
2. Busca por marca, modelo, ID, vendedor.
3. Filtra por: Estado, Tipo de vendedor, Fecha.

### 6.2 Acciones sobre vehículos

- **Pausar:** Ocultar temporalmente del listado.
- **Eliminar:** Remover permanentemente.
- **Destacar:** Agregar a sección de destacados.
- **Aprobar/Rechazar:** Para vehículos que requieren moderación.

### 6.3 Reportes de contenido

1. Ve a **Admin** → **Reportes**.
2. Revisa los vehículos reportados por usuarios.
3. Acciones: Ignorar reporte, Pausar anuncio, Eliminar anuncio, Suspender vendedor.

---

## 7. Gestión de Dealers

### 7.1 Crear un nuevo dealer

1. Ve a **Admin** → **Dealers** → **"+ Nuevo dealer"**.
2. Completa:
   - Nombre del concesionario.
   - Información de contacto.
   - Plan seleccionado.
   - Datos del administrador del dealer.
3. Se enviará un email de activación al dealer.

### 7.2 Gestionar dealers existentes

- **Ver perfil:** Información completa, inventario, métricas.
- **Cambiar plan:** Actualizar/degradar suscripción.
- **Suspender:** Desactivar temporalmente.
- **Eliminar:** Remover dealer y todo su inventario.

---

## 8. Configuración de IA y Chatbots

### 8.1 SearchAgent (Búsqueda IA)

1. Ve a **Admin** → **IA** → **SearchAgent**.
2. Configura:
   - **Modelo:** Claude Haiku 4.5 (default).
   - **Temperature:** 0.1 (recomendado para reducir alucinaciones).
   - **Habilitado:** Sí/No.
   - **Porcentaje de tráfico IA:** % de búsquedas que usan IA.
   - **Cache:** Habilitado, TTL en segundos.
   - **Rate limiting:** Queries por minuto por IP.
   - **Threshold de relajación:** Niveles de relajación de filtros.
   - **Prompt del sistema:** Override del prompt base.

### 8.2 SupportAgent (Soporte)

1. Ve a **Admin** → **IA** → **SupportAgent**.
2. Configura:
   - Base de conocimiento (documentos de soporte).
   - Prompt restrictivo.
   - Temperature (0.1 para reducir alucinaciones).
   - Umbral de similitud mínimo.

### 8.3 ChatbotService (Chat de vehículos)

1. Ve a **Admin** → **IA** → **ChatbotService**.
2. Configura:
   - Modelo (Llama 3 / Claude Sonnet 4.5).
   - Límite de interacciones por sesión.
   - Mensajes de bienvenida.
   - Quick replies iniciales.

---

## 9. Analíticas de la Plataforma

### 9.1 Métricas disponibles

- **Usuarios:** Registros, retención, churn.
- **Vehículos:** Publicaciones, vistas, contactos.
- **Ingresos:** Por tipo, por período, crecimiento.
- **Rendimiento:** Tiempos de carga, errores, uptime.
- **Chat:** Sesiones, satisfacción, tasa de transferencia.
- **Búsqueda IA:** Queries, confianza, latencia, caché hit rate.

### 9.2 Reportes

- Exportar en CSV, PDF.
- Programar reportes semanales/mensuales por email.

---

## 10. Gestión de Notificaciones

### 10.1 Templates

1. Ve a **Admin** → **Notificaciones** → **Templates**.
2. Crea/edita plantillas para:
   - Registro exitoso.
   - Verificación KYC aprobada/rechazada.
   - Nuevo mensaje recibido.
   - Alerta de precio.
   - Recordatorio de cita.
   - Anuncio expirado.

### 10.2 Notificaciones masivas

1. Ve a **Admin** → **Notificaciones** → **Enviar masiva**.
2. Selecciona segmento (todos, buyers, sellers, dealers).
3. Compone el mensaje.
4. Programa la hora de envío o envía inmediatamente.

---

## 11. Roles y Permisos (RBAC)

### 11.1 Roles del sistema

| Rol             | Descripción                                    |
| --------------- | ---------------------------------------------- |
| **Super Admin** | Acceso total sin restricciones                 |
| **Admin**       | Gestión completa excepto configuración crítica |
| **Moderador**   | Revisión de contenido, KYC, reportes           |
| **Soporte**     | Acceso a tickets y chat de soporte             |
| **Analista**    | Solo lectura de métricas y reportes            |

### 11.2 Gestionar roles

1. Ve a **Admin** → **Roles y permisos**.
2. Crea roles personalizados con permisos granulares.
3. Asigna roles a miembros del equipo.

---

## 12. Logs de Auditoría

### 12.1 ¿Qué se registra?

- Todas las acciones de administración.
- Login/logout de usuarios.
- Cambios en configuración.
- Aprobación/rechazo de KYC.
- Suspensión/eliminación de cuentas.
- Cambios en vehículos.

### 12.2 Consultar logs

1. Ve a **Admin** → **Auditoría**.
2. Filtra por: Fecha, Usuario, Acción, Entidad.
3. Exporta logs en CSV.

---

## 13. Mantenimiento del Sistema

### 13.1 Modo mantenimiento

1. Ve a **Admin** → **Sistema** → **Mantenimiento**.
2. Activa el **modo mantenimiento** — muestra página de mantenimiento a todos los usuarios.
3. Define mensaje personalizado y duración estimada.
4. Desactiva cuando el mantenimiento termine.

### 13.2 Salud de servicios

- Monitorea el estado de todos los microservicios.
- Ver métricas de salud: CPU, memoria, latencia.
- Alertas automáticas cuando un servicio falla.

---

## 14. Gestión de Precios y Planes

### 14.1 Planes de dealer

1. Ve a **Admin** → **Precios** → **Planes Dealer**.
2. Configura:
   - Nombre del plan.
   - Precio mensual.
   - Límite de vehículos.
   - Características incluidas.

### 14.2 Precios de publicación

1. Ve a **Admin** → **Precios** → **Publicaciones**.
2. Configura el costo de publicación para vendedores particulares.

---

## 15. Chatbot de Soporte

El **botón de soporte** 🎧 está disponible en todas las páginas:

- Como administrador, el soporte puede brindarte información específica sobre la gestión de la plataforma.
- Reporta incidencias técnicas.
- Obtén ayuda con configuraciones avanzadas.

---

## 16. Comandos Útiles de Administración

### Kubernetes (para equipo técnico)

```bash
# Ver estado de pods
kubectl get pods -n okla

# Ver logs de un servicio
kubectl logs -n okla -l app=authservice --follow

# Reiniciar un servicio
kubectl rollout restart deployment/authservice -n okla

# Ver estado de deployments
kubectl rollout status deployment/authservice -n okla
```

### RabbitMQ

```bash
# Eliminar una queue (necesario al cambiar argumentos)
kubectl exec deployment/rabbitmq -n okla -- rabbitmqctl delete_queue {queue-name}

# Ver queues activas
kubectl exec deployment/rabbitmq -n okla -- rabbitmqctl list_queues
```

---

## 17. Preguntas Frecuentes para Admins

**P: ¿Cómo creo una cuenta de dealer?**  
R: Admin → Dealers → "Nuevo dealer" → Completa formulario → Se envía email de activación.

**P: ¿Cómo suspendo un usuario problemático?**  
R: Admin → Usuarios → Buscar usuario → Suspender → Indicar motivo.

**P: ¿Cómo cambio la configuración del chatbot?**  
R: Admin → IA → Selecciona el servicio (SearchAgent, SupportAgent, ChatbotService) → Editar configuración.

**P: ¿Cómo veo los ingresos de la plataforma?**  
R: Admin → Analíticas → Sección de Ingresos → Filtra por período.

**P: ¿Cómo activo el modo mantenimiento?**  
R: Admin → Sistema → Mantenimiento → Activar → Define mensaje y duración estimada.
