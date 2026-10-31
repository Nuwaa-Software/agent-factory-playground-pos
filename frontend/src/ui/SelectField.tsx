import { useId } from 'react';
import type { SelectHTMLAttributes } from 'react';

interface SelectFieldProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'label'> {
  label: string;
  error?: string | null;
}

export default function SelectField({ label, error, id, className = '', children, ...rest }: SelectFieldProps) {
  const generatedId = useId();
  const fieldId = id ?? `field-${generatedId}`;
  const errorId = `${fieldId}-error`;

  return (
    <div className="mb-4">
      <label htmlFor={fieldId} className="mb-1 block text-sm font-medium text-slate-700">
        {label}
      </label>
      <select
        id={fieldId}
        {...(error ? { 'aria-invalid': true, 'aria-describedby': errorId } : {})}
        className={`min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 text-sm ${className}`}
        {...rest}
      >
        {children}
      </select>
      {error && (
        <p id={errorId} role="alert" className="mt-1 text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
