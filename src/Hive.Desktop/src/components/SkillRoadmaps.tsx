import { useCallback, useEffect, useState } from 'react'
import { ArrowLeft, BookOpen, Check, Cpu, Edit, HeartHandshake, Library, Map as MapIcon, Plus, Trash2 } from 'lucide-react'
import { Card, CardContent, CardHeader } from './Card'
import SkillTree, { SkillTreeLegend } from './SkillTree'
import { Modal, ModalButtons, SkillFields, inputClass, labelClass } from './CareerForms'
import { skillRoadmapsApi } from '../services/api'
import { EMPTY_SKILL_FORM, type SkillFormState } from '../utils/careerDisplay'
import { CAREER_STAGES, type RoadmapLibraryItem, type RoadmapSkill, type SkillCountsByStage, type SkillRoadmap, type SkillRoadmapSummary } from '../types'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { useConfirm } from '../contexts/ConfirmContext'
import { ErrorState, LoadingState } from './PageState'


function SkillCounts({ skillCount, sectionCount, hardSkillCount, softSkillCount }: { skillCount: number; sectionCount: number; hardSkillCount: number; softSkillCount: number }) {
  return (
    <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-slate-500">
      <span>{sectionCount} sections</span>
      <span>{skillCount} skills</span>
      <span className="flex items-center gap-1"><Cpu className="w-3.5 h-3.5 text-slate-400" />{hardSkillCount} hard</span>
      <span className="flex items-center gap-1"><HeartHandshake className="w-3.5 h-3.5 text-pink-500" />{softSkillCount} soft</span>
    </div>
  )
}

/** How many skills are expected by each stage, e.g. "Junior 20 · Mid 28 · Senior 36 · Staff 42". */
function StageCounts({ counts }: { counts: SkillCountsByStage | undefined }) {
  if (!counts) return null
  return (
    <div className="flex flex-wrap items-center gap-1 text-xs" aria-label="Skills per stage">
      {CAREER_STAGES.map(stage => (
        <span key={stage} className="px-1.5 py-0.5 rounded-sm bg-slate-100 text-slate-600" title={`${counts[stage] ?? 0} skills expected by ${stage}`}>
          {stage} <span className="font-semibold text-slate-800 tabular-nums">{counts[stage] ?? 0}</span>
        </span>
      ))}
    </div>
  )
}

interface SkillRoadmapsProps {
  roadmaps: SkillRoadmapSummary[]
  // The roadmap open, from the URL
  roadmapId: string | null
  onOpenRoadmap: (id: string | null) => void
  // A roadmap was added, changed or deleted
  onRoadmapsChanged: () => Promise<void> | void
}

