#!/bin/zsh
#
# 🔍 DIAGNOSTIC: Copilot Agent Monitor — Component Health Check
#

set -e

echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║  🔍 COPILOT AGENT MONITOR — DIAGNOSTIC CHECK                  ║"
echo "║     Version: 1.3.9 | Build: Apr 2, 2026                       ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

PASS="${GREEN}✅${NC}"
FAIL="${RED}❌${NC}"
WARN="${YELLOW}⚠️${NC}"
INFO="${BLUE}ℹ️${NC}"

# Check 1: Extension installed
echo "📌 Check 1: Extension Installation"
echo "────────────────────────────────────────────────────────────────"
if code --list-extensions 2>/dev/null | grep -q "copilot-agent-monitor"; then
    echo -e "${PASS} Extension installed: okla-dev.copilot-agent-monitor"
    EXT_VERSION=$(cd ~/.vscode/extensions/okla-dev.copilot-agent-monitor-* 2>/dev/null && cat package.json 2>/dev/null | grep '"version"' | head -1 | cut -d'"' -f4)
    if [ ! -z "$EXT_VERSION" ]; then
        echo "   Version: $EXT_VERSION"
    fi
else
    echo -e "${FAIL} Extension NOT installed or VS Code not found"
    exit 1
fi
echo ""

# Check 2: CDP Port (9222)
echo "📌 Check 2: Chrome DevTools Protocol (CDP)"
echo "────────────────────────────────────────────────────────────────"
if lsof -i :9222 >/dev/null 2>&1; then
    echo -e "${PASS} CDP port 9222 is OPEN and listening"
    lsof -i :9222 | tail -1 | awk '{print "   Process: " $1 " (PID " $2 ")"}'
else
    echo -e "${FAIL} CDP port 9222 is CLOSED"
    echo "   Action: Start VS Code with: code --remote-debugging-port=9222 /path"
fi
echo ""

# Check 3: VS Code running
echo "📌 Check 3: VS Code Process"
echo "────────────────────────────────────────────────────────────────"
if pgrep -q "^Code$|^code$"; then
    echo -e "${PASS} VS Code is running"
    CODE_PID=$(pgrep -f "^Code|^code" | head -1)
    echo "   PID: $CODE_PID"
    # Check if it has debugging port flag
    if ps p $CODE_PID | grep -q "remote-debugging-port"; then
        echo -e "${PASS} Launched WITH --remote-debugging-port flag"
    else
        echo -e "${WARN} May not have --remote-debugging-port flag (CDP might not work)"
    fi
else
    echo -e "${FAIL} VS Code is NOT running"
fi
echo ""

# Check 4: Workspace storage
echo "📌 Check 4: Copilot Monitor Workspace Storage"
echo "────────────────────────────────────────────────────────────────"
STORAGE_DIRS=$(find ~/.config/Code/User/workspaceStorage -type d -name "*GitHub.copilot-chat*" 2>/dev/null || echo "")
if [ ! -z "$STORAGE_DIRS" ]; then
    echo -e "${PASS} Copilot workspace storage found"
    AUDIT_LOG=$(find ~/.config/Code/User/workspaceStorage -name "*monitor*audit*" -type f 2>/dev/null | head -1)
    if [ ! -z "$AUDIT_LOG" ]; then
        echo -e "${PASS} Audit log exists: $(basename $AUDIT_LOG)"
        LINES=$(wc -l < "$AUDIT_LOG" 2>/dev/null || echo "0")
        echo "   Entries: $LINES"
        if [ "$LINES" -gt 0 ]; then
            echo "   Last entry (truncated):"
            tail -1 "$AUDIT_LOG" 2>/dev/null | jq '.' 2>/dev/null | head -5 | sed 's/^/      /' || tail -1 "$AUDIT_LOG" | sed 's/^/      /'
        fi
    else
        echo -e "${INFO} Audit log not yet created (will appear after first monitor run)"
    fi
else
    echo -e "${WARN} No Copilot workspace storage found (Copilot may not have been used yet)"
fi
echo ""

# Check 5: Configuration
echo "📌 Check 5: VS Code Configuration"
echo "────────────────────────────────────────────────────────────────"
CONFIG_FILE="$HOME/Library/Application Support/Code/User/settings.json"
if [ -f "$CONFIG_FILE" ]; then
    # Check for copilot monitor settings
    if grep -q "copilotMonitor" "$CONFIG_FILE"; then
        echo -e "${PASS} Monitor settings found in settings.json"
        echo "   Settings:"
        grep "copilotMonitor" "$CONFIG_FILE" 2>/dev/null | head -5 | sed 's/^/      /'
    else
        echo -e "${INFO} No copilotMonitor custom settings (defaults will be used)"
    fi
else
    echo -e "${INFO} VS Code settings.json not accessible"
fi
echo ""

# Check 6: Required dependencies
echo "📌 Check 6: TypeScript Build"
echo "────────────────────────────────────────────────────────────────"
DIST_DIR=~/.vscode/extensions/okla-dev.copilot-agent-monitor-*/dist
if ls $DIST_DIR/*.js >/dev/null 2>&1; then
    FILE_COUNT=$(ls $DIST_DIR/*.js 2>/dev/null | wc -l)
    echo -e "${PASS} Compiled JavaScript files found ($FILE_COUNT files)"
    echo "   Key modules:"
    ls $DIST_DIR/*.js 2>/dev/null | xargs -n1 basename | sort | head -8 | sed 's/^/      /'
else
    echo -e "${FAIL} No compiled files found in dist/"
fi
echo ""

# Check 7: Network connectivity
echo "📌 Check 7: Network / API Endpoints"
echo "────────────────────────────────────────────────────────────────"
# Test GitHub Copilot endpoint (basic)
if timeout 2 curl -s -I https://api.github.com >/dev/null 2>&1; then
    echo -e "${PASS} Network connectivity OK (api.github.com reachable)"
else
    echo -e "${WARN} Could not reach api.github.com (may affect model detection)"
fi
echo ""

# Summary
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║  📊 SUMMARY                                                    ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

READY=true

if ! lsof -i :9222 >/dev/null 2>&1; then
    echo -e "${FAIL} CDP not available — DOM monitoring will NOT work"
    echo "   Action: Restart VS Code with --remote-debugging-port=9222"
    READY=false
fi

if ! pgrep -q "^Code$|^code$"; then
    echo -e "${FAIL} VS Code not running"
    READY=false
fi

if [ "$READY" = true ]; then
    echo -e "${PASS} All critical components are operational"
    echo ""
    echo "🚀 Next step: Follow TEST_MONITOR_ACTIVITY.md for activity detection test"
else
    echo ""
    echo "⚠️  Some components need attention before testing"
fi

echo ""
