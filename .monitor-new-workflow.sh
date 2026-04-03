#!/bin/bash
echo "📊 Monitoreando workflow smart-cicd.yml con nueva configuración..."
echo "Branch: main → Build + Test + Docker Push + Deploy DigitalOcean"
echo ""

for i in {1..120}; do
  STATUS=$(gh run view --repo Shiftway-Solutions/cardealer-microservices --workflow smart-cicd.yml 2>&1 | grep -i "status:" | head -1 | awk '{print $NF}')
  
  if [ "$STATUS" = "in_progress" ] || [ "$STATUS" = "queued" ]; then
    echo "[$(date '+%H:%M:%S')] 🟢 Running... (attempt $i/120)"
  elif [ "$STATUS" = "completed" ]; then
    echo "[$(date '+%H:%M:%S')] 🔴 Completed!"
    echo ""
    gh run view --repo Shiftway-Solutions/cardealer-microservices --workflow smart-cicd.yml --json conclusion
    break
  fi
  
  sleep 10
done