/** The Roadmaps tab: the built-in library, the manager's own roadmaps, and one roadmap open. */
export default function SkillRoadmaps({ roadmaps, roadmapId, onOpenRoadmap, onRoadmapsChanged }: SkillRoadmapsProps) {
  const { showError, showSuccess } = useToast()
  const [library, setLibrary] = useState<RoadmapLibraryItem[]>([])
  const [adding, setAdding] = useState<string | null>(null)
  const [showNew, setShowNew] = useState(false)

  const loadLibrary = useCallback(async () => {
    try {
      setLibrary(await skillRoadmapsApi.getLibrary())
    } catch (err) {
      console.error('Failed to load the roadmap library:', err)
    }
  }, [])

  useEffect(() => {
    loadLibrary()
  }, [loadLibrary])

  const refresh = async () => {
    await Promise.all([onRoadmapsChanged(), loadLibrary()])
  }

  const addFromLibrary = async (item: RoadmapLibraryItem) => {
    try {
      setAdding(item.key)
      const roadmap = await skillRoadmapsApi.addFromLibrary(item.key)
      showSuccess(`${roadmap.name} added: ${roadmap.skillCount} skills to play with`)
      await refresh()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    } finally {
      setAdding(null)
    }
  }

  if (roadmapId) {
    return <RoadmapDetail roadmapId={roadmapId} onBack={() => onOpenRoadmap(null)} onChanged={refresh} />
  }

  return (
    <div className="space-y-8">
      {/* The manager's roadmaps */}
      <section className="space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">Your roadmaps</h2>
            <p className="text-sm text-slate-500">A plan copies its roadmap's skills when it is created, so editing a roadmap never changes a plan</p>
          </div>
          <button
            onClick={() => setShowNew(true)}
            className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
          >
            <Plus className="w-5 h-5" />
            New roadmap
          </button>
        </div>

        {roadmaps.length === 0 ? (
          <Card>
            <CardContent className="text-center text-slate-500">
              No roadmaps yet. Add one from the library below, or start your own.
            </CardContent>
          </Card>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            {roadmaps.map(roadmap => (
              <button
                key={roadmap.id}
                type="button"
                onClick={() => onOpenRoadmap(roadmap.id)}
                aria-label={`Open ${roadmap.name}`}
                className="text-left"
              >
                <Card className="h-full hover:shadow-md hover:border-amber-300 transition-all">
                  <CardContent className="space-y-2">
                    <div className="flex items-center gap-3">
                      <div className="p-2 bg-amber-100 rounded-lg">
                        <MapIcon className="w-5 h-5 text-amber-600" />
                      </div>
                      <div className="min-w-0">
                        <h3 className="font-semibold text-slate-900 truncate">{roadmap.name}</h3>
                        <p className="text-xs text-slate-400">{roadmap.libraryKey ? 'From the library' : 'Made by hand'}</p>
                      </div>
                    </div>
                    {roadmap.description && <p className="text-sm text-slate-500 line-clamp-2">{roadmap.description}</p>}
                    <SkillCounts {...roadmap} />
                    <StageCounts counts={roadmap.skillCountsByStage} />
                  </CardContent>
                </Card>
              </button>
            ))}
          </div>
        )}
      </section>

      {/* The built-in library */}
      <section className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold text-slate-900 flex items-center gap-2">
            <Library className="w-5 h-5 text-amber-500" />
            Library
          </h2>
          <p className="text-sm text-slate-500">Ready-made roadmaps, inspired by roadmap.sh. Add one, then make it yours</p>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4" aria-label="Roadmap library">
          {library.map(item => (
            <Card key={item.key}>
              <CardContent className="space-y-2 h-full flex flex-col">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="p-2 bg-slate-100 rounded-lg">
                      <BookOpen className="w-5 h-5 text-slate-600" />
                    </div>
                    <h3 className="font-semibold text-slate-900">{item.name}</h3>
                  </div>
                  {item.isAdded ? (
                    <span className="flex items-center gap-1 px-2.5 py-1 text-xs font-medium rounded-full bg-green-100 text-green-800">
                      <Check className="w-3.5 h-3.5" />
                      Added
                    </span>
                  ) : (
                    <button
                      onClick={() => addFromLibrary(item)}
                      disabled={adding === item.key}
                      aria-label={`Add ${item.name}`}
                      className="flex items-center gap-1 px-3 py-1 text-sm bg-amber-500 text-white rounded-lg hover:bg-amber-600 disabled:opacity-50"
                    >
                      <Plus className="w-4 h-4" />
                      Add
                    </button>
                  )}
                </div>
                <p className="text-sm text-slate-500 flex-1">{item.description}</p>
                <SkillCounts {...item} />
              </CardContent>
            </Card>
          ))}
        </div>
      </section>

      {showNew && (
        <RoadmapFormModal
          title="New roadmap"
          onClose={() => setShowNew(false)}
          onSubmit={async form => {
            const roadmap = await skillRoadmapsApi.create(form)
            setShowNew(false)
            await refresh()
            onOpenRoadmap(roadmap.id)
          }}
        />
      )}
    </div>
  )
}

function RoadmapFormModal({
  title,
  initial = { name: '', description: '' },
  onClose,
  onSubmit
}: {
  title: string
  initial?: { name: string; description: string }
  onClose: () => void
  onSubmit: (form: { name: string; description: string }) => Promise<void>
}) {
  const { showError } = useToast()
  const [form, setForm] = useState(initial)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await onSubmit(form)
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  return (
    <Modal title={title} onClose={onClose}>
      <form onSubmit={submit} className="space-y-4">
        <div>
          <label htmlFor="roadmap-name" className={labelClass}>Name</label>
          <input
            id="roadmap-name"
            value={form.name}
            onChange={e => setForm({ ...form, name: e.target.value })}
            placeholder="e.g. Backend Engineer"
            className={inputClass}
            required
          />
        </div>
        <div>
          <label htmlFor="roadmap-description" className={labelClass}>Description</label>
          <textarea
            id="roadmap-description"
            value={form.description}
            onChange={e => setForm({ ...form, description: e.target.value })}
            rows={3}
            className={inputClass}
          />
        </div>
        <ModalButtons onCancel={onClose} submitLabel="Save" />
      </form>
    </Modal>
  )
}

const EMPTY_SKILL = EMPTY_SKILL_FORM

