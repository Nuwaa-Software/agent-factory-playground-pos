import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from './App';
import { renderWithProviders, signIn, stubFetch } from './test/render';

describe('App', () => {
  it('sends a visitor who is not signed in to sign-in', () => {
    renderWithProviders(<App />, { route: '/products' });
    expect(screen.getByRole('heading', { name: 'تسجيل الدخول (بيئة التطوير)' })).toBeInTheDocument();
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument();
  });

  it('redirects unauthenticated users from /products/:id/edit to sign-in', () => {
    renderWithProviders(<App />, { route: '/products/p1/edit' });
    expect(screen.getByRole('heading', { name: 'تسجيل الدخول (بيئة التطوير)' })).toBeInTheDocument();
  });

  it('starts in Arabic, right-to-left, and switches to English', async () => {
    renderWithProviders(<App />, { route: '/sign-in' });
    expect(document.documentElement).toHaveAttribute('dir', 'rtl');
    expect(document.documentElement).toHaveAttribute('lang', 'ar');

    await userEvent.click(screen.getByRole('button', { name: 'English' }));
    expect(document.documentElement).toHaveAttribute('dir', 'ltr');
    expect(screen.getByRole('heading', { name: 'Sign in (development)' })).toBeInTheDocument();
    expect(localStorage.getItem('minipos.lang')).toBe('en');
  });

  it('opens the products for a signed-in user, and signs out', async () => {
    signIn();
    stubFetch([]);
    renderWithProviders(<App />, { route: '/' });
    expect(await screen.findByRole('heading', { name: 'المنتجات' })).toBeInTheDocument();
    expect(screen.getByRole('navigation', { name: 'التنقل الرئيسي' })).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'تسجيل الخروج' }));
    expect(screen.getByRole('heading', { name: 'تسجيل الدخول (بيئة التطوير)' })).toBeInTheDocument();
  });
});
