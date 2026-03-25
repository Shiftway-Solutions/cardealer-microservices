# OKLA — Tareas Manuales del Owner

> Estas son tareas que **NO puede hacer OpenClaw** y que requieren intervención manual del owner.  
> Fecha de creación: 2026-03-25

---

## 🔴 URGENTE — Infraestructura Rota (Sprint 0)

Antes de que la auditoría automática funcione, debes verificar/arreglar estos servicios:

### 1. Auth Server Action (BLOCKER)
- **Problema**: Login da "Server Action ID not found" — el server action de NextAuth no está compilado
- **Acción**:
  1. SSH al server o accede al cluster DOKS
  2. Verifica que el pod de frontend está corriendo: `kubectl get pods -n okla`
  3. Verifica los logs: `kubectl logs -n okla -l app=frontend --tail=100`
  4. Si hay error de build, redeploy: `kubectl rollout restart deployment/frontend -n okla`
  5. Verifica que `NEXTAUTH_SECRET` y `NEXTAUTH_URL` están configurados en el ConfigMap/Secret
  6. **Test**: Navega a https://okla.com.do/login e intenta hacer login

### 2. AWS S3 — Imágenes 403 Forbidden
- **Problema**: Todas las imágenes de vehículos devuelven 403 de S3
- **Acción**:
  1. Accede a la consola de AWS S3
  2. Verifica el bucket policy del bucket de imágenes
  3. Verifica que el CloudFront distribution apunta al bucket correcto
  4. Verifica CORS configuration del bucket
  5. Si el bucket es nuevo, agrega la policy pública de lectura:
     ```json
     {
       "Version": "2012-10-17",
       "Statement": [{
         "Sid": "PublicReadGetObject",
         "Effect": "Allow",
         "Principal": "*",
         "Action": "s3:GetObject",
         "Resource": "arn:aws:s3:::okla-media-prod/*"
       }]
     }
     ```
  6. **Test**: Abre una URL de imagen directamente en el browser

### 3. Stripe — 503 Service Unavailable
- **Problema**: Stripe devuelve 503 en checkout
- **Acción**:
  1. Verifica en https://dashboard.stripe.com que la cuenta está activa
  2. Verifica que las API keys (publishable + secret) están configuradas en los Secrets de K8s
  3. Verifica que el webhook endpoint está configurado: `https://api.okla.com.do/webhooks/stripe`
  4. Crea los Products y Prices en Stripe que correspondan a los planes:
     - Dealer LIBRE: $0/mes
     - Dealer VISIBLE: $27.80/mes (RD$1,682)
     - Dealer PRO: $89/mes (RD$5,385.50)
     - Dealer ÉLITE: $199/mes (RD$12,039.50)
  5. Mapea los `priceId` de Stripe a los planes en el backend (AdminService o AuthService)
  6. **Test**: Intenta hacer upgrade de plan como dealer

### 4. Dealer Dashboard — 404
- **Problema**: /cuenta/dealer devuelve 404
- **Acción**:
  1. Verifica que el archivo `frontend/web-next/src/app/(main)/cuenta/dealer/page.tsx` existe
  2. Si no existe, la ruta podría ser `/cuenta` (unified) en vez de `/cuenta/dealer`
  3. Verifica las rutas en `src/app/(main)/cuenta/` — ¿hay subcarpetas para cada rol?
  4. **Test**: Login como dealer → navega a /cuenta

---

## 🟡 IMPORTANTE — Alineación de Planes (Sprint 3, 6, 7)

### 5. Decidir estructura de planes de DEALER
- **Problema P0-001**: Frontend muestra 6 planes, backend tiene 4
- **Decisión requerida**: ¿Cuáles son los planes definitivos?

| # | Plan       | Frontend (RD$)   | Backend (USD) | ¿Mantener? |
|---|------------|-------------------|---------------|------------|
| 1 | LIBRE      | RD$0/mes          | $0            | ✅ Sí        |
| 2 | VISIBLE    | RD$1,682/mes      | $27.80        | ✅ Sí        |
| 3 | STARTER    | RD$3,422/mes      | NO EXISTE     | ❓ Decidir  |
| 4 | PRO        | RD$5,742/mes      | $89           | ✅ Sí        |
| 5 | ÉLITE      | RD$20,242/mes     | $199          | ✅ Sí        |
| 6 | ENTERPRISE | RD$34,742/mes     | NO EXISTE     | ❓ Decidir  |

**Opciones**:
- A) Agregar STARTER y ENTERPRISE al backend → 6 planes iguales
- B) Remover STARTER y ENTERPRISE del frontend → 4 planes iguales
- C) Mantener 6 en frontend pero solo 4 funcionales → confuso para usuarios

**Acción**: Decide y marca aquí: [ ] Opción elegida: ___

### 6. Decidir estructura de planes de SELLER
- **Problema P0-002 y P0-004**: Hay DOS páginas con planes diferentes
  - `/vender` dice: Gratis (RD$0), Premium (RD$579/mes), PRO (RD$2,029/mes)
  - `/cuenta/suscripcion` dice: Libre, Estándar ($9.99/pub), Verificado ($34.99/mes)
  - Backend: NO tiene seller plans implementados

**Decisión requerida**: ¿Cuáles son los planes definitivos del seller?
- A) Los de /vender → implementar en backend
- B) Los de /cuenta/suscripcion → actualizar /vender
- C) Unificar con nuevos planes → actualizar ambos + backend

**Acción**: Decide y marca aquí: [ ] Opción elegida: ___

### 7. Corregir precios de ÉLITE
- **Problema P0-003**: ÉLITE cuesta RD$20,242 en frontend pero $199 ≈ RD$12,040 en backend
- **Diferencia**: RD$8,202 (¡68% más caro en frontend!)
- **Acción**: Decide el precio correcto y actualiza frontend O backend

