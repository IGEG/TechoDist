import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import ru from './locales/ru.json';

/**
 * Локализация: русский — единственный язык витрины (магазин РФ).
 * Словари подключаются статически: для второго языка сюда добавится ленивая загрузка.
 */
void i18n.use(initReactI18next).init({
  resources: {
    ru: { translation: ru },
  },
  lng: 'ru',
  fallbackLng: 'ru',
  interpolation: {
    escapeValue: false,
  },
});

export default i18n;
