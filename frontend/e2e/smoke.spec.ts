import { test, expect } from '@playwright/test'

// Fluxo crítico mínimo contra o bundle exportado: sem sessão, o Portao mostra o login.
test('sem sessão, mostra a tela de login', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByPlaceholder('Código institucional')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Entrar' })).toBeVisible()
})

// Rota profunda tem de ser servida pelo fallback SPA (`serve dist --single` /
// `try_files ... /index.html` no nginx), não dar 404.
test('rota profunda carrega direto pela URL', async ({ page }) => {
  const resposta = await page.goto('/inicio')

  expect(resposta?.status()).toBe(200)
  await expect(page.getByPlaceholder('Código institucional')).toBeVisible()
})
