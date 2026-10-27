import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import { renderWithProviders, signIn } from '../test/render';
import ReceiptPage from './ReceiptPage';
import { Routes, Route } from 'react-router-dom';

const SALE_ID = 'sale-1';
const SALE_RESPONSE = {
  id: SALE_ID,
  number: 42,
  qrCode: 'dGVzdC1xci1wYXlsb2Fk',
};
const RECEIPT_TEXT = `CompanyEn / CompanyAr
VAT No: 300012345600003
Sale #42  2026-01-01 12:00 UTC
2 x Shirt / قميص  10.00
Net: 10.00
VAT 15%: 1.50
Total: 11.50`;

/** Stub fetch with per-URL responses (handles text/plain + JSON). */
function stubFetchPerUrl(urlToResponse: Record<string, { body: unknown; status?: number }>) {
  const fetchMock = vi.fn(async (url: string) => {
    const entry = urlToResponse[url];
    if (!entry) return new Response('', { status: 404 });
    const { body, status = 200 } = entry;
    if (typeof body === 'string') {
      return new Response(body, { status, headers: { 'Content-Type': 'text/plain' } });
    }
    return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function stubSuccess() {
  const receiptUrl = `/api/pos/sales/${SALE_ID}/receipt`;
  const saleUrl = `/api/pos/sales/${SALE_ID}`;
  return stubFetchPerUrl({
    [saleUrl]: { body: SALE_RESPONSE },
    [receiptUrl]: { body: RECEIPT_TEXT },
  });
}

function stub404() {
  const receiptUrl = `/api/pos/sales/${SALE_ID}/receipt`;
  const saleUrl = `/api/pos/sales/${SALE_ID}`;
  return stubFetchPerUrl({
    [saleUrl]: { body: {}, status: 404 },
    [receiptUrl]: { body: '', status: 404 },
  });
}

function stub500() {
  const receiptUrl = `/api/pos/sales/${SALE_ID}/receipt`;
  const saleUrl = `/api/pos/sales/${SALE_ID}`;
  return stubFetchPerUrl({
    [saleUrl]: { body: {}, status: 500 },
    [receiptUrl]: { body: '', status: 500 },
  });
}

function renderReceiptPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/sales/:id/receipt" element={<ReceiptPage />} />
    </Routes>,
    { route: `/sales/${SALE_ID}/receipt` },
  );
}

describe('ReceiptPage', () => {
  beforeEach(() => {
    signIn();
  });

  describe('Arabic', () => {
    beforeEach(async () => {
      await i18n.changeLanguage('ar');
    });

    it('shows loading state', async () => {
      vi.stubGlobal('fetch', vi.fn());
      renderReceiptPage();
      expect(screen.getByRole('status')).toHaveTextContent('جارٍ التحميل…');
    });

    it('renders receipt and qrCode on success', async () => {
      stubSuccess();
      renderReceiptPage();

      await waitFor(() => expect(screen.getByRole('heading', { name: 'الإيصال' })).toBeInTheDocument());

      const pre = document.querySelector('pre[aria-label="الإيصال"]');
      expect(pre).toBeInTheDocument();
      expect(pre?.tagName).toBe('PRE');
      expect(screen.getByText('dGVzdC1xci1wYXlsb2Fk')).toBeInTheDocument();
    });

    it('copies qrCode and shows copied status', async () => {
      stubSuccess();
      const writeTextMock = vi.fn().mockResolvedValue(undefined);
      vi.stubGlobal('navigator', { clipboard: { writeText: writeTextMock } });

      renderReceiptPage();

      await waitFor(() => expect(screen.getByRole('heading', { name: 'الإيصال' })).toBeInTheDocument());

      const copyButton = screen.getByRole('button', { name: 'نسخ' });
      await userEvent.click(copyButton);

      await waitFor(() => expect(writeTextMock).toHaveBeenCalledWith(SALE_RESPONSE.qrCode));
      expect(screen.getByRole('status')).toHaveTextContent('تم النسخ');
    });

    it('shows notFound on 404', async () => {
      stub404();
      renderReceiptPage();

      await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
      expect(screen.getByRole('alert')).toHaveTextContent('لم يتم العثور على الفاتورة.');
    });

    it('shows loadError and retry on 500', async () => {
      stub500();
      renderReceiptPage();

      await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
      expect(screen.getByRole('alert')).toHaveTextContent('تعذّر تحميل الإيصال.');
      expect(screen.getByText('إعادة المحاولة')).toBeInTheDocument();
    });

    it('has correct aria attributes', async () => {
      stubSuccess();
      renderReceiptPage();

      await waitFor(() => expect(screen.getByRole('heading', { name: 'الإيصال' })).toBeInTheDocument());

      const pre = document.querySelector('pre[aria-label="الإيصال"]');
      expect(pre).toHaveAttribute('aria-label', 'الإيصال');

      const qrPre = screen.getByLabelText('رمز QR (ZATCA)');
      expect(qrPre).toHaveAttribute('dir', 'ltr');
      expect(qrPre).toHaveAttribute('aria-label', 'رمز QR (ZATCA)');

      const copyButton = screen.getByRole('button', { name: 'نسخ' });
      expect(copyButton).toBeInTheDocument();

      const backLink = screen.getByRole('link', { name: 'رجوع' });
      expect(backLink).toHaveAttribute('href', '/sales');
    });

    it('passes axe checks', async () => {
      stubSuccess();
      const { container } = renderReceiptPage();

      await waitFor(() => expect(screen.getByRole('heading', { name: 'الإيصال' })).toBeInTheDocument());

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });
  });

  describe('English', () => {
    beforeEach(async () => {
      await i18n.changeLanguage('en');
    });

    it('shows loading state', async () => {
      vi.stubGlobal('fetch', vi.fn());
      renderReceiptPage();
      expect(screen.getByRole('status')).toHaveTextContent('Loading…');
    });

    it('renders receipt and qrCode on success', async () => {
      stubSuccess();
      renderReceiptPage();

      await waitFor(() => expect(screen.getByRole('heading', { name: 'Receipt' })).toBeInTheDocument());

      const pre = document.querySelector('pre[aria-label="Receipt"]');
      expect(pre).toBeInTheDocument();
      expect(pre?.tagName).toBe('PRE');
      expect(screen.getByText('dGVzdC1xci1wYXlsb2Fk')).toBeInTheDocument();
    });

    it('copies qrCode and shows copied status', async () => {
      stubSuccess();
      const writeTextMock = vi.fn().mockResolvedValue(undefined);
      vi.stubGlobal('navigator', { clipboard: { writeText: writeTextMock } });

      renderReceiptPage();

      await waitFor(() => expect(screen.getByRole('heading', { name: 'Receipt' })).toBeInTheDocument());

      const copyButton = screen.getByRole('button', { name: 'Copy' });
      await userEvent.click(copyButton);

      await waitFor(() => expect(writeTextMock).toHaveBeenCalledWith(SALE_RESPONSE.qrCode));
      expect(screen.getByRole('status')).toHaveTextContent('Copied');
    });

    it('shows notFound on 404', async () => {
      stub404();
      renderReceiptPage();

      await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
      expect(screen.getByRole('alert')).toHaveTextContent('The invoice was not found.');
    });

    it('shows loadError and retry on 500', async () => {
      stub500();
      renderReceiptPage();

      await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
      expect(screen.getByRole('alert')).toHaveTextContent('The receipt could not be loaded.');
      expect(screen.getByText('Try again')).toBeInTheDocument();
    });

    it('has correct aria attributes', async () => {
      stubSuccess();
      renderReceiptPage();

      await waitFor(() => expect(screen.getByRole('heading', { name: 'Receipt' })).toBeInTheDocument());

      const pre = document.querySelector('pre[aria-label="Receipt"]');
      expect(pre).toHaveAttribute('aria-label', 'Receipt');

      const qrPre = screen.getByLabelText('QR code (ZATCA)');
      expect(qrPre).toHaveAttribute('dir', 'ltr');
      expect(qrPre).toHaveAttribute('aria-label', 'QR code (ZATCA)');

      const copyButton = screen.getByRole('button', { name: 'Copy' });
      expect(copyButton).toBeInTheDocument();

      const backLink = screen.getByRole('link', { name: 'Back' });
      expect(backLink).toHaveAttribute('href', '/sales');
    });

    it('passes axe checks', async () => {
      stubSuccess();
      renderReceiptPage();

      await waitFor(() => expect(screen.getByRole('heading', { name: 'Receipt' })).toBeInTheDocument());

      const results = await axe(document.documentElement);
      expect(results).toHaveNoViolations();
    });
  });
});
