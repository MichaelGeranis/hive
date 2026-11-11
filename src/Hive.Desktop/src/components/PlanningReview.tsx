import { useEffect, useState } from 'react'
import { CheckCircle2, ClipboardCopy, Forward, Lock, PackageCheck } from 'lucide-react'
import { Card, CardContent, CardHeader } from './Card'
import { quarterlyPlanningApi } from '../services/api'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { formatDateTime } from '../utils/dateUtils'
import { STATUS_LABELS, STATUS_STYLES, formatEffort } from '../utils/planningDisplay'
import { InitiativeStatus } from '../types/quarterlyPlanning'
import type { Calibration, PlanReview, PlanningBoard } from '../types/quarterlyPlanning'
import type { RunChange } from '../utils/planningDisplay'

interface PlanningReviewProps {
  board: PlanningBoard
  run: RunChange
  onOpen: (initiativeId: string) => void
}

/**
 * Commit the plan, share it, then hold the quarter against it: each option against its
 * baseline, and how sizes compare with what they took.
 */
export default function PlanningReview({ board, run, onOpen }: PlanningReviewProps) {
  const { showError, showSuccess } = useToast()
  const [review, setReview] = useState<PlanReview | null>(null)
  const [calibration, setCalibration] = useState<Calibration | null>(null)

  useEffect(() => {
    let cancelled = false
    Promise.all([quarterlyPlanningApi.getReview(board.quarter.id), quarterlyPlanningApi.getCalibration()])
      .then(([r, c]) => {
        if (!cancelled) {
          setReview(r)
          setCalibration(c)
        }
      })
      .catch(err => showError(getErrorMessage(err)))
    return () => {
      cancelled = true
    }
  }, [board, showError])

  const c = board.checklist
  const blockers = [
    c.unsizedCount > 0 && `${c.unsizedCount} option${c.unsizedCount === 1 ? '' : 's'} without a size`,
    c.undecidedCount > 0 && `${c.undecidedCount} not decided: commit or drop`,
    c.notReadyCount > 0 && `${c.notReadyCount} still waiting on the product side`,
    c.openDependencyCount > 0 && `${c.openDependencyCount} dependencies on other teams not agreed`,
    c.withoutLeadCount > 0 && `${c.withoutLeadCount} without a lead`,
    c.unallocatedCommittedCount > 0 && `${c.unallocatedCommittedCount} committed with nobody planned`
  ].filter(Boolean) as string[]

  const copySummary = async () => {
    if (!review) return
    try {
      await navigator.clipboard.writeText(review.summary)
      showSuccess('Summary copied')
    } catch (err) {
      showError(getErrorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader
          title="Commitment"
          subtitle={
            board.quarter.committedAt
              ? `Committed ${formatDateTime(board.quarter.committedAt)}. Committing again replaces the baseline.`
              : 'Committing records the plan as it stands, to compare the quarter against.'
          }
          action={
            <button
              onClick={() => run(() => quarterlyPlanningApi.commit(board.quarter.id), 'Plan committed')}
              className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700"
            >
              <Lock className="w-4 h-4" />
              {board.quarter.committedAt ? 'Commit again' : 'Commit the plan'}
            </button>
          }
        />
        <CardContent>
          {blockers.length === 0 ? (
            <p className="flex items-center gap-2 text-sm text-green-700">
              <CheckCircle2 className="w-4 h-4" /> Every option is sized, decided, ready and led.
            </p>
          ) : (
            <ul className="text-sm text-amber-800 list-disc ml-5 space-y-0.5">
              {blockers.map(b => <li key={b}>{b}</li>)}
            </ul>
          )}
          {review && (
            <div className="mt-4">
              <div className="flex items-center justify-between mb-1">
                <p className="text-sm font-medium text-slate-700">For the PM</p>
                <button onClick={copySummary} className="flex items-center gap-1 text-sm text-slate-600 hover:text-slate-900">
                  <ClipboardCopy className="w-4 h-4" /> Copy
                </button>
              </div>
              <pre className="whitespace-pre-wrap text-xs bg-slate-50 border border-slate-200 rounded-lg p-3 text-slate-700 font-sans">{review.summary}</pre>
            </div>
          )}
        </CardContent>
      </Card>

      {review && (
        <>
          <Card>
            <CardHeader title="Options against the commitment" subtitle="How each option stood when committed, and where it is now." />
            <CardContent className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="text-xs text-slate-500">
                  <tr>
                    <th className="text-left py-1">Option</th>
                    <th className="text-left">At commit</th>
                    <th className="text-left">Now</th>
                    <th className="text-right">Planned then → now</th>
                    <th className="text-left px-3">Done by</th>
                    <th />
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {review.options.map(option => {
                    const then = option.atCommit
                    const takenOn = option.statusNow === InitiativeStatus.Committed || option.statusNow === InitiativeStatus.Stretch
                    return (
                      <tr key={option.initiativeId}>
                        <td className="py-1.5">
                          <button onClick={() => onOpen(option.initiativeId)} className="flex items-center gap-2 hover:underline">
                            <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: option.color }} />
                            {option.name}
                          </button>
                        </td>
                        <td>
                          {then ? (
                            <span className={`text-[11px] px-1.5 py-0.5 rounded-full ${STATUS_STYLES[then.status]}`}>
                              {STATUS_LABELS[then.status]}{then.aboveLine ? '' : ' · below line'}
                            </span>
                          ) : <span className="text-xs text-slate-400">not in the baseline</span>}
                        </td>
                        <td>
                          <span className={`text-[11px] px-1.5 py-0.5 rounded-full ${STATUS_STYLES[option.statusNow]}`}>{STATUS_LABELS[option.statusNow]}</span>
                        </td>
                        <td className="text-right tabular-nums">
                          {then ? formatEffort(then.allocated) : '—'} → {formatEffort(option.allocatedNow)}
                        </td>
                        <td className="px-3 text-slate-600">
                          {then?.doneSprintName ?? '—'} → {option.doneSprintNow ?? (option.coveredNow != null ? `${option.coveredNow}%` : '—')}
                        </td>
                        <td className="text-right whitespace-nowrap pl-2">
                          {takenOn && (
                            <button
                              onClick={() => run(() => quarterlyPlanningApi.changeStatus(option.initiativeId, InitiativeStatus.Delivered), `${option.name} delivered`)}
                              className="p-1 text-slate-400 hover:text-green-700"
                              title="Mark delivered"
                            >
                              <PackageCheck className="w-4 h-4" />
                            </button>
                          )}
                          {option.statusNow !== InitiativeStatus.Delivered && option.statusNow !== InitiativeStatus.Dropped && (
                            <button
                              onClick={() => run(() => quarterlyPlanningApi.carryOver(option.initiativeId), `${option.name} carried over to next quarter`)}
                              className="p-1 text-slate-400 hover:text-amber-700"
                              title="Carry over to next quarter as a leftover"
                            >
                              <Forward className="w-4 h-4" />
                            </button>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </CardContent>
          </Card>
        </>
      )}

      {calibration && (
        <Card>
          <CardHeader
            title="Estimation calibration"
            subtitle="Delivered options across quarters: what their size stands for, against the dev-sprints their plan gave them."
          />
          <CardContent>
            {calibration.deliveredCount === 0 ? (
              <p className="text-sm text-slate-500">Nothing delivered yet. Mark options delivered at the end of a quarter to see how sizes hold up.</p>
            ) : (
              <table className="w-full text-sm">
                <thead className="text-xs text-slate-500">
                  <tr>
                    <th className="text-left py-1">Size</th>
                    <th className="text-right">Estimate</th>
                    <th className="text-right">Delivered</th>
                    <th className="text-right">Took on average</th>
                    <th className="text-right">Ratio</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {calibration.sizes.map(row => (
                    <tr key={row.size}>
                      <td className="py-1 font-medium">{row.size}</td>
                      <td className="text-right tabular-nums">{formatEffort(row.estimate)}</td>
                      <td className="text-right tabular-nums">{row.deliveredCount}</td>
                      <td className="text-right tabular-nums">{formatEffort(row.averageActual)}</td>
                      <td className={`text-right tabular-nums ${row.ratio != null && Math.abs(row.ratio - 1) >= 0.25 ? 'text-amber-700 font-semibold' : ''}`}>
                        {row.ratio != null ? `×${row.ratio.toFixed(2)}` : '—'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  )
}