function RoadmapDetail({ roadmapId, onBack, onChanged }: { roadmapId: string; onBack: () => void; onChanged: () => Promise<void> }) {
  const { showError } = useToast()
  const confirm = useConfirm()
  const [roadmap, setRoadmap] = useState<SkillRoadmap | null>(null)
  const [loadFailed, setLoadFailed] = useState(false)
  const [editingDetails, setEditingDetails] = useState(false)
  // The skill being edited, or a new one (id null) for a section
  const [skillForm, setSkillForm] = useState<{ id: string | null; form: SkillFormState } | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoadFailed(false)
    skillRoadmapsApi.getById(roadmapId)
      .then(result => { if (!cancelled) setRoadmap(result) })
      .catch(err => {
        console.error('Failed to load roadmap:', err)
        if (!cancelled) setLoadFailed(true)
      })
    return () => { cancelled = true }
  }, [roadmapId])

  const apply = (next: SkillRoadmap) => {
    setRoadmap(next)
    onChanged()
  }

  const handleDelete = async () => {
    if (!roadmap) return
    if (!(await confirm({
      title: `Delete the ${roadmap.name} roadmap?`,
      message: 'Plans started from it keep their own copy of its skills.',
      confirmLabel: 'Delete'
    }))) return
    try {
      await skillRoadmapsApi.delete(roadmap.id)
      await onChanged()
      onBack()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  const openSkill = (skillId: string) => {
    const skill = roadmap?.sections.flatMap(s => s.skills).find(s => s.id === skillId)
    if (!skill) return
    setSkillForm({ id: skill.id, form: toForm(skill) })
  }

  const saveSkill = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!roadmap || !skillForm) return
    try {
      apply(skillForm.id
        ? await skillRoadmapsApi.updateSkill(roadmap.id, skillForm.id, skillForm.form)
        : await skillRoadmapsApi.addSkill(roadmap.id, skillForm.form))
      setSkillForm(null)
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  const deleteSkill = async () => {
    if (!roadmap || !skillForm?.id) return
    if (!(await confirm({
      title: `Remove ${skillForm.form.title} from this roadmap?`,
      message: 'Plans already started keep it.',
      confirmLabel: 'Remove'
    }))) return
    try {
      apply(await skillRoadmapsApi.deleteSkill(roadmap.id, skillForm.id))
      setSkillForm(null)
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  const back = (
    <button onClick={onBack} className="flex items-center gap-1 text-sm text-slate-500 hover:text-slate-800">
      <ArrowLeft className="w-4 h-4" />
      All roadmaps
    </button>
  )

  if (loadFailed) {
    return (
      <div className="space-y-4">
        {back}
        <ErrorState what="this roadmap" detail="It may have been deleted." />
      </div>
    )
  }

  if (!roadmap) {
    return <LoadingState fill />
  }

  return (
    <div className="space-y-6">
      {back}
      <Card>
        <CardHeader
          title={roadmap.name}
          subtitle={roadmap.description || undefined}
          action={
            <div className="flex items-center gap-2">
              <button
                onClick={() => setEditingDetails(true)}
                className="flex items-center gap-2 px-3 py-2 text-sm border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50"
              >
                <Edit className="w-4 h-4" />
                Rename
              </button>
              <button
                onClick={() => setSkillForm({ id: null, form: EMPTY_SKILL })}
                className="flex items-center gap-2 px-3 py-2 text-sm bg-amber-500 text-white rounded-lg hover:bg-amber-600"
              >
                <Plus className="w-4 h-4" />
                Add skill
              </button>
              <button
                onClick={handleDelete}
                aria-label="Delete roadmap"
                title="Delete roadmap"
                className="p-2 text-sm border border-slate-300 text-red-600 rounded-lg hover:bg-red-50"
              >
                <Trash2 className="w-4 h-4" />
              </button>
            </div>
          }
        />
        <CardContent className="space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="space-y-1.5">
              <SkillCounts {...roadmap} />
              <StageCounts counts={roadmap.skillCountsByStage} />
            </div>
            <SkillTreeLegend />
          </div>
          <SkillTree
            sections={roadmap.sections}
            onSelectSkill={openSkill}
            onAddSkill={section => setSkillForm({ id: null, form: { ...EMPTY_SKILL, section } })}
            emptyMessage="This roadmap has no skills yet. Add the first one."
          />
        </CardContent>
      </Card>

      {editingDetails && (
        <RoadmapFormModal
          title="Edit roadmap"
          initial={{ name: roadmap.name, description: roadmap.description }}
          onClose={() => setEditingDetails(false)}
          onSubmit={async form => {
            apply(await skillRoadmapsApi.update(roadmap.id, form))
            setEditingDetails(false)
          }}
        />
      )}

      {skillForm && (
        <Modal title={skillForm.id ? 'Edit skill' : 'Add skill'} subtitle={roadmap.name} onClose={() => setSkillForm(null)}>
          <form onSubmit={saveSkill} className="space-y-4">
            <SkillFields
              form={skillForm.form}
              setForm={form => setSkillForm({ ...skillForm, form })}
              sections={roadmap.sections.map(s => s.name)}
              idPrefix="roadmap-skill"
              stageHint="A plan copies every skill; this is the stage it is expected from."
            />
            {skillForm.id && (
              <button type="button" onClick={deleteSkill} className="flex items-center gap-1.5 text-sm text-red-600 hover:underline">
                <Trash2 className="w-4 h-4" />
                Remove from roadmap
              </button>
            )}
            <ModalButtons onCancel={() => setSkillForm(null)} submitLabel={skillForm.id ? 'Save' : 'Add skill'} />
          </form>
        </Modal>
      )}
    </div>
  )
}

function toForm(skill: RoadmapSkill): SkillFormState {
  return {
    section: skill.section,
    title: skill.title,
    description: skill.description,
    category: skill.category,
    isOptional: skill.isOptional,
    fromStage: skill.fromStage ?? 'Junior'
  }
}
