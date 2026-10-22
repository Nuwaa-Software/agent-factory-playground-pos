import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import { renderWithProviders, stubFetch } from '../test/render';
import SalesPage from './SalesPage';

describe('SalesPage', () => {
  const saleSummary = {
    id: 'sale-1',
    number: 123,
    createdAt: '2026-10-14T10:30:00Z',
    netHalalas: 1250,
    vatHalalas: 125,
    totalHalalas: 1375,
  };

  const salesListResponse = {
    sales: [saleSummary],
    page: 1,
    pageSize: 20,
    totalCount: 1,
    totalPages: 1,
  };

  beforeEach(() => {
    stubFetch(salesListResponse);
  });

  it('lists sales in Arabic with prices in riyals', async () => {
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('رقم الفاتورة');

    // Check headers are in Arabic
    expect(screen.getByText('رقم الفاتورة')).toBeInTheDocument();
    expect(screen.getByText('التاريخ')).toBeInTheDocument();
    expect(screen.getByText('صافي')).toBeInTheDocument();
    expect(screen.getByText('الضريبة')).toBeInTheDocument();
    expect(screen.getByText('الإجمالي')).toBeInTheDocument();

    // Check sale data
    expect(screen.getByText('123')).toBeInTheDocument();
    // The actual rendered date has different Unicode characters for slashes
    const dateCell = screen.getByText(/١٤‏\/١٠‏\/٢٠٢٦/);
    expect(dateCell).toBeInTheDocument();
    expect(screen.getByText('12.50 ر.س')).toBeInTheDocument(); // Arabic money format
    expect(screen.getByText('1.25 ر.س')).toBeInTheDocument();
    expect(screen.getByText('13.75 ر.س')).toBeInTheDocument();
  });

  it('lists sales in English', async () => {
    await i18n.changeLanguage('en');
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('Invoice #');

    // Check headers are in English
    expect(screen.getByText('Invoice #')).toBeInTheDocument();
    expect(screen.getByText('Date')).toBeInTheDocument();
    expect(screen.getByText('Net')).toBeInTheDocument();
    expect(screen.getByText('VAT')).toBeInTheDocument();
    expect(screen.getByText('Total')).toBeInTheDocument();

    // Check sale data
    expect(screen.getByText('123')).toBeInTheDocument();
    expect(screen.getByText('10/14/2026')).toBeInTheDocument(); // English date format
    expect(screen.getByText('SAR 12.50')).toBeInTheDocument(); // English money format
    expect(screen.getByText('SAR 1.25')).toBeInTheDocument();
    expect(screen.getByText('SAR 13.75')).toBeInTheDocument();
  });

  it('shows empty state', async () => {
    stubFetch({
      sales: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
    });
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('لا توجد مبيعات بعد.');

    expect(screen.getByText('لا توجد مبيعات بعد.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('shows error state with retry button', async () => {
    stubFetch({}, 500);
    renderWithProviders(<SalesPage />);

    // Wait for error to appear
    await screen.findByRole('alert');

    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(screen.getByText('تعذّر تحميل المبيعات.')).toBeInTheDocument();
    expect(screen.getByText('إعادة المحاولة')).toBeInTheDocument();

    // Mock successful response and click retry
    stubFetch(salesListResponse);
    await userEvent.click(screen.getByText('إعادة المحاولة'));

    // Wait for data to load after retry
    await screen.findByText('123');
    expect(screen.getByText('123')).toBeInTheDocument();
  });

  it('disables previous button on first page', async () => {
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('رقم الفاتورة');

    const prevButton = screen.getByRole('button', { name: 'السابقة' });
    expect(prevButton).toBeDisabled();
  });

  it('disables next button on last page', async () => {
    stubFetch({
      ...salesListResponse,
      totalPages: 1,
    });
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('رقم الفاتورة');

    const nextButton = screen.getByRole('button', { name: 'التالية' });
    expect(nextButton).toBeDisabled();
  });

  it('enables next button on first page with multiple pages', async () => {
    stubFetch({
      ...salesListResponse,
      totalPages: 2,
    });
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('رقم الفاتورة');

    const nextButton = screen.getByRole('button', { name: 'التالية' });
    expect(nextButton).not.toBeDisabled();
  });

  it('fetches page 2 when clicking next button', async () => {
    stubFetch({
      ...salesListResponse,
      totalPages: 2,
    });
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('رقم الفاتورة');

    const nextButton = screen.getByRole('button', { name: 'التالية' });
    await userEvent.click(nextButton);

    // Check that fetch was called with page 2
    expect(fetch).toHaveBeenCalledWith('/api/pos/sales?page=2&pageSize=20', expect.anything());
  });

  it('passes accessibility checks in Arabic', async () => {
    const { container } = renderWithProviders(<SalesPage />);
    
    // Wait for data to load
    await screen.findByText('رقم الفاتورة');

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('passes accessibility checks in English', async () => {
    await i18n.changeLanguage('en');
    const { container } = renderWithProviders(<SalesPage />);
    
    // Wait for data to load
    await screen.findByText('Invoice #');

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});