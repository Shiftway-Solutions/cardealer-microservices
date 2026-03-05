# 🔄 Plan de Migración: Consolidación de Bases de Datos

**Fecha:** Enero 8, 2026  
**Objetivo:** Migrar de múltiples contenedores PostgreSQL individuales (`*_db`) a un solo contenedor consolidado (`postgres_db`)

---

## 🎯 Objetivos

1. **Consolidar bases de datos:** Reducir de ~25 contenedores PostgreSQL individuales a 1 solo contenedor
2. **Optimizar recursos:** Menor uso de memoria y CPU
3. **Simplificar administración:** Un solo contenedor para gestionar
4. **Mantener separación:** Cada microservicio tendrá su propia base de datos dentro del contenedor consolidado

---

## 📋 Estado Actual vs. Estado Objetivo

### 🔴 Estado Actual

```
errorservice-db:25432      → Database: errorservice
authservice-db:25434       → Database: authservice
notificationservice-db:25433 → Database: notificationservice
userservice-db:25435       → Database: userservice
...
(25 contenedores individuales)
```

### 🟢 Estado Objetivo

```
postgres_db:5433 → {
  errorservice
  authservice
  notificationservice
  userservice
  roleservice
  adminservice
  mediaservice
  reportsservice
  billingservice
  financeservice
  messagebusservice
  vehiclessaleservice
  invoicingservice
  crmservice
  contactservice
  appointmentservice
  marketingservice
  realestateservice
  auditservice
  backupdrservice
  schedulerservice
  configurationservice
  featuretoggleservice
  ratelimitingservice
  maintenanceservice
  comparisonservice
  alertservice
}
```

---

## 🚀 Plan de Ejecución (Paso a Paso)

### Fase 1: Preparación (✅ COMPLETADO)

1. **✅ Agregar servicio postgres_db**

   - Puerto: 5433 (para no interferir con existentes)
   - Volumen: postgres_data
   - Script de inicialización: postgres-init.sh

2. **✅ Crear scripts de migración**
   - `scripts/migrate-to-postgres-db.sh` - Migración principal
   - `scripts/rollback-migration.sh` - Rollback de emergencia
   - `scripts/postgres-init.sh` - Inicialización de bases de datos

### Fase 2: Prueba del Nuevo Contenedor

```bash
# 1. Levantar solo postgres_db para probar
docker-compose up -d postgres_db

# 2. Verificar que esté corriendo y las bases de datos se crearon
docker exec -it postgres_db psql -U postgres -l

# 3. Verificar script de inicialización
docker logs postgres_db
```

### Fase 3: Migración de Datos

```bash
# 1. Ejecutar script de migración (IMPORTANTE: hacer cuando todos los servicios estén corriendo)
./scripts/migrate-to-postgres-db.sh

# 2. Verificar que los backups se crearon
ls -la db_migration_backups/

# 3. Verificar que los datos se restauraron correctamente
docker exec -it postgres_db psql -U postgres -d errorservice -c "\dt"
```

### Fase 4: Actualización de Connection Strings

Solo después de verificar que la migración fue exitosa:

```bash
# Cambiar todas las connection strings de:
Host=errorservice-db;Database=errorservice;Username=postgres;Password=password
# A:
Host=postgres_db;Database=errorservice;Username=postgres;Password=password

# Y actualizar todos los depends_on de:
errorservice-db:
  condition: service_healthy
# A:
postgres_db:
  condition: service_healthy
```

### Fase 5: Testing y Verificación

```bash
# 1. Reiniciar servicios con nuevas connection strings
docker-compose restart errorservice authservice notificationservice

# 2. Verificar health checks
docker-compose ps

# 3. Verificar logs de aplicaciones
docker-compose logs -f errorservice

# 4. Hacer pruebas de funcionalidad básica
curl http://localhost:15083/health
curl http://localhost:15085/health
curl http://localhost:15084/health
```

### Fase 6: Limpieza Final

Solo después de confirmar que TODO funciona correctamente:

```bash
# 1. Detener y eliminar contenedores individuales
docker-compose stop errorservice-db authservice-db notificationservice-db
docker-compose rm errorservice-db authservice-db notificationservice-db

# 2. Eliminar secciones de servicios *_db del compose.yaml

# 3. Limpiar volumes no utilizados
docker volume prune
```

---

## ⚠️ Puntos Críticos de Atención

### 🛑 NUNCA hacer esto

- NO eliminar servicios `*_db` antes de migrar los datos
- NO cambiar connection strings antes de migrar los datos
- NO eliminar los backups hasta confirmar que todo funciona

### ✅ Siempre hacer esto

- Crear backups ANTES de cualquier cambio
- Probar la migración en un paso a la vez
- Verificar cada servicio individualmente después del cambio
- Mantener los scripts de rollback listos

---

## 🔄 Plan de Rollback

Si algo sale mal en cualquier momento:

```bash
# Ejecutar script de rollback
./scripts/rollback-migration.sh

# Verificar que los servicios originales estén funcionando
docker-compose ps
docker-compose logs -f errorservice
```

---

## 📊 Beneficios Esperados

### Recursos

- **Memoria:** ~25 contenedores × 256MB = 6.4GB → 1 contenedor × 1GB = 1GB
- **CPU:** ~25 contenedores × 0.25 CPU = 6.25 CPU → 1 contenedor × 1 CPU = 1 CPU
- **Ahorro:** ~5.4GB RAM y 5.25 CPU

### Administración

- **Contenedores:** 25 → 1 (96% reducción)
- **Puertos:** 25 puertos → 1 puerto
- **Volumes:** 25 volumes → 1 volume
- **Health checks:** 25 → 1

### Desarrollo

- **docker-compose up:** Más rápido
- **Logs:** Más fácil de gestionar
- **Networking:** Simplificado

---

## 🏃‍♂️ Comandos Quick Start

```bash
# Fase de prueba
docker-compose up -d postgres_db
docker logs postgres_db

# Migración (cuando esté listo)
./scripts/migrate-to-postgres-db.sh

# Rollback (si algo sale mal)
./scripts/rollback-migration.sh
```

---

**⚡ Próximo paso:** Ejecutar `docker-compose up -d postgres_db` para probar el nuevo contenedor consolidado.
