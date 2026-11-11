import { useCallback, useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  AlertTriangle,
  Check,
  ChevronLeft,
  ChevronRight,
  Download,
  Loader2,
  MoreHorizontal,
  Plus,
  RefreshCw,
  Target
} from 'lucide-react'
import { Card } from '../components/Card'
import { quarterlyPlanningApi } from '../services/api'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { useConfirm } from '../contexts/ConfirmContext'
import { Modal, ModalCancelButton } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { LoadingState, EmptyState } from '../components/PageState'
import { formatDate } from '../utils/dateUtils'
import { formatEffort } from '../utils/planningDisplay'
import { QuarterStatus } from '../types/quarterlyPlanning'
import type { PlanningBoard, Quarter } from '../types/quarterlyPlanning'
import PlanningOptions from '../components/PlanningOptions'
import PlanningCapacity from '../components/PlanningCapacity'
import AllocationGrid from '../components/AllocationGrid'
import PlanningReview from '../components/PlanningReview'
import OptionDrawer from '../components/OptionDrawer'

type Step = 'options' | 'capacity' | 'allocation' | 'review'

const STEPS: { key: Step; title: string; hint: string }[] = [
  { key: 'options', title: 'Options', hint: 'What the PM proposes, discovered and designed' },
  { key: 'capacity', title: 'Capacity', hint: 'Who is here, the mix, and what fits' },
  { key: 'allocation', title: 'Allocation', hint: 'People on options, sprint by sprint' },
  { key: 'review', title: 'Commit & review', hint: 'The commitment, then how it went' }
]

const QUARTER_STATUS_LABELS: Record<number, string> = {
  [QuarterStatus.Planning]: 'Planning',
  [QuarterStatus.Active]: 'Active',
  [QuarterStatus.Completed]: 'Completed'
}

/** What each step still needs, read from the server's checklist. */
function stepState(board: PlanningBoard, step: Step): { done: boolean; note: string } {
  const c = board.checklist
  switch (step) {
    case 'options': {
      const open = [
        c.unsizedCount && `${c.unsizedCount} unsized`,
        c.inDiscoveryCount && `${c.inDiscoveryCount} in discovery`,
        c.notReadyCount && `${c.notReadyCount} not ready`,
        c.openDependencyCount && `${c.openDependencyCount} open dependencies`
      ].filter(Boolean)
      return {
        done: c.optionCount > 0 && open.length === 0,
        note: c.optionCount === 0 ? 'No options yet' : open.length ? open.join(' · ') : `${c.optionCount} option${c.optionCount === 1 ? '' : 's'} ready`
      }
    }
    case 'capacity':
      return {
        done: c.hasSprints && c.hasRoster,
        note: !c.hasSprints
          ? 'No sprints for this quarter'
          : `${formatEffort(board.capacity.totalSupply)} dev-sprints · ${formatEffort(board.waterline.capacity)} for options`
      }
    case 'allocation': {
      const open = [
        c.withoutLeadCount && `${c.withoutLeadCount} without a lead`,
        c.unallocatedCommittedCount && `${c.unallocatedCommittedCount} committed, not planned`,
        board.mix.hasPlan && !c.mixOnTarget && 'mix off target'
      ].filter(Boolean)
      return {
        done: board.mix.hasPlan && open.length === 0,
        note: !board.mix.hasPlan ? 'Nothing planned yet' : open.length ? open.join(' · ') : 'Planned, mix on target'
      }
    }
    case 'review':
      return {
        done: c.committed,
        note: board.quarter.committedAt ? `Committed ${formatDate(board.quarter.committedAt)}` : `${c.undecidedCount} undecided`
      }
  }
}

