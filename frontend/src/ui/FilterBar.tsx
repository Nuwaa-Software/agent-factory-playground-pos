import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

interface FilterBarProps {
  children: ReactNode;
  className?: string;
}

export default function FilterBar({ children, className = '' }: FilterBarProps) {
  const { t } = useTranslation();
  return (
    <div
      role="group"
      aria-label={t('ui.filters')}
      className={`flex flex-wrap items-end gap-3 ${className}`}
    >
      {children}
    </div>
  );
}
