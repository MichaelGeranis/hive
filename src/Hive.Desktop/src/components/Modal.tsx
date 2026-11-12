import { ReactNode, useEffect, useId, useRef } from 'react'
import { createPortal } from 'react-dom'
import { X } from 'lucide-react'

const SIZES = {
  sm: 'max-w-md',
  md: 'max-w-lg',
  lg: 'max-w-2xl',
  xl: 'max-w-4xl',
  '2xl': 'max-w-6xl'
} as const

export type ModalSize = keyof typeof SIZES

export interface ModalProps {
  /** Names the dialog; shown as its heading */
  title: string
  subtitle?: ReactNode
  onClose: () => void
  children: ReactNode
  /** Buttons along the bottom, right-aligned */
  footer?: ReactNode
  /** Extra controls in the header, left of the close button */
  headerAction?: ReactNode
  size?: ModalSize
  /** Close on a click outside the dialog. Off by default, so a stray click never loses a form */
  closeOnBackdrop?: boolean
  /** Hide the × in the header, when the footer is the only way out */
  hideClose?: boolean
}

// Open modals, innermost last: only the innermost answers Escape
const openModals: string[] = []

const FOCUSABLE = 'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'

/**
 * The one dialog every page uses: a titled card over a dimmed page. Escape and the × close it,
 * focus moves into it and back to where it was, Tab stays inside it, and the page behind does
 * not scroll.
 */
export function Modal({
  title,
  subtitle,
  onClose,
  children,
  footer,
  headerAction,
  size = 'md',
  closeOnBackdrop = false,
  hideClose = false
}: ModalProps) {
  const id = useId()
  const titleId = `${id}-title`
  const dialogRef = useRef<HTMLDivElement>(null)
  const onCloseRef = useRef(onClose)
  onCloseRef.current = onClose

  useEffect(() => {
    openModals.push(id)
    const previouslyFocused = document.activeElement as HTMLElement | null
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    // Focus the first field, or the dialog itself
    const dialog = dialogRef.current
    const firstField = dialog?.querySelector<HTMLElement>('input:not([disabled]), textarea:not([disabled]), select:not([disabled])')
    ;(firstField ?? dialog)?.focus()

    const onKeyDown = (event: KeyboardEvent) => {
      if (openModals[openModals.length - 1] !== id) return
      if (event.key === 'Escape') {
        event.stopPropagation()
        onCloseRef.current()
      } else if (event.key === 'Tab' && dialogRef.current) {
        const focusable = Array.from(dialogRef.current.querySelectorAll<HTMLElement>(FOCUSABLE))
        if (focusable.length === 0) return
        const first = focusable[0]
        const last = focusable[focusable.length - 1]
        if (event.shiftKey && document.activeElement === first) {
          event.preventDefault()
          last.focus()
        } else if (!event.shiftKey && document.activeElement === last) {
          event.preventDefault()
          first.focus()
        }
      }
    }
    document.addEventListener('keydown', onKeyDown)

    return () => {
      document.removeEventListener('keydown', onKeyDown)
      openModals.splice(openModals.indexOf(id), 1)
      if (openModals.length === 0) document.body.style.overflow = previousOverflow
      previouslyFocused?.focus?.()
    }
  }, [id])

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      onMouseDown={event => {
        if (closeOnBackdrop && event.target === event.currentTarget) onClose()
      }}
    >
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        className={`flex w-full ${SIZES[size]} max-h-[90vh] flex-col rounded-xl border border-slate-200 bg-white shadow-xl outline-hidden`}
      >
        <div className="flex items-start justify-between gap-3 border-b border-slate-100 px-6 py-4">
          <div className="min-w-0">
            <h2 id={titleId} className="text-lg font-semibold text-slate-900">{title}</h2>
            {subtitle && <div className="mt-0.5 text-sm text-slate-500">{subtitle}</div>}
          </div>
          <div className="flex shrink-0 items-center gap-2">
            {headerAction}
            {!hideClose && (
              <button
                type="button"
                onClick={onClose}
                aria-label="Close"
                className="rounded-sm p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
              >
                <X className="h-5 w-5" />
              </button>
            )}
          </div>
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto px-6 py-4">{children}</div>
        {footer && (
          <div className="flex justify-end gap-2 border-t border-slate-100 px-6 py-3">{footer}</div>
        )}
      </div>
    </div>,
    document.body
  )
}

/** The secondary button of a dialog footer */
export function ModalCancelButton({ onClick, children = 'Cancel' }: { onClick: () => void; children?: ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
    >
      {children}
    </button>
  )
}
