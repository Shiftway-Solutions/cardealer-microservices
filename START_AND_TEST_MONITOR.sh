#!/bin/zsh
#
# 🚀 START MONITOR WITH CDP + PRE-TEST CHECKS
#

set -e

WORKSPACE="/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices"
CDP_PORT=9222

echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║  🚀 STARTING MONITOR WITH CDP ENABLED                         ║"
echo "║     Monitor: v1.3.9 | CDP Port: $CDP_PORT                      ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

# Step 1: Close VS Code if running
echo "📋 Step 1: Preparing environment..."
if pgrep -q Code; then
    echo "   Closing existing VS Code instance..."
    killall Code 2>/dev/null || true
    sleep 2
fi
echo "   ✓ Ready"
echo ""

# Step 2: Start VS Code with CDP
echo "🚀 Step 2: Starting VS Code with --remote-debugging-port=$CDP_PORT"
echo "   Command: code --remote-debugging-port=$CDP_PORT $WORKSPACE"
echo ""
code --remote-debugging-port=$CDP_PORT "$WORKSPACE" &
CODE_PID=$!
echo "   VS Code PID: $CODE_PID"
echo "   Waiting for startup (~5 seconds)..."
sleep 5
echo ""

# Step 3: Verify CDP
echo "✅ Step 3: Verifying CDP port..."
if lsof -i :$CDP_PORT 2>/dev/null | grep -q LISTEN; then
    echo "   ✓ CDP is LISTENING on port $CDP_PORT"
else
    echo "   ✗ FAILED: CDP not listening"
    echo "   This might take a few more seconds... waiting 3s"
    sleep 3
fi
echo ""

# Step 4: Instructions
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║  📋 ACTIVITY DETECTION TEST — MANUAL STEPS                     ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

echo "1️⃣  START THE MONITOR"
echo "   In VS Code:"
echo "   • Press: Cmd+Shift+P"
echo "   • Type: Copilot Monitor: Start"
echo "   • Press: Enter"
echo "   • Wait 2 seconds"
echo ""
echo "   Expected: Icon appears in status bar (eye icon = IDLE)"
echo ""

echo "2️⃣  OPEN COPILOT CHAT"
echo "   • Press: Cmd+Shift+L (or open Chat panel)"
echo ""

echo "3️⃣  WATCH THE STATUS BAR (bottom-right)"
echo "   Before sending message:"
echo "   • Status: $(eye) IDLE"
echo ""

echo "4️⃣  SEND A REQUEST (choose one):"
echo ""
echo "   📝 SHORT (~5 sec) — This is BEST for first test:"
echo "   └─ Write, don't paste! Type:"
echo "      > Escribe un validator de email con Vitest"
echo ""
echo "   📝 MEDIUM (~20 sec):"
echo "   └─ Analiza el pattern CQRS en backend/"
echo ""
echo "   📝 INSTANT (~2 sec):"
echo "   └─ What is Event Sourcing?"
echo ""

echo "5️⃣  OBSERVE WHILE PROCESSING (< 30 seconds):"
echo "   Watch the status bar text CHANGE to:"
echo "   • $(loading) GENERATING — DOM: Thinking..."
echo "   • $(loading) GENERATING — DOM: Processing..."
echo "   • $(loading) GENERATING — DOM: Analyzing..."
echo ""
echo "   Then when done:"
echo "   • $(check) COMPLETED"
echo ""

echo "6️⃣  CHECK ACTIVITY LOG"
echo "   • Press: Cmd+Shift+P"
echo "   • Type: Copilot Monitor: Show Activity Log"
echo "   • You should see: DOM: Thinking... | DOM events"
echo ""

echo "7️⃣  ADVANCED DEBUG (optional)"
echo "   Press: Cmd+Shift+U"
echo "   Select: Copilot Agent Monitor (Output Channel)"
echo "   Look for lines like:"
echo "   • [🔭 DOM] 12 events — Thinking..."
echo "   • [⚡ Log] active — model processing"
echo ""

echo "╔════════════════════════════════════════════════════════════════╗"
echo "║  ✅ WHAT SUCCESS LOOKS LIKE                                    ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""
echo "Timeline of a SUCCESSFUL test:"
echo ""
echo "  14:32:45 — You start typing a message"
echo "  14:32:47 — Status bar shows: 🔄 GENERATING — DOM: Thinking..."
echo "  14:32:52 — Status bar shows: 🔄 GENERATING — DOM: Analyzing..."
echo "  14:33:08 — Chat shows response"
echo "  14:33:10 — Status bar shows: ✅ COMPLETED"
echo ""
echo "All of this happens in < 30 seconds with no delays."
echo ""

echo "╔════════════════════════════════════════════════════════════════╗"
echo "║  🔴 TROUBLESHOOTING                                             ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

echo "❌ Problem: Status bar shows only $(eye) IDLE during processing"
echo "   → DOM watcher is NOT detecting activity"
echo "   → Check: Cmd+Shift+U → Copilot Agent Monitor"
echo "   → Look for: 'ChatDOMWatcher' messages"
echo "   → If 'CDP not available' → you need --remote-debugging-port"
echo ""

echo "❌ Problem: Status bar shows 🔄 but stays GENERATING for 90+ seconds"
echo "   → This is Bug 2 (should be fixed in v1.3.9)"
echo "   → Try: Reload Window (Cmd+Shift+P → Developer: Reload Window)"
echo ""

echo "❌ Problem: Activity Log is empty"
echo "   → Give it 5-10 seconds after starting the monitor"
echo "   → Or manually start a cycle: Cmd+Shift+P → Force Analyze"
echo ""

echo ""
echo "Ready? Follow the steps above starting with step 1️⃣"
echo ""

