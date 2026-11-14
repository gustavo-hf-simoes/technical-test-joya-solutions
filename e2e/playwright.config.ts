import { defineConfig } from '@playwright/test';

// Usa o Chrome instalado no sistema (nenhum navegador do Playwright é baixado).
export default defineConfig({
  testDir: '.',
  testMatch: '*.spec.ts',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 30_000,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: process.env['E2E_BASE_URL'] ?? 'http://localhost:4200',
    channel: 'chrome',
    locale: 'pt-BR',
    trace: 'retain-on-failure',
  },
});
