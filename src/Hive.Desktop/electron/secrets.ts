import { randomBytes } from 'crypto'
import * as fs from 'fs'

/**
 * Encrypts and decrypts with a key the operating system keeps for this user (Electron's
 * safeStorage: Keychain on macOS, DPAPI on Windows, the secret service on Linux).
 */
export interface KeyProtector {
  isEncryptionAvailable(): boolean
  encryptString(plainText: string): Buffer
  decryptString(encrypted: Buffer): string
}

/**
 * The password the backend accepts for this launch only. It reaches the backend in its
 * environment and the window through the preload bridge, and is never written down, so
 * nothing else on the machine (or a web page) can know it.
 */
export function newApiPassword(): string {
  return randomBytes(32).toString('hex')
}

export type DatabaseKeyResult =
  | { encrypted: true; key: string; created: boolean }
  | { encrypted: false; key: null; reason: string }

/**
 * The key that encrypts Hive's database: 256 random bits, created on first use and stored
 * only wrapped by the OS keystore, in `keyFile`.
 *
 * A key file that exists but cannot be unwrapped throws instead of being replaced: a new key
 * could never open the existing database.
 */
export function loadOrCreateDatabaseKey(keyFile: string, protector: KeyProtector): DatabaseKeyResult {
  if (fs.existsSync(keyFile)) {
    const key = protector.decryptString(fs.readFileSync(keyFile))
    if (!/^[0-9a-f]{64}$/.test(key)) {
      throw new Error(`The database key in ${keyFile} is not valid`)
    }
    return { encrypted: true, key, created: false }
  }

  if (!protector.isEncryptionAvailable()) {
    return { encrypted: false, key: null, reason: 'the operating system keystore is not available' }
  }

  const key = randomBytes(32).toString('hex')
  fs.writeFileSync(keyFile, protector.encryptString(key), { mode: 0o600 })
  return { encrypted: true, key, created: true }
}
