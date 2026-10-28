import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { voidSale, type SaleResponse } from '../api/sales';
import { formatSar } from '../lib/money';

interface VoidSaleDialogProps {
  saleId: string;
  saleNumber: number;
  createdAt: string;
  totalHalalas: number;
  onSuccess(sale: SaleResponse): void;
  onClose(): void;
  triggerElement: HTMLElement | null;
}

export default function VoidSaleDialog({
  saleId,
  saleNumber,
  createdAt,
  totalHalalas,
  onSuccess,
  onClose,
  triggerElement,
}: VoidSaleDialogProps) {
  const { t, i18n } = useTranslation();
  const [reason, setReason] = useState('');
  const [status, setStatus] = useState<'idle' | 'submitting'>('idle');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const dialogRef = useRef<HTMLDivElement>(null);
  const confirmButtonRef = useRef<HTMLButtonElement>(null);

  // Move focus to confirm button on open
  useEffect(() => {
    confirmButtonRef.current?.focus();
  }, []);

  // Focus trap: Tab / Shift+Tab cycles within the dialog
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      onClose();
      return;
    }
    if (e.key !== 'Tab') return;

    const container = dialogRef.current;
    if (!container) return;

    const focusable = Array.from(
      container.querySelectorAll<HTMLElement>(
        'button:not([disabled]), input, [tabindex]:not([tabindex="0"])'
      )
    );
    if (focusable.length === 0) return;

    const first = focusable[0];
    const last = focusable[focusable.length - 1];

    if (e.shiftKey) {
      if (document.activeElement === first) {
        e.preventDefault();
        last.focus();
      }
    } else {
      if (document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    }
  };

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) onClose();
  };

  // Restore focus to the trigger element on close
  useEffect(() => {
    return () => {
      triggerElement?.focus();
    };
  }, [triggerElement]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setStatus('submitting');
    setErrorMessage(null);

    try {
      const result = await voidSale(saleId);
      onSuccess(result);
    } catch (err) {
      if (!(err instanceof Error)) throw err;
      const apiErr = err as Error & { status: number; code?: string };
      let msgKey: string;

      if (apiErr.status === 403) {
        msgKey = 'void.forbidden';
      } else if (apiErr.status === 404) {
        msgKey = 'void.notFound';
      } else if (apiErr.code === 'sale.void_window_expired') {
        msgKey = 'void.windowExpired';
      } else {
        msgKey = 'void.error';
      }

      setErrorMessage(t(msgKey));
      setStatus('idle');
    }
  };

  const formattedDate = new Date(createdAt).toLocaleDateString(
    i18n.language === 'ar' ? 'ar-SA' : 'en-SA'
  );

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
      onClick={handleOverlayClick}
      onKeyDown={handleKeyDown}
      role="presentation"
    >
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="void-dialog-title"
        className="w-full max-w-md rounded-lg border border-slate-300 bg-white p-6 shadow-lg"
      >
        <h2 id="void-dialog-title" className="mb-4 text-lg font-semibold">
          {t('void.confirmTitle')}
        </h2>

        <div className="mb-4 space-y-1 text-sm">
          <p>
            <span className="font-medium">{t('void.saleNumber')}:</span>{' '}
            {saleNumber}
          </p>
          <p>
            <span className="font-medium">{t('void.date')}:</span>{' '}
            {formattedDate}
          </p>
          <p>
            <span className="font-medium">{t('void.total')}:</span>{' '}
            {t('money.sar', { amount: formatSar(totalHalalas) })}
          </p>
        </div>

        <form onSubmit={handleSubmit}>
          <label
            htmlFor="void-reason"
            className="mb-1 block text-sm font-medium"
          >
            {t('void.reasonLabel')}
          </label>
          <input
            id="void-reason"
            type="text"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder={t('void.reasonPlaceholder')}
            className="mb-4 min-h-11 w-full rounded-md border border-slate-300 px-3 text-sm"
          />

          {errorMessage && (
            <p role="alert" className="mb-4 text-sm text-red-700">
              {errorMessage}
            </p>
          )}

          <button
            ref={confirmButtonRef}
            type="submit"
            disabled={status === 'submitting'}
            className="min-h-11 w-full rounded-md bg-teal-600 px-4 py-2 text-white hover:bg-teal-700 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {status === 'submitting' ? t('void.voiding') : t('void.confirm')}
          </button>
        </form>
      </div>
    </div>
  );
}
