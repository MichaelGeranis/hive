import { ReactNode } from 'react'
import { Bee } from './AiBeeBadge'

interface CardProps {
  children: ReactNode
  className?: string
}

export function Card({ children, className = '' }: CardProps) {
  return (
    <div className={`bg-white rounded-xl shadow-sm border border-slate-200 ${className}`}>
      {children}
    </div>
  )
}

export interface CardHeaderProps {
  title?: string
  subtitle?: string
  badge?: ReactNode
  action?: ReactNode
  /** The card's content is AI work: the bee marks it, the AI bees working for the hive. */
  ai?: boolean
  children?: ReactNode
  className?: string
}

export function CardHeader({ title, subtitle, badge, action, ai = false, children, className = '' }: CardHeaderProps) {
  return (
    <div className={`px-6 py-4 border-b border-slate-100 ${className}`}>
      {children ? (
        children
      ) : (
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            {title && (
              <div className="flex items-center gap-2">
                {ai && (
                  <span title="Done by the AI bees" aria-label="Uses AI" className="inline-flex">
                    <Bee size={20} />
                  </span>
                )}
                <h3 className="text-lg font-semibold text-slate-900">{title}</h3>
                {badge}
              </div>
            )}
            {subtitle && <p className="text-sm text-slate-500 mt-0.5">{subtitle}</p>}
          </div>
          {action && <div>{action}</div>}
        </div>
      )}
    </div>
  )
}

interface CardContentProps {
  children: ReactNode
  className?: string
}

export function CardContent({ children, className = '' }: CardContentProps) {
  return <div className={`px-6 py-4 ${className}`}>{children}</div>
}

interface StatCardProps {
  title: string
  value: string | number
  subtitle?: string
  icon?: ReactNode
  badge?: ReactNode
  trend?: {
    value: number
    isPositive: boolean
  }
  color?: 'blue' | 'green' | 'amber' | 'red' | 'purple' | 'slate'
  onClick?: () => void
}

const colorStyles = {
  blue: 'bg-blue-50 text-blue-600',
  green: 'bg-green-50 text-green-600',
  amber: 'bg-amber-50 text-amber-600',
  red: 'bg-red-50 text-red-600',
  purple: 'bg-purple-50 text-purple-600',
  slate: 'bg-slate-50 text-slate-600',
}

export function StatCard({ title, value, subtitle, icon, badge, trend, color = 'blue', onClick }: StatCardProps) {
  const isClickable = !!onClick

  return (
    <div
      onClick={onClick}
      className={`h-full min-h-[140px] ${isClickable ? 'cursor-pointer hover:scale-105 transition-transform' : ''}`}
    >
      <Card className="h-full">
        <CardContent className="flex items-start justify-between h-full">
          <div className="flex-1">
            <div className="flex items-center gap-2">
              <p className="text-sm font-medium text-slate-500">{title}</p>
              {badge}
            </div>
            <p className="text-2xl font-bold text-slate-900 mt-1">{value}</p>
            {subtitle && <p className="text-sm text-slate-500 mt-1">{subtitle}</p>}
            {trend && (
              <p className={`text-sm mt-1 ${trend.isPositive ? 'text-green-600' : 'text-red-600'}`}>
                {trend.isPositive ? '+' : ''}{trend.value}%
              </p>
            )}
          </div>
          {icon && (
            <div className={`p-3 rounded-lg ${colorStyles[color]} flex-shrink-0`}>
              {icon}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
