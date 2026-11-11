import { describe, it, expect } from 'vitest'
import { parseBackendOutput } from './backendOutput'

describe('parseBackendOutput', () => {
  it('reads the level and message from the console logger format', () => {
    const output = 'info: Microsoft.Hosting.Lifetime[14]\n      Now listening on: http://localhost:5002\n'

    expect(parseBackendOutput(output, 'info')).toEqual([
      {
        level: 'info',
        message: 'Now listening on: http://localhost:5002',
        details: 'Category: Microsoft.Hosting.Lifetime[14]',
      },
    ])
  })

  it('maps fail and crit to error, and keeps the stack trace in the details', () => {
    const output = [
      'fail: Microsoft.AspNetCore.Server.Kestrel[13]',
      '      Unhandled exception.',
      '      System.InvalidOperationException: Boom',
      '         at Hive.Api.Program.Main()',
      'crit: Hive.Api[0]',
      '      Stopping',
    ].join('\n')

    const entries = parseBackendOutput(output, 'info')

    expect(entries).toHaveLength(2)
    expect(entries[0].level).toBe('error')
    expect(entries[0].message).toBe('Unhandled exception.')
    expect(entries[0].details).toContain('System.InvalidOperationException: Boom')
    expect(entries[0].details).toContain('   at Hive.Api.Program.Main()')
    expect(entries[1].level).toBe('error')
  })

  it('maps trce and dbug to debug, and warn to warn', () => {
    const output = 'dbug: A[1]\n      one\ntrce: B[2]\n      two\nwarn: C[3]\n      three'

    expect(parseBackendOutput(output, 'info').map(e => e.level)).toEqual(['debug', 'debug', 'warn'])
  })

  it('turns unformatted output into one entry per line at the fallback level', () => {
    const entries = parseBackendOutput('Unhandled exception. Boom\nsecond line\n', 'error')

    expect(entries).toEqual([
      { level: 'error', message: 'Unhandled exception. Boom' },
      { level: 'error', message: 'second line' },
    ])
  })

  it('uses the category as the message when no message line follows', () => {
    expect(parseBackendOutput('warn: Hive.Api[7]', 'info')).toEqual([
      { level: 'warn', message: 'Hive.Api[7]' },
    ])
  })
})
