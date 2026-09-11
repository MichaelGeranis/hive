# Hive — Business Concepts

This document defines the business rules, domain concepts, and use cases of Hive.
For technical architecture and implementation details, see [ARCHITECTURE.md](ARCHITECTURE.md).

---

## Core Concept

Hive is the **Engineering Manager's operating system**. An EM's job is spread across
half a dozen disconnected tools: performance reviews in an HR system, 1:1 notes in a
personal doc, sprint data in Jira, capacity in a spreadsheet, quarterly plans in slides.
None of them talk to each other, so the EM is the integration layer — and the questions
that actually matter ("is this person growing?", "can the team absorb this initiative?",
"who is the only person who understands this project?") take an afternoon to answer.

Hive puts all of it in one local, single-user desktop application, and derives the
answers instead of making the EM assemble them.

The fundamental contract: **the EM owns the data, Hive owns the analysis.**

**Rules:**
- Hive is single-user. There is one manager, and everything in the database is their view.
- Hive is local-first. The database is a file on the manager's machine, not a server.
- Hive is a system of record for management data, not a system of record for delivery.
  Task data is *imported* from Jira; Hive never writes back.

---

## The Manager's Job

Hive divides the EM role into six domains. Every entity belongs to exactly one.

| Domain | Question it answers | Core entities |
|--------|---------------------|---------------|
| People & Growth | Who is on the team and are they developing? | `DirectReport`, `PerformanceReview`, `Skill`, `SkillAssessment` |
| Conversations | What have we talked about? | `OneOnOneMeeting`, `ManagerNote`, `NoteFolder` |
| Availability | Who is here, and when? | `Leave` |
| Delivery | What is the team working on and how is it going? | `Project`, `TeamTask`, `Parent`, `Sprint`, `SprintCapacity` |
| Planning | What are we committing to next quarter, and can we? | `Quarter`, `Initiative`, `Allocation`, `InitiativeDependency` |
| Process | What repeatable procedures are in flight? | `ChecklistTemplate`, `ChecklistInstance` |

---

## Domain Model

### DirectReport

A person the manager is responsible for.

| Property | Business meaning |
|----------|------------------|
| FirstName / LastName | The person's name; `FullName` is the display form |
| Email | Unique identifier for the person, also used to match Jira assignees on import |
| JobTitle | Current role |
| Department | Organisational unit |
| HireDate | Start date — drives tenure and onboarding checklists |
| IsDirect | `true` for a direct report, `false` for a skip-level or dotted-line report |

**Rules:**
- Name fields cannot be empty and cannot exceed 100 characters.
- Email must be a valid address and cannot exceed 255 characters.
- `IsDirect = false` people are tracked and reportable, but are excluded from analyses
  that model the manager's own team — notably knowledge-level suggestions.
- A `DirectReport` is never deleted to represent a leaver; the record is the history.

### Parent

A parent work item — an epic or a Jira parent issue that groups tasks. Despite the name
this is **not** an organisational parent or a manager.

| Property | Business meaning |
|----------|------------------|
| Name | The parent item's title |
| Labels | Comma-separated labels inherited by, or shared with, its children |
| TimeSpentMinutes | Aggregate time logged against the parent in Jira |
| TeamTaskId | The `TeamTask` this parent corresponds to, once one is imported |

**Rules:**
- Name cannot be empty and cannot exceed 500 characters.
- Parents are created by the Jira import, not by hand. A Jira issue that is referenced
  as another issue's parent becomes a `Parent`.

### PerformanceReview

A formal review of a direct report covering a named period.

| Property | Business meaning |
|----------|------------------|
| ReviewPeriod | Human-readable period label, e.g. `H1 2026` |
| ReviewDate | When the review was held |
| Rating | `NotRated`, `NeedsImprovement`, `MeetsExpectations`, `ExceedsExpectations`, `Outstanding` |
| Strengths | What went well |
| AreasForImprovement | What needs to develop |
| ManagerNotes | Private commentary |

**Rules:**
- Review period cannot be empty and cannot exceed 50 characters.
- `NotRated` is a real state, not a missing value. It means the review exists but no
  rating was assigned — a draft, or a review deliberately held without a score.
- Reviews are append-only in spirit: a new period gets a new review, never an edit of
  the previous one.

### Skill, SkillCategory and SkillAssessment

The team's capability map.

