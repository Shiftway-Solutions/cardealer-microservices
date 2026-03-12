# ==============================================================================
# Makefile — Python Backend Linting, Formatting & Security
# ==============================================================================
# Usage:
#   make lint          — Check all (flake8 + black --check + isort --check)
#   make security      — Run Bandit security scan (SAST)
#   make security-sarif — Generate SARIF report for GitHub Security tab
#   make format        — Auto-format all (black + isort)
#   make lint-install  — Install linting dependencies
# ==============================================================================

PYTHON_DIRS := AIProcessingService/workers ChatbotService/LlmServer

.PHONY: lint format lint-install security security-sarif

lint-install:
	pip install -r requirements-lint.txt

lint:
	@echo "🔍 Running flake8 (PEP8)..."
	flake8 $(PYTHON_DIRS)
	@echo "🎨 Checking black formatting..."
	black --check --diff $(PYTHON_DIRS)
	@echo "📦 Checking isort import order..."
	isort --check-only --diff $(PYTHON_DIRS)
	@echo "✅ All Python lint checks passed!"

security:
	@echo "🔒 Running Bandit security scan..."
	bandit -r $(PYTHON_DIRS) -c pyproject.toml -ll -ii
	@echo "✅ Bandit security scan passed!"

security-sarif:
	@echo "🔒 Running Bandit with SARIF output..."
	bandit -r $(PYTHON_DIRS) -c pyproject.toml -f sarif -o bandit-results.sarif -ll -ii || true
	@echo "📄 SARIF report: bandit-results.sarif"

format:
	@echo "🎨 Formatting with black..."
	black $(PYTHON_DIRS)
	@echo "📦 Sorting imports with isort..."
	isort $(PYTHON_DIRS)
	@echo "✅ All Python files formatted!"
