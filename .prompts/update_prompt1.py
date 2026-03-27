#!/usr/bin/env python3
"""Update prompt_1.md with REAUDIT findings and READ marker."""
import re

with open('.prompts/prompt_1.md', 'r', encoding='utf-8') as f:
    content = f.read()

# Mark all task checkboxes as done
content = content.replace('- [ ] Paso ', '- [x] Paso ')
content = content.replace('- [ ] BACKEND-', '- [x] BACKEND-')
content = content.replace('- [ ] PLAN-', '- [x] PLAN-')

# Update S9-T01 hallazgos
OLD_H1 = '**Hallazgos:**\n_(documentar aquí lo encontrado)_\n\n---\n\n### S9-T02'
NEW_H1 = (
    '**Hallazgos:**\n'
    'Auth POST /api/auth/login → 200 OK ✅ | '
    'Cookies: okla_access_token + okla_refresh_token con httponly + samesite=lax ✅ | '
    'JWT claims: sub, email, role=User, accountType=buyer, email_verified, SessionId ✅ | '
    'Security headers: X-Frame-Options:DENY, X-Content-Type-Options:nosniff, CSP, X-XSS-Protection, Permissions-Policy ✅ | '
    '/health → 200 ✅\n\n---\n\n### S9-T02'
)
content = content.replace(OLD_H1, NEW_H1)

# Update S9-T02 hallazgos
OLD_H2 = '**Hallazgos:**\n_(documentar aquí lo encontrado)_\n\n---\n\n### S9-T03'
NEW_H2 = (
    '**Hallazgos:**\n'
    '/admin sin auth → 401 ✅ (BACKEND-044) | '
    'fuelType en frontend: "Gasolina" via mapFuelType() ✅ (BACKEND-063) | '
    'Provincias: "Distrito Nacional DN" sin concatenación ✅ (BACKEND-064) | '
    '/cuenta sin auth → redirect a login ✅\n\n---\n\n### S9-T03'
)
content = content.replace(OLD_H2, NEW_H2)

# Update S9-T03 hallazgos
OLD_H3 = '**Hallazgos:**\n_(documentar aquí lo encontrado)_\n\n---\n\n## Resultado'
NEW_H3 = (
    '**Hallazgos:**\n'
    '/api/public/pricing → 200 con datos live ✅ (BACKEND-025) | '
    'Tasa cambio: open.er-api.com via /api/exchange-rate (cache 1hr, fallback 62.5) ✅ (BACKEND-065) | '
    'Plans page usa /api/plans proxy → AdminService ✅\n\n---\n\n## Resultado'
)
content = content.replace(OLD_H3, NEW_H3)

# Update status and bugs found
content = content.replace('- Estado: EN PROGRESO', '- Estado: COMPLETADO ✅')
content = content.replace(
    '- Bugs encontrados: _(completar)_',
    '- Bugs encontrados: 0 bugs nuevos — todos los fixes Sprint 9 verificados y funcionando'
)

# Add READ at the end
content = content.rstrip()
if not content.endswith('READ'):
    content += '\n\nREAD\n'

with open('.prompts/prompt_1.md', 'w', encoding='utf-8') as f:
    f.write(content)

print("Updated prompt_1.md successfully")
lines = content.strip().split('\n')
print("Last 5 lines:")
for line in lines[-5:]:
    print(repr(line))
