import { useEffect, useState, type CSSProperties } from 'react'

// Three cells of a honeycomb, people, delivery and planning, meeting at one point: the dot.
// Pointy-top hexagons of radius R whose centres sit R away from that point, at the top and
// the lower left and right, so each pair shares an edge and all three share the dot.
const R = 10
const CELL_CENTRES: [number, number][] = [-90, 150, 30].map(degrees => {
  const radians = (degrees * Math.PI) / 180
  return [R * Math.cos(radians), R * Math.sin(radians)]
})

function hexagon([cx, cy]: [number, number]) {
  return [30, 90, 150, 210, 270, 330]
    .map(degrees => {
      const radians = (degrees * Math.PI) / 180
      return `${(cx + R * Math.cos(radians)).toFixed(3)},${(cy + R * Math.sin(radians)).toFixed(3)}`
    })
    .join(' ')
}

const CELLS = CELL_CENTRES.map(hexagon)

// Long enough for the last cell and the dot to arrive
const ENTRANCE_MS = 1200

// The cells span x ±√3R and y -2R..1.5R; the view box centres that, with room for the stroke
const VIEW_BOX = '-20 -22.5 40 40'

interface HiveMarkProps {
  /** Pixel size of the square mark */
  size?: number
  /** What needs attention: a number lights the dot amber, 0 dims it, null is not known yet */
  attention?: number | null
  /** Hive is working (a sync, a backup, starting up): the cells fill in turn */
  busy?: boolean
  /** The colour behind the mark, cut around the dot so it reads on the cells' junction */
  surface?: string
  /** Pop the cells in one by one, then light the dot, when the mark first appears */
  animateIn?: boolean
}

/**
 * Hive's mark. Quiet unless there is something to say: the cells pop in when Hive opens, the
 * dot lights up and its glow breathes while something needs attention, the cells fill in turn
 * while Hive is working, and a hover sends one wave through them. The motion is in index.css
 * and stops for anyone who prefers reduced motion.
 */
export function HiveMark({ size = 32, attention = null, busy = false, surface = '#0f172a', animateIn = false }: HiveMarkProps) {
  const alert = attention !== null && attention > 0
  const label = hiveMarkLabel(attention)

  // The entrance plays once: the class goes when it is over, so a hover or a sync ending never
  // brings it back
  const [entering, setEntering] = useState(animateIn)
  useEffect(() => {
    if (!entering) return
    const timer = setTimeout(() => setEntering(false), ENTRANCE_MS)
    return () => clearTimeout(timer)
  }, [entering])

  return (
    <svg
      width={size}
      height={size}
      viewBox={VIEW_BOX}
      role="img"
      aria-label={label}
      className={['hive-mark', busy && 'hive-mark-busy', alert && 'hive-mark-alert', entering && 'hive-mark-enter']
        .filter(Boolean)
        .join(' ')}
    >
      {CELLS.map((points, index) => (
        <polygon
          key={points}
          points={points}
          className="hive-mark-cell"
          style={{ '--i': index } as CSSProperties}
          fill="#fbbf24"
          fillOpacity={0.15}
          stroke="#fbbf24"
          strokeWidth={2}
          strokeLinejoin="round"
        />
      ))}
      {alert && <circle className="hive-mark-glow" cx={0} cy={0} r={8} fill="#fde047" opacity={0.45} />}
      <circle
        className="hive-mark-dot"
        cx={0}
        cy={0}
        r={alert ? 4.4 : 3.2}
        fill={alert ? '#fde047' : '#64748b'}
        stroke={surface}
        strokeWidth={2}
      />
    </svg>
  )
}

/**
 * What the mark says, for its label and a tooltip beside it.
 */
// eslint-disable-next-line react-refresh/only-export-components -- a small helper that belongs with the mark
export function hiveMarkLabel(attention: number | null) {
  if (attention === null) return 'Hive'
  if (attention === 0) return 'Hive: nothing needs attention'
  return `Hive: ${attention} ${attention === 1 ? 'thing needs' : 'things need'} attention`
}

/**
 * The name beside the mark: "hive", and what it is.
 */
export function HiveWordmark({ large = false }: { large?: boolean }) {
  return (
    <span className="flex flex-col leading-none">
      <span className={`lowercase font-semibold tracking-tight text-white ${large ? 'text-4xl' : 'text-xl'}`}>Hive</span>
      <span className={`mt-1 uppercase tracking-[0.2em] text-slate-400 ${large ? 'text-xs' : 'text-[9px]'}`}>EM OS</span>
    </span>
  )
}
