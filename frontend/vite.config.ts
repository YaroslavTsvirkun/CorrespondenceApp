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
  build: {
    rolldownOptions: {
      output: {
        // React — окремим файлом: змінюється рідко, тож після оновлення застосунку браузер бере його з кешу.
        // antd свідомо не виділяємо: компоненти, потрібні лише окремим сервісам, мають вантажитися разом
        // із сервісом (lazy-імпорт у services/registry.tsx), а не всі одразу.
        codeSplitting: {
          groups: [
            { name: 'react', test: /node_modules[\\/](react|react-dom|react-router|react-router-dom|scheduler)[\\/]/ },
          ],
        },
      },
    },
  },
  server: {
    port: process.env.PORT ? Number(process.env.PORT) : 5180,
    proxy: {
      // Усі запити /api прокидаються на бекенд (один origin -> без CORS)
      '/api': { target: backendTarget, changeOrigin: true, secure: false },
    },
  },
})
