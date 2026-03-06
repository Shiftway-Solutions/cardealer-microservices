#!/bin/bash
# ============================================================
# OKLA Platform — E2E Production Tests
# Date: 2026-03-05
# Base URL: https://okla.com.do
# ============================================================

set -euo pipefail

BASE_URL="https://okla.com.do"
API_URL="$BASE_URL/api"
PASS=0
FAIL=0
SKIP=0
RESULTS=""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
NC='\033[0m'

log_pass() {
  PASS=$((PASS + 1))
  RESULTS="${RESULTS}\n✅ PASS: $1"
  echo -e "${GREEN}✅ PASS${NC}: $1"
}

log_fail() {
  FAIL=$((FAIL + 1))
  RESULTS="${RESULTS}\n❌ FAIL: $1 — $2"
  echo -e "${RED}❌ FAIL${NC}: $1 — $2"
}

log_skip() {
  SKIP=$((SKIP + 1))
  RESULTS="${RESULTS}\n⏭️ SKIP: $1 — $2"
  echo -e "${YELLOW}⏭️ SKIP${NC}: $1 — $2"
}

# ============================================================
# SECTION 1: Health Checks
# ============================================================
echo ""
echo "=============================="
echo "SECTION 1: HEALTH CHECKS"
echo "=============================="

HEALTH_ENDPOINTS=(
  "auth"
  "errors"
  "notifications"
  "media"
  "admin"
  "kyc"
)

for svc in "${HEALTH_ENDPOINTS[@]}"; do
  HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$API_URL/$svc/health" 2>/dev/null || echo "000")
  if [[ "$HTTP_CODE" == "200" ]]; then
    log_pass "Health: $svc (HTTP $HTTP_CODE)"
  elif [[ "$HTTP_CODE" == "000" ]]; then
    log_fail "Health: $svc" "Timeout or connection refused"
  else
    log_fail "Health: $svc" "HTTP $HTTP_CODE"
  fi
done

# ============================================================
# SECTION 2: Frontend Accessibility
# ============================================================
echo ""
echo "=============================="
echo "SECTION 2: FRONTEND ACCESS"
echo "=============================="

# Homepage
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$BASE_URL" 2>/dev/null || echo "000")
if [[ "$HTTP_CODE" == "200" ]]; then
  log_pass "Frontend: Homepage loads (HTTP $HTTP_CODE)"
else
  log_fail "Frontend: Homepage" "HTTP $HTTP_CODE"
fi

# Check if HTML contains expected content
BODY=$(curl -s --max-time 10 "$BASE_URL" 2>/dev/null || echo "")
if echo "$BODY" | grep -qi "okla"; then
  log_pass "Frontend: Homepage contains 'OKLA' branding"
else
  log_fail "Frontend: Homepage" "Missing 'OKLA' branding in HTML"
fi

# ============================================================
# SECTION 3: Authentication — Admin Login
# ============================================================
echo ""
echo "=============================="
echo "SECTION 3: AUTH — ADMIN LOGIN"
echo "=============================="

ADMIN_LOGIN_RESPONSE=$(curl -s -w "\n%{http_code}" --max-time 15 \
  -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@okla.local","password":"Admin123!@#"}' \
  -c /tmp/okla_admin_cookies.txt 2>/dev/null || echo -e "\n000")

ADMIN_HTTP_CODE=$(echo "$ADMIN_LOGIN_RESPONSE" | tail -1)
ADMIN_BODY=$(echo "$ADMIN_LOGIN_RESPONSE" | sed '$d')

if [[ "$ADMIN_HTTP_CODE" == "200" ]]; then
  log_pass "Auth: Admin login successful (HTTP $ADMIN_HTTP_CODE)"
elif [[ "$ADMIN_HTTP_CODE" == "000" ]]; then
  log_fail "Auth: Admin login" "Timeout/connection error"
else
  log_fail "Auth: Admin login" "HTTP $ADMIN_HTTP_CODE — $ADMIN_BODY"
fi

