import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import StateMessage from './StateMessage';

describe('StateMessage', () => {
  it('renders empty state', () => {
    render(<StateMessage state="empty" />);
    expect(screen.getByText(/لا توجد بيانات/)).toBeInTheDocument();
  });

  it('renders error state with role=alert', () => {
    render(<StateMessage state="error" message="Something failed" />);
    expect(screen.getByRole('alert')).toHaveTextContent('Something failed');
  });

  it('shows retry button when onRetry is provided', async () => {
    const onRetry = vi.fn();
    render(<StateMessage state="error" onRetry={onRetry} />);
    await userEvent.click(screen.getByRole('button'));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it('renders loading state with role=status', () => {
    render(<StateMessage state="loading" />);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('has no accessibility violations for each state', async () => {
    for (const state of ['empty' as const, 'error' as const, 'loading' as const]) {
      const { container } = render(<StateMessage state={state} />);
      expect(await axe(container)).toHaveNoViolations();
    }
  });
});
