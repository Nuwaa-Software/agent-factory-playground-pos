import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    // The API has no CORS: in development the browser talks to Vite, which forwards /api to it
    // (`dotnet run --project backend/src/MiniPos.Api` listens on 5080, see launchSettings.json).
    proxy: { '/api': { target: 'http://localhost:5080', changeOrigin: true } },
  },
});
