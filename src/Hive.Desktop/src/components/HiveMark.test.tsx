import { describe, it, expect, vi, afterEach } from 'vitest'
import { act } from 'react'
import { render, screen, userEvent, waitFor } from '../test/test-utils'
import { HiveMark, HiveWordmark } from './HiveMark'
import { HiveStatusProvider, useHiveStatus } from '../contexts/HiveStatusContext'

describe('HiveMark', () => {
  it('says what needs attention, and lights the dot', () => {
    const { container } = render(<HiveMark attention={3} />)

    expect(screen.getByRole('img', { name: 'Hive: 3 things need attention' })).toBeInTheDocument()
    expect(container.querySelectorAll('polygon')).toHaveLength(3)
    expect(container.querySelector('circle[r="4.4"]')).toHaveAttribute('fill', '#fde047')
  })

  it('stays quiet when nothing needs attention, or before it is known', () => {
    const { container, rerender } = render(<HiveMark attention={0} />)
    expect(screen.getByRole('img', { name: 'Hive: nothing needs attention' })).toBeInTheDocument()
    expect(container.querySelector('circle[r="3.2"]')).toHaveAttribute('fill', '#64748b')

    rerender(<HiveMark attention={null} />)
    expect(screen.getByRole('img', { name: 'Hive' })).toBeInTheDocument()
  })

  it('fills its cells in turn only while Hive is working', () => {
    const { rerender } = render(<HiveMark />)
    expect(screen.getByRole('img')).not.toHaveClass('hive-mark-busy')

    rerender(<HiveMark busy />)
    expect(screen.getByRole('img')).toHaveClass('hive-mark-busy')
  })

  it('breathes the glow around the dot only while something needs attention', () => {
    const { rerender } = render(<HiveMark attention={2} />)
    expect(screen.getByRole('img')).toHaveClass('hive-mark-alert')

    rerender(<HiveMark attention={0} />)
    expect(screen.getByRole('img')).not.toHaveClass('hive-mark-alert')
  })

  describe('entrance', () => {
    afterEach(() => {
      vi.useRealTimers()
    })

    it('plays once when asked, then leaves the mark still', () => {
      vi.useFakeTimers()
      render(<HiveMark animateIn />)
      expect(screen.getByRole('img')).toHaveClass('hive-mark-enter')

      act(() => { vi.advanceTimersByTime(1200) })

      expect(screen.getByRole('img')).not.toHaveClass('hive-mark-enter')
    })

    it('does not play unless asked', () => {
      render(<HiveMark />)
      expect(screen.getByRole('img')).not.toHaveClass('hive-mark-enter')
    })
  })

  it('writes the name as text, lowercased only in how it looks', () => {
    render(<HiveWordmark />)

    expect(screen.getByText('Hive')).toHaveClass('lowercase')
    expect(screen.getByText('EM OS')).toBeInTheDocument()
  })
})

describe('HiveStatusProvider', () => {
  function Worker({ work }: { work: Promise<void> }) {
    const { busy, trackWork } = useHiveStatus()
    return (
      <>
        <HiveMark busy={busy} />
        <button onClick={() => trackWork(work).catch(() => {})}>Sync</button>
      </>
    )
  }

  it('is busy while the work runs, and not after, however it ends', async () => {
    const user = userEvent.setup()
    let finish!: () => void
    const work = new Promise<void>((_, reject) => { finish = () => reject(new Error('Jira is down')) })
    work.catch(() => {})
    render(<HiveStatusProvider><Worker work={work} /></HiveStatusProvider>)

    await user.click(screen.getByRole('button', { name: 'Sync' }))
    expect(screen.getByRole('img')).toHaveClass('hive-mark-busy')

    finish()
    await waitFor(() => expect(screen.getByRole('img')).not.toHaveClass('hive-mark-busy'))
  })
})
