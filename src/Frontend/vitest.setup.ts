import '@testing-library/jest-dom/vitest';
// Словари подключаются глобально: компоненты используют useTranslation в тестах.
import '@/i18n';

// jsdom не умеет прокручивать: каталог вызывает scrollTo при смене страницы.
window.scrollTo = () => undefined;
