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

Hive divides the EM role into five domains. Every entity belongs to exactly one.

| Domain | Question it answers | Core entities |
|--------|---------------------|---------------|
| People & Growth | Who is on the team and are they developing? | `DirectReport`, `SkillRoadmap`, `CareerPlan` |
| Conversations | What have we talked about? | `OneOnOneMeeting`, `ManagerNote`, `NoteFolder` |
| Availability | Who is here, and when? | `Leave` |
| Delivery | What is the team working on and how is it going? | `Product`, `Project`, `TeamTask`, `Parent`, `Sprint`, `SprintCapacity` |
| Planning | What are we committing to next quarter, and can we? | `Quarter`, `QuarterMember`, `Initiative`, `ExternalDependency`, `Allocation`, `InitiativeDependency` |

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
| HireDate | Start date — drives tenure and the milestones on the dashboard |
| IsDirect | `true` for a direct report, `false` for a skip-level or dotted-line report |

**Rules:**
- Name fields cannot be empty and cannot exceed 100 characters.
- Email must be a valid address and cannot exceed 255 characters.
- `IsDirect = false` people are tracked and reportable, but are excluded from analyses
  that model the manager's own team — notably the unengaged-member warning and the
  knowledge matrix.
- A `DirectReport` is never deleted to represent a leaver; the record is the history.

### SkillRoadmap and CareerPlan

Each direct report's skills: which they already have, which not yet, and which they are
working on now, with where that puts them on the career ladder and their own strengths and
weaknesses. It is the manager's own view; direct reports never see it. **A plan has no goal**:
it is never made to reach a stage, and **people are never compared with each other**.

| Entity | Business meaning |
|--------|------------------|
| `SkillRoadmap` | A reusable roadmap for a role, such as "Backend Engineer": sections of skills, in order |
| `RoadmapSkill` | One skill on a roadmap, in a section; *hard* (technical) or *soft* (people and ways of working), optionally *optional*, and expected from a career stage |
| `CareerPlan` | A direct report's plan: the roadmap's skills, and the stage of their current role |
| `CareerPlanSkill` | One skill on a plan: its level, whether it is being worked on now, and the evidence |

**The career ladder** is Junior → Mid → Senior → Staff. It describes what each stage
**expects**; it is a yardstick, not a target.

**Roadmap rules:**
- Roadmap names cannot be empty, cannot exceed 200 characters, and no two roadmaps share a
  name (ignoring case). Sections are shown in the order of their first skill.
- Hive ships a **library** of built-in roadmaps in the spirit of roadmap.sh: Backend
  Engineer, Frontend Engineer, ASP.NET Core Developer, DevOps Engineer, QA Engineer, Software
  Architect and Engineering Manager, each with hard and soft skills. Adding one copies it into
  an ordinary roadmap that the manager edits like any other; Hive never links to roadmap.sh.
- A skill title cannot exceed 200 characters and a section name 100.
- A skill is **expected from a stage**: Junior (the default), Mid, Senior or Staff. The
  engineer roadmaps expect *Senior: technical leadership* from Senior, *Staff: scope and
  influence* from Staff, and their most advanced technical section from Mid.
- An *optional* skill is never expected: it earns XP, but its absence is no weakness.

**Plan rules:**
- **A direct report has one plan**, ongoing, made from a roadmap. It copies **every** skill of
  the roadmap, whatever stage it is expected from. Editing or deleting the roadmap later never
  changes the plan, and a plan can gain or lose skills of its own.
- The plan records the **stage of the person's current role**. It is a fact, changed when their
  role changes (a promotion), never a goal.
- A skill's **level** runs from 0 to 5: Not started, Aware, Learning, Practising, Proficient,
  Expert. Raising it is a **level-up**; lowering it corrects a rating and is not.
- Each skill is in one of three states: **working on** (the manager has put it in focus,
  whatever its level), **has** (Practising or above), or **not yet**.
- A plan has **gone quiet** when neither it nor any skill on it has changed for 60 days. The
  dashboard warns about it, unless the person is on leave today.

**Ladder position (an indication, never a verdict):**
- Each stage **expects** a level on each skill: Junior Learning (2), Mid Practising (3), Senior
  Proficient (4), and Staff Proficient on hard skills and Expert (5) on soft ones, since Staff is
  mostly about influence. A skill expected from a later stage is expected at Practising in the
  stage it arrives, and one level more for each stage after. Optional skills are never expected.
- A person's skills **meet a stage** when they meet at least 80% of its expectations. Their
  skills sit at the highest stage they meet, together with every stage before it.
- Against the stage of their role, that reads **above their role**, **at their role** or
  **below their role**; with nothing rated yet it is **not assessed**.
- **Strengths** are skills above what their stage expects, and skills of a later stage they
  already have. **Weaknesses** are required skills below what their stage expects, the widest
  gap first. Both are read against their own stage only.

