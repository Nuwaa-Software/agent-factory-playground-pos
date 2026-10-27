import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { apiGet, apiPost, apiPatch, ApiError } from '../api/client';
import { formatSar } from '../lib/money';

interface ShiftResponse {
  id: string;
  cashierName: string;
  startingCashHalalas: number;
  openedAt: string;
  closedAt?: string | null;
  actualCashHalalas?: number | null;
  expectedCashHalalas?: number | null;
  discrepancyHalalas?: number | null;
}

type State = 'loading' | 'loadError' | 'openForm' | 'openShift' | 'closedSummary';

export default function ShiftsPage() {
  const { t, i18n } = useTranslation();
  const [state, setState] = useState<State>('loading');
  const [shift, setShift] = useState<ShiftResponse | null>(null);
  const [cashierName, setCashierName] = useState('');
  const [startingCash, setStartingCash] = useState('');
  const [actualCash, setActualCash] = useState('');
  const [inFlight, setInFlight] = useState(false);
  const [nameError, setNameError] = useState(false);
  const [startingError, setStartingError] = useState(false);
  const [actualError, setActualError] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  useEffect(() => {
    let cancelled = false;
    apiGet<ShiftResponse[]>('/api/pos/shifts?status=open')
      .then((list) => {
        if (cancelled) return;
        if (list.length === 0) {
          setState('openForm');
        } else {
          setShift(list[0]);
          setState('openShift');
        }
      })
      .catch(() => {
        if (!cancelled) setState('loadError');
      });
    return () => { cancelled = true; };
  }, []);

  const validateCash = (value: string): boolean => {
    if (value === '') return false;
    const trimmed = value.trim();
    const parsed = parseFloat(trimmed);
    return !isNaN(parsed) && parsed >= 0 && /^\d+(\.\d+)?$/.test(trimmed);
  };

  const handleOpenSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setNameError(false);
    setStartingError(false);
    setErrorMessage('');

    if (cashierName.trim() === '') {
      setNameError(true);
      return;
    }
    if (!validateCash(startingCash)) {
      setStartingError(true);
      return;
    }

    setInFlight(true);
    try {
      const halalas = Math.round(parseFloat(startingCash) * 100);
      const result = await apiPost<ShiftResponse>('/api/pos/shifts/open', {
        cashierName: cashierName.trim(),
        startingCashHalalas: halalas,
      });
      setShift(result);
      setState('openShift');
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setErrorMessage(t('shift.alreadyClosed'));
      } else if (err instanceof ApiError && err.status === 404) {
        setErrorMessage(t('shift.notFound'));
      } else {
        setErrorMessage(t('shift.openError'));
      }
    } finally {
      setInFlight(false);
    }
  };

  const handleCloseSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setActualError(false);
    setErrorMessage('');

    if (!validateCash(actualCash)) {
      setActualError(true);
      return;
    }

    if (!shift) return;

    setInFlight(true);
    try {
      const halalas = Math.round(parseFloat(actualCash) * 100);
      const result = await apiPatch<ShiftResponse>(`/api/pos/shifts/${shift.id}/close`, {
        id: shift.id,
        actualCashHalalas: halalas,
      });
      setShift(result);
      setState('closedSummary');
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setErrorMessage(t('shift.alreadyClosed'));
      } else if (err instanceof ApiError && err.status === 404) {
        setErrorMessage(t('shift.notFound'));
      } else {
        setErrorMessage(t('shift.closeError'));
      }
    } finally {
      setInFlight(false);
    }
  };

  const formatOpenedAt = (dateStr: string): string => {
    return new Date(dateStr).toLocaleDateString(
      i18n.language === 'ar' ? 'ar-SA' : 'en-SA',
      { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' },
    );
  };

  if (state === 'loading') {
    return <p role="status">{t('common.loading')}</p>;
  }

  if (state === 'loadError') {
    return (
      <div role="alert" className="flex flex-col items-start gap-2">
        <p>{t('shift.loadError')}</p>
        <button
          type="button"
          onClick={() => window.location.reload()}
          className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
        >
          {t('common.retry')}
        </button>
      </div>
    );
  }

  if (state === 'openForm') {
    return (
      <section aria-labelledby="shift-open-title" className="max-w-md">
        <h1 id="shift-open-title" className="mb-4 text-xl font-semibold">
          {t('shift.openTitle')}
        </h1>
        {errorMessage && (
          <div role="alert" className="mb-4 text-sm text-red-700">
            {errorMessage}
          </div>
        )}
        <form onSubmit={handleOpenSubmit} noValidate className="flex flex-col gap-4">
          <div className="flex flex-col gap-1">
            <label htmlFor="cashier-name">{t('shift.cashierName')}</label>
            <input
              id="cashier-name"
              value={cashierName}
              onChange={(e) => {
                setCashierName(e.target.value);
                setNameError(false);
              }}
              aria-invalid={nameError}
              aria-describedby={nameError ? 'cashier-name-error' : undefined}
              className="min-h-11 rounded-md border border-slate-300 px-3"
            />
            {nameError && (
              <p id="cashier-name-error" role="alert" className="text-sm text-red-700">
                {t('shift.errRequired')}
              </p>
            )}
          </div>
          <div className="flex flex-col gap-1">
            <label htmlFor="starting-cash">{t('shift.startingCash')}</label>
            <input
              id="starting-cash"
              type="text"
              dir="ltr"
              value={startingCash}
              onChange={(e) => {
                setStartingCash(e.target.value);
                setStartingError(false);
              }}
              aria-invalid={startingError}
              aria-describedby={startingError ? 'starting-cash-error' : undefined}
              className="min-h-11 rounded-md border border-slate-300 px-3"
            />
            {startingError && (
              <p id="starting-cash-error" role="alert" className="text-sm text-red-700">
                {t('shift.errMoney')}
              </p>
            )}
          </div>
          <button
            type="submit"
            disabled={inFlight}
            className="min-h-11 self-start rounded-md bg-teal-700 px-4 text-white hover:bg-teal-800 disabled:opacity-50"
          >
            {inFlight ? t('shift.opening') : t('shift.open')}
          </button>
        </form>
      </section>
    );
  }

  if (state === 'openShift' && shift) {
    return (
      <section aria-labelledby="shift-close-title" className="max-w-md">
        <h1 id="shift-close-title" className="mb-4 text-xl font-semibold">
          {t('shift.closeTitle')}
        </h1>
        {!errorMessage && (
          <p className="mb-2 text-sm text-green-700">{t('shift.opened')}</p>
        )}
        {errorMessage && (
          <div role="alert" className="mb-4 text-sm text-red-700">
            {errorMessage}
          </div>
        )}
        <div className="mb-6 flex flex-col gap-2 rounded-md border border-slate-200 p-3">
          <p>
            <span className="font-medium">{t('shift.cashier')}:</span>{' '}
            {shift.cashierName}
          </p>
          <p>
            <span className="font-medium">{t('shift.openedAt')}:</span>{' '}
            {formatOpenedAt(shift.openedAt)}
          </p>
          <p>
            <span className="font-medium">{t('shift.starting')}:</span>{' '}
            {t('money.sar', { amount: formatSar(shift.startingCashHalalas) })}
          </p>
        </div>
        <form onSubmit={handleCloseSubmit} noValidate className="flex flex-col gap-4">
          <div className="flex flex-col gap-1">
            <label htmlFor="actual-cash">{t('shift.actualCash')}</label>
            <input
              id="actual-cash"
              type="text"
              dir="ltr"
              value={actualCash}
              onChange={(e) => {
                setActualCash(e.target.value);
                setActualError(false);
                setErrorMessage('');
              }}
              aria-invalid={actualError}
              aria-describedby={actualError ? 'actual-cash-error' : undefined}
              className="min-h-11 rounded-md border border-slate-300 px-3"
            />
            {actualError && (
              <p id="actual-cash-error" role="alert" className="text-sm text-red-700">
                {t('shift.errMoney')}
              </p>
            )}
          </div>
          <button
            type="submit"
            disabled={inFlight}
            className="min-h-11 self-start rounded-md bg-teal-700 px-4 text-white hover:bg-teal-800 disabled:opacity-50"
          >
            {inFlight ? t('shift.closing') : t('shift.close')}
          </button>
        </form>
      </section>
    );
  }

  if (state === 'closedSummary' && shift) {
    return (
      <section aria-labelledby="shift-closed-title" className="max-w-md">
        <h1 id="shift-closed-title" className="mb-4 text-xl font-semibold">
          {t('shift.closed')}
        </h1>
        <div className="flex flex-col gap-2 rounded-md border border-slate-200 p-3">
          <p>
            <span className="font-medium">{t('shift.cashier')}:</span>{' '}
            {shift.cashierName}
          </p>
          <p>
            <span className="font-medium">{t('shift.starting')}:</span>{' '}
            {t('money.sar', { amount: formatSar(shift.startingCashHalalas) })}
          </p>
          <p>
            <span className="font-medium">{t('shift.actual')}:</span>{' '}
            {shift.actualCashHalalas != null
              ? t('money.sar', { amount: formatSar(shift.actualCashHalalas) })
              : '—'}
          </p>
        </div>
      </section>
    );
  }

  return null;
}
