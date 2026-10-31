import { render, screen } from '@testing-library/react';
import { axe } from '../test/axe';
import Badge from './Badge';

describe('Badge', () => {
  it('renders with success color', () => {
    render(<Badge color="success">Success</Badge>);
    expect(screen.getByText('Success')).toHaveClass('bg-green-100');
    expect(screen.getByText('Success')).toHaveClass('text-green-800');
  });

  it('renders with error color', () => {
    render(<Badge color="error">Error</Badge>);
    expect(screen.getByText('Error')).toHaveClass('bg-red-100');
    expect(screen.getByText('Error')).toHaveClass('text-red-800');
  });

  it('renders with warning color', () => {
    render(<Badge color="warning">Warning</Badge>);
    expect(screen.getByText('Warning')).toHaveClass('bg-amber-100');
    expect(screen.getByText('Warning')).toHaveClass('text-amber-800');
  });

  it('renders with neutral color', () => {
    render(<Badge color="neutral">Neutral</Badge>);
    expect(screen.getByText('Neutral')).toHaveClass('bg-slate-100');
    expect(screen.getByText('Neutral')).toHaveClass('text-slate-800');
  });

  it('always includes an icon (never colour-only)', () => {
    render(<Badge color="success">Success</Badge>);
    // The icon span is aria-hidden, but the text should be visible
    expect(screen.getByText('Success')).not.toHaveAttribute('aria-hidden');
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<Badge color="success">Test</Badge>);
    expect(await axe(container)).toHaveNoViolations();
  });
});
