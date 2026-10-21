import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import type { Product } from '../api/products';
import { COMPANY_ID, renderWithProviders, signIn, stubFetch } from '../test/render';
import ProductsPage from './ProductsPage';

const PRODUCTS: Product[] = [
  { id: 'p1', nameEn: 'Black coffee', nameAr: 'قهوة سوداء', sku: 'COF-1', priceHalalas: 1250, stock: 7 },
  { id: 'p2', nameEn: 'Dates box', nameAr: 'علبة تمر', sku: 'DAT-2', priceHalalas: 4505, stock: 0 },
];

describe('ProductsPage', () => {
  beforeEach(() => signIn('Viewer'));

  it('lists the products in Arabic with prices in riyals', async () => {
    stubFetch(PRODUCTS);
    renderWithProviders(<ProductsPage />);
    const row = (await screen.findByText('قهوة سوداء')).closest('tr')!;
    expect(within(row).getByText('12.50 ر.س')).toBeInTheDocument();
    expect(within(row).getByText('COF-1')).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'السعر' })).toBeInTheDocument();
  });

  it('lists the products in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(PRODUCTS);
    renderWithProviders(<ProductsPage />);
    expect(await screen.findByText('Dates box')).toBeInTheDocument();
    expect(screen.getByText('SAR 45.05')).toBeInTheDocument();
    expect(screen.queryByText('علبة تمر')).not.toBeInTheDocument();
  });

  it('asks the API as the signed-in company and role', async () => {
    const fetchMock = stubFetch(PRODUCTS);
    renderWithProviders(<ProductsPage />);
    await screen.findByText('قهوة سوداء');
    expect(fetchMock).toHaveBeenCalledWith('/api/inventory/products', {
      headers: { Accept: 'application/json', 'X-Company-Id': COMPANY_ID, 'X-Role': 'Viewer' },
    });
  });

  it('says so when there are no products', async () => {
    stubFetch([]);
    renderWithProviders(<ProductsPage />);
    expect(await screen.findByText('لا توجد منتجات بعد.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('shows an error and loads again on retry', async () => {
    const fetchMock = stubFetch({ title: 'boom' }, 500);
    renderWithProviders(<ProductsPage />);
    expect(await screen.findByRole('alert')).toHaveTextContent('تعذّر تحميل المنتجات.');
    fetchMock.mockImplementation(async () => new Response(JSON.stringify(PRODUCTS), { status: 200 }));
    await userEvent.click(screen.getByRole('button', { name: 'إعادة المحاولة' }));
    expect(await screen.findByText('قهوة سوداء')).toBeInTheDocument();
  });

  it.each(['ar', 'en'])('has no accessibility violations (%s)', async (lang) => {
    await i18n.changeLanguage(lang);
    stubFetch(PRODUCTS);
    const { container } = renderWithProviders(<ProductsPage />);
    await screen.findByRole('table');
    expect(await axe(container)).toHaveNoViolations();
  });
});
