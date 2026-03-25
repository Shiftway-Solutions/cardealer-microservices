# 🚨 MIGRACIÓN CRÍTICA: AWS S3 → DigitalOcean Spaces

**Prioridad**: P0 (Crítico - Inmediato)  
**Fecha**: 2026-03-24  
**Responsable**: Gregory Moreno (Senior Developer)  
**Deadline**: 48 horas máximo  

## 📋 RESUMEN EJECUTIVO

**Problema identificado**: La plataforma OKLA tiene dependencia completa de AWS S3 que está causando:
- ❌ **403 Forbidden errors** en vehicle images (URLs expiradas)
- 💰 **Costos elevados** comparado con DigitalOcean Spaces
- 🔒 **Vendor lock-in** con Amazon
- ⚡ **Performance issues** por región (RD está más cerca de NYC que Virginia)

## 🎯 OBJETIVO

**Migrar COMPLETAMENTE** de AWS S3 a DigitalOcean Spaces para:
- ✅ Reducir costos 60-80%
- ✅ Mejorar performance (latencia RD → NYC)
- ✅ Mantener compatibilidad S3 (SDK same)
- ✅ Eliminar vendor lock-in

---

## 🔍 ANÁLISIS ACTUAL

### URLs AWS S3 Detectadas en Producción:
```
https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/ec183b7f-55fb-46bf-ad64-dde762cbd962.jpg
```

### Patrones de URL a Migrar:
- `https://{bucket}.s3.{region}.amazonaws.com/{key}`
- `https://s3.{region}.amazonaws.com/{bucket}/{key}`
- Presigned URLs con `X-Amz-*` parameters

### Servicios Afectados (estimado):
- **MediaService** - Upload/storage de imágenes vehículos
- **AdminService** - Gestión de assets
- **Frontend** - Display de imágenes
- **Gateway** - Proxy/CDN para assets

---

## 📝 TAREAS DETALLADAS

### **FASE 1: CONFIGURACIÓN DIGITALOCEAN SPACES (2 horas)**

#### 1.1 Crear DigitalOcean Space
- [ ] Crear Space: `okla-media-prod` en región `nyc3` 
- [ ] Configurar CDN endpoint
- [ ] Generar Access Keys (Spaces API)
- [ ] Configurar CORS policy
- [ ] Establecer bucket policy (public read para imágenes)

#### 1.2 Configurar Variables de Entorno
```bash
# Agregar a todos los microservices
DO_SPACES_KEY=dop_v1_xxxxx
DO_SPACES_SECRET=xxxxx
DO_SPACES_ENDPOINT=https://nyc3.digitaloceanspaces.com
DO_SPACES_BUCKET=okla-media-prod
DO_SPACES_REGION=nyc3
DO_SPACES_CDN=https://okla-media-prod.nyc3.cdn.digitaloceanspaces.com
```

### **FASE 2: BACKEND MIGRATION (8 horas)**

#### 2.1 MediaService - Core Storage Layer
```typescript
// backend/MediaService/src/services/StorageService.ts
class StorageService {
  private s3Client: S3Client;
  
  constructor() {
    // CAMBIO CRÍTICO: Usar DigitalOcean endpoint
    this.s3Client = new S3Client({
      endpoint: process.env.DO_SPACES_ENDPOINT, // https://nyc3.digitaloceanspaces.com
      region: process.env.DO_SPACES_REGION,     // nyc3
      credentials: {
        accessKeyId: process.env.DO_SPACES_KEY,
        secretAccessKey: process.env.DO_SPACES_SECRET
      },
      forcePathStyle: false // DigitalOcean usa virtual-hosted-style
    });
  }
}
```

**Archivos a modificar**:
- [ ] `backend/MediaService/src/services/StorageService.ts`
- [ ] `backend/MediaService/src/controllers/UploadController.ts`
- [ ] `backend/MediaService/src/utils/UrlGenerator.ts`
- [ ] `backend/MediaService/src/config/storage.ts`

