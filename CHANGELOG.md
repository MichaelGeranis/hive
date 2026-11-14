# Changelog

All notable changes are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project
follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- The desktop app encrypts the database and its automatic backups, with a key
  kept in the operating system's keychain. An existing database, and its
  backups, are encrypted in place on the first start of this version.
- Installers for macOS, Windows and Linux attached to each GitHub release.
- Apache 2.0 licence, contributing guide, security policy, code of conduct,
  issue and pull request templates, and Dependabot.

### Changed

- The desktop app gives the API a new random password on every launch instead
  of the public `admin` / `admin123`, which the backend now refuses outside
  Development.
- The API answers only loopback host names (`localhost`, `127.0.0.1`, `[::1]`),
  so web pages cannot reach it through DNS rebinding.
- Only web and mail links open outside the app, and the window cannot be
  navigated away from Hive.
- .NET 11 and EF Core 11, supported until November 2028 (the same month as
  .NET 10); .NET 9 support ended on 10 November 2026. The database schema is
  unchanged, and databases created by earlier builds open without a migration.
- The Plan-Catalogue spreadsheet is written with ClosedXML (MIT) instead of
  EPPlus, whose licence does not allow commercial use without a paid licence.
- `scripts/build-backend.sh` builds for Linux on Linux.
- Every dependency updated to its latest version: Anthropic C# SDK 13 (API
  version `2026-10-21`; the SDK is now Apache-2.0), Swashbuckle 11, React 19,
  Electron 45, Tailwind CSS 4, Recharts 3, lucide-react 1, Vite 8.3, Vitest 5,
  MSW 3, ESLint 10 and the rest. TypeScript stays on 5.9 until
  typescript-eslint supports TypeScript 7.
- Tailwind CSS 4 keeps Hive's look: v3's colour palette, placeholder colour
  and button cursor are kept. Some icons are redrawn by lucide-react 1.
- CI and release builds run on Node.js 26, with every GitHub Action pinned to a
  commit SHA. Dependabot opens one grouped pull request per ecosystem a week.

### Fixed

- The Windows installer build: electron-builder copied the backend into the
  package twice, and Windows' file locking failed the second copy (`EBUSY`).