| Entity | Business meaning |
|--------|------------------|
| `SkillCategory` | A grouping of skills — Technical, Soft Skills, Leadership, Domain Knowledge, Tools |
| `Skill` | A single named capability within a category |
| `SkillAssessment` | One person's current and target level in one skill |

`ProficiencyLevel` is `None`, `Novice`, `Beginner`, `Intermediate`, `Advanced`, `Expert`.

**Rules:**
- Skills and categories are **deactivated, never deleted** — `Deactivate()` sets
  `IsActive = false`. Historical assessments stay meaningful because the skill still exists.
- An assessment carries both a current `Level` and an optional `TargetLevel`. The gap
  between them is the development plan; `MeetsTarget()` reports whether it is closed.
- A skill with no assessment for a person is not the same as an assessment at `None`.
  The first means "never evaluated", the second means "evaluated, has none".
- Categories carry a `SortOrder` so the heatmap renders in a stable, meaningful order.

### OneOnOneMeeting

The 1:1 record. **A 1:1 is a single markdown note**, written while the meeting happens.
There is no agenda field, no notes inside the note, and no action items.

| Property | Business meaning |
|----------|------------------|
| Content | What was discussed, written as markdown |
| Title | The heading, **derived from the first line of the content** |
| MeetingDate | The day the 1:1 happened |
| Tags | Comma-separated tags. One names the person, one may set the date |
| DirectReportId | The person the tags resolved to; **null when the 1:1 is not linked to anyone** |

**Rules:**
- The manager never picks a person from a list. **The person is derived from the tags**,
  the same way the title is derived from the first line. A tag matches a direct report by
  first name, last name, or the two joined in either order, compared on letters and digits
  only — so `#badredin`, `#panagiotis`, `#panagiotisbadredin` and `#badredinpanagiotis` all
  name the same person.
- **A 1:1 whose tags name nobody, or name more than one person, is kept and shown as
  unlinked.** Hive never guesses between two people, and never silently drops the note out
  of the record. Re-tagging it re-resolves the link.
- The meeting date defaults to the day the note was started. A `#YYYYMMDD` tag moves it,
  so a 1:1 written up late still lands on the day it happened.
- Title cannot exceed 200 characters; an empty note is titled `New 1:1`. The body is free
  text with no length limit, and whitespace is preserved exactly as typed.
- The 1:1 is the whole record: **sentiment analysis reads these bodies**, and the number of
  1:1s logged is what the dashboard reports.

### ManagerNote and NoteFolder

The manager's own private notes — not attached to any person or meeting. A note is a page
to write on: it is created empty, saved as it is written, and its body is markdown.

| Property | Business meaning |
|----------|------------------|
| Content | The body of the note, written as markdown |
| Title | The heading of the note, **derived from the first line of the content** |
| FolderId | The folder the note is filed in; none means it sits at the root |
| IsPinned | Pinned notes are listed above every other note in their folder |
| Tags | Comma-separated free-form tags |
| IsTodo | Whether this note is also tracked as a to-do |
| Priority | `Low`, `Normal`, `High`, `Urgent` — only meaningful for a to-do |
| IsCompleted / CompletedAt | Whether this to-do got done |
| DueDate | Optional deadline |

**Rules:**
- Title cannot be empty and cannot exceed 200 characters. When a note is written rather
  than filled in on a form, the title is **derived**: the first non-empty line of the
  content with its markdown decoration stripped, truncated to 200 characters. An empty
  note is titled `New Note`. The manager never types a title separately.
- A blank note is a valid note. It exists from the moment it is created, so nothing typed
  into it can be lost, and it is saved as it is written rather than on a Save button.
- The body is free text with no length limit, and whitespace is preserved exactly as
  typed — leading indentation and blank lines are part of what was written.
- **Only a note flagged `IsTodo` is a to-do.** Pending, overdue and completed counts —
  including the dashboard's TODO figure — count those notes and no others. Clearing the
  flag also clears completion: a note that is not a to-do cannot be done.
- A `ManagerNote` is not about a person. Notes about a direct report belong on a meeting
  or a review, where they are part of that person's record.

A `NoteFolder` groups notes. Folders may be nested.

| Property | Business meaning |
|----------|------------------|
| Name | The folder's name |
| ParentFolderId | The folder it sits in; none means a top-level folder |
| SortOrder | Position among its siblings |