export default function QuarterlyPlanning() {
  const { showError, showSuccess } = useToast()
  const confirm = useConfirm()
  const [searchParams, setSearchParams] = useSearchParams()
  const step = (STEPS.some(s => s.key === searchParams.get('step')) ? searchParams.get('step') : 'options') as Step
  const openOptionId = searchParams.get('option')

  const [quarters, setQuarters] = useState<Quarter[]>([])
  const [quarterId, setQuarterId] = useState<string | null>(null)
  const [board, setBoard] = useState<PlanningBoard | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [menuOpen, setMenuOpen] = useState(false)
  const [exporting, setExporting] = useState(false)

  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(searchParams)
    if (value) next.set(key, value)
    else next.delete(key)
    setSearchParams(next, { replace: true })
  }

  const loadBoard = useCallback(async (id: string) => {
    try {
      setError(null)
      setBoard(await quarterlyPlanningApi.getBoard(id))
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [])

  const loadQuarters = useCallback(async (select?: string) => {
    try {
      const data = await quarterlyPlanningApi.getAllQuarters()
      setQuarters(data)
      const chosen = select
        ?? data.find(q => q.status === QuarterStatus.Planning)?.id
        ?? data.find(q => q.status === QuarterStatus.Active)?.id
        ?? data[0]?.id
        ?? null
      setQuarterId(chosen)
      if (!chosen) setLoading(false)
    } catch (err) {
      setError(getErrorMessage(err))
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadQuarters()
  }, [loadQuarters])

  useEffect(() => {
    if (quarterId) {
      setLoading(true)
      loadBoard(quarterId)
    }
  }, [quarterId, loadBoard])

  const reload = useCallback(async () => {
    if (quarterId) await loadBoard(quarterId)
  }, [quarterId, loadBoard])

  /** Runs a change, shows what went wrong, and reloads the board either way. */
  const run = useCallback(async (action: () => Promise<unknown>, success?: string) => {
    try {
      await action()
      if (success) showSuccess(success)
    } catch (err) {
      showError(getErrorMessage(err))
    } finally {
      await reload()
    }
  }, [reload, showError, showSuccess])

  const index = quarters.findIndex(q => q.id === quarterId)
  const quarter = board?.quarter ?? quarters[index]
  const openOption = useMemo(
    () => board?.initiatives.find(i => i.id === openOptionId) ?? null,
    [board, openOptionId]
  )

  const handleExport = async () => {
    if (!quarterId || !quarter) return
    try {
      setExporting(true)
      const blob = await quarterlyPlanningApi.exportToExcel(quarterId)
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `${quarter.name.replace(' ', '-')}_Plan-Catalogue.xlsx`
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)
    } catch (err) {
      showError(getErrorMessage(err))
    } finally {
      setExporting(false)
    }
  }

  const quarterAction = async (action: 'activate' | 'complete' | 'delete') => {
    if (!quarterId || !quarter) return
    setMenuOpen(false)
    if (action === 'delete' && !(await confirm({
      title: `Delete ${quarter.name}?`,
      message: 'Everything planned in it goes too: options, lanes, people and allocations.',
      confirmLabel: 'Delete'
    }))) return
    try {
      if (action === 'activate') await quarterlyPlanningApi.activateQuarter(quarterId)
      if (action === 'complete') await quarterlyPlanningApi.completeQuarter(quarterId)
      if (action === 'delete') {
        await quarterlyPlanningApi.deleteQuarter(quarterId)
        await loadQuarters()
        return
      }
      await loadQuarters(quarterId)
      await reload()
    } catch (err) {
      showError(getErrorMessage(err))
    }
  }

  if (loading && !board) {
    return <LoadingState fill />
  }

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title="Quarterly Planning"
        icon={<Target className="w-6 h-6 text-amber-500" />}
        actions={
          <>
            {quarters.length > 0 && (
              <div className="flex items-center gap-1">
                <button
                  onClick={() => setQuarterId(quarters[index + 1]?.id ?? quarterId)}
                  disabled={index >= quarters.length - 1}
                  className="p-1 rounded-sm hover:bg-slate-200 disabled:opacity-40"
                  title="Earlier quarter"
                >
                  <ChevronLeft className="w-5 h-5" />
                </button>
                <select
                  aria-label="Quarter"
                  value={quarterId ?? ''}
                  onChange={e => setQuarterId(e.target.value)}
                  className="px-3 py-1 border border-slate-300 rounded-lg bg-white text-slate-800"
                >
                  {quarters.map(q => (
                    <option key={q.id} value={q.id}>
                      {q.name} · {QUARTER_STATUS_LABELS[q.status]}
                    </option>
                  ))}
                </select>
                <button
                  onClick={() => setQuarterId(quarters[index - 1]?.id ?? quarterId)}
                  disabled={index <= 0}
                  className="p-1 rounded-sm hover:bg-slate-200 disabled:opacity-40"
                  title="Later quarter"
                >
                  <ChevronRight className="w-5 h-5" />
                </button>
              </div>
            )}
            <button onClick={reload} className="p-2 rounded-lg hover:bg-slate-200" title="Refresh">
              <RefreshCw className={`w-5 h-5 ${loading ? 'animate-spin' : ''}`} />
            </button>
            {quarterId && (
              <>
                <button
                  onClick={handleExport}
                  disabled={exporting}
                  className="flex items-center gap-2 px-3 py-2 border border-slate-300 bg-white rounded-lg hover:bg-slate-50 disabled:opacity-50"
                  title="Download the Plan-Catalogue spreadsheet"
                >
                  {exporting ? <Loader2 className="w-4 h-4 animate-spin" /> : <Download className="w-4 h-4" />}
                  Export
                </button>
                <div className="relative">
                  <button onClick={() => setMenuOpen(o => !o)} className="p-2 rounded-lg hover:bg-slate-200" aria-label="Quarter actions">
                    <MoreHorizontal className="w-5 h-5" />
                  </button>
                  {menuOpen && (
                    <div className="absolute right-0 mt-1 w-48 bg-white border border-slate-200 rounded-lg shadow-lg z-30 py-1 text-sm">
                      {quarter?.status !== QuarterStatus.Active && (
                        <button className="block w-full text-left px-3 py-2 hover:bg-slate-50" onClick={() => quarterAction('activate')}>
                          Start the quarter
                        </button>
                      )}
                      {quarter?.status === QuarterStatus.Active && (
                        <button className="block w-full text-left px-3 py-2 hover:bg-slate-50" onClick={() => quarterAction('complete')}>
                          Complete the quarter
                        </button>
                      )}
                      <button className="block w-full text-left px-3 py-2 text-red-600 hover:bg-red-50" onClick={() => quarterAction('delete')}>
                        Delete quarter
                      </button>
                    </div>
                  )}
                </div>
              </>
            )}
            <button
              onClick={() => setShowCreate(true)}
              className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
            >
              <Plus className="w-4 h-4" />
              New Quarter
            </button>
          </>
        }
      />

      {error && (
        <div className="p-3 bg-red-50 border border-red-200 rounded-lg flex items-center gap-2 text-red-700 text-sm">
          <AlertTriangle className="w-4 h-4" />
          {error}
        </div>
      )}

      {quarters.length === 0 ? (
        <Card>
          <EmptyState
            icon={<Target />}
            title="No quarters yet"
            description="Create the next quarter one or two sprints before the current one ends."
            action={
              <button
                onClick={() => setShowCreate(true)}
                className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
              >
                <Plus className="w-4 h-4" />
                Create Quarter
              </button>
            }
          />
        </Card>
      ) : board && (
        <>
          {/* Stepper */}
          <nav className="grid grid-cols-2 lg:grid-cols-4 gap-2" aria-label="Planning steps">
            {STEPS.map((s, i) => {
              const state = stepState(board, s.key)
              const active = s.key === step
              return (
                <button
                  key={s.key}
                  onClick={() => setParam('step', s.key)}
                  className={`text-left rounded-xl border px-4 py-3 transition-colors ${
                    active ? 'border-amber-400 bg-amber-50 ring-1 ring-amber-300' : 'border-slate-200 bg-white hover:bg-slate-50'
                  }`}
                  aria-current={active ? 'step' : undefined}
                >
                  <div className="flex items-center gap-2">
                    <span
                      className={`flex items-center justify-center w-6 h-6 rounded-full text-xs font-semibold ${
                        state.done ? 'bg-green-500 text-white' : active ? 'bg-amber-500 text-white' : 'bg-slate-200 text-slate-600'
                      }`}
                    >
                      {state.done ? <Check className="w-3.5 h-3.5" /> : i + 1}
                    </span>
                    <span className="font-semibold text-slate-800">{s.title}</span>
                  </div>
                  <p className="text-xs text-slate-500 mt-1 truncate" title={state.note}>{state.note}</p>
                </button>
              )
            })}
          </nav>

          <div>
            {step === 'options' && (
              <PlanningOptions board={board} run={run} onOpen={id => setParam('option', id)} />
            )}
            {step === 'capacity' && (
              <PlanningCapacity board={board} run={run} onOpen={id => setParam('option', id)} />
            )}
            {step === 'allocation' && (
              <AllocationGrid board={board} run={run} onOpen={id => setParam('option', id)} />
            )}
            {step === 'review' && (
              <PlanningReview board={board} run={run} onOpen={id => setParam('option', id)} />
            )}
          </div>
        </>
      )}

      {openOption && board && (
        <OptionDrawer
          key={openOption.id}
          board={board}
          initiative={openOption}
          run={run}
          onClose={() => setParam('option', null)}
        />
      )}

      {showCreate && (
        <CreateQuarterModal
          quarters={quarters}
          onClose={() => setShowCreate(false)}
          onCreated={async id => {
            setShowCreate(false)
            await loadQuarters(id)
          }}
        />
      )}
    </div>
  )
}