---

## 🟢 NORMAL — Datos y Contenido

### 8. Limpiar datos de prueba en producción
- **Problema P0-005**: Vehículo "Toyota Corolla 2022 — E2E mm8mioxc" visible en producción
- **Acción**:
  1. Conecta a la base de datos de producción (PostgreSQL)
  2. Busca: `SELECT * FROM vehicles WHERE title LIKE '%E2E%' OR title LIKE '%mm8mioxc%';`
  3. Marca como inactivo: `UPDATE vehicles SET status = 'inactive' WHERE ...;`
  4. O elimina con soft delete si el sistema lo soporta

### 9. Corregir datos en español
- **Problema P0-006**: Algunos vehículos tienen "gasoline" en vez de "Gasolina"
- **Acción**:
  ```sql
  UPDATE vehicles SET fuel_type = 'Gasolina' WHERE fuel_type = 'gasoline';
  UPDATE vehicles SET fuel_type = 'Diésel' WHERE fuel_type = 'diesel';
  UPDATE vehicles SET fuel_type = 'Híbrido' WHERE fuel_type = 'hybrid';
  UPDATE vehicles SET fuel_type = 'Eléctrico' WHERE fuel_type = 'electric';
  ```

### 10. Corregir ubicaciones
- **Problema P0-008**: "Santo DomingoNorte" sin espacio
- **Acción**:
  ```sql
  UPDATE vehicles SET location = 'Santo Domingo Norte' WHERE location = 'Santo DomingoNorte';
  UPDATE vehicles SET location = 'Santo Domingo Este' WHERE location = 'Santo DomingoEste';
  UPDATE vehicles SET location = 'Santo Domingo Oeste' WHERE location = 'Santo DomingoOeste';
  ```

### 11. Eliminar vehículos duplicados
- **Problema P0-007**: Maserati Ghibli y otros aparecen duplicados en carruseles
- **Acción**:
  1. Verificar si son duplicados reales en DB o si el frontend los muestra 2 veces
  2. Si es DB: `SELECT vin, COUNT(*) FROM vehicles GROUP BY vin HAVING COUNT(*) > 1;`
  3. Si es frontend: revisar lógica del componente de carrusel

### 12. Verificar testimonios
- **Problema**: Juan Pérez, María García, Carlos Martínez en /dealers — ¿son reales?
- **Acción**: Si son ficticios, agregar disclaimer legal visible "Testimonios ilustrativos"
- **Ley aplicable**: Ley 358-05 Art. 109 — Publicidad engañosa

---

## 🔵 CONFIGURACIÓN — Backend / DevOps

### 13. Arreglar ClockSkew de JWT
- **Problema P0-009**: Gateway tiene `ClockSkew = TimeSpan.Zero` pero AuthService usa 5 minutos
- **Acción**: 
  1. En Gateway: `TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5)`
  2. O en AuthService: reducir a `TimeSpan.Zero` (más seguro pero menos tolerante)
  3. Recomendación: usar `TimeSpan.FromMinutes(2)` en ambos

### 14. Configurar CORS correctamente
- **Acción**:
  1. Verificar que el Gateway permite `https://okla.com.do` en CORS
  2. Verificar que `Access-Control-Allow-Credentials: true` está habilitado
  3. Verificar que `SameSite=Lax` en cookies de auth

### 15. Configurar DNS y SSL
- **Acción**:
  1. Verificar que `okla.com.do` apunta al LoadBalancer de DOKS
  2. Verificar que `api.okla.com.do` apunta al Gateway
  3. Verificar SSL con: `curl -vI https://okla.com.do 2>&1 | grep -i "SSL\|certificate\|expire"`
  4. Renovar cert si expira pronto

### 16. Configurar monitoreo
- **Acción**:
  1. Verificar que Prometheus está scraping métricas de cada microservicio
  2. Verificar Grafana dashboards
  3. Configurar alertas para: CPU > 80%, Memory > 80%, 5xx > 1%, latency p99 > 2s
  4. Configurar UptimeRobot o similar para https://okla.com.do

---

## 📋 Checklist Pre-Auditoría

Antes de ejecutar `python3 .prompts/monitor_prompt1.py --sprint 1`, verifica:

- [ ] Auth funciona (login manual OK en browser)
- [ ] Imágenes cargan (no 403 S3)
- [ ] Stripe configurado (o al menos no 503)
- [ ] Frontend desplegado (no 404 en rutas principales)
- [ ] 4 cuentas de prueba funcionan:
  - [ ] admin@okla.local / Admin123!@#
  - [ ] buyer002@okla-test.com / BuyerTest2026!
  - [ ] nmateo@okla.com.do / Dealer2026!@#
  - [ ] gmoreno@okla.com.do / $Gregory1

---

## 🚀 Cómo Ejecutar la Auditoría

Una vez que la infraestructura está OK:

```bash
# 1. Ver estado actual
python3 .prompts/monitor_prompt1.py --status

# 2. Despachar Sprint 1 (Guest — homepage)
python3 .prompts/monitor_prompt1.py --sprint 1

# 3. OpenClaw lo ejecuta automáticamente (monitorea prompt_1.md)
#    Cuando termina, agrega READ al final

# 4. Verificar si terminó
python3 .prompts/monitor_prompt1.py --check

# 5. Avanzar al siguiente sprint
python3 .prompts/monitor_prompt1.py --next

# 6. O ejecutar todos automáticamente (espera entre sprints)
python3 .prompts/monitor_prompt1.py --all
```

---

_Última actualización: 2026-03-25_
