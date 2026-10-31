import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import Dialog from './Dialog';

describe('Dialog', () => {
  function renderDialog({ open = true }: { open?: boolean } = {}) {
    const onClose = vi.fn();
    const triggerRef = { current: null as HTMLElement | null };

    // Create a trigger button outside the dialog to test focus restore
    const trigger = (
      <button
        ref={(el) => {
          if (el) triggerRef.current = el;
        }}
        id="trigger-btn"
      >
        Open Dialog
      </button>
    );

    return {
      onClose,
      triggerRef,
      ...render(
        <>
          {trigger}
          <Dialog open={open} onClose={onClose} titleId="dialog-title">
            <h2 id="dialog-title">Dialog Title</h2>
            <input id="dialog-input" aria-label="Test input" />
            <button id="dialog-close">Close</button>
          </Dialog>
        </>,
      ),
    };
  }

  it('renders when open', () => {
    renderDialog();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('does not render when closed', () => {
    renderDialog({ open: false });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('has aria-modal="true"', () => {
    const { container } = renderDialog();
    expect(container.querySelector('[role="dialog"]')).toHaveAttribute('aria-modal', 'true');
  });

  it('calls onClose on Escape key', async () => {
    const { onClose } = renderDialog();
    // Dispatch Escape directly on the overlay which has the onKeyDown handler
    const dialogContainer = document.querySelector('[role="presentation"]')!;
    dialogContainer.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('calls onClose on overlay click', async () => {
    const { onClose, container } = renderDialog();
    // Click the overlay (the outer div)
    await userEvent.click(container.querySelector('[role="presentation"]')!);
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('has no accessibility violations', async () => {
    const { container } = renderDialog();
    expect(await axe(container)).toHaveNoViolations();
  });

  it('focuses the first focusable element when opened', async () => {
    renderDialog();
    const dialog = screen.getByRole('dialog');
    // The first focusable element should be the close button (aria-label)
    await waitFor(() => {
      expect(document.activeElement).toBe(dialog.querySelector('button[aria-label]'));
    });
  });

  it('restores focus to trigger on close (controlled open=false)', async () => {
    const onClose = vi.fn();
    const triggerRef = { current: null as HTMLElement | null };

    // Create a trigger button and focus it
    render(
      <button
        ref={(el) => {
          if (el) triggerRef.current = el;
        }}
        id="trigger-btn"
      >
        Open Dialog
      </button>,
    );
    const triggerBtn = screen.getByRole('button');
    triggerBtn.focus();
    expect(document.activeElement).toBe(triggerBtn);

    // Now open the dialog (controlled)
    const { rerender } = render(
      <>
        <button
          ref={(el) => {
            if (el) triggerRef.current = el;
          }}
          id="trigger-btn"
        >
          Open Dialog
        </button>
        <Dialog open onClose={onClose} titleId="dialog-title">
          <h2 id="dialog-title">Dialog Title</h2>
          <input id="dialog-input" aria-label="Test input" />
          <button id="dialog-close">Close</button>
        </Dialog>
      </>,
    );

    // Verify dialog is open and focus moved inside
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    const closeBtn = document.querySelector('button[aria-label]');
    expect(closeBtn).toBe(document.activeElement);

    // Close via controlled open=false (the natural usage path)
    rerender(
      <>
        <button
          ref={(el) => {
            if (el) triggerRef.current = el;
          }}
          id="trigger-btn"
        >
          Open Dialog
        </button>
        <Dialog open={false} onClose={onClose} titleId="dialog-title">
          <h2 id="dialog-title">Dialog Title</h2>
        </Dialog>
      </>,
    );

    // Focus should be restored to the trigger button (component stays mounted)
    await waitFor(() => {
      expect(document.activeElement).toBe(triggerBtn);
    });
  });

  it('restores focus to trigger on unmount', async () => {
    const onClose = vi.fn();
    const triggerRef = { current: null as HTMLElement | null };

    // Create a trigger button and focus it
    render(
      <button
        ref={(el) => {
          if (el) triggerRef.current = el;
        }}
        id="trigger-btn"
      >
        Open Dialog
      </button>,
    );
    const triggerBtn = screen.getByRole('button');
    triggerBtn.focus();
    expect(document.activeElement).toBe(triggerBtn);

    // Now open the dialog and unmount it (simulating conditional rendering)
    const { rerender } = render(
      <>
        <button
          ref={(el) => {
            if (el) triggerRef.current = el;
          }}
          id="trigger-btn"
        >
          Open Dialog
        </button>
        <Dialog open onClose={onClose} titleId="dialog-title">
          <h2 id="dialog-title">Dialog Title</h2>
          <input id="dialog-input" aria-label="Test input" />
          <button id="dialog-close">Close</button>
        </Dialog>
      </>,
    );

    // Verify dialog is open and focus moved inside
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    const closeBtn = document.querySelector('button[aria-label]');
    expect(closeBtn).toBe(document.activeElement);

    // Unmount the entire tree (simulating conditional rendering)
    rerender(<button id="trigger-btn">Open Dialog</button>);

    // Focus should be restored to the trigger button on unmount
    await waitFor(() => {
      expect(document.activeElement).toBe(triggerBtn);
    });
  });

  it('implements focus trap (Tab cycles within dialog)', async () => {
    renderDialog();
    const dialog = screen.getByRole('dialog');

    // Focus is already on the close button from focus-on-open
    expect(dialog.querySelector('button[aria-label]')).toBe(document.activeElement);

    // Tab should cycle to the next focusable element (the input)
    await userEvent.tab();
    expect(document.activeElement).toBe(dialog.querySelector('input'));

    // Shift+Tab should cycle back to the close button
    await userEvent.tab({ shift: true });
    expect(document.activeElement).toBe(dialog.querySelector('button[aria-label]'));
  });
});
