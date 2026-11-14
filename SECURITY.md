# Security Policy

## Supported versions

Only the latest release receives security fixes.

## Reporting a vulnerability

**Please do not report security problems in public issues, discussions or pull
requests.**

Report them privately through GitHub:
[**Report a vulnerability**](https://github.com/MichaelGeranis/hive/security/advisories/new).

Please include what the problem is, steps to reproduce, and the impact you
expect. Hive is maintained by one person in their own time. The aim is to
acknowledge a report within 7 days and agree a disclosure date once a fix is
understood. You will be credited in the advisory unless you prefer not to be.

## Security model

Hive is a single-user, local-first desktop app. Its API listens on localhost,
answers only loopback host names, and, in the desktop app, accepts a random
password made for each launch. The desktop app encrypts the SQLite database and
its backups with a key kept in the operating system's keychain. Running from
source uses a publicly known development password and an unencrypted database.
See the [Security note in the README](README.md#security-note).

Of particular interest:

- reaching the API from another machine, or from a web page in the browser;
- reading the database, its backups or the stored credentials without the key
  in the user's keychain;
- the Electron shell loading remote content or exposing Node.js to the page;
- injection through imported CSV, Jira data or calendar feeds.

Out of scope: running Hive's API on a shared host, which the README says not to do.
