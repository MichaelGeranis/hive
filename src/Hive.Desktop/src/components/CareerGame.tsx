import {
  Award,
  CheckCircle2,
  Flame,
  Footprints,
  Gauge,
  Lock,
  Moon,
  Rocket,
  Sparkles,
  Users,
  Wand2,
  Waves
} from 'lucide-react'
import type { CareerBadge, CareerGame, CareerStage } from '../types'

/**
 * The presentational pieces of a career plan: the stage chip, the quiet chip, and the game
 * (player level, XP bar, badges), which is only ever shown on a person's own plan. Every number
 * they show comes from the backend.
 */

const BADGE_ICONS: Record<string, { icon: typeof Award; color: string; bg: string }> = {
  'first-steps': { icon: Footprints, color: 'text-emerald-600', bg: 'bg-emerald-100' },
  'got-it': { icon: CheckCircle2, color: 'text-green-600', bg: 'bg-green-100' },
  'deep-diver': { icon: Waves, color: 'text-sky-600', bg: 'bg-sky-100' },
  'on-a-roll': { icon: Flame, color: 'text-orange-600', bg: 'bg-orange-100' },
  'trailblazer': { icon: Rocket, color: 'text-violet-600', bg: 'bg-violet-100' },
  'tech-wizard': { icon: Wand2, color: 'text-indigo-600', bg: 'bg-indigo-100' },
  'people-person': { icon: Users, color: 'text-pink-600', bg: 'bg-pink-100' },
  'role-ready': { icon: Gauge, color: 'text-teal-600', bg: 'bg-teal-100' },
  'ahead-of-the-curve': { icon: Sparkles, color: 'text-amber-600', bg: 'bg-amber-100' }
}

const DEFAULT_BADGE = { icon: Award, color: 'text-amber-600', bg: 'bg-amber-100' }

// Each rung of the career ladder in its own colour, deepening up the ladder
const STAGE_STYLES: Record<CareerStage, string> = {
  Junior: 'bg-sky-50 text-sky-800 border-sky-200',
  Mid: 'bg-indigo-50 text-indigo-800 border-indigo-200',
  Senior: 'bg-violet-100 text-violet-800 border-violet-300',
  Staff: 'bg-slate-800 text-white border-slate-800'
}

/** The stage of someone's current role: the job level, as opposed to the game's player level. */
export function StageChip({ stage, size = 'md' }: { stage: CareerStage; size?: 'sm' | 'md' | 'lg' }) {
  const sizes = {
    sm: 'px-1.5 py-0 text-[11px]',
    md: 'px-2 py-0.5 text-xs',
    lg: 'px-3 py-1 text-sm'
  }
  return (
    <span
      title={`Current role's stage: ${stage}`}
      aria-label={`Career stage ${stage}`}
      className={`inline-flex items-center rounded-md border font-semibold ${sizes[size]} ${STAGE_STYLES[stage] ?? STAGE_STYLES.Mid}`}
    >
      {stage}
    </span>
  )
}

export function QuietChip() {
  return (
    <span
      className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-600 border border-slate-200"
      title="Nothing on this plan has changed for 60 days"
    >
      <Moon className="w-3 h-3" />
      Quiet
    </span>
  )
}

/** The player level in a token, the way Hive's logo looks. */
export function LevelToken({ level, size = 'md', celebrate = false }: { level: number; size?: 'sm' | 'md' | 'lg'; celebrate?: boolean }) {
  const sizes = {
    sm: 'w-8 h-8 text-sm',
    md: 'w-11 h-11 text-lg',
    lg: 'w-16 h-16 text-2xl'
  }
  return (
    <div
      aria-label={`Player level ${level}`}
      className={`${sizes[size]} ${celebrate ? 'animate-bounce' : ''} flex-shrink-0 rounded-xl bg-gradient-to-br from-amber-400 to-amber-600 text-white font-bold flex items-center justify-center shadow-sm ring-2 ring-amber-200`}
    >
      {level}
    </div>
  )
}

/** Progress from this player level's XP to the next. */
export function XpBar({ game }: { game: CareerGame }) {
  const span = game.nextLevelXp - game.currentLevelXp
  const into = game.xp - game.currentLevelXp
  const percent = span > 0 ? Math.max(0, Math.min(100, (into / span) * 100)) : 100
  return (
    <div>
      <div className="flex justify-between gap-3 text-xs text-slate-500 mb-1">
        <span className="font-medium text-slate-700">{game.xp} XP</span>
        <span>{game.nextLevelXp - game.xp} XP to level {game.playerLevel + 1}</span>
      </div>
      <div
        className="w-full bg-slate-100 rounded-full h-2 overflow-hidden"
        role="progressbar"
        aria-label="XP to next level"
        aria-valuemin={game.currentLevelXp}
        aria-valuemax={game.nextLevelXp}
        aria-valuenow={game.xp}
      >
        <div className="h-full rounded-full bg-gradient-to-r from-amber-400 to-amber-500 transition-all" style={{ width: `${percent}%` }} />
      </div>
    </div>
  )
}

function BadgeIcon({ badge }: { badge: CareerBadge }) {
  const style = BADGE_ICONS[badge.key] ?? DEFAULT_BADGE
  const Icon = style.icon
  return (
    <div className={`w-11 h-11 rounded-full flex items-center justify-center relative ${badge.isEarned ? `${style.bg} ring-2 ring-white shadow-sm` : 'bg-slate-100'}`}>
      <Icon className={`w-5 h-5 ${badge.isEarned ? style.color : 'text-slate-300'}`} />
      {!badge.isEarned && (
        <Lock className="w-3 h-3 text-slate-400 absolute -bottom-0.5 -right-0.5 bg-white rounded-full p-px" />
      )}
    </div>
  )
}

/** Every badge: earned ones in colour, locked ones greyed with how to earn them. */
export function BadgeShelf({ badges }: { badges: CareerBadge[] }) {
  return (
    <ul className="grid grid-cols-3 sm:grid-cols-5 lg:grid-cols-9 gap-3" aria-label="Badges">
      {badges.map(badge => (
        <li
          key={badge.key}
          title={`${badge.name}: ${badge.description}${badge.isEarned ? '' : ' (locked)'}`}
          aria-label={`${badge.name}${badge.isEarned ? ' (earned)' : ' (locked)'}`}
          className="flex flex-col items-center text-center gap-1.5"
        >
          <BadgeIcon badge={badge} />
          <span className={`text-xs leading-tight ${badge.isEarned ? 'font-medium text-slate-800' : 'text-slate-400'}`}>
            {badge.name}
          </span>
        </li>
      ))}
    </ul>
  )
}
