import { useCallback, useEffect, useState } from 'react'
import {
  ArrowLeft,
  ChevronDown,
  Crosshair,
  Edit,
  Map as MapIcon,
  Plus,
  Sparkles,
  Trash2,
  TrendingDown,
  TrendingUp
} from 'lucide-react'
import { Card, CardContent, CardHeader } from './Card'
import SkillTree, { SkillTreeLegend } from './SkillTree'
import { BadgeShelf, LevelToken, StageChip, XpBar } from './CareerGame'
import { Modal, ModalButtons, SkillFields, StageSelect, inputClass, labelClass } from './CareerForms'
import { EMPTY_SKILL_FORM, LEVEL_CHIP_STYLES, SKILL_LEVEL_LABELS, SKILL_STATUS_LABELS, type SkillFormState } from '../utils/careerDisplay'
import { careerPlansApi } from '../services/api'
import type { CareerLadder, CareerPlan, CareerPlanSkill, CareerSkillStatus, CareerStage, SkillLevelChange } from '../types'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { useConfirm } from '../contexts/ConfirmContext'
import { ErrorState, LoadingState } from './PageState'
import { formatDate } from '../utils/dateUtils'

interface CareerPlanDetailProps {
  planId: string
  onBack: () => void
  // The plan changed or was deleted, so the list behind it is out of date
  onChanged: () => void
}

/**
 * One direct report's career plan: what they have, what they are working on, what not yet,
 * and how their skills read against their own role's stage. The game (player level, XP,
 * badges) stays on this page and is never shown next to anyone else's.
 */
