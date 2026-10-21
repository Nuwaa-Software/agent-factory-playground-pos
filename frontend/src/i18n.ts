import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import ar from './locales/ar.json';
import en from './locales/en.json';

export type Language = 'ar' | 'en';
const STORAGE_KEY = 'minipos.lang';

function storedLanguage(): Language {
  try {
    const value = localStorage.getItem(STORAGE_KEY);
    if (value === 'ar' || value === 'en') return value;
  } catch {
    // storage blocked (private mode): fall back to Arabic
  }
  return 'ar';
}

// <html lang dir> follow the language, so the whole page (and every logical `ms-`/`text-start`
// class) flips to right-to-left for Arabic.
i18n.on('languageChanged', (lang) => {
  document.documentElement.lang = lang;
  document.documentElement.dir = i18n.dir(lang);
  try {
    localStorage.setItem(STORAGE_KEY, lang);
  } catch {
    // not remembered; the next visit starts in Arabic
  }
});

void i18n.use(initReactI18next).init({
  resources: { ar: { translation: ar }, en: { translation: en } },
  lng: storedLanguage(),
  fallbackLng: 'ar',
  initAsync: false,
  interpolation: { escapeValue: false }, // React escapes already
});

export default i18n;
