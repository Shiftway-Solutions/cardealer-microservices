#!/usr/bin/env bash
# =============================================================================
# SETUP BRANCH PROTECTION RULES — OKLA Repository
# =============================================================================
# This script configures GitHub branch protection rules for the `main` branch
# using the GitHub CLI (gh). Run once after repo creation, or whenever rules
# need to be updated.
#
# Prerequisites:
#   - GitHub CLI installed: brew install gh
#   - Authenticated: gh auth login
#   - Admin permissions on the repository
#
# Usage:
#   bash .github/scripts/setup-branch-protection.sh
#
# What it configures:
#   1. Branch protection enabled on `main`
#   2. Require PR reviews with minimum 1 approval
#   3. Dismiss stale approvals when new commits are pushed
#   4. Require status checks to pass before merging
#   5. Require branches to be up to date before merging
#   6. Restrict direct pushes to main (including admin/owner)
#   7. Restrict force pushes to main
#   8. Require conversation resolution before merging
# =============================================================================

set -euo pipefail

# ── Configuration ──────────────────────────────────────────────────────────────
BRANCH="main"

# Required status checks — these must pass before merging
# These are the job names from smart-cicd.yml and pr-checks.yml
REQUIRED_STATUS_CHECKS=(
  "🔍 Detect Changes"
  "🌐 Frontend Checks"
  "🔧 Backend Checks"
  "🔍 Lint & Type Check"
  "🧪 Unit Tests"
  "🐳 Docker Build"
  "🔨 Build & Test"
  "📊 Pipeline Summary"
  "🔗 Integration Tests"
)

# ── Validate prerequisites ─────────────────────────────────────────────────────
if ! command -v gh &> /dev/null; then
  echo "❌ GitHub CLI (gh) not found. Install with: brew install gh"
  exit 1
fi

if ! gh auth status &> /dev/null; then
  echo "❌ Not authenticated. Run: gh auth login"
  exit 1
fi

REPO=$(gh repo view --json nameWithOwner -q '.nameWithOwner' 2>/dev/null)
if [ -z "$REPO" ]; then
  echo "❌ Not inside a GitHub repository"
  exit 1
fi

echo "🔧 Configuring branch protection for $REPO → branch: $BRANCH"
echo ""

# ── Build required_status_checks JSON array ────────────────────────────────────
CHECKS_JSON="["
FIRST=true
for CHECK in "${REQUIRED_STATUS_CHECKS[@]}"; do
  if [ "$FIRST" = true ]; then
    FIRST=false
  else
    CHECKS_JSON+=","
  fi
  CHECKS_JSON+="{\"context\":\"$CHECK\",\"app_id\":-1}"
done
CHECKS_JSON+="]"

# ── Apply branch protection via GitHub API ─────────────────────────────────────
echo "📋 Applying branch protection rules..."

gh api \
  --method PUT \
  "repos/$REPO/branches/$BRANCH/protection" \
  --input - <<EOF
{
  "required_status_checks": {
    "strict": true,
    "checks": $CHECKS_JSON
  },
  "enforce_admins": true,
  "required_pull_request_reviews": {
    "dismiss_stale_reviews": true,
    "require_code_owner_reviews": true,
    "required_approving_review_count": 1,
    "require_last_push_approval": false
  },
  "restrictions": null,
  "required_linear_history": false,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "block_creations": false,
  "required_conversation_resolution": true,
  "lock_branch": false,
  "allow_fork_syncing": false
}
EOF

echo ""
echo "✅ Branch protection rules applied successfully!"
echo ""
echo "📋 Rules configured:"
echo "  ✅ Require PR reviews: 1 approval minimum"
echo "  ✅ Dismiss stale approvals on new commits"
echo "  ✅ Require CODEOWNERS review"
echo "  ✅ Require status checks to pass before merge"
echo "  ✅ Require branch to be up to date before merge (strict)"
echo "  ✅ Enforce rules on admins (no bypass for owner)"
echo "  ✅ Block force pushes"
echo "  ✅ Block branch deletion"
echo "  ✅ Require conversation resolution"
echo ""
echo "📋 Required status checks:"
for CHECK in "${REQUIRED_STATUS_CHECKS[@]}"; do
  echo "  • $CHECK"
done
echo ""
echo "⚠️  NOTE: CODEOWNERS enforcement requires the CODEOWNERS file"
echo "   to be on the default branch. Current CODEOWNERS file covers:"
echo "   .github/, k8s/, compose*.yaml, Dockerfile, backend/AuthService/,"
echo "   backend/KYCService/, backend/BillingService/, backend/Gateway/,"
echo "   backend/_Shared/, frontend/, docs/, *.md"

echo ""
echo "🔧 Configuring GitHub Environments..."
echo ""

# ── Production environment — requires manual approval ──────────────────────────
OWNER_ID=$(gh api repos/$REPO --jq '.owner.id')

gh api \
  --method PUT \
  "repos/$REPO/environments/production" \
  --input - <<ENVEOF
{
  "wait_timer": 0,
  "prevent_self_review": false,
  "reviewers": [
    {
      "type": "User",
      "id": $OWNER_ID
    }
  ],
  "deployment_branch_policy": {
    "protected_branches": true,
    "custom_branch_policies": false
  }
}
ENVEOF

echo "  ✅ Production environment: manual approval from owner required"
echo "  ✅ Production environment: deploy only from protected branches"

# ── Staging environment — auto-deploy (no reviewers) ───────────────────────────
gh api \
  --method PUT \
  "repos/$REPO/environments/staging" \
  --input - <<ENVEOF
{
  "reviewers": [],
  "deployment_branch_policy": null
}
ENVEOF

echo "  ✅ Staging environment: auto-deploy (no approval required)"
echo ""
echo "🎉 All branch protection and environment rules configured!"