export default function CareerPlanDetail({ planId, onBack, onChanged }: CareerPlanDetailProps) {
  const { showError, showSuccess } = useToast()
  const confirm = useConfirm()
  const [plan, setPlan] = useState<CareerPlan | null>(null)
  const [loadFailed, setLoadFailed] = useState(false)
  const [selectedSkillId, setSelectedSkillId] = useState<string | null>(null)
  const [addToSection, setAddToSection] = useState<string | null>(null)
  const [editingDetails, setEditingDetails] = useState(false)
  const [celebrate, setCelebrate] = useState(false)
  const [statusFilter, setStatusFilter] = useState<CareerSkillStatus | 'All'>('All')

  const load = useCallback(async () => {
    try {
      setLoadFailed(false)
      setPlan(await careerPlansApi.getById(planId))
    } catch (err) {
      console.error('Failed to load career plan:', err)
      setLoadFailed(true)
    }
  }, [planId])

  useEffect(() => {
    load()
  }, [load])

  useEffect(() => {
    if (!celebrate) return
    const timer = setTimeout(() => setCelebrate(false), 2500)
    return () => clearTimeout(timer)
  }, [celebrate])

  const applyPlan = (next: CareerPlan) => {
    setPlan(next)
    onChanged()
  }

  const setFocus = async (skill: CareerPlanSkill, isFocus: boolean) => {
    if (!plan) return
    try {
      applyPlan(await careerPlansApi.setSkillFocus(plan.id, skill.id, isFocus))
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  const handleDelete = async () => {
    if (!plan) return
    if (!(await confirm({
      title: `Delete ${plan.directReportName}'s career plan?`,
      message: 'Every level and piece of evidence goes with it.',
      confirmLabel: 'Delete'
    }))) return
    try {
      await careerPlansApi.delete(plan.id)
      onChanged()
      onBack()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  if (loadFailed) {
    return (
      <div className="space-y-4">
        <BackButton onBack={onBack} />
        <ErrorState what="this career plan" detail="It may have been deleted." />
      </div>
    )
  }

  if (!plan) {
    return <LoadingState fill />
  }

  const firstName = plan.directReportName.split(' ')[0] || plan.directReportName
  const allSkills = plan.sections.flatMap(s => s.skills)
  const selectedSkill = allSkills.find(s => s.id === selectedSkillId) ?? null
  const focusSkills = allSkills.filter(s => s.isFocus)
  const { game } = plan

  // The map shows the skills of the chosen status; the status itself comes from the backend
  const visibleSections = statusFilter === 'All'
    ? plan.sections
    : plan.sections
        .map(section => ({ ...section, skills: section.skills.filter(s => s.status === statusFilter) }))
        .filter(section => section.skills.length > 0)

  const celebrateChange = (result: SkillLevelChange, skill: CareerPlanSkill) => {
    const updated = result.plan.sections.flatMap(s => s.skills).find(s => s.id === skill.id)
    if (result.playerLevelledUp) {
      setCelebrate(true)
      const title = result.plan.game.playerTitle
      showSuccess(`Level up! ${firstName} is now ${/^[aeiou]/i.test(title) ? 'an' : 'a'} ${title} (level ${result.plan.game.playerLevel})`)
    } else if (result.skillLevelledUp) {
      showSuccess(`${skill.title} levelled up to ${updated?.levelLabel ?? SKILL_LEVEL_LABELS[updated?.level ?? 0]}`)
    }
    for (const badge of result.newBadges) {
      showSuccess(`Badge unlocked: ${badge.name}`)
    }
  }

  const filters: { key: CareerSkillStatus | 'All'; label: string; count: number }[] = [
    { key: 'Has', label: SKILL_STATUS_LABELS.Has, count: plan.hasCount },
    { key: 'WorkingOn', label: SKILL_STATUS_LABELS.WorkingOn, count: plan.workingOnCount },
    { key: 'NotYet', label: SKILL_STATUS_LABELS.NotYet, count: plan.notYetCount },
    { key: 'All', label: 'All', count: plan.skillCount }
  ]

  return (
    <div className="space-y-6">
      <BackButton onBack={onBack} />

      {/* Header: who, their role's stage, and the game on the side */}
      <Card>
        <CardContent className="flex flex-wrap items-start justify-between gap-6 py-5">
          <div className="min-w-0 space-y-2">
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="text-2xl font-bold text-slate-900">{plan.directReportName}</h1>
              <StageChip stage={plan.currentStage} size="lg" />
            </div>
            <p className="text-xs text-slate-500 flex flex-wrap items-center gap-x-3 gap-y-1">
              <span className="flex items-center gap-1"><MapIcon className="w-3.5 h-3.5" /> {plan.roadmapName}</span>
              <span>Last changed {formatDate(plan.lastChangedAt)}</span>
            </p>
            {plan.notes && <p className="text-sm text-slate-600 whitespace-pre-wrap max-w-2xl">{plan.notes}</p>}
            <div className="flex items-center gap-2 pt-1">
              <button
                onClick={() => setEditingDetails(true)}
                className="flex items-center gap-2 px-3 py-1.5 text-sm border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50"
              >
                <Edit className="w-4 h-4" />
                Edit
              </button>
              <button
                onClick={handleDelete}
                className="flex items-center gap-2 px-3 py-1.5 text-sm border border-slate-300 text-red-600 rounded-lg hover:bg-red-50"
              >
                <Trash2 className="w-4 h-4" />
                Delete
              </button>
            </div>
          </div>

          <div className="w-full sm:w-64 flex items-start gap-3 rounded-lg bg-amber-50/60 border border-amber-100 px-3 py-2.5" aria-label="Game">
            <LevelToken level={game.playerLevel} celebrate={celebrate} />
            <div className="flex-1 min-w-0 space-y-1.5">
              <p className="text-xs text-slate-600">
                Player level {game.playerLevel} · <span className="font-semibold text-amber-700">{game.playerTitle}</span>
              </p>
              <XpBar game={game} />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Working on now, and where their skills sit on the ladder */}
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <Card>
          <CardHeader title="Working on now" subtitle="The skills in focus" />
          <CardContent>
            {focusSkills.length === 0 ? (
              <p className="text-sm text-slate-500">Nothing in focus. Open a skill below and mark it as one they are working on.</p>
            ) : (
              <ul className="divide-y divide-slate-100" aria-label="Working on now">
                {focusSkills.map(skill => (
                  <li key={skill.id} className="flex items-start gap-3 py-2.5">
                    <Crosshair className="w-4 h-4 text-amber-600 mt-0.5 flex-shrink-0" aria-hidden="true" />
                    <div className="flex-1 min-w-0">
                      <button
                        type="button"
                        onClick={() => setSelectedSkillId(skill.id)}
                        className="text-sm font-medium text-slate-900 hover:text-amber-700 text-left"
                      >
                        {skill.title}
                      </button>
                      <p className="text-xs text-slate-500">
                        Level {skill.level} {skill.levelLabel}
                        {skill.expectedLevel !== null ? ` · expects ${skill.expectedLevel}` : ` · ${skill.fromStage} skill`}
                      </p>
                      {skill.evidence && <p className="text-xs text-slate-600 mt-0.5 line-clamp-2">{skill.evidence}</p>}
                    </div>
                    <button
                      type="button"
                      onClick={() => setFocus(skill, false)}
                      aria-label={`Done focusing on ${skill.title}`}
                      className="flex-shrink-0 px-2 py-1 text-xs border border-slate-300 text-slate-600 rounded-md hover:bg-slate-50"
                    >
                      Done focusing
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>

        <LadderCard ladder={plan.ladder} />
      </div>

      {/* Strengths and weaknesses against their own stage */}
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <SkillListCard
          title="Strengths"
          subtitle={`Above what ${plan.currentStage} expects`}
          icon={<TrendingUp className="w-4 h-4 text-emerald-600" />}
          skills={plan.ladder.strengths}
          empty="No strengths recorded yet."
          onSelect={setSelectedSkillId}
        />
        <SkillListCard
          title="Weaknesses"
          subtitle={`Below what ${plan.currentStage} expects`}
          icon={<TrendingDown className="w-4 h-4 text-rose-600" />}
          skills={plan.ladder.weaknesses}
          empty="Nothing below what their stage expects."
          onSelect={setSelectedSkillId}
        />
      </div>

      {/* The skill map */}
      <Card>
        <CardHeader
          title="Skill map"
          subtitle="Click a skill to rate it or put it in focus"
          action={
            <button
              onClick={() => setAddToSection('')}
              className="flex items-center gap-2 px-3 py-2 text-sm bg-amber-500 text-white rounded-lg hover:bg-amber-600"
            >
              <Plus className="w-4 h-4" />
              Add skill
            </button>
          }
        />
        <CardContent className="space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div role="tablist" aria-label="Skill status" className="inline-flex rounded-lg bg-slate-100 p-0.5">
              {filters.map(filter => (
                <button
                  key={filter.key}
                  role="tab"
                  aria-selected={statusFilter === filter.key}
                  onClick={() => setStatusFilter(filter.key)}
                  className={`px-3 py-1.5 text-sm font-medium rounded-md transition-colors ${
                    statusFilter === filter.key ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-700'
                  }`}
                >
                  {filter.label} ({filter.count})
                </button>
              ))}
            </div>
            <SkillTreeLegend showLevels />
          </div>
          <SkillTree
            sections={visibleSections}
            showLevels
            onSelectSkill={setSelectedSkillId}
            onAddSkill={statusFilter === 'All' ? setAddToSection : undefined}
            emptyMessage={statusFilter === 'All' ? 'This plan has no skills. Add one to start.' : 'No skills here.'}
          />
        </CardContent>
      </Card>

      {/* The game's badges, tucked away */}
      <Card>
        <details className="group">
          <summary className="flex items-center justify-between gap-3 px-6 py-4 cursor-pointer list-none">
            <span className="flex items-center gap-2">
              <Sparkles className="w-4 h-4 text-amber-500" />
              <span className="font-semibold text-slate-900">Badges</span>
              <span className="text-sm text-slate-500">
                {game.badges.filter(b => b.isEarned).length} of {game.badges.length} earned
              </span>
            </span>
            <ChevronDown className="w-4 h-4 text-slate-400 group-open:rotate-180 transition-transform" />
          </summary>
          <div className="px-6 pb-5">
            <BadgeShelf badges={game.badges} />
          </div>
        </details>
      </Card>

      {selectedSkill && (
        <SkillEditor
          key={selectedSkill.id}
          plan={plan}
          skill={selectedSkill}
          onClose={() => setSelectedSkillId(null)}
          onFocusChanged={applyPlan}
          onSaved={(next, result) => {
            applyPlan(next)
            if (result) celebrateChange(result, selectedSkill)
            setSelectedSkillId(null)
          }}
        />
      )}

      {addToSection !== null && (
        <AddSkillModal
          plan={plan}
          section={addToSection}
          onClose={() => setAddToSection(null)}
          onSaved={next => {
            applyPlan(next)
            setAddToSection(null)
          }}
        />
      )}

      {editingDetails && (
        <EditPlanModal
          plan={plan}
          onClose={() => setEditingDetails(false)}
          onSaved={next => {
            applyPlan(next)
            setEditingDetails(false)
          }}
        />
      )}
    </div>
  )
}

function BackButton({ onBack }: { onBack: () => void }) {
  return (
    <button onClick={onBack} className="flex items-center gap-1 text-sm text-slate-500 hover:text-slate-800">
      <ArrowLeft className="w-4 h-4" />
      All plans
    </button>
  )
}

/** The reading of their skills against their own role, in plain words. */
function describeReading(ladder: CareerLadder): string {
  const role = `their ${ladder.currentStage} role`
  const meets = ladder.skillsStage ? ` (they meet ${ladder.skillsStage}'s expectations)` : ''
  switch (ladder.reading) {
    case 'AboveRole':
      return `Skills read above ${role}${meets}`
    case 'AtRole':
      return `Skills read at ${role}`
    case 'BelowRole':
      return `Skills read below ${role}${meets}`
    default:
      return 'Not assessed yet'
  }
}

const READING_STYLES: Record<CareerLadder['reading'], string> = {
  AboveRole: 'bg-violet-50 border-violet-200 text-violet-900',
  AtRole: 'bg-emerald-50 border-emerald-200 text-emerald-900',
  BelowRole: 'bg-amber-50 border-amber-200 text-amber-900',
  NotAssessed: 'bg-slate-50 border-slate-200 text-slate-600'
}

function LadderCard({ ladder }: { ladder: CareerLadder }) {
  return (
    <Card>
      <CardHeader title="Ladder position" subtitle="An indication from the skills recorded here, not a verdict" />
      <CardContent className="space-y-4">
        <p className={`px-3 py-2 rounded-lg border text-sm font-medium ${READING_STYLES[ladder.reading]}`} aria-label="Ladder reading">
          {describeReading(ladder)}
        </p>
        {ladder.reading === 'NotAssessed' && (
          <p className="text-xs text-slate-500">Rate a few skills and a reading appears here.</p>
        )}
        <ol className="space-y-2" aria-label="Career ladder">
          {ladder.stages.map(fit => {
            const isCurrent = fit.stage === ladder.currentStage
            const isSkillsStage = fit.stage === ladder.skillsStage
            return (
              <li
                key={fit.stage}
                aria-label={`${fit.stage}: ${fit.metPercent}% of expectations met`}
                className={`grid grid-cols-[5.5rem_1fr_auto] items-center gap-3 px-2 py-1.5 rounded-lg ${isCurrent ? 'bg-slate-50 ring-1 ring-slate-200' : ''}`}
              >
                <span className="flex items-center gap-1.5">
                  <StageChip stage={fit.stage as CareerStage} size="sm" />
                  {isCurrent && <span className="text-[10px] font-semibold uppercase text-slate-500">Role</span>}
                </span>
                <div className="w-full bg-slate-100 rounded-full h-2 overflow-hidden">
                  <div
                    className={`h-full rounded-full ${isSkillsStage ? 'bg-emerald-500' : 'bg-slate-300'}`}
                    style={{ width: `${fit.metPercent}%` }}
                  />
                </div>
                <span className="text-xs text-slate-500 tabular-nums w-28 text-right">
                  {fit.metCount}/{fit.expectedCount} met · {fit.metPercent}%
                </span>
              </li>
            )
          })}
        </ol>
      </CardContent>
    </Card>
  )
}

const SHOWN_AT_FIRST = 6

function SkillListCard({
  title,
  subtitle,
  icon,
  skills,
  empty,
  onSelect
}: {
  title: string
  subtitle: string
  icon: React.ReactNode
  skills: CareerPlanSkill[]
  empty: string
  onSelect: (skillId: string) => void
}) {
  const [showAll, setShowAll] = useState(false)
  const shown = showAll ? skills : skills.slice(0, SHOWN_AT_FIRST)
  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          {icon}
          <h3 className="text-lg font-semibold text-slate-900">{title}</h3>
        </div>
        <p className="text-sm text-slate-500 mt-0.5">{subtitle}</p>
      </CardHeader>
      <CardContent>
        {skills.length === 0 ? (
          <p className="text-sm text-slate-500">{empty}</p>
        ) : (
          <>
            <ul className="flex flex-wrap gap-2" aria-label={title}>
              {shown.map(skill => (
                <li key={skill.id}>
                  <button
                    type="button"
                    onClick={() => onSelect(skill.id)}
                    className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md border border-slate-200 bg-white text-xs text-slate-800 hover:border-amber-300"
                  >
                    <span className="font-medium">{skill.title}</span>
                    <span className="text-slate-500 tabular-nums">
                      {skill.level}{skill.expectedLevel !== null && ` · expects ${skill.expectedLevel}`}
                    </span>
                    {skill.expectedLevel === null && (
                      <span className="px-1 rounded bg-violet-100 text-violet-800 text-[10px] font-semibold">{skill.fromStage} skill</span>
                    )}
                  </button>
                </li>
              ))}
            </ul>
            {skills.length > SHOWN_AT_FIRST && (
              <button type="button" onClick={() => setShowAll(!showAll)} className="mt-3 text-sm text-amber-600 hover:underline">
                {showAll ? 'Show fewer' : `Show all ${skills.length}`}
              </button>
            )}
          </>
        )}
      </CardContent>
    </Card>
  )
}

function SkillEditor({
  plan,
  skill,
  onClose,
  onFocusChanged,
  onSaved
}: {
  plan: CareerPlan
  skill: CareerPlanSkill
  onClose: () => void
  onFocusChanged: (plan: CareerPlan) => void
  onSaved: (plan: CareerPlan, levelResult: SkillLevelChange | null) => void
}) {
  const { showError } = useToast()
  const confirm = useConfirm()
  const [level, setLevel] = useState(skill.level)
  const [evidence, setEvidence] = useState(skill.evidence)
  const [saving, setSaving] = useState(false)

  const changed = level !== skill.level || evidence !== skill.evidence

  const save = async () => {
    try {
      setSaving(true)
      const result = await careerPlansApi.setSkillLevel(plan.id, skill.id, level, evidence)
      onSaved(result.plan, result)
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  const toggleFocus = async () => {
    try {
      onFocusChanged(await careerPlansApi.setSkillFocus(plan.id, skill.id, !skill.isFocus))
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  const remove = async () => {
    if (!(await confirm({
      title: `Remove ${skill.title} from this plan?`,
      message: 'The roadmap keeps it.',
      confirmLabel: 'Remove'
    }))) return
    try {
      onSaved(await careerPlansApi.removeSkill(plan.id, skill.id), null)
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  return (
    <Modal
      title={skill.title}
      subtitle={`${skill.section} · ${skill.category} skill${skill.isOptional ? ' · optional' : ''}`}
      onClose={onClose}
    >
      <div className="space-y-5">
        {skill.description && <p className="text-sm text-slate-600">{skill.description}</p>}

        <p className="text-sm text-slate-600 bg-slate-50 rounded-lg px-3 py-2">
          {skill.expectedLevel !== null
            ? <>At {plan.currentStage}, expected level <span className="font-semibold">{skill.expectedLevel} {SKILL_LEVEL_LABELS[skill.expectedLevel]}</span>.</>
            : <>Expected from {skill.fromStage}, beyond their {plan.currentStage} role.</>}
        </p>

        <fieldset>
          <legend className={labelClass}>Level</legend>
          <div role="radiogroup" aria-label="Level" className="grid grid-cols-3 gap-2">
            {SKILL_LEVEL_LABELS.map((label, value) => (
              <button
                key={value}
                type="button"
                role="radio"
                aria-checked={level === value}
                aria-label={`${value} ${label}`}
                onClick={() => setLevel(value)}
                className={`flex flex-col items-center px-2 py-2 rounded-lg border-2 text-xs transition-colors ${
                  level === value ? `${LEVEL_CHIP_STYLES[value]} ring-2 ring-amber-400` : 'border-slate-200 text-slate-600 hover:border-amber-300'
                }`}
              >
                <span className="text-base font-bold">{value}</span>
                <span>{label}</span>
                {skill.expectedLevel === value && <span className="text-[10px] opacity-75">expected</span>}
              </button>
            ))}
          </div>
        </fieldset>

        <label className="flex items-center justify-between gap-3 px-3 py-2 rounded-lg border border-slate-200">
          <span className="flex items-center gap-2 text-sm font-medium text-slate-700">
            <Crosshair className="w-4 h-4 text-amber-600" />
            Working on this
          </span>
          <input
            type="checkbox"
            role="switch"
            checked={skill.isFocus}
            onChange={toggleFocus}
            className="rounded border-slate-300 text-amber-500 focus:ring-amber-500"
          />
        </label>

        <div>
          <label htmlFor="skill-evidence" className={labelClass}>Evidence</label>
          <textarea
            id="skill-evidence"
            value={evidence}
            onChange={e => setEvidence(e.target.value)}
            rows={3}
            placeholder="What have you seen? A PR, a design review, a talk..."
            className={inputClass}
          />
        </div>

        {skill.lastLevelUpAt && (
          <p className="text-xs text-slate-500">Last levelled up {formatDate(skill.lastLevelUpAt)}.</p>
        )}

        <div className="flex items-center gap-3 pt-2">
          <button
            type="button"
            onClick={remove}
            className="flex items-center gap-1.5 px-3 py-2 text-sm text-red-600 rounded-lg hover:bg-red-50"
          >
            <Trash2 className="w-4 h-4" />
            Remove
          </button>
          <div className="flex-1" />
          <button type="button" onClick={onClose} className="px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50">
            Cancel
          </button>
          <button
            type="button"
            onClick={save}
            disabled={!changed || saving}
            className="flex items-center gap-1.5 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 disabled:opacity-50"
          >
            <Sparkles className="w-4 h-4" />
            Save
          </button>
        </div>
      </div>
    </Modal>
  )
}

function AddSkillModal({ plan, section, onClose, onSaved }: { plan: CareerPlan; section: string; onClose: () => void; onSaved: (plan: CareerPlan) => void }) {
  const { showError } = useToast()
  const [form, setForm] = useState<SkillFormState>({ ...EMPTY_SKILL_FORM, section })

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      onSaved(await careerPlansApi.addSkill(plan.id, form))
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  return (
    <Modal title="Add skill" subtitle="Added to this plan only; the roadmap is not changed" onClose={onClose}>
      <form onSubmit={submit} className="space-y-4">
        <SkillFields
          form={form}
          setForm={setForm}
          sections={plan.sections.map(s => s.name)}
          idPrefix="plan-skill"
          stageHint="The stage this skill is expected from."
        />
        <ModalButtons onCancel={onClose} submitLabel="Add skill" />
      </form>
    </Modal>
  )
}

function EditPlanModal({ plan, onClose, onSaved }: { plan: CareerPlan; onClose: () => void; onSaved: (plan: CareerPlan) => void }) {
  const { showError } = useToast()
  const [form, setForm] = useState({ currentStage: plan.currentStage, notes: plan.notes })

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      onSaved(await careerPlansApi.update(plan.id, form))
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  return (
    <Modal title="Edit plan" subtitle={plan.directReportName} onClose={onClose}>
      <form onSubmit={submit} className="space-y-4">
        <StageSelect
          id="edit-plan-stage"
          label="Their current role's stage"
          value={form.currentStage}
          onChange={currentStage => setForm({ ...form, currentStage })}
          hint="Change it after a promotion. It is only used to read their skills against the ladder."
          required
        />
        <div>
          <label htmlFor="edit-plan-notes" className={labelClass}>Notes</label>
          <textarea
            id="edit-plan-notes"
            value={form.notes}
            onChange={e => setForm({ ...form, notes: e.target.value })}
            rows={4}
            className={inputClass}
          />
        </div>
        <ModalButtons onCancel={onClose} submitLabel="Save" />
      </form>
    </Modal>
  )
}

