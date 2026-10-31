import { useTranslation } from 'react-i18next';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { clearSession, loadSession } from '../session';
import Button from '../ui/Button';

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
      <header className="sticky top-0 z-10 border-b border-slate-200 bg-white">
        <div className="flex items-center gap-3 px-4 py-2">
          <span className="shrink-0 text-lg font-bold">{t('app.name')}</span>
          {signedIn && (
            <nav aria-label={t('app.mainNav')} className="flex min-w-0 flex-nowrap items-center gap-2 overflow-x-auto">
              <NavLink to="/products" className={navLink}>
                {t('nav.products')}
              </NavLink>
              <NavLink to="/sales" className={navLink}>
                {t('nav.sales')}
              </NavLink>
              <NavLink to="/pos" className={navLink}>
                {t('nav.pos')}
              </NavLink>
              <NavLink to="/shifts" className={navLink}>
                {t('nav.shifts')}
              </NavLink>
              <NavLink to="/shifts/history" className={navLink}>
                {t('nav.shiftHistory')}
              </NavLink>
            </nav>
          )}
          <div className="ms-auto flex shrink-0 gap-2">
            <Button variant="secondary" type="button" lang={other} onClick={() => void i18n.changeLanguage(other)}>
              {t('language.switchTo')}
            </Button>
            {signedIn && (
              <Button variant="secondary" type="button" onClick={signOut}>
                {t('nav.signOut')}
              </Button>
            )}
          </div>
        </div>
      </header>
      <main className="max-w-6xl mx-auto px-4 py-4">
        <Outlet />
      </main>
    </div>
  );
}
