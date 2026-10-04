/**
 * Собирает JWT-подобный токен: фронтенду важен только payload —
 * claims читаются без проверки подписи (см. features/auth/jwt.ts).
 */
export function createAccessToken(claims: Record<string, unknown>): string {
  const encode = (value: unknown) =>
    String.fromCharCode(...new TextEncoder().encode(JSON.stringify(value)));

  return `${btoa(encode({ alg: 'RS256', typ: 'JWT' }))}.${btoa(encode(claims))}.signature`;
}