**Rules:**
- Folder name cannot be empty and cannot exceed 100 characters.
- A folder cannot be moved inside itself or inside one of its own sub-folders.
- **Deleting a folder never deletes what was written in it.** Its notes and its
  sub-folders move up to the folder that contained it. Notes are the record; folders are
  only how the manager arranges them.
- A note filed nowhere is not lost — it is simply listed under "All Notes".

### Leave

Time a direct report is not available.

| Property | Business meaning |
|----------|------------------|
| Type | `Vacation`, `Sick`, `Other`, `PublicHoliday` |
| Status | `Active` or `Cancelled` |
| StartDate / EndDate | Inclusive date range |
| Notes | Optional context |

**Rules:**
- End date cannot be before start date, and a single leave cannot exceed **365 days**.
- `DaysCount` is inclusive of both endpoints. `BusinessDaysCount` excludes weekends —
  this is the figure that feeds capacity planning, not the calendar count.
- `OverlapsWith(start, end)` and `IncludesDate(date)` are the domain's overlap primitives.
  Capacity and calendar views must use them rather than re-deriving date maths.
- Leave in Hive is a **record, not a request**. There is no approval workflow, because
  Hive is the manager's own tool — by the time it is entered here it is already decided.
  This is why the only statuses are `Active` and `Cancelled`.

### Project and ProjectKnowledge

| Entity | Business meaning |
|--------|------------------|
| `Project` | A named body of work, with labels used to attribute tasks to it |
| `ProjectKnowledge` | One person's knowledge level (1–5) on one project |

**Rules:**
- Project name cannot be empty and cannot exceed 200 characters.
- `Labels` are the join between a project and imported Jira tasks. A task belongs to a
  project either by explicit `ProjectId` or by sharing a label with it.
- Knowledge level must be **between 1 and 5**. Level 1 is the default for someone who has
  touched the project at all; there is no level 0.
- A project with only one person above level 2 is a bus-factor risk. This is what the
  `MinProjectMembers` threshold surfaces on the dashboard.

### KnowledgePoint

Earned evidence that someone knows a project, used to *suggest* knowledge level changes.

| Property | Business meaning |
|----------|------------------|
| DirectReportId / ProjectId | Whose knowledge, of which project |
| ManualPoints | Points the manager awarded by hand, on top of earned points |
| Notes | Why points were awarded |

**Rules:**
- Points can never be negative — neither manual points nor an `AddPoints` increment.
- **Earned points are derived, not stored.** They are the sum of story points on that
  person's `Done` tasks attributed to the project (a task with no story points counts as 1).
  Total points = earned + manual.
- Attribution is by direct `ProjectId` **or** by shared label, matched case-insensitively.
- Points *suggest*, they never *set*. The thresholds are:

  | Suggested level | Total points |
  |---|---|
  | 2 | 5 |
  | 3 | 13 |
  | 4 | 21 |
  | 5 | 55 |

  Level 1 is the default (0–4 points). The manager accepts or ignores each suggestion —
  Hive never promotes someone automatically, because points measure exposure, not judgement.
- Suggestions are only generated for people where `IsDirect` is true.

### TeamTask

A unit of delivery work, almost always imported from Jira.

| Property | Business meaning |
|----------|------------------|
| Type | `Task`, `Epic`, `Story`, `SubTask`, `Bug`, `Spike`, `Support` |
| Priority | `Low`, `Medium`, `High`, `Critical` |
| Status | See the lifecycle below |
| AssigneeId / ProjectId / ParentId | Who owns it, what it belongs to, what it rolls up into |
| StoryPoints / EstimatedHours / TimeSpentMinutes | Estimate and actuals — the basis of velocity and estimation accuracy |
| Sprint | The sprint name this task is in |
| Labels / Tags / Components | Free-form classification, also used for support/maintenance attribution |
| PreviousSprintsStoryPoints | Points this task carried in earlier sprints, for carry-over analysis |
| OverriddenFields | Which fields the manager edited by hand, so a re-import will not overwrite them |

**Rules:**
- `OverriddenFields` is what makes re-importing from Jira safe. Any field listed there is
  the manager's, and Jira does not get to overwrite it.
- Support and maintenance work is identified by matching a task's labels against the
  configurable `SupportLabels` and `MaintenanceLabels` settings — not by task type.

#### Task lifecycle

