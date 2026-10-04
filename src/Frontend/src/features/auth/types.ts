/** Ответ token-endpoint OpenIddict (OAuth 2.0, snake_case по спецификации). */
export interface TokenResponse {
  access_token: string;
  refresh_token?: string;
  token_type: string;
  expires_in: number;
}

/** Токены сессии администратора. */
export interface AuthTokens {
  accessToken: string;
  refreshToken: string | null;
  /** Момент истечения access-токена (мс, Unix epoch); null — если сервер не сообщил expires_in. */
  expiresAt: number | null;
}

/** Сессия админ-панели: токены + claims, прочитанные из access-токена. */
export interface AuthSession {
  tokens: AuthTokens;
  displayName: string;
  email: string | null;
  roles: string[];
}
