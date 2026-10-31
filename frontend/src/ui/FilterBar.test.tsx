import { render, screen } from '@testing-library/react';
import { axe } from '../test/axe';
import FilterBar from './FilterBar';

describe('FilterBar', () => {
  it('renders children', () => {
    render(
      <FilterBar>
        <span>Filter 1</span>
        <span>Filter 2</span>
      </FilterBar>,
    );
    expect(screen.getByText('Filter 1')).toBeInTheDocument();
    expect(screen.getByText('Filter 2')).toBeInTheDocument();
  });

  it('has role="group"', () => {
    const { container } = render(<FilterBar><span>F</span></FilterBar>);
    expect(container.firstChild).toHaveAttribute('role', 'group');
  });

  it('uses i18n aria-label', () => {
    const { container } = render(<FilterBar><span>F</span></FilterBar>);
    expect(container.firstChild).toHaveAttribute('aria-label', 'التصفية');
  });

  it('applies flex-wrap for mobile wrapping', () => {
    const { container } = render(<FilterBar><span>F</span></FilterBar>);
    expect(container.firstChild).toHaveClass('flex-wrap');
  });

  it('has no accessibility violations', async () => {
    const { container } = render(
      <FilterBar>
        <span>Filter</span>
      </FilterBar>,
    );
    expect(await axe(container)).toHaveNoViolations();
  });
});
