import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    environmentOptions: {
      jsdom: {
        // jsdom 27+ requires explicit storageQuota to enable localStorage.
        // Without this, localStorage.getItem throws TypeError at module init
        // (api-client.ts migrateTokensFromLocalStorage) crashing integration tests.
        storageQuota: 10_000_000,
      },
    },
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.{test,spec}.{js,mjs,cjs,ts,mts,cts,jsx,tsx}'],
    exclude: ['node_modules', 'e2e'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: ['node_modules/', 'src/test/', '**/*.d.ts', '**/*.config.*', '**/types/**'],
      thresholds: {
        global: {
          branches: 80,
          functions: 80,
          lines: 80,
          statements: 80,
        },
      },
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
});
