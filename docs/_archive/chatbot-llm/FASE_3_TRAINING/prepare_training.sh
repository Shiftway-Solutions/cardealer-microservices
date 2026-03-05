#!/bin/bash
# =================================================================
# OKLA Chatbot — Quick Start Guide
# =================================================================
# Script para preparar y ejecutar el entrenamiento del modelo
#
# Pre-requisitos:
#   1. Google Colab con GPU T4 (gratis) o A100 (Pro)
#   2. Cuenta de HuggingFace con acceso a Meta-Llama-3-8B-Instruct
#   3. Dataset generado (ya incluido en FASE_2_DATASET/output/)
#
# Uso:
#   ./prepare_training.sh
# =================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DATASET_DIR="$SCRIPT_DIR/../FASE_2_DATASET/output"
TRAINING_DIR="$SCRIPT_DIR"
OUTPUT_DIR="$SCRIPT_DIR/output"

echo "============================================================"
echo "🤖 OKLA Chatbot — Preparación para Fine-Tuning"
echo "============================================================"

# 1. Verificar que el dataset existe
echo ""
echo "📊 Verificando dataset..."
if [ -f "$DATASET_DIR/okla_train.jsonl" ] && [ -f "$DATASET_DIR/okla_eval.jsonl" ]; then
    TRAIN_COUNT=$(wc -l < "$DATASET_DIR/okla_train.jsonl")
    EVAL_COUNT=$(wc -l < "$DATASET_DIR/okla_eval.jsonl")
    TEST_COUNT=$(wc -l < "$DATASET_DIR/okla_test.jsonl" 2>/dev/null || echo "0")
    echo "   ✅ okla_train.jsonl: $TRAIN_COUNT conversaciones"
    echo "   ✅ okla_eval.jsonl:  $EVAL_COUNT conversaciones"
    echo "   ✅ okla_test.jsonl:  $TEST_COUNT conversaciones"
else
    echo "   ❌ Dataset no encontrado en $DATASET_DIR"
    echo "   Ejecuta primero: cd ../FASE_2_DATASET && python generate_dataset.py"
    exit 1
fi

# 2. Verificar notebook
echo ""
echo "📓 Verificando notebook..."
if [ -f "$TRAINING_DIR/okla_finetune_llama3.ipynb" ]; then
    CELLS=$(python3 -c "import json; nb=json.load(open('$TRAINING_DIR/okla_finetune_llama3.ipynb')); print(len([c for c in nb['cells'] if c['cell_type']=='code']))")
    echo "   ✅ okla_finetune_llama3.ipynb: $CELLS celdas de código"
else
    echo "   ❌ Notebook no encontrado"
    exit 1
fi

# 3. Crear directorio de output
mkdir -p "$OUTPUT_DIR"

# 4. Instrucciones
echo ""
echo "============================================================"
echo "📋 INSTRUCCIONES PARA EJECUTAR EL ENTRENAMIENTO"
echo "============================================================"
echo ""
echo "OPCIÓN A — Google Colab (Recomendado)"
echo "───────────────────────────────────────"
echo "  1. Sube el dataset a Google Drive:"
echo "     Drive/OKLA/dataset/"
echo "       ├── okla_train.jsonl"
echo "       ├── okla_eval.jsonl"
echo "       └── okla_test.jsonl"
echo ""
echo "  2. Abre el notebook en Colab:"
echo "     - Ve a colab.research.google.com"
echo "     - File → Upload → okla_finetune_llama3.ipynb"
echo "     - Runtime → Change runtime type → T4 GPU"
echo ""
echo "  3. Ejecuta todas las celdas secuencialmente (Ctrl+F9)"
echo ""
echo "  4. El modelo entrenado se guardará en:"
echo "     Drive/OKLA/models/"
echo "       ├── okla-llama3-8b-lora/     (~100 MB, adaptadores)"
echo "       └── okla-llama3-8b-chatbot.Q4_K_M.gguf (~4.7 GB)"
echo ""
echo "  5. Descarga el GGUF y colócalo en:"
echo "     backend/ChatbotService/LlmServer/models/"
echo ""
echo ""
echo "OPCIÓN B — VS Code + Colab Extension"
echo "───────────────────────────────────────"
echo "  1. Instala la extensión 'Google Colab' en VS Code"
echo "  2. Abre okla_finetune_llama3.ipynb"
echo "  3. Conecta al runtime de Colab (necesita cuenta Google)"
echo "  4. Ejecuta celdas desde VS Code"
echo ""
echo ""
echo "OPCIÓN C — GPU Local / Cloud GPU"
echo "───────────────────────────────────────"
echo "  Si tienes GPU con ≥16GB VRAM:"
echo "  pip install -r requirements-training.txt"
echo "  jupyter notebook okla_finetune_llama3.ipynb"
echo ""
echo "============================================================"
echo ""
echo "⏱️  Tiempo estimado: ~2-3 horas en T4, ~45 min en A100"
echo "💰 Costo: Gratis (Colab Free) o ~\$10/mes (Colab Pro)"
echo ""
echo "============================================================"
echo "🚀 Después del entrenamiento, ejecuta:"
echo "   cd backend/ChatbotService"
echo "   docker compose -f docker-compose.llm.yml up"
echo "============================================================"
