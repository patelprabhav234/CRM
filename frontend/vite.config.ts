import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  // Default matches ASP.NET "https" profile (7096). HTTP 5254 is refused if you only launch HTTPS.
  const apiTarget = env.VITE_API_PROXY_TARGET || 'https://127.0.0.1:7096'

  return {
    plugins: [react()],
    server: {
      proxy: {
        '/api': {
          target: apiTarget,
          changeOrigin: true,
          secure: false, // dev HTTPS + self-signed cert from dotnet dev-certs
        },
      },
    },
  }
})
