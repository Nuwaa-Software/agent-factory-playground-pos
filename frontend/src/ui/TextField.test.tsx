import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import TextField from './TextField';

describe('TextField', () => {
  it('renders label and input', () => {
    render(<TextField label="Name" />);
    expect(screen.getByLabelText('Name')).toBeInTheDocument();
  });

  it('shows error message when provided', () => {
    render(<TextField label="Email" error="Invalid email" />);
    expect(screen.getByRole('alert')).toHaveTextContent('Invalid email');
  });

  it('sets aria-invalid when there is an error', () => {
    render(<TextField label="Email" error="Invalid email" />);
    const input = screen.getByLabelText('Email') as HTMLInputElement;
    expect(input).toHaveAttribute('aria-invalid', 'true');
    expect(input).toHaveAttribute('aria-describedby', `${input.id}-error`);
  });

  it('does not set aria-invalid when there is no error', () => {
    render(<TextField label="Email" />);
    const input = screen.getByLabelText('Email') as HTMLInputElement;
    expect(input).not.toHaveAttribute('aria-invalid');
  });

  it('calls onChange when value changes', async () => {
    const onChange = vi.fn();
    render(<TextField label="Name" onChange={onChange} />);
    await userEvent.type(screen.getByLabelText('Name'), 'Alice');
    expect(onChange).toHaveBeenCalled();
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<TextField label="Name" />);
    expect(await axe(container)).toHaveNoViolations();
  });
});