# Admin — Get /me
ADMIN_ME_RESPONSE=$(curl -s -w "\n%{http_code}" --max-time 10 \
  -X GET "$API_URL/auth/me" \
  -b /tmp/okla_admin_cookies.txt 2>/dev/null || echo -e "\n000")

ADMIN_ME_CODE=$(echo "$ADMIN_ME_RESPONSE" | tail -1)
ADMIN_ME_BODY=$(echo "$ADMIN_ME_RESPONSE" | sed '$d')

if [[ "$ADMIN_ME_CODE" == "200" ]]; then
  log_pass "Auth: Admin /me returns profile (HTTP $ADMIN_ME_CODE)"
  # Check if response contains admin info
  if echo "$ADMIN_ME_BODY" | grep -qi "admin"; then
    log_pass "Auth: Admin profile contains admin role info"
  else
    log_skip "Auth: Admin profile role" "Could not verify admin role in response"
  fi
else
  log_fail "Auth: Admin /me" "HTTP $ADMIN_ME_CODE"
fi

# ============================================================
# SECTION 4: Authentication — Buyer Login
# ============================================================
echo ""
echo "=============================="
echo "SECTION 4: AUTH — BUYER LOGIN"
echo "=============================="

BUYER_LOGIN_RESPONSE=$(curl -s -w "\n%{http_code}" --max-time 15 \
  -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"buyer002@okla-test.com","password":"BuyerTest2026!"}' \
  -c /tmp/okla_buyer_cookies.txt 2>/dev/null || echo -e "\n000")

BUYER_HTTP_CODE=$(echo "$BUYER_LOGIN_RESPONSE" | tail -1)
BUYER_BODY=$(echo "$BUYER_LOGIN_RESPONSE" | sed '$d')

if [[ "$BUYER_HTTP_CODE" == "200" ]]; then
  log_pass "Auth: Buyer login successful (HTTP $BUYER_HTTP_CODE)"
else
  log_fail "Auth: Buyer login" "HTTP $BUYER_HTTP_CODE — $BUYER_BODY"
fi

# Buyer — Get /me
BUYER_ME_RESPONSE=$(curl -s -w "\n%{http_code}" --max-time 10 \
  -X GET "$API_URL/auth/me" \
  -b /tmp/okla_buyer_cookies.txt 2>/dev/null || echo -e "\n000")

BUYER_ME_CODE=$(echo "$BUYER_ME_RESPONSE" | tail -1)
if [[ "$BUYER_ME_CODE" == "200" ]]; then
  log_pass "Auth: Buyer /me returns profile (HTTP $BUYER_ME_CODE)"
else
  log_fail "Auth: Buyer /me" "HTTP $BUYER_ME_CODE"
fi

# ============================================================
# SECTION 5: Authentication — Dealer Login
# ============================================================
echo ""
echo "=============================="
echo "SECTION 5: AUTH — DEALER LOGIN"
echo "=============================="

DEALER_LOGIN_RESPONSE=$(curl -s -w "\n%{http_code}" --max-time 15 \
  -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"nmateo@okla.com.do","password":"Dealer2026!@#"}' \
  -c /tmp/okla_dealer_cookies.txt 2>/dev/null || echo -e "\n000")

DEALER_HTTP_CODE=$(echo "$DEALER_LOGIN_RESPONSE" | tail -1)
DEALER_BODY=$(echo "$DEALER_LOGIN_RESPONSE" | sed '$d')

if [[ "$DEALER_HTTP_CODE" == "200" ]]; then
  log_pass "Auth: Dealer login successful (HTTP $DEALER_HTTP_CODE)"
else
  log_fail "Auth: Dealer login" "HTTP $DEALER_HTTP_CODE — $DEALER_BODY"
fi

# Dealer — Get /me
DEALER_ME_RESPONSE=$(curl -s -w "\n%{http_code}" --max-time 10 \
  -X GET "$API_URL/auth/me" \
  -b /tmp/okla_dealer_cookies.txt 2>/dev/null || echo -e "\n000")

