import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import * as fs from 'fs'
import * as os from 'os'
import * as path from 'path'
import { KeyProtector, loadOrCreateDatabaseKey, newApiPassword } from './secrets'

// Stands in for the OS keystore: reversible, and visibly not plain text
function fakeProtector(available = true): KeyProtector {
  return {
    isEncryptionAvailable: () => available,
    encryptString: (text) => Buffer.from(`wrapped:${Buffer.from(text).toString('base64')}`),
    decryptString: (data) => {
      const text = data.toString()
      if (!text.startsWith('wrapped:')) throw new Error('cannot decrypt')
      return Buffer.from(text.slice('wrapped:'.length), 'base64').toString()
    },
  }
}

describe('newApiPassword', () => {
  it('is 256 random bits, different on every launch', () => {
    const first = newApiPassword()
    expect(first).toMatch(/^[0-9a-f]{64}$/)
    expect(newApiPassword()).not.toBe(first)
  })
})

describe('loadOrCreateDatabaseKey', () => {
  let dir: string
  let keyFile: string

  beforeEach(() => {
    dir = fs.mkdtempSync(path.join(os.tmpdir(), 'hive-key-'))
    keyFile = path.join(dir, 'database-key')
  })

  afterEach(() => {
    fs.rmSync(dir, { recursive: true, force: true })
  })

  it('creates a key on first use and stores it only wrapped', () => {
    const result = loadOrCreateDatabaseKey(keyFile, fakeProtector())

    expect(result).toMatchObject({ encrypted: true, created: true })
    expect(result.key).toMatch(/^[0-9a-f]{64}$/)
    expect(fs.readFileSync(keyFile, 'utf8')).not.toContain(result.key as string)
  })

  it('returns the same key on later launches', () => {
    const first = loadOrCreateDatabaseKey(keyFile, fakeProtector())

    const second = loadOrCreateDatabaseKey(keyFile, fakeProtector())

    expect(second).toEqual({ encrypted: true, key: first.key, created: false })
  })

  it('leaves the database unencrypted when there is no keystore', () => {
    const result = loadOrCreateDatabaseKey(keyFile, fakeProtector(false))

    expect(result.key).toBeNull()
    expect(fs.existsSync(keyFile)).toBe(false)
  })

  it('throws, and keeps the file, when the stored key cannot be unwrapped', () => {
    fs.writeFileSync(keyFile, 'garbage')

    expect(() => loadOrCreateDatabaseKey(keyFile, fakeProtector())).toThrow()
    expect(fs.readFileSync(keyFile, 'utf8')).toBe('garbage')
  })
})
