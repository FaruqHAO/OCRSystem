import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// In docker-compose the proxy targets the api service; locally it targets localhost.
const proxyTarget = process.env.VITE_PROXY_TARGET || 'http://localhost:8080';

export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: 5173,
    proxy: {
      '/api': {
        target: proxyTarget,
        changeOrigin: true,
      },
    },
  },
});