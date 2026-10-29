import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import type { Product } from '../api/products';
import { COMPANY_ID, renderWithProviders, signIn, stubFetch } from '../test/render';
import LowStockPage from './LowStockPage';

const PRODUCTS: Product[] = [
  { id: 'p1', nameEn: 'Black coffee', nameAr: 'قهوة سوداء', sku: 'COF-1', priceHalalas: 1250, stock: 7 },
  { id: 'p2', nameEn: 'Dates box', nameAr: 'علبة تمر', sku: 'DAT-2', priceHalalas: 4505, stock: 3 },
];

describe('LowStockPage', () => {
  beforeEach(() => signIn('Viewer'));

  it('lists the low-stock products in Arabic with prices in riyals', async () => {
    stubFetch(PRODUCTS);
    renderWithProviders(<LowStockPage />);
    const row = (await screen.findByText('قهوة سوداء')).closest('tr')!;
    expect(within(row).getByText('12.50 ر.س')).toBeInTheDocument();
    expect(within(row).getByText('COF-1')).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'السعر' })).toBeInTheDocument();
  });

  it('lists the products in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(PRODUCTS);
    renderWithProviders(<LowStockPage />);
    expect(await screen.findByText('Dates box')).toBeInTheDocument();
    expect(screen.getByText('SAR 45.05')).toBeInTheDocument();
    expect(screen.queryByText('علبة تمر')).not.toBeInTheDocument();
  });

  it('asks the API as the signed-in company and role', async () => {
    const fetchMock = stubFetch(PRODUCTS);
    renderWithProviders(<LowStockPage />);
    await screen.findByText('قهوة سوداء');
    expect(fetchMock).toHaveBeenCalledWith('/api/pos/catalog/low-stock', {
      headers: { Accept: 'application/json', 'X-Company-Id': COMPANY_ID, 'X-Role': 'Viewer' },
    });
  });

  it('sends the threshold when Apply is clicked', async () => {
    const fetchMock = stubFetch(PRODUCTS);
    renderWithProviders(<LowStockPage />);
    await screen.findByText('قهوة سوداء'); // wait for initial load
    const input = screen.getByLabelText('الحد');
    await userEvent.type(input, '5');
    const applyBtn = screen.getByRole('button', { name: 'تطبيق' });
    expect(applyBtn).not.toBeDisabled();
    await userEvent.click(applyBtn);
    // Check the last call includes threshold
    const calls = fetchMock.mock.calls as unknown as [string, RequestInit][];
    expect(calls.length).toBeGreaterThan(0);
    expect(calls[calls.length - 1][0]).toBe('/api/pos/catalog/low-stock?threshold=5');
  });

  it('disables Apply when the input is empty or zero', async () => {
    stubFetch(PRODUCTS);
    renderWithProviders(<LowStockPage />);
    const applyBtn = screen.getByRole('button', { name: 'تطبيق' });
    expect(applyBtn).toBeDisabled();
    const input = screen.getByLabelText('الحد');
    await userEvent.type(input, '0');
    expect(applyBtn).toBeDisabled();
  });

  it('says so when there are no low-stock products', async () => {
    stubFetch([]);
    renderWithProviders(<LowStockPage />);
    expect(await screen.findByText('لا توجد منتجات أقل من الحد.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('shows an error and loads again on retry', async () => {
    const fetchMock = stubFetch({ title: 'boom' }, 500);
    renderWithProviders(<LowStockPage />);
    expect(await screen.findByRole('alert')).toHaveTextContent('تعذّر تحميل قائمة المخزون المنخفض.');
    fetchMock.mockImplementation(async () => new Response(JSON.stringify(PRODUCTS), { status: 200 }));
    await userEvent.click(screen.getByRole('button', { name: 'إعادة المحاولة' }));
    expect(await screen.findByText('قهوة سوداء')).toBeInTheDocument();
  });

  it('shows loading while pending', async () => {
    stubFetch(new Promise(() => {}));
    renderWithProviders(<LowStockPage />);
    expect(await screen.findByRole('status')).toHaveTextContent('جارٍ التحميل…');
  });

  it.each(['ar', 'en'])('has no accessibility violations (%s)', async (lang) => {
    await i18n.changeLanguage(lang);
    stubFetch(PRODUCTS);
    const { container } = renderWithProviders(<LowStockPage />);
    await screen.findByRole('table');
    expect(await axe(container)).toHaveNoViolations();
  });
});
