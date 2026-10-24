import { useTranslation } from 'react-i18next';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { clearSession, loadSession } from '../session';

const navLink = ({ isActive }: { isActive: boolean }) =>
  `inline-flex min-h-11 items-center rounded-md px-3 ${isActive ? 'bg-teal-700 text-white' : 'hover:bg-slate-100'}`;

export default function Layout() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const signedIn = loadSession() !== null;
  const other = i18n.language === 'ar' ? 'en' : 'ar';

  const signOut = () => {
    clearSession();
    navigate('/sign-in');
  };

  return (
    <div className="min-h-svh bg-white text-slate-900">
      <header className="flex flex-wrap items-center gap-3 border-b border-slate-200 px-4 py-2">
        <span className="text-lg font-bold">{t('app.name')}</span>
        {signedIn && (
          <nav aria-label={t('app.mainNav')} className="flex gap-2">
            <NavLink to="/products" className={navLink}>
              {t('nav.products')}
            </NavLink>
            <NavLink to="/sales" className={navLink}>
              {t('nav.sales')}
            </NavLink>
          </nav>
        )}
        <div className="ms-auto flex gap-2">
          <button
            type="button"
            lang={other}
            onClick={() => void i18n.changeLanguage(other)}
            className="min-h-11 rounded-md border border-slate-300 px-3 hover:bg-slate-100"
          >
            {t('language.switchTo')}
          </button>
          {signedIn && (
            <button
              type="button"
              onClick={signOut}
              className="min-h-11 rounded-md border border-slate-300 px-3 hover:bg-slate-100"
            >
              {t('nav.signOut')}
            </button>
          )}
        </div>
      </header>
      <main className="p-4">
        <Outlet />
      </main>
    </div>
  );
}
