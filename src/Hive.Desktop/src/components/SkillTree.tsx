import { CheckCircle2, Cpu, Crosshair, HeartHandshake, Plus } from 'lucide-react'
import { CAREER_STAGES, type CareerSkillStatus, type CareerStage, type SkillCategory } from '../types'
import { SKILL_LEVEL_LABELS, SKILL_STATUS_LABELS, SKILL_STATUS_STYLES } from '../utils/careerDisplay'

/**
 * A roadmap drawn the way roadmap.sh draws one: sections as nodes down a central path, each
 * with its skills branching off to either side. It shows a roadmap (skills only) or a career
 * plan (each skill coloured by its status, with its level and what their stage expects). The
 * status, expected level and whether a section is cleared all come from the backend.
 */

export interface TreeSkill {
  id: string
  title: string
  description?: string
  category: SkillCategory
  isOptional: boolean
  // The stage the skill is expected from
  fromStage?: CareerStage
  // Only on a career plan
  level?: number
  status?: CareerSkillStatus
  // Null when the skill belongs to a later stage than their role's
  expectedLevel?: number | null
}

export interface TreeSection {
  name: string
  skills: TreeSkill[]
  // Only on a career plan
  isCleared?: boolean
  hasCount?: number
  skillCount?: number
}

interface SkillTreeProps {
  sections: TreeSection[]
  // True for a career plan: chips show status, level and expected level
  showLevels?: boolean
  onSelectSkill?: (skillId: string) => void
  // When given, each section offers to add a skill to it
  onAddSkill?: (section: string) => void
  emptyMessage?: string
}

export default function SkillTree({ sections, showLevels = false, onSelectSkill, onAddSkill, emptyMessage }: SkillTreeProps) {
  if (sections.length === 0) {
    return (
      <p className="py-10 text-center text-sm text-slate-500">
        {emptyMessage ?? 'No skills yet.'}
      </p>
    )
  }

  return (
    <div className="relative py-2">
      {/* The path everything hangs off */}
      <div className="absolute left-1/2 top-2 bottom-2 -translate-x-1/2 w-1 rounded-full bg-amber-200" aria-hidden="true" />

      <ol className="relative space-y-2" aria-label="Skill roadmap">
        {sections.map(section => {
          // Alternate the skills either side of the path so the tree stays balanced
          const right = section.skills.filter((_, i) => i % 2 === 0)
          const left = section.skills.filter((_, i) => i % 2 === 1)
          const sectionStage = sectionIntroducedAt(section)
          return (
            <li
              key={section.name}
              aria-label={`Section ${section.name}`}
              className="relative grid grid-cols-[1fr_auto_1fr] items-center gap-x-5 py-3"
            >
              {section.skills.length > 0 && (
                <div
                  aria-hidden="true"
                  className={`absolute top-1/2 border-t-2 border-dashed border-slate-300 ${left.length > 0 ? 'left-[6%]' : 'left-1/2'} ${right.length > 0 || onAddSkill ? 'right-[6%]' : 'right-1/2'}`}
                />
              )}

              <div className="relative flex flex-wrap justify-end gap-2">
                {left.map(skill => (
                  <SkillChip key={skill.id} skill={skill} showLevels={showLevels} onSelect={onSelectSkill} sectionStage={sectionStage} />
                ))}
              </div>

              <SectionNode section={section} showLevels={showLevels} stage={sectionStage} />

              <div className="relative flex flex-wrap justify-start gap-2">
                {right.map(skill => (
                  <SkillChip key={skill.id} skill={skill} showLevels={showLevels} onSelect={onSelectSkill} sectionStage={sectionStage} />
                ))}
                {onAddSkill && (
                  <button
                    type="button"
                    onClick={() => onAddSkill(section.name)}
                    aria-label={`Add a skill to ${section.name}`}
                    title={`Add a skill to ${section.name}`}
                    className="inline-flex items-center justify-center w-8 h-8 rounded-full border-2 border-dashed border-slate-300 bg-white text-slate-400 hover:border-amber-400 hover:text-amber-600 transition-colors"
                  >
                    <Plus className="w-4 h-4" />
                  </button>
                )}
              </div>
            </li>
          )
        })}
      </ol>
    </div>
  )
}

/**
 * The stage a section of a roadmap starts at: the earliest stage of its skills, when every
 * skill says. Junior, the bottom rung, is not worth marking.
 */
function sectionIntroducedAt(section: TreeSection): CareerStage | null {
  const stages = section.skills.map(s => s.fromStage)
  if (stages.length === 0 || stages.some(s => !s)) return null
  const earliest = CAREER_STAGES.find(stage => stages.includes(stage)) ?? null
  return earliest === 'Junior' ? null : earliest
}

/** A small "Senior+" tag: from this stage of the career ladder upward. */
function StageTag({ stage, inverted = false }: { stage: CareerStage; inverted?: boolean }) {
  return (
    <span
      title={`Expected from ${stage} upward`}
      className={`inline-flex items-center px-1 rounded-sm text-[10px] font-semibold leading-4 ${inverted ? 'bg-slate-900/80 text-white' : 'bg-violet-100 text-violet-800'}`}
    >
      {stage}+
    </span>
  )
}

