import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Crosshair, Map as MapIcon, Plus, Route, UserPlus } from 'lucide-react'
import { Card, CardContent } from '../components/Card'
import CareerPlanDetail from '../components/CareerPlanDetail'
import SkillRoadmaps from '../components/SkillRoadmaps'
import { Modal, ModalButtons, StageSelect, inputClass, labelClass } from '../components/CareerForms'
import { QuietChip, StageChip } from '../components/CareerGame'
import { careerPlansApi, skillRoadmapsApi } from '../services/api'
import type { CareerAttention, CareerPlanSummary, CareerStage, SkillRoadmapSummary } from '../types'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { formatDate } from '../utils/dateUtils'
import { PageHeader } from '../components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../components/PageState'

type Tab = 'plans' | 'roadmaps'

/**
 * Career plans: one per direct report, tracking the skills they have, are working on, and do
 * not have yet, read against their own role's stage. For the manager's eyes only, and never
 * a comparison between people. `?plan=<id>` opens a plan, `?tab=roadmaps` the roadmaps and
 * `&roadmap=<id>` one of them.
 */
export default function CareerPlans() {
  const [searchParams, setSearchParams] = useSearchParams()
  const tab: Tab = searchParams.get('tab') === 'roadmaps' ? 'roadmaps' : 'plans'
  const planId = searchParams.get('plan')
  const roadmapId = searchParams.get('roadmap')

  const [plans, setPlans] = useState<CareerPlanSummary[]>([])
  const [withoutPlan, setWithoutPlan] = useState<CareerAttention[]>([])
  const [roadmaps, setRoadmaps] = useState<SkillRoadmapSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  // The modal is open with this person chosen ('' for nobody yet), or closed (null)
  const [newPlanFor, setNewPlanFor] = useState<string | null>(null)

  const loadPlans = useCallback(async () => {
    const [all, overview] = await Promise.all([careerPlansApi.getAll(), careerPlansApi.getOverview()])
    setPlans(all)
    setWithoutPlan(overview.directReportsWithoutPlan)
  }, [])

  const loadRoadmaps = useCallback(async () => {
    setRoadmaps(await skillRoadmapsApi.getAll())
  }, [])

  useEffect(() => {
    Promise.all([loadPlans(), loadRoadmaps()])
      .catch(err => {
        console.error('Failed to load career plans:', err)
        setError('Failed to load career plans. Make sure the API is running.')
      })
      .finally(() => setLoading(false))
  }, [loadPlans, loadRoadmaps])

  const navigateTo = (params: Record<string, string>) => setSearchParams(params)

  const quietReload = (load: () => Promise<void>) => async () => {
    try {
      await load()
    } catch (err) {
      console.error(err)
    }
  }

  if (loading) {
    return <LoadingState fill />
  }

  if (error) {
    return <ErrorState what="career plans" detail="Make sure the API is running." />
  }

  if (tab === 'plans' && planId) {
    return (
      <CareerPlanDetail
        planId={planId}
        onBack={() => navigateTo({})}
        onChanged={quietReload(loadPlans)}
      />
    )
  }

  const canCreate = roadmaps.length > 0 && withoutPlan.length > 0

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title={<>Career Plans<Route className="w-6 h-6 text-amber-500" /></>}
        subtitle="What each direct report has, is working on, and has not yet. For your eyes only"
        actions={tab === 'plans' && (
          <button
            onClick={() => setNewPlanFor('')}
            disabled={!canCreate}
            title={roadmaps.length === 0 ? 'Add a roadmap first' : withoutPlan.length === 0 ? 'Everyone has a plan' : undefined}
            className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <Plus className="w-5 h-5" />
            New plan
          </button>
        )}
      />

      {/* Tabs */}
      <div role="tablist" aria-label="Career plans" className="flex gap-1 border-b border-slate-200">
        {([['plans', 'Plans', Route], ['roadmaps', 'Roadmaps', MapIcon]] as const).map(([key, label, Icon]) => (
          <button
            key={key}
            role="tab"
            aria-selected={tab === key}
            onClick={() => navigateTo(key === 'plans' ? {} : { tab: key })}
            className={`flex items-center gap-2 px-4 py-2 -mb-px text-sm font-medium border-b-2 transition-colors ${
              tab === key ? 'border-amber-500 text-amber-700' : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            <Icon className="w-4 h-4" />
            {label}
          </button>
        ))}
      </div>

      {tab === 'roadmaps' ? (
        <SkillRoadmaps
          roadmaps={roadmaps}
          roadmapId={roadmapId}
          onOpenRoadmap={id => navigateTo(id ? { tab: 'roadmaps', roadmap: id } : { tab: 'roadmaps' })}
          onRoadmapsChanged={quietReload(loadRoadmaps)}
        />
      ) : (
        <>
          {roadmaps.length === 0 && (
            <Card className="border-amber-200 bg-amber-50/50">
              <CardContent className="flex flex-wrap items-center justify-between gap-4">
                <div className="flex items-center gap-3">
                  <MapIcon className="w-8 h-8 text-amber-500" />
                  <div>
                    <p className="font-semibold text-slate-900">Start with a roadmap</p>
                    <p className="text-sm text-slate-600">A plan copies its skills from a skill roadmap. Add one from the library in a click.</p>
                  </div>
                </div>
                <button
                  onClick={() => navigateTo({ tab: 'roadmaps' })}
                  className="px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
                >
                  Browse the library
                </button>
              </CardContent>
            </Card>
          )}

          {withoutPlan.length > 0 && (
            <div className="flex flex-wrap items-center gap-2 text-sm" aria-label="No plan yet">
              <span className="font-medium text-slate-700">No plan yet:</span>
              {withoutPlan.map(person => (
                <button
                  key={person.directReportId}
                  type="button"
                  onClick={() => setNewPlanFor(person.directReportId)}
                  disabled={roadmaps.length === 0}
                  aria-label={`Create plan for ${person.fullName}`}
                  className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full border border-dashed border-slate-300 text-slate-700 hover:border-amber-400 hover:text-amber-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <UserPlus className="w-3.5 h-3.5" />
                  {person.fullName}
                </button>
              ))}
            </div>
          )}

          {plans.length === 0 ? (
            <Card>
              <EmptyState title="No career plans yet" description="Create one for each direct report to track their skills." />
            </Card>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
              {plans.map(plan => (
                <PlanCard key={plan.id} plan={plan} onOpen={() => navigateTo({ plan: plan.id })} />
              ))}
            </div>
          )}
        </>
      )}

      {newPlanFor !== null && (
        <NewPlanModal
          roadmaps={roadmaps}
          people={withoutPlan}
          initialDirectReportId={newPlanFor}
          onClose={() => setNewPlanFor(null)}
          onCreated={async id => {
            setNewPlanFor(null)
            await quietReload(loadPlans)()
            navigateTo({ plan: id })
          }}
        />
      )}
    </div>
  )
}

/** A plan in the list. Deliberately nothing that compares one person with another. */
function PlanCard({ plan, onOpen }: { plan: CareerPlanSummary; onOpen: () => void }) {
  return (
    <button type="button" onClick={onOpen} aria-label={`Open ${plan.directReportName}'s plan`} className="text-left">
      <Card className="h-full hover:shadow-md hover:border-amber-300 transition-all">
        <CardContent className="space-y-3">
          <div className="flex items-start justify-between gap-2">
            <h3 className="font-semibold text-slate-900 truncate">{plan.directReportName}</h3>
            <StageChip stage={plan.currentStage} />
          </div>
          <p className="text-xs text-slate-500 flex items-center gap-1 truncate">
            <MapIcon className="w-3.5 h-3.5 flex-shrink-0" /> {plan.roadmapName}
          </p>
          <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-slate-500">
            <span className="flex items-center gap-1">
              <Crosshair className="w-3.5 h-3.5 text-amber-600" />
              {plan.workingOnCount} working on
            </span>
            <span>Last changed {formatDate(plan.lastChangedAt)}</span>
            {plan.isQuiet && <QuietChip />}
          </div>
        </CardContent>
      </Card>
    </button>
  )
}

function NewPlanModal({
  roadmaps,
  people,
  initialDirectReportId,
  onClose,
  onCreated
}: {
  roadmaps: SkillRoadmapSummary[]
  // Only the direct reports without a plan
  people: CareerAttention[]
  initialDirectReportId: string
  onClose: () => void
  onCreated: (id: string) => void
}) {
  const { showError } = useToast()
  const [form, setForm] = useState({
    directReportId: initialDirectReportId,
    roadmapId: roadmaps[0]?.id ?? '',
    currentStage: '' as CareerStage | '',
    notes: ''
  })

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.currentStage) return
    try {
      const plan = await careerPlansApi.create({
        directReportId: form.directReportId,
        roadmapId: form.roadmapId,
        currentStage: form.currentStage,
        ...(form.notes ? { notes: form.notes } : {})
      })
      onCreated(plan.id)
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  return (
    <Modal title="New career plan" subtitle="The plan copies every skill of the roadmap" onClose={onClose}>
      <form onSubmit={submit} className="space-y-4">
        <div>
          <label htmlFor="plan-direct-report" className={labelClass}>Direct report</label>
          <select
            id="plan-direct-report"
            value={form.directReportId}
            onChange={e => setForm({ ...form, directReportId: e.target.value })}
            className={inputClass}
            required
          >
            <option value="">Choose someone...</option>
            {people.map(person => (
              <option key={person.directReportId} value={person.directReportId}>{person.fullName}</option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="plan-roadmap" className={labelClass}>Roadmap</label>
          <select
            id="plan-roadmap"
            value={form.roadmapId}
            onChange={e => setForm({ ...form, roadmapId: e.target.value })}
            className={inputClass}
            required
          >
            {roadmaps.map(r => (
              <option key={r.id} value={r.id}>{r.name}</option>
            ))}
          </select>
        </div>
        <StageSelect
          id="plan-current-stage"
          label="Their current role's stage"
          value={form.currentStage}
          onChange={currentStage => setForm({ ...form, currentStage })}
          hint="Only used to read their skills against the career ladder, not a goal to reach."
          required
        />
        <div>
          <label htmlFor="plan-notes" className={labelClass}>Notes</label>
          <textarea
            id="plan-notes"
            value={form.notes}
            onChange={e => setForm({ ...form, notes: e.target.value })}
            rows={3}
            className={inputClass}
          />
        </div>
        <ModalButtons onCancel={onClose} submitLabel="Create plan" />
      </form>
    </Modal>
  )
}