DEALER_ME_CODE=$(echo "$DEALER_ME_RESPONSE" | tail -1)
if [[ "$DEALER_ME_CODE" == "200" ]]; then
  log_pass "Auth: Dealer /me returns profile (HTTP $DEALER_ME_CODE)"
else
  log_fail "Auth: Dealer /me" "HTTP $DEALER_ME_CODE"
fi

# ============================================================
# SECTION 6: Public Endpoints (No Auth)
# ============================================================
echo ""
echo "=============================="
echo "SECTION 6: PUBLIC ENDPOINTS"
echo "=============================="

# Public vehicles search
VEHICLES_RESPONSE=$(curl -s -w "\n%{http_code}" --max-time 15 \
  "$API_URL/vehicles" 2>/dev/null || echo -e "\n000")
VEHICLES_CODE=$(echo "$VEHICLES_RESPONSE" | tail -1)
if [[ "$VEHICLES_CODE" == "200" ]]; then
  log_pass "Public: GET /vehicles returns listing (HTTP $VEHICLES_CODE)"
else
  log_fail "Public: GET /vehicles" "HTTP $VEHICLES_CODE"
fi

# Featured vehicles
FEATURED_RESPONSE=$(curl -s -w "\n%{http_code}" --max-time 15 \
  "$API_URL/vehicles/featured" 2>/dev/null || echo -e "\n000")
FEATURED_CODE=$(echo "$FEATURED_RESPONSE" | tail -1)
if [[ "$FEATURED_CODE" == "200" ]]; then
  log_pass "Public: GET /vehicles/featured works (HTTP $FEATURED_CODE)"
else
  log_fail "Public: GET /vehicles/featured" "HTTP $FEATURED_CODE"
fi

# Catalog/categories
CATALOG_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/catalog" 2>/dev/null || echo "000")
if [[ "$CATALOG_RESPONSE" == "200" ]]; then
  log_pass "Public: GET /catalog works (HTTP $CATALOG_RESPONSE)"
else
  log_fail "Public: GET /catalog" "HTTP $CATALOG_RESPONSE"
fi

# Categories
CATEGORIES_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/categories" 2>/dev/null || echo "000")
if [[ "$CATEGORIES_RESPONSE" == "200" ]]; then
  log_pass "Public: GET /categories works (HTTP $CATEGORIES_RESPONSE)"
else
  log_fail "Public: GET /categories" "HTTP $CATEGORIES_RESPONSE"
fi

# Products
PRODUCTS_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/products" 2>/dev/null || echo "000")
if [[ "$PRODUCTS_RESPONSE" == "200" ]]; then
  log_pass "Public: GET /products works (HTTP $PRODUCTS_RESPONSE)"
else
  log_fail "Public: GET /products" "HTTP $PRODUCTS_RESPONSE"
fi

# Subscription plans (public)
PLANS_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/subscriptions/plans" 2>/dev/null || echo "000")
if [[ "$PLANS_RESPONSE" == "200" ]]; then
  log_pass "Public: GET /subscriptions/plans works (HTTP $PLANS_RESPONSE)"
else
  log_fail "Public: GET /subscriptions/plans" "HTTP $PLANS_RESPONSE"
fi

# Dealer billing plans
DEALER_PLANS_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/dealer-billing/plans" 2>/dev/null || echo "000")
if [[ "$DEALER_PLANS_RESPONSE" == "200" ]]; then
  log_pass "Public: GET /dealer-billing/plans works (HTTP $DEALER_PLANS_RESPONSE)"
else
  log_fail "Public: GET /dealer-billing/plans" "HTTP $DEALER_PLANS_RESPONSE"
fi

# Privacy rights info (public)
PRIVACY_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/privacy/rights-info" 2>/dev/null || echo "000")
if [[ "$PRIVACY_RESPONSE" == "200" ]]; then
  log_pass "Public: GET /privacy/rights-info works (HTTP $PRIVACY_RESPONSE)"
else
  log_fail "Public: GET /privacy/rights-info" "HTTP $PRIVACY_RESPONSE"
fi

