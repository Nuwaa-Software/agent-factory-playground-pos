import type { ReactNode } from 'react';

type BadgeColor = 'success' | 'error' | 'warning' | 'neutral';

interface BadgeProps {
  color: BadgeColor;
  children: ReactNode;
  className?: string;
}

const colorMap: Record<BadgeColor, string> = {
  success: 'bg-green-100 text-green-800',
  error: 'bg-red-100 text-red-800',
  warning: 'bg-amber-100 text-amber-800',
  neutral: 'bg-slate-100 text-slate-800',
};

const iconMap: Record<BadgeColor, string> = {
  success: '✓',
  error: '✕',
  warning: '⚠',
  neutral: '•',
};

export default function Badge({ color, children, className = '' }: BadgeProps) {
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium ${colorMap[color]} ${className}`}
    >
      <span aria-hidden="true">{iconMap[color]}</span>
      {children}
    </span>
  );
}
