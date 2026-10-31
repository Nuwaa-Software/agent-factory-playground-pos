import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from '../App';
import { axe } from '../test/axe';
import { renderWithProviders, signIn, stubFetch } from '../test/render';

describe('Layout', () => {
  describe('signed out', () => {
    it('shows the app name and language switch; no nav or sign-out button', () => {
      renderWithProviders(<App />, { route: '/sign-in' });
      expect(screen.getByText('ميني بوس')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'English' })).toBeInTheDocument();
      expect(screen.queryByRole('navigation')).not.toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'تسجيل الخروج' })).not.toBeInTheDocument();
    });
  });

  describe('signed in', () => {
    beforeEach(() => {
      signIn();
      stubFetch([]);
    });

    it('renders the nav with 5 links and the sign-out button', () => {
      renderWithProviders(<App />, { route: '/' });
      const nav = screen.getByRole('navigation', { name: 'التنقل الرئيسي' });
      expect(nav).toBeInTheDocument();
      expect(screen.getByRole('link', { name: 'المنتجات' })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: 'المبيعات' })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: 'نقطة البيع' })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: 'الوردية' })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: 'سجل الورديات' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'تسجيل الخروج' })).toBeInTheDocument();
    });

    it('header has sticky class and main has max-w-6xl mx-auto px-4', () => {
      renderWithProviders(<App />, { route: '/' });
      const header = screen.getByRole('banner');
      expect(header).toHaveClass('sticky');
      const main = screen.getByRole('main');
      expect(main.className).toContain('max-w-6xl');
      expect(main.className).toContain('mx-auto');
      expect(main.className).toContain('px-4');
    });

    it('nav row has flex-nowrap and overflow-x-auto', () => {
      renderWithProviders(<App />, { route: '/' });
      const nav = screen.getByRole('navigation', { name: 'التنقل الرئيسي' });
      expect(nav.className).toContain('flex-nowrap');
      expect(nav.className).toContain('overflow-x-auto');
    });

    it('language switch flips dir/lang and button name', async () => {
      renderWithProviders(<App />, { route: '/sign-in' });
      expect(document.documentElement).toHaveAttribute('dir', 'rtl');
      expect(document.documentElement).toHaveAttribute('lang', 'ar');

      await userEvent.click(screen.getByRole('button', { name: 'English' }));
      expect(document.documentElement).toHaveAttribute('dir', 'ltr');
      expect(document.documentElement).toHaveAttribute('lang', 'en');
      expect(screen.getByRole('button', { name: 'العربية' })).toBeInTheDocument();
    });

    it('sign-out lands on sign-in screen', async () => {
      renderWithProviders(<App />, { route: '/' });
      await userEvent.click(screen.getByRole('button', { name: 'تسجيل الخروج' }));
      expect(screen.getByRole('heading', { name: 'تسجيل الدخول (بيئة التطوير)' })).toBeInTheDocument();
    });

    it('passes axe in Arabic', async () => {
      renderWithProviders(<App />, { route: '/' });
      const main = screen.getByRole('main');
      expect(await axe(main)).toHaveNoViolations();
    });

    it('passes axe in English', async () => {
      signIn();
      stubFetch([]);
      // Start at sign-in to avoid fetch calls; the language button is always rendered
      renderWithProviders(<App />, { route: '/sign-in' });
      // Language starts as Arabic (set by beforeEach in setup.ts)
      expect(document.documentElement).toHaveAttribute('lang', 'ar');
      // Switch to English
      const langBtn = screen.getByRole('button', { name: 'English' });
      await userEvent.click(langBtn);
      expect(document.documentElement).toHaveAttribute('lang', 'en');
      // Check accessibility of the sign-in page in English
      const main = screen.getByRole('main');
      expect(await axe(main)).toHaveNoViolations();
    });
  });
});