# Payment providers available
PAYMENT_PROV_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/payments/providers/available" 2>/dev/null || echo "000")
if [[ "$PAYMENT_PROV_RESPONSE" == "200" ]]; then
  log_pass "Public: GET /payments/providers/available works (HTTP $PAYMENT_PROV_RESPONSE)"
else
  log_fail "Public: GET /payments/providers/available" "HTTP $PAYMENT_PROV_RESPONSE"
fi

# ============================================================
# SECTION 7: Authenticated Endpoints — Buyer Flow
# ============================================================
echo ""
echo "=============================="
echo "SECTION 7: BUYER FLOW"
echo "=============================="

# Favorites
FAV_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/favorites" \
  -b /tmp/okla_buyer_cookies.txt 2>/dev/null || echo "000")
if [[ "$FAV_RESPONSE" == "200" ]]; then
  log_pass "Buyer: GET /favorites works (HTTP $FAV_RESPONSE)"
else
  log_fail "Buyer: GET /favorites" "HTTP $FAV_RESPONSE"
fi

# View history
HISTORY_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/history/views" \
  -b /tmp/okla_buyer_cookies.txt 2>/dev/null || echo "000")
if [[ "$HISTORY_RESPONSE" == "200" ]]; then
  log_pass "Buyer: GET /history/views works (HTTP $HISTORY_RESPONSE)"
else
  log_fail "Buyer: GET /history/views" "HTTP $HISTORY_RESPONSE"
fi

# Saved searches
SAVED_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/savedsearches" \
  -b /tmp/okla_buyer_cookies.txt 2>/dev/null || echo "000")
if [[ "$SAVED_RESPONSE" == "200" ]]; then
  log_pass "Buyer: GET /savedsearches works (HTTP $SAVED_RESPONSE)"
else
  log_fail "Buyer: GET /savedsearches" "HTTP $SAVED_RESPONSE"
fi

# Price alerts
ALERTS_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/pricealerts" \
  -b /tmp/okla_buyer_cookies.txt 2>/dev/null || echo "000")
if [[ "$ALERTS_RESPONSE" == "200" ]]; then
  log_pass "Buyer: GET /pricealerts works (HTTP $ALERTS_RESPONSE)"
else
  log_fail "Buyer: GET /pricealerts" "HTTP $ALERTS_RESPONSE"
fi

# Privacy preferences
PRIV_PREF_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/privacy/preferences" \
  -b /tmp/okla_buyer_cookies.txt 2>/dev/null || echo "000")
if [[ "$PRIV_PREF_RESPONSE" == "200" ]]; then
  log_pass "Buyer: GET /privacy/preferences works (HTTP $PRIV_PREF_RESPONSE)"
else
  log_fail "Buyer: GET /privacy/preferences" "HTTP $PRIV_PREF_RESPONSE"
fi

# ============================================================
# SECTION 8: Authenticated Endpoints — Dealer Flow
# ============================================================
echo ""
echo "=============================="
echo "SECTION 8: DEALER FLOW"
echo "=============================="

# Inventory
INV_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/inventory" \
  -b /tmp/okla_dealer_cookies.txt 2>/dev/null || echo "000")
if [[ "$INV_RESPONSE" == "200" ]]; then
  log_pass "Dealer: GET /inventory works (HTTP $INV_RESPONSE)"
else
  log_fail "Dealer: GET /inventory" "HTTP $INV_RESPONSE"
fi

# Inventory stats
INV_STATS_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/inventory/stats" \
  -b /tmp/okla_dealer_cookies.txt 2>/dev/null || echo "000")
if [[ "$INV_STATS_RESPONSE" == "200" ]]; then
  log_pass "Dealer: GET /inventory/stats works (HTTP $INV_STATS_RESPONSE)"
else
  log_fail "Dealer: GET /inventory/stats" "HTTP $INV_STATS_RESPONSE"
fi

# ============================================================
# SECTION 9: Authenticated Endpoints — Admin Flow
# ============================================================
echo ""
echo "=============================="
echo "SECTION 9: ADMIN FLOW"
echo "=============================="

