import { setupServer } from 'msw/node';

/**
 * Общий msw-сервер: обработчики добавляются в самих тестах через server.use(...),
 * а resetHandlers в afterEach очищает их между тестами.
 */
export const server = setupServer();
