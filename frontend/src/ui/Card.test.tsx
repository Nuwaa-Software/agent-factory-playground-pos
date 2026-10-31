import { render, screen } from '@testing-library/react';
import { axe } from '../test/axe';
import Card from './Card';

describe('Card', () => {
  it('renders children', () => {
    render(<Card><p>Content</p></Card>);
    expect(screen.getByText('Content')).toBeInTheDocument();
  });

  it('applies the correct base classes', () => {
    const { container } = render(<Card><p>Content</p></Card>);
    const card = container.firstChild as HTMLElement;
    expect(card).toHaveClass('bg-white');
    expect(card).toHaveClass('rounded-lg');
    expect(card).toHaveClass('shadow-sm');
    expect(card).toHaveClass('p-4');
  });

  it('merges custom className', () => {
    const { container } = render(<Card className="custom-class"><p>Content</p></Card>);
    const card = container.firstChild as HTMLElement;
    expect(card).toHaveClass('custom-class');
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<Card><p>Content</p></Card>);
    expect(await axe(container)).toHaveNoViolations();
  });
});