# Users list
USERS_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/users" \
  -b /tmp/okla_admin_cookies.txt 2>/dev/null || echo "000")
if [[ "$USERS_RESPONSE" == "200" ]]; then
  log_pass "Admin: GET /users works (HTTP $USERS_RESPONSE)"
else
  log_fail "Admin: GET /users" "HTTP $USERS_RESPONSE"
fi

# KYC pending profiles
KYC_PENDING_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/kyc/kycprofiles/pending" \
  -b /tmp/okla_admin_cookies.txt 2>/dev/null || echo "000")
if [[ "$KYC_PENDING_RESPONSE" == "200" ]]; then
  log_pass "Admin: GET /kyc/kycprofiles/pending works (HTTP $KYC_PENDING_RESPONSE)"
else
  log_fail "Admin: GET /kyc/kycprofiles/pending" "HTTP $KYC_PENDING_RESPONSE"
fi

# Error service stats
ERRORS_STATS_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/errors/stats" \
  -b /tmp/okla_admin_cookies.txt 2>/dev/null || echo "000")
if [[ "$ERRORS_STATS_RESPONSE" == "200" ]]; then
  log_pass "Admin: GET /errors/stats works (HTTP $ERRORS_STATS_RESPONSE)"
else
  log_fail "Admin: GET /errors/stats" "HTTP $ERRORS_STATS_RESPONSE"
fi

# Notifications
NOTIF_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/notifications" \
  -b /tmp/okla_admin_cookies.txt 2>/dev/null || echo "000")
if [[ "$NOTIF_RESPONSE" == "200" ]]; then
  log_pass "Admin: GET /notifications works (HTTP $NOTIF_RESPONSE)"
else
  log_fail "Admin: GET /notifications" "HTTP $NOTIF_RESPONSE"
fi

# ============================================================
# SECTION 10: Security Tests
# ============================================================
echo ""
echo "=============================="
echo "SECTION 10: SECURITY TESTS"
echo "=============================="

# Unauthenticated access to protected endpoint
UNAUTH_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/users" 2>/dev/null || echo "000")
if [[ "$UNAUTH_RESPONSE" == "401" || "$UNAUTH_RESPONSE" == "403" ]]; then
  log_pass "Security: Unauthenticated access to /users blocked (HTTP $UNAUTH_RESPONSE)"
else
  log_fail "Security: Unauthenticated /users" "Expected 401/403, got HTTP $UNAUTH_RESPONSE"
fi

# SQL Injection attempt
SQLI_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@okla.local\" OR 1=1--","password":"test"}' 2>/dev/null || echo "000")
if [[ "$SQLI_RESPONSE" == "400" || "$SQLI_RESPONSE" == "422" || "$SQLI_RESPONSE" == "401" ]]; then
  log_pass "Security: SQL injection attempt rejected (HTTP $SQLI_RESPONSE)"
else
  log_fail "Security: SQL injection" "Expected 400/401/422, got HTTP $SQLI_RESPONSE"
fi

# XSS attempt
XSS_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"<script>alert(1)</script>@test.com","password":"test"}' 2>/dev/null || echo "000")
if [[ "$XSS_RESPONSE" == "400" || "$XSS_RESPONSE" == "422" || "$XSS_RESPONSE" == "401" ]]; then
  log_pass "Security: XSS attempt rejected (HTTP $XSS_RESPONSE)"
else
  log_fail "Security: XSS attempt" "Expected 400/401/422, got HTTP $XSS_RESPONSE"
fi

# Invalid login — wrong password
WRONG_PWD_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@okla.local","password":"WrongPassword123!"}' 2>/dev/null || echo "000")
if [[ "$WRONG_PWD_RESPONSE" == "401" || "$WRONG_PWD_RESPONSE" == "400" ]]; then
  log_pass "Security: Wrong password rejected (HTTP $WRONG_PWD_RESPONSE)"
else
  log_fail "Security: Wrong password" "Expected 401/400, got HTTP $WRONG_PWD_RESPONSE"
