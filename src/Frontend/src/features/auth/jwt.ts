/** Claims access-токена, которые нужны фронтенду (см. AuthorizationController.GetDestinations). */
export interface AccessTokenClaims {
  sub?: string;
  name?: string;
  email?: string;
  role?: string | string[];
  exp?: number;
}

/**
 * Разбор payload access-токена. Токен подписан, но не зашифрован, поэтому claims читаются
 * на клиенте без обращения к серверу: этим заполняется имя и роли текущего администратора.
 * Доверять этим данным в вопросах безопасности нельзя — доступ всё равно проверяет API.
 */
export function decodeAccessToken(token: string): AccessTokenClaims | null {
  const segments = token.split('.');
  const payload = segments[1];

  if (!payload) {
    return null;
  }

  try {
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    const bytes = Uint8Array.from(atob(padded), (char) => char.charCodeAt(0));

    return JSON.parse(new TextDecoder().decode(bytes)) as AccessTokenClaims;
  } catch {
    return null;
  }
}

/** Роль в токене может быть одной строкой или массивом (несколько ролей у администратора). */
export function normalizeRoles(role: string | string[] | undefined): string[] {
  if (!role) {
    return [];
  }

  return Array.isArray(role) ? role : [role];
}
