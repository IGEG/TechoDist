/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Базовый адрес API Gateway, например http://localhost:5100 */
  readonly VITE_API_BASE_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