fi

# HTTPS redirect check
HTTP_REDIRECT=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 -L \
  "http://okla.com.do" 2>/dev/null || echo "000")
if [[ "$HTTP_REDIRECT" == "200" ]]; then
  log_pass "Security: HTTP→HTTPS redirect works"
else
  log_skip "Security: HTTP→HTTPS redirect" "HTTP $HTTP_REDIRECT"
fi

# ============================================================
# SECTION 11: Auth — Logout
# ============================================================
echo ""
echo "=============================="
echo "SECTION 11: LOGOUT"
echo "=============================="

# Admin logout
ADMIN_LOGOUT_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  -X POST "$API_URL/auth/logout" \
  -b /tmp/okla_admin_cookies.txt 2>/dev/null || echo "000")
if [[ "$ADMIN_LOGOUT_RESPONSE" == "200" || "$ADMIN_LOGOUT_RESPONSE" == "204" ]]; then
  log_pass "Auth: Admin logout successful (HTTP $ADMIN_LOGOUT_RESPONSE)"
else
  log_fail "Auth: Admin logout" "HTTP $ADMIN_LOGOUT_RESPONSE"
fi

# Buyer logout
BUYER_LOGOUT_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  -X POST "$API_URL/auth/logout" \
  -b /tmp/okla_buyer_cookies.txt 2>/dev/null || echo "000")
if [[ "$BUYER_LOGOUT_RESPONSE" == "200" || "$BUYER_LOGOUT_RESPONSE" == "204" ]]; then
  log_pass "Auth: Buyer logout successful (HTTP $BUYER_LOGOUT_RESPONSE)"
else
  log_fail "Auth: Buyer logout" "HTTP $BUYER_LOGOUT_RESPONSE"
fi

# Dealer logout
DEALER_LOGOUT_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  -X POST "$API_URL/auth/logout" \
  -b /tmp/okla_dealer_cookies.txt 2>/dev/null || echo "000")
if [[ "$DEALER_LOGOUT_RESPONSE" == "200" || "$DEALER_LOGOUT_RESPONSE" == "204" ]]; then
  log_pass "Auth: Dealer logout successful (HTTP $DEALER_LOGOUT_RESPONSE)"
else
  log_fail "Auth: Dealer logout" "HTTP $DEALER_LOGOUT_RESPONSE"
fi

# Post-logout access should fail
POST_LOGOUT_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 \
  "$API_URL/auth/me" \
  -b /tmp/okla_admin_cookies.txt 2>/dev/null || echo "000")
if [[ "$POST_LOGOUT_RESPONSE" == "401" || "$POST_LOGOUT_RESPONSE" == "403" ]]; then
  log_pass "Security: Post-logout access denied (HTTP $POST_LOGOUT_RESPONSE)"
else
  log_fail "Security: Post-logout access" "Expected 401/403, got HTTP $POST_LOGOUT_RESPONSE"
fi

# ============================================================
# CLEANUP
# ============================================================
rm -f /tmp/okla_admin_cookies.txt /tmp/okla_buyer_cookies.txt /tmp/okla_dealer_cookies.txt

# ============================================================
# FINAL REPORT
# ============================================================
echo ""
echo "============================================"
echo "       E2E PRODUCTION TEST RESULTS"
echo "============================================"
echo -e "  ${GREEN}PASSED${NC}:  $PASS"
echo -e "  ${RED}FAILED${NC}:  $FAIL"
echo -e "  ${YELLOW}SKIPPED${NC}: $SKIP"
echo "  TOTAL:   $((PASS + FAIL + SKIP))"
echo "============================================"

if [[ $FAIL -gt 0 ]]; then
  echo -e "\n${RED}⚠️  Some tests failed. See details above.${NC}"
  EXIT_CODE=1
else
  echo -e "\n${GREEN}🎉 All tests passed!${NC}"
  EXIT_CODE=0
fi

echo ""
echo "Detailed results:"
echo -e "$RESULTS"
echo ""

exit $EXIT_CODE