```
Backlog ──> Todo ──> InProgress ──> InReview ──> InTest ──> POAcceptance ──> ReadyToRelease ──> Done
              ▲          │                                                                        │
              │          └──> Blocked                                                             │
              └──────────────────── Reopen ◄─────────────────────────────────────────────────────┘
                                                        Cancelled  (from any non-Done state)
```

**Transition rules — enforced on the entity, not in a service:**
- `Backlog` and `Todo` are always reachable; they have no preconditions.
- `Start()`, `Block()`, `MoveToTest()`, `MoveToPOAcceptance()` and `MoveToReadyToRelease()`
  are rejected for a task that is `Done` or `Cancelled`.
- `MoveToReview()` is the strictest transition: only from `InProgress`, `Blocked` or
  `InTest`. Work cannot enter review without having been worked on.
- `Complete()` is rejected for a `Cancelled` task. `Cancel()` is rejected for a `Done` task.
  A finished task cannot be un-finished by cancellation, and vice versa.
- `Reopen()` is the only way back, valid only from `Done` or `Cancelled`, and it always
  lands in `Todo` — never in the status the task previously held.

### Sprint and SprintCapacity

| Entity | Business meaning |
|--------|------------------|
| `Sprint` | A time-boxed iteration, identified by team, year, quarter and sprint number |
| `SprintCapacity` | The team's total capacity in points for one sprint, and how many people were available |
| `SprintGoal` | A stated goal for one sprint within a quarter |

**Rules:**
- A sprint's identity is `TeamName_QuarterQYear_SSprintNumber` (e.g. `LP_1Q25_S4`). This
  is the format the Jira import parses; sprints not matching it are ignored.
- Sprints have a deterministic ordering derived from year, quarter and number
  (`GetSortOrder()`, `IsBefore()`, `IsAfter()`). Never sort sprints by name or by date —
  dates are optional and names sort lexically, which is wrong.
- Capacity and available members cannot be negative. `GetCapacityPerMember()` is the
  per-person figure used to project future sprints.
- Sprint goals cannot exceed 4000 characters.

### Quarter, Initiative and Allocation

Quarterly planning is **initiative-centric**: the quarter holds initiatives, and people
are allocated to initiatives sprint by sprint.

| Entity | Business meaning |
|--------|------------------|
| `Quarter` | A planning period, with a status and an optional OKR reference |
| `Initiative` | A body of work committed to within a quarter |
| `Allocation` | One person, on one initiative, for one sprint |
| `InitiativeMember` | A person nominally on an initiative, independent of sprint allocation |
| `InitiativeDependency` | A directed dependency between two initiatives |

**Quarter rules:**
- Year must be between 2000 and 2100; quarter number between 1 and 4.
- `QuarterStatus` moves `Planning → Active → Completed`. A completed quarter cannot be
  re-activated, and a quarter still in `Planning` cannot be completed — planning must be
  finished before delivery can be declared done.
- `ResetToPlanning()` is the deliberate escape hatch for a mis-advanced quarter.

**Initiative rules:**
- Name cannot be empty and cannot exceed 200 characters.
- `Color` must be a valid hex colour in `#RGB` or `#RRGGBB` form. Colour is not decoration —
  it is how an initiative is tracked across the planning matrix and spreadsheet.
- `TshirtSize` must be one of the configured sizes and defaults to `M`. Sizes map to
  effort via the `TshirtSizeMappings` setting, which is how planned effort is computed.
- `Url` must be a valid HTTP or HTTPS URL and cannot exceed 500 characters.
- `WorkType` is `Maintenance`, `ProductRoadmap` or `TechRoadmap` — the split the EM
  reports upward, so every initiative must be attributable to one of the three.

**Allocation and dependency rules:**
- An allocation is the triple (initiative, person, sprint). Allocating the same person to
  the same initiative across three sprints is three allocations.
- Effort per initiative counts **distinct** initiatives per member per sprint. A person
  allocated twice to the same initiative in one sprint is not double the work.
- **An initiative cannot depend on itself** — rejected at construction.
- A dependency is a risk when the dependent initiative's earliest allocated sprint is not
  after the dependency's last sprint. Overlapping or inverted ordering is flagged.

### ChecklistTemplate and ChecklistInstance

Repeatable procedures — currently interviewing and onboarding.

| Entity | Business meaning |
|--------|------------------|
| `ChecklistTemplate` + `ChecklistTemplateItem` | The reusable definition |
| `ChecklistInstance` + `ChecklistInstanceItem` | One run of that definition |