#### 2.2 AdminService - Dashboard Assets
```typescript
// backend/AdminService/src/services/ImageService.ts
generateVehicleImageUrl(key: string): string {
  // ANTES: https://okla-images-2026.s3.us-east-2.amazonaws.com/
  // DESPUÉS: https://okla-media-prod.nyc3.cdn.digitaloceanspaces.com/
  return `${process.env.DO_SPACES_CDN}/${key}`;
}
```

**Archivos a modificar**:
- [ ] `backend/AdminService/src/services/ImageService.ts`
- [ ] `backend/AdminService/src/controllers/VehicleController.ts`

#### 2.3 Gateway - Proxy Configuration
```typescript
// backend/Gateway/src/middleware/assetsProxy.ts
app.use('/assets/*', (req, res) => {
  // REDIRIGIR: /assets/* → DigitalOcean CDN
  const cdnUrl = `${process.env.DO_SPACES_CDN}${req.path}`;
  res.redirect(301, cdnUrl);
});
```

#### 2.4 Database URL Updates
```sql
-- Actualizar URLs existentes en BD
UPDATE vehicles 
SET image_url = REPLACE(
  image_url, 
  'https://okla-images-2026.s3.us-east-2.amazonaws.com/', 
  'https://okla-media-prod.nyc3.cdn.digitaloceanspaces.com/'
) 
WHERE image_url LIKE '%s3.us-east-2.amazonaws.com%';

UPDATE vehicle_images 
SET url = REPLACE(
  url, 
  'https://okla-images-2026.s3.us-east-2.amazonaws.com/', 
  'https://okla-media-prod.nyc3.cdn.digitaloceanspaces.com/'
) 
WHERE url LIKE '%s3.us-east-2.amazonaws.com%';
```

### **FASE 3: FRONTEND MIGRATION (4 horas)**

#### 3.1 Next.js Image Domains
```typescript
// frontend/web-next/next.config.js
module.exports = {
  images: {
    domains: [
      // REMOVER: 'okla-images-2026.s3.us-east-2.amazonaws.com',
      'okla-media-prod.nyc3.cdn.digitaloceanspaces.com', // AGREGAR
      'okla-media-prod.nyc3.digitaloceanspaces.com',     // BACKUP
    ],
  },
};
```

#### 3.2 Environment Variables Frontend
```bash
# frontend/web-next/.env.local
NEXT_PUBLIC_CDN_URL=https://okla-media-prod.nyc3.cdn.digitaloceanspaces.com
```

#### 3.3 Image Component Updates
```typescript
// frontend/web-next/src/components/VehicleImage.tsx
const VehicleImage: React.FC<{src: string}> = ({ src }) => {
  // Transformar URLs AWS → DigitalOcean automáticamente
  const cdnSrc = src.includes('s3.us-east-2.amazonaws.com') 
    ? src.replace(
        'https://okla-images-2026.s3.us-east-2.amazonaws.com/',
        process.env.NEXT_PUBLIC_CDN_URL + '/'
      )
    : src;
    
  return <Image src={cdnSrc} alt="Vehicle" />;
};
```

### **FASE 4: DATA MIGRATION (6 horas)**

#### 4.1 Migración de Assets Existentes
```bash
#!/bin/bash
# scripts/migrate-s3-to-spaces.sh

# 1. Sync existing S3 → DigitalOcean Spaces
aws s3 sync s3://okla-images-2026 s3://okla-media-prod \
  --endpoint-url https://nyc3.digitaloceanspaces.com \
  --region nyc3 \
  --no-progress

# 2. Verificar integridad
aws s3 ls s3://okla-media-prod/vehicles/ \
  --endpoint-url https://nyc3.digitaloceanspaces.com \
  --recursive | wc -l
```

