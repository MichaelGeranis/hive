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

Hive is a single-user, local-first desktop app. Its API listens on localhost
with HTTP Basic credentials that default to a publicly known pair, and it
stores the optional Claude API key, Jira token and Google Calendar address in
plain text in the local SQLite database. See the
[Security note in the README](README.md#security-note).

Of particular interest:

- reaching the API from another machine, or from a web page in the browser;
- reading the stored credentials without access to the user's files;
- the Electron shell loading remote content or exposing Node.js to the page;
- injection through imported CSV, Jira data or calendar feeds.

Out of scope: running Hive's API on a shared host, which the README says not to do.