`ChecklistType` is `Interview` or `Onboarding`. `ChecklistItemType` is `Question`, `Topic`,
`Task`, `Document` or `Training`.

**Rules:**
- An instance **copies** its items from the template at creation. Editing a template never
  changes a checklist already in flight.
- The template's type must match the instance being created — an onboarding template
  cannot back an interview checklist.
- An instance carries the fields for its type: `CandidateName`, `Position` and
  `InterviewDate` for interviews; `NewHireName`, `StartDate` and `TargetCompletionDate`
  for onboarding.
- Instance status is `NotStarted → InProgress → Completed`, with `Cancelled` available
  throughout. A completed or cancelled instance cannot be started; a cancelled one cannot
  be completed; a completed one cannot be cancelled.
- Item status is `Pending`, `InProgress`, `Completed`, `Skipped`, `NotApplicable`.
  **A completed item's status cannot be changed**, and **a required item cannot be skipped**.
- An item may carry a `Score` from 1 to 5 — this is what makes an interview checklist a
  scorecard rather than a to-do list.

### Document

A reference document or link the manager keeps: team charters, runbooks, process notes.

**Rules:**
- Title cannot be empty and cannot exceed 500 characters.
- A document holds either inline `Content` or an external `Url`, or both.

### Activity

An append-only audit trail of what changed in Hive.

| Property | Business meaning |
|----------|------------------|
| ActivityType | `Created`, `Updated`, `StatusChanged`, `Approved`, `Rejected`, `Completed`, `Deleted`, `Cancelled` |
| EntityType / EntityId / EntityName | What was affected |
| Description | Human-readable summary |
| Timestamp | When it happened |

**Rules:**
- Activities are **never modified or deleted**. The feed is the history.
- Entity name cannot exceed 500 characters; description cannot exceed 1000.
- Every entity type that appears in the UI has a matching `EntityType` value, so the feed
  can link back to the thing that changed.

### SentimentAnalysisCache

Cached output of Claude-powered sentiment analysis over a person's meeting notes.

**Rules:**
- This is a **cache**, not a record. It can be discarded and regenerated at any time.
- It stores what it analysed (`NotesAnalyzed`, `DaysAnalyzed`, `LatestNoteDate`) so a
  stale result can be detected when new notes arrive.
- Sentiment analysis is **off by default** and requires the manager to supply their own
  Claude API key. No note ever leaves the machine unless the manager enables it.

### AppSettings

Runtime configuration, stored as a database row rather than in a config file, because the
manager edits it from the Settings page while the app is running.

| Setting | Business meaning |
|---------|------------------|
| StoryPointMappings | Maps story point values to effort, for capacity maths |
| TshirtSizeMappings | Maps initiative T-shirt sizes to effort |
| ClaudeApiKey | The manager's own Claude key; sentiment analysis is disabled without it |
| SentimentAnalysisEnabled / SentimentAnalysisDays | Whether to analyse, and over what window (default 90 days) |
| SprintTeamFilter | Restricts the Jira import to one team's sprints |
| MaxInProgressTasks / MaxBlockedTasks / MaxInReviewTasks | Per-person WIP thresholds that raise dashboard warnings (defaults 2 / 1 / 1) |
| MinProjectMembers | Minimum people who should know a project before it is a bus-factor risk (default 2) |
| SupportLabels / MaintenanceLabels | JSON label lists that classify a task as support or maintenance |
| JiraBaseUrl | Used to build deep links from a task back to Jira |

---

## Cross-Cutting Use Cases

These are the questions Hive exists to answer. Each combines several domains.

### Capacity analysis

**Question:** can the team absorb what is planned for the next sprints?

Takes sprint capacity, subtracts business days lost to `Active` leave, compares the result
against committed story points, and projects forward using `GetCapacityPerMember()`.
This is the only place leave, sprints and tasks meet.

### Team velocity and estimation accuracy

**Question:** how much do we actually deliver, and are our estimates honest?

Velocity is completed story points per sprint. Estimation accuracy compares
`EstimatedHours` against `TimeSpentMinutes` on completed tasks. Both are scoped by the
dashboard's sprint-history filter, so a team that changed shape six months ago is not
judged on its old numbers.

### Knowledge-level suggestions

**Question:** who has quietly become an expert, and where is the bus factor?

