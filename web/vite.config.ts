import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'Frontenis Ligas y Torneos',
        short_name: 'Frontenis',
        description: 'Gestión de ligas y torneos de frontenis',
        theme_color: '#0f766e',
        background_color: '#f4f7f6',
        display: 'standalone',
        start_url: '/',
        icons: [{ src: 'favicon.svg', sizes: 'any', type: 'image/svg+xml', purpose: 'any' }],
      },
      workbox: { navigateFallback: '/index.html', navigateFallbackDenylist: [/^\/api/] },
    }),
  ],
  server: { port: 5173 },
})
