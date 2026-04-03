#!/bin/bash
RUN_ID=23958971057
WORKFLOW="smart-cicd.yml"
echo "🔍 Monitoreando $WORKFLOW (Run #$RUN_ID)..."
echo ""

for i in {1..60}; do
  STATUS=$(gh run view $RUN_ID --json status,conclusion 2>&1 | grep -o '"status":"[^"]*"' | cut -d'"' -f4)
  CONCLUSION=$(gh run view $RUN_ID --json status,conclusion 2>&1 | grep -o '"conclusion":"[^"]*"' | cut -d'"' -f4)
  
  ICON="🟢"
  [ "$STATUS" = "completed" ] && ICON="🔴"
  [ "$STATUS" = "queued" ] && ICON="⟳"
  
  RESULT=""
  [ "$CONCLUSION" = "success" ] && RESULT=" ✅ SUCCESS"
  [ "$CONCLUSION" = "failure" ] && RESULT=" ❌ FAILED"
  [ "$CONCLUSION" = "cancelled" ] && RESULT=" ⛔ CANCELLED"
  
  echo "[$(date '+%H:%M:%S')] $ICON $STATUS$RESULT"
  
  if [ "$STATUS" = "completed" ]; then
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "✅ Workflow completado"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    gh run view $RUN_ID --json jobs 2>&1 | grep -E '"name"|"conclusion"' | head -20
    break
  fi
  
  [ $i -lt 60 ] && sleep 30
done
