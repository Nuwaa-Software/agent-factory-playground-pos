import { useTranslation } from 'react-i18next';
import { useCallback, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useReceipt } from '../api/sales';
import { ApiError } from '../api/client';

export default function ReceiptPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const receiptQuery = useReceipt(id ?? '');

  const [copied, setCopied] = useState(false);

  const handleCopy = useCallback(async () => {
    if (!receiptQuery.data) return;
    await navigator.clipboard.writeText(receiptQuery.data.sale.qrCode);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }, [receiptQuery.data]);

  if (receiptQuery.isPending) {
    return <p role="status">{t('common.loading')}</p>;
  }

  if (receiptQuery.isError) {
    const apiError = receiptQuery.error as ApiError;
    if (apiError.status === 404) {
      return (
        <div className="flex flex-col items-start gap-2">
          <p role="alert">{t('receipt.notFound')}</p>
          <Link
            to="/sales"
            className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
          >
            {t('receipt.back')}
          </Link>
        </div>
      );
    }
    return (
      <div className="flex flex-col items-start gap-2">
        <p role="alert">{t('receipt.loadError')}</p>
        <button
          type="button"
          onClick={() => void receiptQuery.refetch()}
          className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
        >
          {t('common.retry')}
        </button>
      </div>
    );
  }

  const { sale, receiptText } = receiptQuery.data;

  return (
    <section aria-labelledby="receipt-title" aria-label={t('receipt.title')}>
      <h1 id="receipt-title" className="mb-4 text-xl font-semibold">
        {t('receipt.title')}
      </h1>

      <pre
        aria-label={t('receipt.title')}
        className="whitespace-pre-wrap break-words rounded-md border border-slate-300 bg-slate-50 p-4 font-mono"
      >
        {receiptText}
      </pre>

      <div className="mt-4">
        <h2 className="mb-2 text-lg font-medium">{t('receipt.qrTitle')}</h2>
        <pre
          dir="ltr"
          aria-label={t('receipt.qrTitle')}
          className="whitespace-pre-wrap break-all rounded-md border border-slate-300 bg-slate-50 p-4 font-mono"
        >
          {sale.qrCode}
        </pre>
      </div>

      <div className="mt-4 flex items-center gap-2">
        <button
          type="button"
          onClick={handleCopy}
          disabled={copied}
          className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {copied ? t('receipt.copied') : t('receipt.copy')}
        </button>
        <span role="status" aria-live="polite">
          {copied ? t('receipt.copied') : ''}
        </span>
      </div>

      <div className="mt-4">
        <Link
          to="/sales"
          className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
        >
          {t('receipt.back')}
        </Link>
      </div>
    </section>
  );
}