#### 4.2 Validación Post-Migración
```typescript
// scripts/validate-migration.ts
async function validateImageUrls() {
  const vehicles = await db.vehicle.findMany({
    select: { id: true, image_url: true }
  });
  
  const results = await Promise.allSettled(
    vehicles.map(async (vehicle) => {
      const response = await fetch(vehicle.image_url, { method: 'HEAD' });
      return {
        id: vehicle.id,
        url: vehicle.image_url,
        status: response.status
      };
    })
  );
  
  const errors = results
    .filter(result => result.status === 'fulfilled' && result.value.status !== 200)
    .map(result => result.value);
    
  console.log(`${errors.length} imágenes con errores de ${vehicles.length} total`);
}
```

### **FASE 5: TESTING & ROLLBACK (2 horas)**

#### 5.1 Testing Checklist
- [ ] Subir nueva imagen desde admin → DigitalOcean ✓
- [ ] Visualizar imágenes existentes en /vehiculos ✓  
- [ ] Verificar CDN performance (latency < 200ms RD) ✓
- [ ] Confirmar URLs no contienen AWS S3 ✓
- [ ] Testing mobile + desktop ✓

#### 5.2 Plan de Rollback
```bash
# Si algo sale mal, rollback inmediato:
# 1. Revertir environment variables a AWS S3
# 2. Revertir código: git revert {commits}
# 3. Aplicar hotfix de URLs en BD si necesario
```

---

## ⏱️ TIMELINE ESTIMADO

| Fase | Duración | Responsable | Dependencies |
|------|----------|-------------|--------------|
| **Setup DO Spaces** | 2h | Gregory | Access to DO account |
| **Backend Migration** | 8h | Gregory | Environment variables ready |
| **Frontend Migration** | 4h | Gregory | Backend deployed |
| **Data Migration** | 6h | Gregory | Maintenance window |
| **Testing** | 2h | Gregory | All previous phases |
| **Total** | **22h** | **2-3 días** | **Coordinar con equipo** |

---

## 💰 BENEFICIOS ESTIMADOS

### Costos (mensual):
- **AWS S3**: ~$120/mes (us-east-2 + transfer costs)
- **DO Spaces**: ~$25/mes (250GB incluido + CDN)  
- **Ahorro**: **$95/mes = $1,140/año** 

### Performance:
- **Latency RD → AWS us-east-2**: ~80-120ms
- **Latency RD → DO NYC3**: ~35-50ms
- **Mejora**: **60% faster**

---

## 🚨 RIESGOS Y MITIGACIONES

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| **Downtime durante migración** | Media | Alto | Migración fuera de horas pico + rollback plan |
| **URLs rotas post-migración** | Baja | Alto | Validación automatizada + testing extensivo |
| **Performance degradation** | Baja | Medio | DO NYC3 más cercano que AWS us-east-2 |
| **API compatibility issues** | Muy Baja | Medio | S3 API compatible, mismo SDK |

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### Pre-Migration
- [ ] Crear backup completo de S3 actual
- [ ] Configurar DigitalOcean Space
- [ ] Actualizar variables de entorno (staging first)
- [ ] Crear script de migración de data

### Migration Day
- [ ] **06:00 AST**: Iniciar maintenance window  
- [ ] **06:15**: Deploy backend changes
- [ ] **06:30**: Ejecutar data migration script
- [ ] **07:30**: Deploy frontend changes
- [ ] **08:00**: Ejecutar validación completa
- [ ] **08:30**: Testing funcional completo
- [ ] **09:00**: Abrir plataforma al público

### Post-Migration
- [ ] Monitorear logs por 48h
- [ ] Confirmar 0 errores 403 en imágenes  
- [ ] Benchmark performance improvement
- [ ] Documentar proceso para futuras migraciones
- [ ] Programar eliminación de AWS S3 (después 1 semana)

---

## 📞 CONTACTOS DE EMERGENCIA

**Si algo sale mal durante la migración:**
- Gregory Moreno: WhatsApp/Telegram inmediato
- Equipo DevOps: Notificar en Slack #emergency
- Rollback automático si downtime > 30 minutos

---

**Documento creado**: 2026-03-24 10:55 AST  
**Última actualización**: 2026-03-24 10:55 AST  
**Próxima revisión**: Antes de iniciar implementación  