import { defineConfig, devices } from '@playwright/test'

/**
 * E2E ataca o **`expo export` servido**, não o dev server do Metro: quem tem de
 * passar é o artefato que vai para o k8s.
 *
 * `serve dist --single` é obrigatório com `web.output: "single"` — sem o `--single`,
 * rota profunda dá 404 no servidor estático e o teste "passa" por motivo errado.
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:4173',
    trace: 'on-first-retry',
  },
  webServer: {
    command: 'bun run build:web && bun run serve:web',
    url: 'http://localhost:4173',
    timeout: 180_000,
    reuseExistingServer: !process.env.CI,
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
})
