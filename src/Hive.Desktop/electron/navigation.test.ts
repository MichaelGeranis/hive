import { describe, it, expect } from 'vitest'
import { isAppUrl, isSafeExternalUrl } from './navigation'

describe('isSafeExternalUrl', () => {
  it.each(['https://jira.example.com/browse/HIVE-1', 'http://intranet/wiki', 'mailto:someone@example.com'])(
    'opens %s in the browser',
    (url) => {
      expect(isSafeExternalUrl(url)).toBe(true)
    }
  )

  it.each([
    'file:///etc/passwd',
    'javascript:alert(1)',
    'smb://server/share',
    'ms-msdt:/id PCWDiagnostic',
    'vscode://file/c:/x',
    'not a url',
  ])('refuses %s', (url) => {
    expect(isSafeExternalUrl(url)).toBe(false)
  })
})

describe('isAppUrl', () => {
  const packaged = 'file:///Applications/Hive.app/Contents/Resources/app.asar/dist/index.html'

  it('accepts the packaged page, with any hash route', () => {
    expect(isAppUrl(`${packaged}#/notes`, packaged)).toBe(true)
  })

  it('refuses another local file', () => {
    expect(isAppUrl('file:///Users/me/Downloads/page.html', packaged)).toBe(false)
  })

  it('refuses a web page from the packaged app', () => {
    expect(isAppUrl('https://example.com/', packaged)).toBe(false)
  })

  it('accepts the dev server and refuses other origins', () => {
    expect(isAppUrl('http://localhost:5173/#/people', 'http://localhost:5173')).toBe(true)
    expect(isAppUrl('http://localhost:5174/', 'http://localhost:5173')).toBe(false)
  })
})
