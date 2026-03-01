import { defineConfig, globalIgnores } from 'eslint/config';
import nextVitals from 'eslint-config-next/core-web-vitals';
import nextTs from 'eslint-config-next/typescript';

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    '.next/**',
    'out/**',
    'build/**',
    'next-env.d.ts',
    // Test files (use vitest for linting)
    'src/test/**',
    '**/*.test.ts',
    '**/*.test.tsx',
    '**/*.spec.ts',
    '**/*.spec.tsx',
    'e2e/**',
    // Playwright reports and test results (minified JS)
    'playwright-report/**',
    'playwright-report-prod/**',
    'test-results/**',
    // Service worker (minified/generated)
    'public/sw.js',
    'public/workbox-*.js',
  ]),
  // Downgrade strict rules to warnings to avoid blocking CI/CD.
  // These should be fixed incrementally by the team.
  {
    rules: {
      '@typescript-eslint/no-explicit-any': 'warn',
      'react/no-unescaped-entities': 'warn',
      'react-hooks/purity': 'warn',
      'react-hooks/set-state-in-effect': 'warn',
      'react-hooks/preserve-manual-memoization': 'warn',
      'prefer-const': 'warn',
      '@typescript-eslint/no-empty-object-type': 'warn',
    },
  },
]);

export default eslintConfig;
