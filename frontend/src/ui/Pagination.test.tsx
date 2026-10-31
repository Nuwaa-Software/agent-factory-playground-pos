import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import Pagination from './Pagination';

describe('Pagination', () => {
  it('renders page numbers for 5 pages', () => {
    render(<Pagination currentPage={1} totalPages={5} onPageChange={() => {}} />);
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
    // Pages 3 and 4 are collapsed into ellipsis when on page 1
    expect(screen.getByText('…')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
  });

  it('renders more pages when in the middle', () => {
    render(<Pagination currentPage={3} totalPages={5} onPageChange={() => {}} />);
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    expect(screen.getByText('4')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
  });

  it('marks the current page with aria-current', () => {
    render(<Pagination currentPage={3} totalPages={5} onPageChange={() => {}} />);
    const activePage = screen.getByRole('button', { name: '3' });
    expect(activePage).toHaveAttribute('aria-current', 'page');
  });

  it('disables the prev button on page 1', () => {
    render(<Pagination currentPage={1} totalPages={5} onPageChange={() => {}} />);
    const prevBtn = screen.getAllByRole('button')[0];
    expect(prevBtn).toBeDisabled();
  });

  it('disables the next button on the last page', () => {
    render(<Pagination currentPage={5} totalPages={5} onPageChange={() => {}} />);
    const buttons = screen.getAllByRole('button');
    expect(buttons[buttons.length - 1]).toBeDisabled();
  });

  it('calls onPageChange when a page is clicked', async () => {
    const onPageChange = vi.fn();
    render(<Pagination currentPage={1} totalPages={5} onPageChange={onPageChange} />);
    await userEvent.click(screen.getByText('2'));
    expect(onPageChange).toHaveBeenCalledWith(2);
  });

  it('returns null when there is only one page', () => {
    const { container } = render(<Pagination currentPage={1} totalPages={1} onPageChange={() => {}} />);
    expect(container.firstChild).toBeNull();
  });

  it('uses i18n aria-label on the nav', () => {
    const { container } = render(<Pagination currentPage={1} totalPages={5} onPageChange={() => {}} />);
    expect(container.querySelector('nav')).toHaveAttribute('aria-label', 'التنقل بين الصفحات');
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<Pagination currentPage={1} totalPages={5} onPageChange={() => {}} />);
    expect(await axe(container)).toHaveNoViolations();
  });
});
