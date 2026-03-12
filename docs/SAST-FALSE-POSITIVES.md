# SAST False Positive Management — OKLA

## Overview

This document describes how to handle false positives from our SAST (Static Application Security Testing) tools without bypassing security verification.

## Tools

| Tool         | Language         | Config                                   | SARIF Upload                              |
| ------------ | ---------------- | ---------------------------------------- | ----------------------------------------- |
| **Bandit**   | Python           | `backend/pyproject.toml` `[tool.bandit]` | `bandit-results.sarif` → GitHub Security  |
| **Semgrep**  | JS/TS (Frontend) | `p/security-audit` ruleset               | `semgrep-results.sarif` → GitHub Security |
| **Trivy**    | Docker images    | Severity `CRITICAL,HIGH`                 | `trivy-results.sarif` → GitHub Security   |
| **Gitleaks** | All (secrets)    | Default config                           | GitHub Actions annotations                |

## Marking False Positives

### Bandit (Python)

Add `# nosec B<NNN>` with a justification comment:

```python
# nosec B101 — assert used only in test helpers, not production control flow
assert response.status_code == 200
```

### Semgrep (JavaScript/TypeScript)

Add `// nosemgrep: <rule-id>` with a justification comment:

```typescript
// nosemgrep: javascript.lang.security.audit.detect-non-literal-regexp
// Justification: pattern is from a trusted config file, not user input
const regex = new RegExp(configPattern);
```

### Trivy (Docker)

If a CVE is a false positive for your image, add to `.trivyignore`:

```
# CVE-YYYY-NNNNN — False positive: package not used in runtime
CVE-YYYY-NNNNN
```

## Rules

1. **Every suppression MUST have a justification comment** explaining why it's a false positive.
2. **Suppressions are reviewed in PR** — reviewers must verify the justification.
3. **HIGH/CRITICAL findings block PR merge** — suppressions must be approved before merge.
4. **Quarterly audit** — all suppressions are reviewed quarterly for validity.
5. **Never use blanket disables** (`# nosec` without a specific code, `// nosemgrep` without a rule ID).

## CI/CD Integration

- **PR Checks**: `security-scan.yml` runs on every PR to `main`
- **Weekly**: Scheduled scan runs Mondays at 6 AM UTC
- **Blocking**: HIGH/CRITICAL findings in Bandit or Semgrep fail the job and block merge
- **GitHub Security Tab**: All SARIF reports are uploaded and visible in the repository Security tab

## Viewing Results

1. Go to **GitHub → Security → Code scanning alerts**
2. Filter by tool (Bandit, Semgrep, Trivy)
3. Review findings — dismiss false positives with "Won't fix" + reason
