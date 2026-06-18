import * as Dialog from '@radix-ui/react-dialog';
import Button from './Button.jsx';

export default function ConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  destructive = false,
  loading = false,
  onConfirm,
}) {
  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="dialog-overlay" />
        <Dialog.Content className="dialog-content" onEscapeKeyDown={loading ? (e) => e.preventDefault() : undefined}>
          <Dialog.Title className="dialog-title">{title}</Dialog.Title>
          {description && <Dialog.Description className="dialog-description">{description}</Dialog.Description>}
          <div className="dialog-actions">
            <Dialog.Close asChild>
              <Button variant="secondary" disabled={loading}>{cancelLabel}</Button>
            </Dialog.Close>
            <Button variant={destructive ? 'danger' : 'primary'} loading={loading} onClick={onConfirm}>
              {confirmLabel}
            </Button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
