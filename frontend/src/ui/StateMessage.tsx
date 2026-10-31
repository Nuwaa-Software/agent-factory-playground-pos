import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

type StateType = 'empty' | 'error' | 'loading';

interface StateMessageProps {
  state: StateType;
  title?: string;
  message?: string;
  onRetry?: () => void;
  className?: string;
}

export default function StateMessage({
  state,
  title,
  message,
  onRetry,
  className = '',
}: StateMessageProps) {
  const { t } = useTranslation();

  const content: Record<StateType, ReactNode> = {
    empty: (
      <div className={`flex flex-col items-center gap-2 py-8 text-slate-500 ${className}`}>
        <span aria-hidden="true" className="text-4xl">📭</span>
        <p>{title || t('ui.emptyState')}</p>
      </div>
    ),
    error: (
      <div
        role="alert"
        className={`flex flex-col items-center gap-2 py-8 text-red-700 ${className}`}
      >
        <span aria-hidden="true" className="text-4xl">⚠️</span>
        <p>{message || t('ui.errorState')}</p>
        {onRetry && (
          <button
            onClick={onRetry}
            className="min-h-11 rounded-md bg-teal-700 px-4 py-2 text-sm font-medium text-white hover:bg-teal-800"
          >
            {t('ui.retry')}
          </button>
        )}
      </div>
    ),
    loading: (
      <div
        role="status"
        aria-label={t('ui.loadingState')}
        className={`flex items-center justify-center gap-2 py-8 text-slate-500 ${className}`}
      >
        <span className="inline-block h-6 w-6 animate-spin rounded-full border-2 border-slate-300 border-t-teal-700" />
        <p>{title || t('ui.loadingState')}</p>
      </div>
    ),
  };

  return <div>{content[state]}</div>;
}