**The game (on the person's own plan only, derived, never stored):**
- **XP**: 10 per level on every skill, plus 25 for every skill they have.
- **Player level**: reaching level *n* takes 75 × *n* × (*n* − 1) XP (0, 150, 450, 900, 1500…),
  titled Rookie, Explorer, Adventurer, Pathfinder, Ranger, Hero, Champion, then Legend. These
  are game titles, deliberately unlike job titles.
- A section is **cleared** when they have all its required skills.
- **Badges**: First steps (a first level-up), Got it (a first skill they have), Deep diver (a
  skill at Expert), On a roll (3 skills levelled up in 30 days), Trailblazer (3 sections
  cleared), Tech wizard (10 hard skills), People person (5 soft skills), Role ready (everything
  their stage expects) and Ahead of the curve (a skill of a later stage).
- The game is never shown next to anyone else's: not in a list, not on the dashboard.

### Parent

A parent work item — an epic or a Jira parent issue that groups tasks. Despite the name
this is **not** an organisational parent or a manager.

| Property | Business meaning |
|----------|------------------|
| Name | The parent item's title |
| Labels | Comma-separated labels inherited by, or shared with, its children |
| TeamTaskId | The `TeamTask` this parent corresponds to, once one is imported |
| JiraKey | The Jira issue this parent represents, e.g. `MAR-128`; empty for a parent made by hand or by an older CSV import |
| JiraIssueType | That issue's type: `Epic`, or any other type that has sub-tasks, such as `Story`, `Task` or `Support` |
| Components | The issue's Jira components: the products the parent belongs to |

**Rules:**
- Name cannot be empty and cannot exceed 500 characters, and no two parents share a name.
- **In Jira, any issue with children is a parent**, not only an epic: a story or a support
  ticket with sub-tasks is one too. Hive keeps them all, and shows each with its issue type
  so epics can be told apart and filtered on.
- Parents come from Jira, not from typing: the Jira sync keeps one for every parent issue
  in the configured space, and the CSV import makes one for any issue referenced as another
  issue's parent.
- **Once a parent has a Jira key, the key is its identity.** Renaming the issue in Jira
  renames the parent instead of creating a second one. A parent never moves to another issue.
- **A parent's progress is its resolved tasks over its total.** A task is resolved when it
  is `Done` or `Cancelled` (Jira's "Won't Do"): either way nothing more will happen on it,
  so a parent whose remaining tasks were all dropped reads 100%.
- **A parent belongs to the products its Jira components name.** Only the Jira sync sets
  components; a parent made by hand or by the CSV import belongs to no product.
- **A parent is done when it has tasks and every one is resolved.** A parent with no tasks
  is not done: nothing under it has been finished. The Parents page can hide done parents.

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
- The 1:1 is the whole record: **sentiment analysis reads these bodies**, and the latest
  linked 1:1 is what the dashboard's 1:1 cadence measures.
- **A 1:1 is due every `OneOnOneCadenceDays` days** (default 14, between 1 and 90) after the
  last one held with that person. It is *due soon* in the two days before, *overdue* after,
  and a direct report with no linked 1:1 at all has *never met*. A 1:1 dated in the future
  has not happened yet and does not count. Only direct reports (`IsDirect`) are measured,
  and someone on leave today is not flagged as overdue.

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

**Rules:**
- Title cannot be empty and cannot exceed 200 characters. When a note is written rather
  than filled in on a form, the title is **derived**: the first non-empty line of the
  content with its markdown decoration stripped, truncated to 200 characters. An empty
  note is titled `New Note`. The manager never types a title separately.
- A blank note is a valid note. It exists from the moment it is created, so nothing typed
  into it can be lost, and it is saved as it is written rather than on a Save button.
- The body is free text with no length limit, and whitespace is preserved exactly as
  typed — leading indentation and blank lines are part of what was written.
- **A note is not a task.** It has no priority, due date or done state. The manager's
  to-dos are written in the journal (below), and a note is changed by editing it.
- **Notes are listed pinned first, then by title, descending**, ignoring case: in every
  folder, in "All Notes", in search and by tag. A journal whose entries are titled by date
  therefore reads newest first, and its latest entry is the first unpinned note.
- **The journal is the folder named `Journal`**, ignoring case: one note per day, titled
  by its date. The **latest entry** is the note in it whose title sorts last, so a title
  like `2026-09-28` makes the newest day the latest.
- **The retros are the notes in the folder named `Retro`**, ignoring case: one note per
  team retrospective. A retro is **held on the date its title starts with** (`2026-09-28`
  or `28/09/2026`), or else on the day the note was created. A note with nothing written in
  it is not a retro yet. Only the folder's own notes count, not its sub-folders'.
- **The Notes page opens on the journal** when there is one, and on All Notes otherwise. A
  link to a note or a search opens on All Notes.
- **The to-dos are the latest entry's hot items**: the top-level bullets under a heading
  whose first word is `hot` (any level, any decoration, e.g. `## hot 🔥`), up to the next
  heading of the same or a higher level, or a horizontal rule. Nested bullets belong to
  their parent, and a ticked task-list item (`- [x]`) is done and left out.
- A `ManagerNote` is not about a person. Notes about a direct report belong on a 1:1,
  where they are part of that person's record.

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

### Product

Something the team builds and runs. **A product is a Jira component**: the delivery work
that belongs to a product is the work that names it in its components.

| Property | Business meaning |
|----------|------------------|
| Name | The component's name, e.g. `Rights Manager` |
| Description | What the product is |
| JiraComponentId | The Jira component this product is; empty for a product made by hand |

**Rules:**
- Name cannot be empty, cannot exceed 255 characters and cannot contain a comma (tasks list
  their components separated by commas). No two products share a name.
- **A task or parent belongs to every product its components name**, compared ignoring
  case and surrounding spaces. A task with three components counts toward three products.
  A task whose components name no product is *unattributed*, and the dashboard says so.
- A product's progress is its resolved tasks (`Done` or Won't Do) over its total, the same
  rule as a parent's.
- Products come from Jira: the product sync keeps one for every component in the configured
  space. **Once a product has a Jira component id, the id is its identity**, so renaming the
  component renames the product. Jira owns its name and description; it is edited in Jira.
- A product can be made by hand when a component cannot be read from Jira. Only products
  made by hand can be edited or deleted in Hive. The first sync that finds a component with
  the same name links the two.

### Project and ProjectKnowledge

| Entity | Business meaning |
|--------|------------------|
| `Project` | A code repository, such as a GitHub project, serving one or more products |
| `ProjectProduct` | One project serving one product |
| `ProjectKnowledge` | One person's knowledge level (1–5) on one project |

**Rules:**
- Project name cannot be empty and cannot exceed 200 characters.
- **A project is linked to products, never to tasks.** A project can serve several
  products and a product can be served by several projects; the manager chooses a project's
  products by hand. Work reaches a project only through the products it serves.
- Knowledge level must be **between 1 and 5**, and is **set by the manager by hand**. Level
  1 is the default for someone who has touched the project at all; there is no level 0.
  Hive never suggests or changes a level.

### TeamTask

A unit of delivery work, almost always imported from Jira.

| Property | Business meaning |
|----------|------------------|
| Type | `Task`, `Epic`, `Story`, `SubTask`, `Bug`, `Spike`, `Support` |
| Priority | `Low`, `Medium`, `High`, `Critical` |
| Status | See the lifecycle below |
| Description | What the work is, as written in Jira: stored whole, as Markdown |
| AssigneeId / ParentId | Who owns it, and what it rolls up into |
| StoryPoints | The size of the work — the unit of velocity, capacity and every effort figure Hive reports |
| Sprint | Every sprint the task has been in, comma-separated; work carried over lists them all |
| Labels / Tags | Free-form classification, also used for support/maintenance attribution |
| Components | The Jira components of the task: the products it belongs to |
| PreviousSprintsStoryPoints | Points this task carried in earlier sprints, for carry-over analysis |
| StatusChangedAt | When Hive saw the status change: the sync, import or edit that changed it, not Jira's own transition time |
| OverriddenFields | Which fields the manager edited by hand, so a re-import will not overwrite them |

**Rules:**
- **A story or task says what done means**: its description has acceptance criteria, a
  line starting a section such as `## Acceptance criteria` or `AC:`, or scenarios written as
  Given … Then. A bug describes what is wrong instead, and a spike, support ticket, sub-task or
  epic is not built against criteria of its own, so they are never held to it.
- `OverriddenFields` is what makes re-importing from Jira safe. Any field listed there is
  the manager's, and Jira does not get to overwrite it.
- **A task is in every sprint it lists**, like Jira's `sprint = X`: filtering Tasks by a sprint
  includes work carried over into or out of it. Sprint reports (velocity, capacity, the
  current sprint on the dashboard) count a task once, in the latest sprint it lists.
- **Support work is identified by its Jira issue type**, `Support`, everywhere Hive counts it.
  Maintenance work is identified by matching a task's labels against the configurable
  `MaintenanceLabels` setting.
- **Effort is measured in story points, and only in story points.** Hive does not track
  logged time or hour estimates. Workload and component distribution count a task's points
  net of any it already delivered in earlier sprints (`PreviousSprintsStoryPoints`), so
  carried-over work is not counted twice.
- **A task's work category comes from the name of its epic**: the topmost parent it rolls
  up into (a sub-task through its story). An epic whose name contains one of a category's
  names (the `WorkCategoryEpicNames` setting: by default *Tech Maintenance*, *Tech Roadmap*,
  *Tech Debt* and *System Improvement*), ignoring case, spaces and punctuation, gives that
  category; when names of two categories fit, the longest wins. Every other task, including
  one with no epic, is **Productisation**, which takes no names. A name cannot be empty, and
  cannot name two categories. Work mix shows each category's share of issues and of story points
  side by side, because unestimated work shows in one and not the other.
- **Spikes and support are never estimated; every other task is**, except an epic, whose
  size is its children's, and any other parent whose children have story points, which
  they size (spike and support children are left out of that; a parent with only those is
  not sized by them). A task under a spike or a support ticket is part of that unestimated
  work and is not estimated either. Only tasks assigned to someone on the team (the Team
  page) are held to this; any child, whoever has it, still sizes its parent. A story point value of zero counts as no estimate. The dashboard
  warns about spikes with story points, support tickets with story points, and tasks of any
  other type without them, done ones included.
- **Bugs, support and maintenance are counted in tickets, not points**, because they are
  often not estimated: the support and maintenance share per person in Workload and the
  Bug & support inflow both count tasks.

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
- **The current sprint** is the one containing today; between sprints it is the next to
  start, and after the last one it is the most recent (`Sprint.FindCurrent`). The dashboard,
  the capacity analysis and the Tasks page's "Current sprint" filter all use this rule.
- Capacity and available members cannot be negative. `GetCapacityPerMember()` is the
  per-person figure used to project future sprints.
- Sprint goals cannot exceed 4000 characters.

### Quarter, Initiative and Allocation

Quarterly planning follows how the manager builds a quarter. One or two sprints before the
current quarter ends the PM brings **candidate options**; the manager and the team write a
**solution design** for each (a high-level how, a T-shirt size and epics);
then the manager plans **capacity** from the team, their leave, who knows what, dependencies on
other teams, what is not ready on the product side, and the agreed **mix**; and finally
**commits**. During the quarter the plan is held against **reality**, sprint by sprint.

| Entity | Business meaning |
|--------|------------------|
| `Quarter` | A planning period: its status, OKR reference, target mix and, once committed, the baseline |
| `QuarterMember` | A person on the quarter's plan, and how much of their time can be planned (100% full-time) |
| `Initiative` | On the page an **option**: a candidate from the PM, a **leftover** from last quarter, or a **standing lane** (Maintenance, Tech roadmap) |
| `InitiativeProduct` | An initiative touching a product |
| `InitiativeMember` | A person on an initiative: its **lead**, or a contributor |
| `ExternalDependency` | What an initiative needs from another team, and whether it is agreed |
| `InitiativeDependency` | An initiative that needs another one finished first |
| `Allocation` | One person, on one initiative, in one sprint, for a share of the sprint |

**Quarter rules:**
- Year must be between 2000 and 2100; quarter number between 1 and 4.
- `QuarterStatus` moves `Planning → Active → Completed`. A completed quarter cannot be
  re-activated, and a quarter still in `Planning` cannot be completed — planning must be
  finished before delivery can be declared done.
- `ResetToPlanning()` is the deliberate escape hatch for a mis-advanced quarter.
- **The mix** is the agreed split of the team's capacity: product roadmap, tech roadmap and
  maintenance, 50 / 20 / 30 by default. Each share is 0–100% and together they are 100%.
- A new quarter starts with its two **standing lanes**, Maintenance and Tech roadmap.
- **Committing** records the plan as it stands (each option's status, size, what is planned for
  it, whether it fits) as the baseline. Committing again replaces it. A completed quarter
  cannot be committed.

**Initiative rules:**
- Name cannot be empty and cannot exceed 200 characters; notes cannot exceed 4000.
- **An option is sized once it is ready for commitment.** Sizing is what discovery and the
  solution design lead to, so an option in discovery or in solution design has no size, and
  moving it back there (or raising a readiness issue on it) clears its size. An option is created
  in any of the three starting statuses; only one ready for commitment can come with a size.
- `TshirtSize` is S, M or L, widened with X (XS, XL, XXL…) and optionally nudged up with a plus
  (L+, XL+). A size means **dev-sprints** (one person for one sprint) through the
  `TshirtSizeMappings` setting; a size with no mapping counts as nothing and is flagged. The
  defaults are S 1, M 2, L 4, XL 6 and XXL 12: **XXL is an option that takes up the whole
  quarter**. Every length is editable in Settings.
- `Url` is the solution design link: a valid HTTP or HTTPS URL of at most 500 characters.
- `WorkType` is `Maintenance`, `ProductRoadmap` or `TechRoadmap` — the split the EM reports
  upward.
- **Priority** is the PM's rank, 1 first. Leftovers come before every option and read `0`; an
  option that arrived after planning reads `NEW`; lanes read `–`. The order is set by dragging.
- **Status** runs In discovery → In Solution Design → Ready for commitment, then a decision: **Committed** or
  **Dropped**, each with a reason, and at the end **Delivered**. Only committed (or, from older
  plans, stretch) work can be delivered. **Stretch** is no longer offered as a decision.
- **Readiness**: an option can be waiting on product requirements, estimation, the solution
  design or release coordination. An option waiting on any of them **cannot be Ready for
  commitment**, and raising one on such an option puts it back In Solution Design, unsized. It can still be committed, and the
  plan then warns.
- **A standing lane is always committed.** It keeps its work type, is not an option and is never
  carried over; its size is its share of the mix.
- An initiative can say **from which sprint it can start** (for example when requirements land
  late), and has an optional due date.
- **Carrying over** copies an option to the next quarter as a leftover: ready for commitment
  with its size when nothing is pending, otherwise in solution design and unsized; with its
  notes, products, people and the dependencies not yet done.
  The next quarter is created if it does not exist.

**People and dependencies:**
- An initiative has **at most one lead**: naming a new lead makes the previous one a
  contributor.
- A dependency on another team names the team and what is needed, and is *needs coordination*,
  *agreed*, *at risk* or *done*. Needs coordination and at risk are **open**.
- **An initiative cannot depend on itself**, and both initiatives of a dependency are in the same
  quarter.

**Capacity and allocation rules:**
- The quarter's **roster** is every direct report at 100% until the manager changes it; from the
  first change it is the quarter's own. Someone taken off the roster loses their allocations.
- A person's **availability** in a sprint is its working days (Monday to Friday) less the days of
  their active leave, public holidays included. A sprint without dates counts as fully
  available.
- **Supply** is availability × planned capacity, in dev-sprints. An allocation is worth its share
  of the person's supply, so planning someone for a sprint they are mostly away plans little.
- **A person's sprint cannot be allocated beyond 100%**, and an initiative appears in it once.
  Two initiatives in one sprint split it (for example 50% each).
- The standing lanes **reserve** their share of the mix; what is left is the **capacity for
  options**.

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
  can link back to the thing that changed. `Document`, `ChecklistTemplate` and
  `ChecklistInstance` are historical: the features are gone, and the values stay so older
  activity still reads.

### SentimentAnalysisCache

Cached output of AI sentiment analysis over a person's meeting notes.

**Rules:**
- This is a **cache**, not a record. It can be discarded and regenerated at any time.
- It stores what it analysed (`NotesAnalyzed`, `DaysAnalyzed`, `LatestNoteDate`) so a
  stale result can be detected when new notes arrive.
- Sentiment analysis is **off by default**. It runs on a local model through Ollama unless
  the manager chooses Claude and supplies their own API key. No note ever leaves the machine
  unless the manager chooses Claude.

### TaskReadinessReview

Cached output of the AI review of one task before it is built.

**Rules:**
- This is a **cache**, not a record. A task has at most one, and it goes when the task does.
- It records the title, type and description it read, so it is **out of date as soon as any of
  them changes**; a change of status, sprint or assignee does not make it so.
- The verdict is **Ready**, **Needs work** or **Unclear**, and the summary cannot be empty.

### RetroSummary

Cached output of the AI summary of the team's latest retrospectives.

**Rules:**
- This is a **cache**, not a record. It can be discarded and made again from the retros at
  any time. There is only one.
- It records which retros it read, on which days and when each was last changed, so it is
  **out of date as soon as a retro is added, edited or removed**.
- The summary cannot be empty and reads at least one retro.

### AppSettings

Runtime configuration, stored as a database row rather than in a config file, because the
manager edits it from the Settings page while the app is running.

| Setting | Business meaning |
|---------|------------------|
| TshirtSizeMappings | Maps initiative T-shirt sizes to effort |
| SentimentProvider | Which model reads the notes: `Ollama` (local, the default) or `Claude` |
| OllamaBaseUrl / OllamaModel | Where Ollama answers and which model it runs (defaults `http://localhost:11434`, `llama3.1:8b`) |
| ClaudeApiKey | The manager's own Claude key; with Claude chosen, sentiment analysis is disabled without it |
| SentimentAnalysisEnabled / SentimentAnalysisDays | Whether to analyse, and over what window (default 90 days) |
| TaskReviewEnabled | Whether the AI reviews the upcoming tasks before they are built (off by default). It uses the sentiment model |
| RetroSummaryEnabled / RetroSummaryCount | Whether to summarise the retros, and how many of the latest to read (off by default; 6, between 1 and 20). It uses the sentiment model |
| SprintTeamFilter | Restricts the Jira import to one team's sprints |
| MaxInProgressTasks / MaxBlockedTasks / MaxInReviewTasks | Per-person WIP thresholds that raise dashboard warnings (defaults 2 / 1 / 1) |
| MinProductMembers | Minimum people who should have worked on a product before it is a bus-factor risk (default 2) |
| MinMemberProducts | Minimum products a direct report should have worked on before they are narrowly engaged (default 2, 1–50; 1 flags nobody) |
| OneOnOneCadenceDays | How often the manager aims to hold a 1:1 with each direct report, in days (default 14, 1–90) |
| StaleTaskDays | How many days an in-flight task may keep one status before it is stale (default 5, 1–60) |
| MaintenanceLabels | JSON label list that classifies a task as maintenance; support is its task type |
| WorkCategoryEpicNames | The epic names that name each work category (Tech Maintenance, Tech Roadmap, Tech Debt, System Improvements); empty means the defaults |
| JiraBaseUrl | Used to build deep links from a task back to Jira; its site root is also where the Jira sync reads from |
| JiraEmail / JiraApiToken | The manager's own Atlassian account and API token for the Jira sync. The token is never returned by the API |
| JiraBoardId | The Jira board whose sprints the sync reads |
| JiraProjectKey | The Jira space (project) whose components become products and whose epics become parents, e.g. `MAR` |
| GoogleCalendarUrl | The manager's Google Calendar secret address in iCal format. Like the Jira token, it is never returned by the API |

---

## Cross-Cutting Use Cases

These are the questions Hive exists to answer. Each combines several domains.

### What needs attention today

**Question:** what should I deal with before anything else?

The top of the Dashboard answers it, about today rather than any window of sprints:

- **Needs attention** — every dashboard warning, people first: overdue 1:1s,
  overloaded people, unengaged members, members on too few products, quiet career plans; then delivery risks (overdue tasks, stale
  tasks, products nobody has worked on, knowledge silos, scope creep, unattributed tasks, spikes or support with story points, tasks without story points, upcoming stories and tasks without acceptance criteria, low utilization, falling velocity). A task is
  overdue when it is not Done or Won't Do and its due date has passed, whatever sprint it
  is in. A task is **stale** when it is in progress, blocked, in review, in test, awaiting PO
  acceptance or ready to release, and its status has not changed for `StaleTaskDays` days.
  The clock is Hive's own: it starts when a sync, import or edit changes the status, so a task
  that sits unchanged across syncs keeps ageing, and Jira's transition dates are not used.
- **Current sprint** — the sprint containing today (or the next to start): done story
  points against time gone, which gives its **pace**. A sprint is *behind* when the share of
  new points done trails the share of working days gone by 15 points or more, and *ahead*
  when it leads by 10 or more. It also shows scope creep, carried-over points, status mix
  (Blocked always listed, even at zero), and every blocked task.
- **1:1s** — each direct report against the 1:1 cadence, most urgent first.
- **To-dos** — the hot items of the latest journal entry, as written; each opens the note.
- **Who's out** — active leave today and starting in the next 14 days.
- **Milestones** — anyone reaching six months at the company, or another full year after
  that, within the week: each milestone is shown for the seven days up to and including the
  day. In a week with none the tile is not shown at all. Everyone tracked counts, direct or
  not; someone whose hire date is still ahead has none.

### Capacity analysis

**Question:** can the team absorb what is planned for the next sprints?

Takes sprint capacity, subtracts business days lost to `Active` leave, compares the result
against committed story points, and projects forward using `GetCapacityPerMember()`.
This is the only place leave, sprints and tasks meet.

**The next sprint is always predicted**, whatever sprint window the Capacity widget shows. It is
the sprint after the current one; when Hive does not have it yet, it is projected to start
the day after the current sprint ends and to last as long. The prediction is the sprint's
commitment scaled by average utilization when it has one, otherwise the average points the
past sprints delivered, scaled by the people available in it (the team less working days
lost to leave) against the people available in those past sprints.

### Carry-over trend

**Question:** how much of each sprint is work we failed to finish before?

A task is *carried into* a sprint when it lists that sprint and an earlier one, and *carried
out* when it lists a later one: Jira keeps every sprint an unfinished issue passed through.
For each sprint Hive shows the story points carried in against the sprint's total, and how
many tasks moved on unfinished. It covers the widget's own sprint window, and only sprints
Hive knows that have started.

### Bug & support inflow

**Question:** how much unplanned work arrives each sprint?

A task *arrives* in the first sprint it was in; later sprints only carry it. Per sprint, Hive
counts the new tickets of Jira issue type **Bug** and **Support**, and **maintenance**: any
ticket carrying one of the `MaintenanceLabels`. The columns overlap: a bug labelled
maintenance is shown as a bug and as maintenance, so they sit side by side rather than stacked.
The **share of new tasks** counts each ticket once, so there it is not counted twice. They are counted in tickets, never story
points, because these tickets are often not estimated, and shown against all new tickets in
the sprint. It covers the widget's own sprint window.

### Team velocity

**Question:** how much do we actually deliver?

Velocity is completed story points per sprint, over the widget's own sprint window, so a
team that changed shape six months ago is not judged on its old numbers.

### Delivery time frames

**Question:** how far back is each Delivery widget looking?

There is no page-wide sprint filter. Each widget that charts sprint by sprint (Velocity,
Capacity, Carry-over trend, Bug & support inflow) has its own **sprint window**: the current
sprint, the last 3, 6 or 12, or all sprints (the default). Changing one never changes another.
Workload, Work mix and Team sentiment always cover all sprints and are marked **All sprints**.
Product engagement has its own period in months. Needs attention is about today, and its
warnings never depend on what any widget is showing.

### Product engagement and bus factor

**Question:** who works on which product, and which product depends on too few people?

A direct report has worked on a product when they are the assignee of a task whose
components name it. **Only direct reports (`IsDirect`) count**, and **every task counts
except an epic**: an epic's components say what it is about, not who did the work. A parent
that is not an epic, such as a story with sub-tasks, is real work and counts. The dashboard
shows how many products each person has worked on and how many people have worked on each
product, every product included, and raises five warnings from the same data:

- **Product nobody has worked on:** a product no direct report has a task on. It is the worst
  bus factor there is, so it is its own warning rather than a knowledge silo.
- **Knowledge silo:** a product that someone has worked on, but fewer than
  `MinProductMembers` people.
- **Unengaged member:** a direct report with no task on any product, unless they are on
  leave today.
- **Member on too few products:** a direct report who has worked on a product, but on fewer
  than `MinMemberProducts`, unless they are on leave today. Someone on no product at all is an
  unengaged member instead, never both.
- **Unattributed tasks:** tasks whose components name no product, and so are missing from
  every product breakdown.

The Product engagement widget looks back **3 months** by default, or 6 months, or all time,
and has no sprint window. A task falls in a period when one of its sprints
overlaps it; work planned in a sprint that has not started, and a task in no sprint Hive
knows, fall in no period but all time. **The warnings always cover all time**, whatever
period the widget shows, so the Needs attention count never depends on it.

### Quarterly planning

**Question:** what can we commit to next quarter, and is it happening?

- **The waterline.** Options in priority order (leftovers first, dropped ones out) against the
  capacity for options: those whose running total fits are **above the line**. An option
  without a size never fits. Moving an option
  up or down the priority moves the line.
- **The mix.** The target against the plan (by planned dev-sprints), next to the previous
  quarter's plan and the share of its tickets that were bugs, support or maintenance, so a
  maintenance reservation that history says is too small shows. The **team's past work** is
  every task's story points split by the same work categories as the Dashboard's Work mix:
  productisation and system improvements count as the product roadmap, tech roadmap and tech debt
  as the tech roadmap, tech maintenance as maintenance. One click puts it in the target, which
  stays editable and is saved only on request.
- **Suggestions.** For an option on a product, the people who know its projects best (the
  knowledge matrix) and who have done its tasks, with their free time in the quarter.
- **The forecast.** For each option, what is planned against its size, and the sprint in which the
  plan covers it; below 100% it will not be delivered this quarter.
- **Draft plan** fills the free cells: options above the line or committed, in priority order, the
  lead first, from the sprint the option can start, never taking the lanes' share; then the lanes
  towards the mix. It proposes first and changes nothing already planned.
- **Warnings**, critical first: committed beyond capacity; a committed option not ready, or
  depending on a team at risk; an option committed below the line; no lead, or a lead who knows
  little of it, is away for a sprint they lead in, or leads three things at once; nobody planned
  on a committed option; not delivered this quarter, or after its due date; an option starting
  before the one it needs finishes, or needing a dropped one; someone planned in a sprint they are
  away all of; a size with no mapping; the mix 10 points or more off target.
- **Review.** Each option at commit against now, each sprint's goals against how they went, and a
  plain-text summary for the PM: committed (with lead and when it lands), stretch, dropped and why.
- **Calibration.** Across quarters, each size's estimate against what delivered options of that
  size actually took, so the size mapping can be corrected.

### Sentiment insights

**Question:** how is this person actually doing, beyond any single conversation?

Analyses recent meeting notes with an AI model and reports positive/neutral/negative
balance, key themes, and a trend over time. Opt-in, cached, and never a substitute for the
manager's own judgement.

**Rules:**
- **The manager chooses the model.** A local model through Ollama is the default, and with
  it nothing leaves the machine. Claude, with the manager's own API key, judges better but
  sends the notes to Anthropic. A manager who had set up Claude before the choice existed
  stays on Claude.
- Both are asked exactly the same question and give the same shape of answer, so switching
  changes the judgement, not what is shown. A result already cached stays until the next
  new 1:1 or a refresh, whichever model made it.
- The bodies of the person's 1:1s inside the analysis window are sent to the chosen model,
  and nothing else. Names come only from what the manager wrote in the notes.
- A local model reads every note in the window or none: if the notes do not fit in what it
  can read at once, the manager is told to shorten the window rather than shown a result
  that silently left the oldest notes out.
- If Claude declines to analyse the notes for policy reasons, Anthropic may complete the
  analysis on a different Claude model instead. If no model will, the manager is told the
  analysis was declined; Hive never shows a guessed or partial result.
- **Every result explains itself.** The model gives a few sentences of reasoning: why it
  judged the notes as it did, and what in them led it there. Hovering a direct report in
  Team sentiment shows it. A result cached before Hive asked for reasoning is analysed again
  once, the next time the dashboard loads, to get it.
- A failed analysis never overwrites the cached one.

### Task readiness

**Question:** is the work we are about to build written well enough to build it?

The **Task readiness** tile, first in Delivery, lists the **upcoming tasks**: not started
(backlog or to do), not an epic, and planned for the current or the next sprint by the latest
sprint they list. Current sprint first, then by priority.

**Rules:**
- **Facts and judgement are kept apart.** Whether a story or task has acceptance criteria is a
  fact Hive checks itself, with no AI. It is shown on the tile and raises the **Needs attention**
  warning "upcoming tasks have no acceptance criteria", whether or not the AI review is on.
- **The AI review is judgement** and stays on the tile. It reads each task's title, type and
  description, and gives a verdict (**Ready**, **Needs work**, **Unclear**), one or two
  sentences on what the task asks for, and what is missing, by type: acceptance criteria for a
  story or task; steps to reproduce and expected and actual behaviour for a bug; scope,
  dependencies, designs, data or edge cases for anything.
- **Off by default**, and switched on separately. It uses the sentiment model: with Ollama,
  nothing leaves the machine.
- **One task at a time.** While the Dashboard is open, the tile reviews the tasks that have no
  up-to-date review, one after another, and can be paused. A task is reviewed again only when
  its title, type or description changes, or when the manager asks.
- **Every reviewed task carries the AI bee badge**, on the tile and on the Tasks page, in the
  colour of its verdict. Hovering it shows the summary and what is missing; a review of an
  earlier version of the task has a dashed ring.
- A failed review never overwrites the last one; the tile stops, says why, and offers to try
  again.
- The tile shows what needs attention first; ready tasks are one click away.

### Retro summary

**Question:** what does the team keep raising in its retros, and what should I do about it?

The **Retro summary** tile in Team health reads the latest retros (6 by default) with the model
chosen for sentiment and shows, in this order: **warnings**, a **summary**, the **action items**
and **suggestions**, then which retros it read (each opens its note) and when.

**Rules:**
- **Off by default**, and switched on separately from sentiment analysis. It uses the same
  model: a local model through Ollama, with which nothing leaves the machine, or Claude with
  the manager's own key, which sends the retros to Anthropic.
- The bodies of the latest retros are sent to the model, oldest first, and nothing else.
- **Warnings** are only what needs the manager: a problem raised in several retros, an action
  item agreed and not followed up in a later retro, falling morale, or anything serious. There
  may be none. They stay on the tile: **they are not dashboard warnings**, because Needs
  attention is built from facts, never from a model's judgement.
- **Action items** are what the team agreed to do, as the retros put it, each with the date of
  the retro it was agreed in and who took it, when the retro says. Items a later retro reports
  as done are left out.
- **Suggestions** are what the manager could do next, grounded in what the retros say.
- **It is made again automatically** when the dashboard loads and a retro has changed since
  the last summary; otherwise the cached one is shown. **Refresh** makes it again on demand.
- A failed summary never overwrites the last one: the tile keeps showing it, says it is out of
  date, and says why. With no Retro folder, or nothing written in it, no model is asked.
- A local model reads every retro or none: if they do not fit in what it can read at once, the
  manager is told to read fewer.

### Career growth

**Question:** what skills does each person have, what are we working on, and where does that
put them on the ladder?

Each direct report has one career plan, made from a roadmap. The Career Plans page shows, for
one person at a time, what they are working on now, what they have and what not yet, where their
skills read against the stage of their role, and their strengths and weaknesses; with a small
game (XP, a player level, badges) to make keeping it current fun. Nothing on the page, or
anywhere in Hive, compares one person's plan with another's. The dashboard's **Career plans**
tile lists only what needs the manager: who has no plan, whose plan has nothing in focus, and
whose has gone quiet.

### Jira import

**Question:** how does delivery data get in without manual entry?

A CSV export from Jira is previewed, validated and imported. Sprints are parsed from the
`TeamName_QuarterQYear_SSprintNumber` pattern and filtered by `SprintTeamFilter`; tasks
whose sprints all fail the pattern are skipped. Assignees are matched to `DirectReport`
records by email. Jira parents become `Parent` entities. Fields listed in a task's
`OverriddenFields` are preserved.

The API sync (§ Jira sync) is the everyday path; the CSV import is kept for one-off
corrections and shares the task sync's field mapping.

**Rules:**
- Import is one-way. Hive never writes to Jira.
- Duplicate detection is explicit, not automatic. The import matches rows against existing
  tasks on a caller-chosen field (issue key or summary); a match is **skipped** unless the
  request sets `UpdateExisting`.
- Preview validates every row but samples only the first ten for display.

### Jira sync

**Question:** how does Hive stay current with Jira without a CSV export?

**Sync Jira** on the Dashboard pulls everything below: sprint dates, then products, then
parents, then tasks. Each page that shows Jira data has its own **Sync Jira** button that
pulls only that page's part: sprint dates on Sprints, products on Products, parents on
Parents, tasks (with their parents) on Tasks. The parts run independently, so one that is not configured, or that Jira refuses,
is reported without stopping the others. The CSV import remains for one-off corrections.

**Jira is the source of truth for products, parents and tasks.** Hive shows what the API
returns: a product, parent or task that came from Jira and is no longer returned is removed
on the next sync. Removal only ever follows a complete, non-empty answer from Jira, so a
failed or empty sync never empties Hive. Anything made in Hive by hand, without a Jira key
or component id, is never removed.

### Jira product sync

**Question:** what are the products, without typing them in?

Hive reads every component of the configured Jira space and keeps one product per component.

**Rules:**
- Read-only and on demand, like every Jira sync.
- A component is matched to its product by component id. A product made by hand has no id
  yet, so the first sync recognises it by name, links it, and matches it by id from then on.
- **Jira owns the name and the description.**
- A product whose component is gone from the space is removed, and so are the links from
  projects to it. Products made by hand are never removed.
- A component whose name another product already holds is not applied, and is reported.

### Jira parent sync

**Question:** what are all the epics, and everything else that groups work, in my space?

Hive reads every parent issue in the configured Jira space, done ones included: every epic,
and every other issue that has sub-tasks. It keeps one parent per issue, with its type.

**Rules:**
- Read-only and on demand, like every Jira sync.
- An issue is matched to its parent by Jira key. A parent from an older CSV import has no
  key yet, so the first sync recognises it by name, links it, and matches it by key from
  then on.
- **Jira owns the name, the issue type and the components.** **The labels are the
  manager's** and a sync never changes them.
- A new parent issue gets a new parent. If the issue itself was imported as a task, its
  parent is linked to that task.
- A parent whose Jira issue is gone from the space is removed. Tasks still pointing at it
  are unlinked first, never left pointing at a parent that no longer exists.
- An issue whose name another parent already holds is not applied, and is reported; Hive
  never merges two issues into one parent.

### Jira task sync

**Question:** what is the team working on, without exporting a CSV?

Hive reads every work item in the configured space that is in at least one of the team's
sprints, done ones included: stories, tasks, bugs, spikes, support tickets and sub-tasks.
Epics are parents, not tasks.

**Rules:**
- **Scope is the team's sprints**, exactly as for the CSV import: a sprint counts when its
  name follows `TeamName_QuarterQYear_SSprintNumber` and, when `SprintTeamFilter` is set,
  its team matches. Backlog items and non-sprint queues ("REFINEMENT QUEUE", "READY FOR
  DELIVERY") are left out. A task keeps only its team sprints.
- Parents are synced first, and each task links to its parent by the parent's Jira key: a
  sub-task to its story, a story to its epic.
- A task is matched by the Jira key it carries (`jira:MAR-123`). One task per issue: when an
  older import left several copies of an issue, the oldest is kept and the rest removed.
- Fields map exactly as the CSV import maps them (the two share one set of rules): status,
  type and priority; assignee by email, or by full name when Jira hides the email; story
  points rounded up. The description comes across whole, as Markdown: headings, lists,
  checklists, tables, code and bold stay recognisable. It is never cut short.
- **Fields the manager overrode are never overwritten**, as with the CSV import.
- A task Jira moves back from Done or Won't Do is reopened and moved to its new status.
- A task no longer in the team's sprints is removed. A sprint Hive has not seen is created
  with Jira's dates.

### Jira sprint date sync

**Question:** when does each sprint actually start and end, without typing it in?

On demand, from the Dashboard, Hive reads every sprint on the configured Jira board through
the Jira Cloud API and copies its start and end dates onto the Hive sprint with the same
name. It also creates the team's **upcoming** sprints that Hive does not have yet.

**Rules:**
- Sync is one-way and read-only, like the import. Hive never writes to Jira.
- It runs only when the manager asks. Nothing is fetched in the background.
- Sprints are matched by **name**, ignoring case and surrounding spaces, because the name
  is the identity the import already uses. The sync never deletes a sprint.
- **Upcoming sprints are created.** A *future* or *active* sprint on the board that Hive does
  not have is created, with Jira's dates when it has both (otherwise Hive estimates them
  from the name), as long as its name follows `TeamName_QuarterQYear_SSprintNumber`, its
  team matches `SprintTeamFilter` when that is set, and the name appears only once on the
  board. Closed sprints are never created: empty past sprints would distort velocity,
  utilization and the next-sprint prediction.
- **Jira is the source of truth for dates.** A matched sprint takes Jira's dates even if
  they were entered by hand. Changes go through the same path as a manual edit, so capacity
  is recalculated and each change is recorded in the activity feed.
- The dates used are Jira's *planned* start and end dates, reduced to calendar days in the
  local time zone of the manager's machine.
- A sprint is left untouched, and reported, when Jira has no sprint of that name, when the
  Jira sprint does not have both dates yet (usually a future sprint), or when the name
  appears more than once on the board. Hive never guesses between two Jira sprints.
- The sync needs the Jira base URL, email, API token and board ID. Until all four are set
  it refuses to run and says which are missing.

### Google Calendar

**Question:** what else is in my day, next to my 1:1s and notes?

The Daily Planner shows the manager's own Google Calendar events alongside date-tagged
notes and 1:1s, read from the calendar's secret address in iCal format.

**Rules:**
- Read-only and on demand: Google is asked only when the manager presses **Sync Google
  Calendar**. Opening the page never asks Google.
- **A sync is reused for 30 minutes.** Within that time, opening the page, moving to another
  month or syncing again shows the events at once, without asking Google. After 30 minutes the
  events are gone from the page until the next sync, which fetches the calendar again. The page
  says when the last sync ran.
- Hive never writes to Google Calendar and never stores its events: they are held in memory
  only, lost when Hive restarts, and not part of the record, the activity feed or a backup.
- No Google sign-in and no API key. The secret address is the credential: it is entered on
  the Settings page, must be `https`, and is never shown again once saved. If it leaks, the
  manager resets it in Google Calendar and saves the new one.
- Recurring events appear on each day they occur; moved and cancelled occurrences follow
  Google. An all-day event appears on every day it covers.
- A calendar that cannot be read is reported on the page, and never hides the notes and 1:1s.

---

## Error Handling

| Scenario | Behaviour |
|----------|-----------|
| Invalid entity input (empty name, bad email, out-of-range value) | `ArgumentException` from the entity constructor or method |
| Illegal state transition (complete a cancelled task, complete a quarter still in planning) | `InvalidOperationException` from the entity |
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
| **Allocation** | One person on one initiative in one sprint, for a share of the sprint |
| **Dev-sprint** | One person for one whole sprint: the unit of planned effort |
| **Dev-day** | One person for one working day; the Team this quarter tile shows each person's supply in dev-days (supply × the sprint's working days) |
| **Career plan** | A direct report's skills, copied from a skill roadmap and rated 0–5: what they have, what not yet, what they are working on |
| **Career stage** | A rung on the ladder: Junior, Mid, Senior or Staff. A plan records the stage of the person's role; each stage expects a level on each skill |
| **Ladder position** | Where a person's skills read against the stage of their role: above, at or below it. An indication, never a goal |
| **Bus factor** | The number of people who have worked on a product, so it survives one of them being away |
| **Direct report** | A person the manager is responsible for; `IsDirect = false` marks skip-levels |
| **Initiative** | A body of work on a quarter's plan; shown as an option, a leftover or a standing lane |
| **Option** | A candidate initiative the PM brings for a quarter |
| **Standing lane** | Maintenance or the Tech roadmap: work every quarter carries, sized by the mix |
| **Waterline** | The options in priority order against the capacity for options; above the line fits |
| **Level-up** | Raising a career plan skill's level; the XP, player level and badges follow from it |
| **Knowledge level** | A 1–5 rating of how well a person knows a project |
| **Note folder** | A nestable grouping of manager notes; deleting one keeps its notes |
| **Unlinked 1:1** | A 1:1 note whose tags name nobody Hive recognises, or name two people |
| **Parent** | A Jira parent issue or epic that groups tasks — not an organisational parent |
| **Product** | A Jira component: something the team builds and runs, served by one or more projects |
| **Project** | A code repository, such as a GitHub project, serving one or more products |
| **Skill roadmap** | A reusable, sectioned list of hard and soft skills for a role |
| **Sprint window** | A Delivery widget's own choice of how many recent sprints it charts: the current one, the last 3, 6 or 12, or all |
| **T-shirt size** | A coarse effort estimate on an initiative, mapped to dev-sprints via settings |
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
| `QuarterMember` | A person on a quarter's plan, with their planned capacity | resource, headcount |
| `ExternalDependency` | What an initiative needs from another team | blocker, ask |
| `WorkCategory` | The kind of work a task is, from the name of its epic | work type (an initiative's), epic label |
| `Leave` | Time a person is unavailable | PTO, holiday, absence, timeoff |
| `Product` | A Jira component the team builds and runs | component (outside Jira code), app, service |
| `ProjectProduct` | A project serving a product | project link, mapping |
| `NoteFolder` | A folder grouping manager notes | notebook, category, directory |
| `OneOnOneMeeting` | One 1:1, written as a single markdown note | meeting note, catch-up, sync |
| `Activity` | An audit trail entry | log, event, history |
| `SkillRoadmap` | A reusable list of skills for a role | skill matrix, track, ladder |
| `RoadmapSkill` | One skill on a roadmap | topic, node, competency |
| `CareerPlan` | One direct report's skills and the stage of their role | development plan, IDP, growth plan |
| `CareerPlanSkill` | One skill on a career plan, with its level and whether it is being worked on | assessment, rating |
| `CareerStage` | A rung on the ladder: Junior, Mid, Senior, Staff | grade, band, seniority level |
| `Retrospective` | One team retro: a note in the Retro folder, and the day it was held | retro meeting, review, post-mortem |
| `RetroSummary` | The cached AI summary of the latest retros | retro report, retro insights |
| `TaskReadinessReview` | The cached AI review of one task before it is built | ticket review, grooming check |
