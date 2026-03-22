/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Call API directly (e.g. https://127.0.0.1:7096) when not using Vite /api proxy. */
  readonly VITE_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
