import { defineConfig } from 'vitest/config'

export default defineConfig({
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
    // Las fechas de la app van en hora local: se fija la zona del club para que los tests no dependan de la máquina.
    env: { TZ: 'Europe/Madrid' },
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html'],
      include: ['src/cardStats.ts', 'src/dates.ts', 'src/auditView.ts', 'src/avatar.ts'],
    },
  },
})
