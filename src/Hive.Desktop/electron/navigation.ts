/**
 * Which links leave the app for the browser, and which navigations the window may make.
 * Notes, Jira data and imported files can carry links, so neither is taken on trust.
 */

const EXTERNAL_PROTOCOLS = new Set(['https:', 'http:', 'mailto:'])

/** Only web and mail links are handed to the operating system; never file:, javascript: or app schemes. */
export function isSafeExternalUrl(url: string): boolean {
  try {
    return EXTERNAL_PROTOCOLS.has(new URL(url).protocol)
  } catch {
    return false
  }
}

/**
 * True when `url` is the app's own page: the dev server's origin, or the packaged index.html.
 * The app routes with hashes, so any other document means the window is being taken away.
 */
export function isAppUrl(url: string, appUrl: string): boolean {
  try {
    const target = new URL(url)
    const app = new URL(appUrl)
    if (app.protocol === 'file:') {
      return target.protocol === 'file:' && target.pathname === app.pathname
    }
    return target.origin === app.origin
  } catch {
    return false
  }
}