Combines `KnowledgePoint` totals with `ProjectKnowledge` levels to suggest promotions, and
flags projects with fewer than `MinProjectMembers` knowledgeable people.

### Quarterly planning insights

**Question:** is this quarter's plan actually deliverable?

Generated from allocations, initiatives and dependencies. It surfaces:
- **Over-allocation** — a person on too many distinct initiatives in one sprint.
- **Dependency risk** — a dependent initiative starting before its dependency finishes.
- **Estimate mismatch** — planned allocation materially above or below the initiative's
  T-shirt size estimate.

### Sentiment insights

**Question:** how is this person actually doing, beyond what the ratings say?

Analyses recent meeting notes via Claude and reports positive/neutral/negative balance,
key themes, and a trend over time. Opt-in, cached, and never a substitute for the
manager's own judgement.

### Jira import

**Question:** how does delivery data get in without manual entry?

A CSV export from Jira is previewed, validated and imported. Sprints are parsed from the
`TeamName_QuarterQYear_SSprintNumber` pattern and filtered by `SprintTeamFilter`; tasks
whose sprints all fail the pattern are skipped. Assignees are matched to `DirectReport`
records by email. Jira parents become `Parent` entities. Fields listed in a task's
`OverriddenFields` are preserved.

**Rules:**
- Import is one-way. Hive never writes to Jira.
- Duplicate detection is explicit, not automatic. The import matches rows against existing
  tasks on a caller-chosen field (issue key or summary); a match is **skipped** unless the
  request sets `UpdateExisting`.
- Preview validates every row but samples only the first ten for display.

---

## Error Handling

| Scenario | Behaviour |
|----------|-----------|
| Invalid entity input (empty name, bad email, out-of-range value) | `ArgumentException` from the entity constructor or method |
| Illegal state transition (complete a cancelled task, skip a required item) | `InvalidOperationException` from the entity |
| Domain rule violation in a service | `DomainException` |
| Duplicate of something that must be unique | `ConflictException` |
| Entity not found | `NotFoundException` → 404 response |
| Missing or wrong credentials | 401 response |

**Rules:**
- Validation lives on the **entity**, not in the service or the controller. An entity can
  never be constructed in an invalid state.
- The frontend re-checks nothing for correctness. It may guide input, but the backend is
  the only authority.

---

## Glossary

| Term | Definition |
|------|-----------|
| **Allocation** | One person assigned to one initiative for one sprint |
| **Bus factor** | The number of people who know a project well enough for it to survive their absence |
| **Direct report** | A person the manager is responsible for; `IsDirect = false` marks skip-levels |
| **Initiative** | A body of work committed to within a quarter |
| **Knowledge level** | A 1–5 rating of how well a person knows a project |
| **Knowledge points** | Earned evidence (completed story points) that suggests a knowledge level |
| **Note folder** | A nestable grouping of manager notes; deleting one keeps its notes |
| **Unlinked 1:1** | A 1:1 note whose tags name nobody Hive recognises, or name two people |
| **Parent** | A Jira parent issue or epic that groups tasks — not an organisational parent |
| **Sprint history filter** | The dashboard control limiting analytics to the last N sprints |
| **T-shirt size** | A coarse effort estimate on an initiative, mapped to points via settings |
| **Work type** | Whether an initiative is Maintenance, Product Roadmap or Tech Roadmap |

---

## Ubiquitous Language

These terms must be used consistently across code, comments, and variable names.

| Term | Meaning | Never use instead |
|------|---------|-------------------|
| `DirectReport` | A person the manager is responsible for | employee, member, staff, user |
| `Parent` | A Jira parent work item grouping tasks | manager, supervisor, org unit |
| `TeamTask` | A unit of delivery work | ticket, issue, story, card |
| `Initiative` | A quarterly body of work | epic, project, workstream |
| `Allocation` | Person + initiative + sprint | assignment, booking |
| `Leave` | Time a person is unavailable | PTO, holiday, absence, timeoff |
| `KnowledgePoint` | Earned evidence of project knowledge | score, xp, credit |
| `NoteFolder` | A folder grouping manager notes | notebook, category, directory |
| `OneOnOneMeeting` | One 1:1, written as a single markdown note | meeting note, catch-up, sync |
| `ChecklistInstance` | One run of a checklist template | checklist, run, session |
| `Activity` | An audit trail entry | log, event, history |