function CreateQuarterModal({
  quarters,
  onClose,
  onCreated
}: {
  quarters: Quarter[]
  onClose: () => void
  onCreated: (id: string) => void
}) {
  const { showError } = useToast()

  // Suggest the quarter after the latest one, or the next calendar quarter
  const latest = quarters[0]
  const now = new Date()
  const suggestion = latest
    ? latest.quarterNumber === 4 ? { year: latest.year + 1, number: 1 } : { year: latest.year, number: latest.quarterNumber + 1 }
    : Math.ceil((now.getMonth() + 1) / 3) === 4
      ? { year: now.getFullYear() + 1, number: 1 }
      : { year: now.getFullYear(), number: Math.ceil((now.getMonth() + 1) / 3) + 1 }

  const [year, setYear] = useState(suggestion.year)
  const [number, setNumber] = useState(suggestion.number)
  const [okr, setOkr] = useState('')
  const [saving, setSaving] = useState(false)

  const create = async () => {
    try {
      setSaving(true)
      const created = await quarterlyPlanningApi.createQuarter({ year, quarterNumber: number, okrReference: okr || undefined })
      onCreated(created.id)
    } catch (err) {
      showError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      title="New quarter"
      subtitle="It starts with the Maintenance and Tech roadmap lanes and a 50 / 20 / 30 mix."
      onClose={onClose}
      size="sm"
      footer={
        <>
          <ModalCancelButton onClick={onClose} />
          <button
            onClick={create}
            disabled={saving}
            className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 disabled:opacity-50"
          >
            {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
            Create
          </button>
        </>
      }
    >
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <label className="block text-sm font-medium text-slate-700">
            Year
            <input
              type="number"
              value={year}
              onChange={e => setYear(parseInt(e.target.value))}
              className="mt-1 w-full px-3 py-2 border border-slate-300 rounded-lg"
            />
          </label>
          <label className="block text-sm font-medium text-slate-700">
            Quarter
            <select
              value={number}
              onChange={e => setNumber(parseInt(e.target.value))}
              className="mt-1 w-full px-3 py-2 border border-slate-300 rounded-lg bg-white"
            >
              {[1, 2, 3, 4].map(n => <option key={n} value={n}>Q{n}</option>)}
            </select>
          </label>
        </div>
        <label className="block text-sm font-medium text-slate-700">
          OKR reference (optional)
          <input
            type="text"
            value={okr}
            onChange={e => setOkr(e.target.value)}
            placeholder="Link to company OKRs…"
            className="mt-1 w-full px-3 py-2 border border-slate-300 rounded-lg"
          />
        </label>
      </div>
    </Modal>
  )
}
