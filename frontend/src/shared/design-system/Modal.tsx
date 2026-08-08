import { useEffect, useId, useRef, type MouseEvent, type ReactNode } from "react";

export interface ModalProps {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
}

/**
 * Built on the native `<dialog>` element: focus-trapping, Escape-to-close and top-layer
 * stacking all come from the browser rather than being reimplemented.
 */
export function Modal({ open, onClose, title, children }: ModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) {
      return;
    }

    if (open && !dialog.open) {
      dialog.showModal();
    } else if (!open && dialog.open) {
      dialog.close();
    }
  }, [open]);

  const handleBackdropClick = (event: MouseEvent<HTMLDialogElement>) => {
    if (event.target === dialogRef.current) {
      onClose();
    }
  };

  return (
    <dialog
      ref={dialogRef}
      tabIndex={-1}
      onClose={onClose}
      onCancel={onClose}
      onClick={handleBackdropClick}
      aria-labelledby={titleId}
      className="rounded-lg border border-border-default bg-surface p-6 shadow-lg backdrop:bg-black/40"
    >
      <h2 id={titleId} className="text-lg font-semibold text-text-primary">
        {title}
      </h2>
      <div className="mt-4">{children}</div>
    </dialog>
  );
}
