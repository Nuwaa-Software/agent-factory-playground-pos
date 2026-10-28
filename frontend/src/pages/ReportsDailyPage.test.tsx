import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import { renderWithProviders, signIn, stubFetch } from '../test/render';
import ReportsDailyPage from './ReportsDailyPage';

describe('ReportsDailyPage', () => {
  const dailySalesResponse = {
    date: '2026-10-27',
    saleCount: 5,
    netTotal: 5000,
    vatTotal: 750,
    grossTotal: 5750,
  };

  beforeEach(() => {
    signIn();
    stubFetch(dailySalesResponse);
  });

  it('renders the title and date input', async () => {
    renderWithProviders(<ReportsDailyPage />);

    expect(screen.getByText('تقرير المبيعات اليومي')).toBeInTheDocument();
    expect(screen.getByLabelText('التاريخ')).toBeInTheDocument();
  });

  it('displays sale count and totals in Arabic', async () => {
    renderWithProviders(<ReportsDailyPage />);

    await screen.findByRole('table');

    expect(screen.getByText('عدد المبيعات')).toBeInTheDocument();
    expect(screen.getByText('صافي')).toBeInTheDocument();
    expect(screen.getByText('الضريبة')).toBeInTheDocument();
    expect(screen.getByText('الإجمالي')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('50.00 ر.س')).toBeInTheDocument();
    expect(screen.getByText('7.50 ر.س')).toBeInTheDocument();
    expect(screen.getByText('57.50 ر.س')).toBeInTheDocument();
  });

  it('displays sale count and totals in English', async () => {
    await i18n.changeLanguage('en');
    renderWithProviders(<ReportsDailyPage />);

    await screen.findByRole('table');

    expect(screen.getByText('Sales')).toBeInTheDocument();
    expect(screen.getByText('Net')).toBeInTheDocument();
    expect(screen.getByText('VAT')).toBeInTheDocument();
    expect(screen.getByText('Gross')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('SAR 50.00')).toBeInTheDocument();
    expect(screen.getByText('SAR 7.50')).toBeInTheDocument();
    expect(screen.getByText('SAR 57.50')).toBeInTheDocument();
  });

  it('fetches with date= in the URL', async () => {
    renderWithProviders(<ReportsDailyPage />);

    await screen.findByRole('table');

    const calls = (fetch as ReturnType<typeof vi.fn>).mock.calls;
    expect(calls.length).toBeGreaterThan(0);
    const url = calls[0][0];
    expect(typeof url).toBe('string');
    expect(url).toContain('date=');
  });

  it('shows empty state when saleCount is 0', async () => {
    stubFetch({ date: '2026-10-27', saleCount: 0, netTotal: 0, vatTotal: 0, grossTotal: 0 });
    renderWithProviders(<ReportsDailyPage />);

    await screen.findByText('لا توجد مبيعات في هذا التاريخ.');

    expect(screen.getByText('لا توجد مبيعات في هذا التاريخ.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('shows error state with retry button', async () => {
    stubFetch({}, 500);
    renderWithProviders(<ReportsDailyPage />);

    await screen.findByRole('alert');

    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(screen.getByText('تعذّر تحميل التقرير.')).toBeInTheDocument();
    expect(screen.getByText('إعادة المحاولة')).toBeInTheDocument();

    // Mock successful response and click retry
    stubFetch(dailySalesResponse);
    await userEvent.click(screen.getByText('إعادة المحاولة'));

    // Wait for data to load after retry
    await screen.findByRole('table');
    expect(screen.getByText('5')).toBeInTheDocument();
  });

  it('passes accessibility checks in Arabic', async () => {
    const { container } = renderWithProviders(<ReportsDailyPage />);

    await screen.findByRole('table');

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('passes accessibility checks in English', async () => {
    await i18n.changeLanguage('en');
    const { container } = renderWithProviders(<ReportsDailyPage />);

    await screen.findByRole('table');

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
