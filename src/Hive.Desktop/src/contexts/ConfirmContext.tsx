import { createContext, ReactNode, useCallback, useContext, useRef, useState } from 'react'
import { Modal, ModalCancelButton } from '../components/Modal'

export interface ConfirmOptions {
  /** The question, e.g. "Delete this task?" */
  title: string
  /** What happens, and what is lost */
  message?: ReactNode
  /** The button that goes ahead, e.g. "Delete" */
  confirmLabel?: string
  cancelLabel?: string
  /** Red for anything that deletes or cannot be undone; the default */
  tone?: 'danger' | 'default'
}

type Confirm = (options: ConfirmOptions) => Promise<boolean>

const ConfirmContext = createContext<Confirm | undefined>(undefined)

/**
 * Asks before something happens, in Hive's own dialog rather than the browser's:
 * `if (!(await confirm({ title: 'Delete this task?', confirmLabel: 'Delete' }))) return`.
 */
export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [options, setOptions] = useState<ConfirmOptions | null>(null)
  const resolveRef = useRef<((answer: boolean) => void) | null>(null)

  const confirm = useCallback<Confirm>(next => {
    // A question still open is answered "no" before the next one replaces it
    resolveRef.current?.(false)
    setOptions(next)
    return new Promise<boolean>(resolve => {
      resolveRef.current = resolve
    })
  }, [])

  const answer = (value: boolean) => {
    resolveRef.current?.(value)
    resolveRef.current = null
    setOptions(null)
  }

  const danger = (options?.tone ?? 'danger') === 'danger'

  return (
    <ConfirmContext.Provider value={confirm}>
      {children}
      {options && (
        <Modal
          title={options.title}
          onClose={() => answer(false)}
          size="sm"
          footer={
            <>
              <ModalCancelButton onClick={() => answer(false)}>{options.cancelLabel ?? 'Cancel'}</ModalCancelButton>
              <button
                type="button"
                onClick={() => answer(true)}
                className={`rounded-lg px-4 py-2 text-sm font-medium text-white ${
                  danger ? 'bg-red-600 hover:bg-red-700' : 'bg-amber-500 hover:bg-amber-600'
                }`}
              >
                {options.confirmLabel ?? 'OK'}
              </button>
            </>
          }
        >
          {options.message && <div className="whitespace-pre-line text-sm text-slate-600">{options.message}</div>}
        </Modal>
      )}
    </ConfirmContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components -- the hook belongs with its provider
export function useConfirm(): Confirm {
  const confirm = useContext(ConfirmContext)
  if (confirm === undefined) {
    throw new Error('useConfirm must be used within a ConfirmProvider')
  }
  return confirm
}
