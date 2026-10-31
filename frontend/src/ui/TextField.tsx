import { useId } from 'react';
import type { InputHTMLAttributes } from 'react';

interface TextFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'label'> {
  label: string;
  error?: string | null;
}

export default function TextField({ label, error, id, className = '', ...rest }: TextFieldProps) {
  const generatedId = useId();
  const fieldId = id ?? `field-${generatedId}`;
  const errorId = `${fieldId}-error`;

  return (
    <div className="mb-4">
      <label htmlFor={fieldId} className="mb-1 block text-sm font-medium text-slate-700">
        {label}
      </label>
      <input
        id={fieldId}
        {...(error ? { 'aria-invalid': true, 'aria-describedby': errorId } : {})}
        className={`min-h-11 w-full rounded-md border border-slate-300 px-3 text-sm ${className}`}
        {...rest}
      />
      {error && (
        <p id={errorId} role="alert" className="mt-1 text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
