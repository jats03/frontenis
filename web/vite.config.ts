import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'Club Frontenis Ciudad de Jaén',
        short_name: 'Frontenis Jaén',
        description: 'Gestión de ligas y torneos de frontenis',
        theme_color: '#0f766e',
        background_color: '#f4f7f6',
        display: 'standalone',
        start_url: '/',
        icons: [
          { src: 'icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: 'icon-512.png', sizes: '512x512', type: 'image/png' },
        ],
      },
      workbox: {
        navigateFallback: '/index.html',
        navigateFallbackDenylist: [/^\/api/],
        // Recarga las pestañas abiertas cuando un service worker nuevo sustituye a otro (ver public/sw-reload.js).
        importScripts: ['sw-reload.js'],
      },
    }),
  ],
  server: { port: 5173 },
})
