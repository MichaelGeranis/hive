import type { ReactNode } from 'react'
import { Cpu, HeartHandshake } from 'lucide-react'
import { Modal as SharedModal } from './Modal'
import { CAREER_STAGES, type CareerStage, type SkillCategory } from '../types'
import type { SkillFormState } from '../utils/careerDisplay'

/** The modal and form pieces shared by career plans and skill roadmaps. */

export const inputClass = 'w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500'
export const labelClass = 'block text-sm font-medium text-slate-700 mb-1'

export function Modal({ title, subtitle, onClose, children }: { title: string; subtitle?: string; onClose: () => void; children: ReactNode }) {
  return (
    <SharedModal title={title} subtitle={subtitle} onClose={onClose}>
      {children}
    </SharedModal>
  )
}

export function ModalButtons({ onCancel, submitLabel }: { onCancel: () => void; submitLabel: string }) {
  return (
    <div className="flex gap-3 pt-2">
      <button type="button" onClick={onCancel} className="flex-1 px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50">
        Cancel
      </button>
      <button type="submit" className="flex-1 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600">
        {submitLabel}
      </button>
    </div>
  )
}

/** A Junior/Mid/Senior/Staff select. */
export function StageSelect({
  id,
  label,
  value,
  onChange,
  hint,
  required = false
}: {
  id: string
  label: string
  value: CareerStage | ''
  onChange: (stage: CareerStage) => void
  hint?: string
  required?: boolean
}) {
  return (
    <div>
      <label htmlFor={id} className={labelClass}>{label}</label>
      <select id={id} value={value} onChange={e => onChange(e.target.value as CareerStage)} className={inputClass} required={required}>
        {value === '' && <option value="">Choose a stage...</option>}
        {CAREER_STAGES.map(stage => <option key={stage} value={stage}>{stage}</option>)}
      </select>
      {hint && <p className="text-xs text-slate-500 mt-1">{hint}</p>}
    </div>
  )
}

/** The fields a skill has on a roadmap and on a plan: section, title, description, kind, stage. */
export function SkillFields({
  form,
  setForm,
  sections,
  idPrefix,
  stageHint
}: {
  form: SkillFormState
  setForm: (form: SkillFormState) => void
  sections: string[]
  idPrefix: string
  stageHint: string
}) {
  return (
    <>
      <div>
        <label htmlFor={`${idPrefix}-section`} className={labelClass}>Section</label>
        <input
          id={`${idPrefix}-section`}
          list={`${idPrefix}-sections`}
          value={form.section}
          onChange={e => setForm({ ...form, section: e.target.value })}
          placeholder="Pick one, or name a new section"
          className={inputClass}
          required
        />
        <datalist id={`${idPrefix}-sections`}>
          {sections.map(name => <option key={name} value={name} />)}
        </datalist>
      </div>
      <div>
        <label htmlFor={`${idPrefix}-title`} className={labelClass}>Skill</label>
        <input
          id={`${idPrefix}-title`}
          value={form.title}
          onChange={e => setForm({ ...form, title: e.target.value })}
          className={inputClass}
          required
        />
      </div>
      <div>
        <label htmlFor={`${idPrefix}-description`} className={labelClass}>Description</label>
        <textarea
          id={`${idPrefix}-description`}
          value={form.description}
          onChange={e => setForm({ ...form, description: e.target.value })}
          rows={2}
          className={inputClass}
        />
      </div>
      <div className="flex flex-wrap items-center gap-4">
        <div role="radiogroup" aria-label="Kind of skill" className="inline-flex rounded-lg bg-slate-100 p-0.5">
          {(['Hard', 'Soft'] as SkillCategory[]).map(category => (
            <button
              key={category}
              type="button"
              role="radio"
              aria-checked={form.category === category}
              onClick={() => setForm({ ...form, category })}
              className={`flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md transition-colors ${
                form.category === category ? 'bg-white text-slate-900 shadow-xs' : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              {category === 'Hard' ? <Cpu className="w-4 h-4" /> : <HeartHandshake className="w-4 h-4" />}
              {category}
            </button>
          ))}
        </div>
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input
            type="checkbox"
            checked={form.isOptional}
            onChange={e => setForm({ ...form, isOptional: e.target.checked })}
            className="rounded-sm border-slate-300 text-amber-500 focus:ring-amber-500"
          />
          Optional
        </label>
      </div>
      <StageSelect
        id={`${idPrefix}-stage`}
        label="Introduced at"
        value={form.fromStage}
        onChange={fromStage => setForm({ ...form, fromStage })}
        hint={stageHint}
      />
    </>
  )
}
