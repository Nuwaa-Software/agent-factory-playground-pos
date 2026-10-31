import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import SelectField from './SelectField';

describe('SelectField', () => {
  it('renders label and select', () => {
    render(
      <SelectField label="Status">
        <option value="">اختر</option>
        <option value="active">نشط</option>
      </SelectField>,
    );
    expect(screen.getByLabelText('Status')).toBeInTheDocument();
  });

  it('shows error message when provided', () => {
    render(
      <SelectField label="Status" error="Invalid selection">
        <option value="">اختر</option>
      </SelectField>,
    );
    expect(screen.getByRole('alert')).toHaveTextContent('Invalid selection');
  });

  it('sets aria-invalid when there is an error', () => {
    render(
      <SelectField label="Status" error="Invalid selection">
        <option value="">اختر</option>
      </SelectField>,
    );
    const select = screen.getByLabelText('Status') as HTMLSelectElement;
    expect(select).toHaveAttribute('aria-invalid', 'true');
    expect(select).toHaveAttribute('aria-describedby', `${select.id}-error`);
  });

  it('does not set aria-invalid when there is no error', () => {
    render(
      <SelectField label="Status">
        <option value="">اختر</option>
      </SelectField>,
    );
    const select = screen.getByLabelText('Status') as HTMLSelectElement;
    expect(select).not.toHaveAttribute('aria-invalid');
  });

  it('calls onChange when value changes', async () => {
    const onChange = vi.fn();
    render(
      <SelectField label="Status" onChange={onChange}>
        <option value="">اختر</option>
        <option value="active">نشط</option>
      </SelectField>,
    );
    await userEvent.selectOptions(screen.getByLabelText('Status'), 'active');
    expect(onChange).toHaveBeenCalled();
  });

  it('has no accessibility violations', async () => {
    const { container } = render(
      <SelectField label="Status">
        <option value="">اختر</option>
      </SelectField>,
    );
    expect(await axe(container)).toHaveNoViolations();
  });
});
