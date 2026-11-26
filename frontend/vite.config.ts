/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Під час запуску через .NET Aspire (AddViteApp) бекенд приходить як
// BACKEND_HTTPS / BACKEND_HTTP. Поза Aspire — резерв на LocalDB-бекенд :5080.
const backendTarget =
  process.env.BACKEND_HTTPS || process.env.BACKEND_HTTP || 'http://localhost:5080'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  // Модульні тести (npm test): хуки й HTTP-клієнт у jsdom
  test: { environment: 'jsdom', include: ['src/**/*.test.{ts,tsx}'] },
  server: {
    port: process.env.PORT ? Number(process.env.PORT) : 5180,
    proxy: {
      // Усі запити /api прокидаються на бекенд (один origin -> без CORS)
      '/api': { target: backendTarget, changeOrigin: true, secure: false },
    },
  },
})