function SectionNode({ section, showLevels, stage }: { section: TreeSection; showLevels: boolean; stage: CareerStage | null }) {
  const cleared = showLevels && section.isCleared
  return (
    <div
      className={`relative z-10 min-w-36 max-w-56 px-4 py-2.5 rounded-lg border-2 text-center shadow-xs ${
        cleared ? 'bg-emerald-100 border-emerald-600 text-emerald-900' : 'bg-amber-300 border-slate-800 text-slate-900'
      }`}
    >
      <div className="flex items-center justify-center gap-1.5 font-semibold text-sm leading-tight">
        {cleared && <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" aria-label="Cleared" />}
        <span>{section.name}</span>
        {stage && <StageTag stage={stage} inverted />}
      </div>
      {showLevels && section.skillCount !== undefined && (
        <div className="text-xs mt-0.5 opacity-75">
          has {section.hasCount ?? 0} of {section.skillCount}
        </div>
      )}
    </div>
  )
}

function SkillChip({ skill, showLevels, onSelect, sectionStage }: { skill: TreeSkill; showLevels: boolean; onSelect?: (id: string) => void; sectionStage: CareerStage | null }) {
  // On a roadmap a skill is tagged when it starts later than Junior and its section does not
  // already say so; on a plan, when it belongs to a later stage than their role's
  const stageTag = showLevels
    ? (skill.expectedLevel === null && skill.fromStage ? skill.fromStage : null)
    : (skill.fromStage && skill.fromStage !== 'Junior' && skill.fromStage !== sectionStage ? skill.fromStage : null)
  const isSoft = skill.category === 'Soft'
  const level = skill.level ?? 0
  const status = skill.status ?? 'NotYet'
  const colour = showLevels
    ? SKILL_STATUS_STYLES[status]
    : isSoft ? 'bg-pink-50 border-pink-200 text-slate-800' : 'bg-white border-slate-300 text-slate-800'
  const shape = isSoft ? 'rounded-full' : 'rounded-md'
  const border = skill.isOptional ? 'border-dashed' : 'border-solid'
  const CategoryIcon = isSoft ? HeartHandshake : Cpu
  const expected = skill.expectedLevel ?? null

  const tooltip = [
    `${skill.title} (${skill.category} skill${skill.isOptional ? ', optional' : ''})`,
    skill.fromStage ? `Expected from ${skill.fromStage}` : '',
    showLevels ? `${SKILL_STATUS_LABELS[status]}: level ${level} ${SKILL_LEVEL_LABELS[level] ?? ''}${expected !== null ? `, their stage expects ${expected}` : ''}` : '',
    skill.description ?? ''
  ].filter(Boolean).join('\n')

  const content = (
    <>
      {showLevels && status === 'WorkingOn'
        ? <Crosshair className="w-3.5 h-3.5 shrink-0 text-amber-700" aria-hidden="true" />
        : <CategoryIcon className={`w-3.5 h-3.5 shrink-0 ${isSoft ? 'text-pink-500' : 'text-slate-400'}`} aria-hidden="true" />}
      <span className="truncate max-w-48">{skill.title}</span>
      {stageTag && <StageTag stage={stageTag} />}
      {showLevels && (
        <>
          <LevelDots level={level} />
          <span className="text-[11px] font-semibold tabular-nums opacity-80">
            {level}{expected !== null && ` / exp ${expected}`}
          </span>
        </>
      )}
    </>
  )

  const className = `relative z-10 inline-flex items-center gap-1.5 px-2.5 py-1.5 border-2 ${border} ${shape} ${colour} text-xs font-medium shadow-xs`

  if (!onSelect) {
    return <span className={className} title={tooltip}>{content}</span>
  }

  return (
    <button
      type="button"
      onClick={() => onSelect(skill.id)}
      title={tooltip}
      aria-label={showLevels ? `${skill.title}, ${SKILL_STATUS_LABELS[status].toLowerCase()}, level ${level}` : skill.title}
      className={`${className} hover:ring-2 hover:ring-amber-300 hover:-translate-y-0.5 transition-all`}
    >
      {content}
    </button>
  )
}

/** Five dots, filled up to a skill's level. */
export function LevelDots({ level }: { level: number }) {
  return (
    <span className="inline-flex gap-0.5" aria-hidden="true">
      {[1, 2, 3, 4, 5].map(dot => (
        <span key={dot} className={`w-1.5 h-1.5 rounded-full ${dot <= level ? 'bg-current' : 'bg-current opacity-20'}`} />
      ))}
    </span>
  )
}

/** Explains the chips: status colours on a plan, hard and soft, optional. */
export function SkillTreeLegend({ showLevels = false }: { showLevels?: boolean }) {
  return (
    <div className="flex flex-wrap items-center gap-x-5 gap-y-2 text-xs text-slate-500">
      {showLevels && (['Has', 'WorkingOn', 'NotYet'] as CareerSkillStatus[]).map(status => (
        <span key={status} className="flex items-center gap-1">
          <span className={`w-4 h-3 rounded-xs border-2 ${SKILL_STATUS_STYLES[status]}`} />
          {SKILL_STATUS_LABELS[status]}
        </span>
      ))}
      {showLevels && <span className="flex items-center gap-1"><Crosshair className="w-3.5 h-3.5 text-amber-700" /> Focus</span>}
      <span className="flex items-center gap-1"><Cpu className="w-3.5 h-3.5 text-slate-400" /> Hard skill</span>
      <span className="flex items-center gap-1"><HeartHandshake className="w-3.5 h-3.5 text-pink-500" /> Soft skill</span>
      <span className="flex items-center gap-1"><span className="w-4 h-3 border-2 border-dashed border-slate-400 rounded-xs" /> Optional</span>
    </div>
  )
}
