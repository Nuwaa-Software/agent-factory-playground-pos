import { render } from '@testing-library/react';
import { axe } from '../test/axe';
import Table from './Table';

describe('Table', () => {
  it('wraps content in overflow-x-auto', () => {
    const { container } = render(
      <Table>
        <thead>
          <tr>
            <th>Name</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>Alice</td>
          </tr>
        </tbody>
      </Table>,
    );
    const wrapper = container.firstChild as HTMLElement;
    expect(wrapper).toHaveClass('overflow-x-auto');
  });

  it('renders thead and tbody', () => {
    const { container } = render(
      <Table>
        <thead>
          <tr>
            <th>Name</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>Alice</td>
          </tr>
        </tbody>
      </Table>,
    );
    expect(container.querySelector('thead')).toBeInTheDocument();
    expect(container.querySelector('tbody')).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = render(
      <Table>
        <thead>
          <tr>
            <th>Name</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>Alice</td>
          </tr>
        </tbody>
      </Table>,
    );
    expect(await axe(container)).toHaveNoViolations();
  });
});
