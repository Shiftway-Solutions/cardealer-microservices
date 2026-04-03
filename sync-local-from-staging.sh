#!/bin/bash
#
# sync-local-from-staging.sh
# Automatiza el pull de imágenes y restart de containers tras push a staging
#
# Uso:
#   ./sync-local-from-staging.sh
#   ./sync-local-from-staging.sh --profile vehicles
#   ./sync-local-from-staging.sh --wait 300  # esperar 5 min a que CI/CD termine
#

set -e

# Color output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Parámetros
PROFILE="business"  # profile por defecto
WAIT_TIME=0

# Parse argumentos
while [[ $# -gt 0 ]]; do
    case $1 in
        --profile)
            PROFILE="$2"
            shift 2
            ;;
        --wait)
            WAIT_TIME="$2"
            shift 2
            ;;
        *)
            echo "Uso: $0 [--profile PROFILE] [--wait SECONDS]"
            echo "  --profile   Docker compose profile (default: business)"
            echo "  --wait      Esperar N segundos antes de pull (útil tras push a staging)"
            exit 1
            ;;
    esac
done

echo -e "${YELLOW}🔄 Sincronizando dockers locales desde staging...${NC}"
echo "Profile: $PROFILE"

# Si se especifica espera, mostrarlo
if [ "$WAIT_TIME" -gt 0 ]; then
    echo -e "${YELLOW}⏳ Esperando $WAIT_TIME segundos a que CI/CD termine (staging)...${NC}"
    for ((i = WAIT_TIME; i > 0; i--)); do
        printf "\r⏳ Esperando... %d seg" "$i"
        sleep 1
    done
    echo ""
fi

# Step 1: Pull de imágenes nuevas
echo -e "${YELLOW}📥 Pull de imágenes...${NC}"
if docker compose pull 2>&1 | grep -q "Downloaded newer image"; then
    echo -e "${GREEN}✅ Imágenes actualizadas${NC}"
else
    echo -e "${YELLOW}ℹ️  Sin cambios en imágenes${NC}"
fi

# Step 2: Restart de containers
echo -e "${YELLOW}🔄 Reiniciando containers (profile: $PROFILE)...${NC}"
docker compose --profile "$PROFILE" down --remove-orphans 2>&1 | grep -v "removing" || true
docker compose --profile "$PROFILE" up -d

# Health check
echo ""
echo -e "${YELLOW}🏥 Health check...${NC}"
sleep 3
docker compose ps --format "table {{.Name}}\t{{.Status}}" | grep -E "(healthy|UP)" || true

echo ""
echo -e "${GREEN}✅ Sincronización completada!${NC}"
echo -e "${GREEN}URLs:${NC}"
echo "  Local:   https://okla.local"
echo "  Tunnel:  https://biological-robinson-videos-ward.trycloudflare.com (si está corriendo)"
echo ""
echo "Próximos pasos:"
echo "  1. Abrir tunnel (opcional): docker compose --profile tunnel up -d cloudflared"
echo "  2. Frontend: cd frontend/web-next && pnpm dev"
echo "  3. Verificar en navegador"
