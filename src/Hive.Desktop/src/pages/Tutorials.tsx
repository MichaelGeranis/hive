import { useState, useEffect, useRef, type ReactNode } from 'react'
import {
  BookOpen, ChevronRight, Home, TrendingUp, Zap, Award, Users, BarChart3, Search, X, ArrowUp, ArrowDown,
  CalendarDays, StickyNote, Package, Layers, CheckSquare, FolderKanban, RefreshCw, MessageSquare, Palmtree,
  Activity, Settings as SettingsIcon, Terminal
} from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { PageHeader } from '../components/PageHeader'

interface TutorialSection {
  title: string
  body: ReactNode
}

interface Tutorial {
  id: string
  title: string
  icon: typeof BookOpen
  description: string
  category: string
  subtitle: string
  sections: TutorialSection[]
}

function Tip({ label = 'Tip', children }: { label?: string; children: ReactNode }) {
  return (
    <p className="text-sm bg-amber-50 border border-amber-200 rounded-lg p-3">
      <strong>{label}:</strong> {children}
    </p>
  )
}

function Note({ label = 'Note', children }: { label?: string; children: ReactNode }) {
  return (
    <p className="text-sm bg-blue-50 border border-blue-200 rounded-lg p-3">
      <strong>{label}:</strong> {children}
    </p>
  )
}

function Warning({ children }: { children: ReactNode }) {
  return (
    <p className="text-sm bg-red-50 border border-red-200 rounded-lg p-3">
      <strong>Warning:</strong> {children}
    </p>
  )
}

function Bullets({ children }: { children: ReactNode }) {
  return <ul className="list-disc list-inside space-y-1 text-sm bg-slate-50 rounded-lg p-4">{children}</ul>
}

function Steps({ children }: { children: ReactNode }) {
  return <ol className="list-decimal list-inside space-y-1 text-sm bg-slate-50 rounded-lg p-4">{children}</ol>
}

function Code({ children }: { children: ReactNode }) {
  return <code className="bg-slate-200 px-1 rounded text-xs">{children}</code>
}

const MARKDOWN_EDITOR_SECTION: TutorialSection = {
  title: 'Writing in markdown',
  body: (
    <>
      <p>
        The editor has a toolbar with <strong>Bold</strong>, <strong>Italic</strong>, <strong>Heading</strong>,{' '}
        <strong>Bulleted list</strong>, <strong>Numbered list</strong> and <strong>Checklist</strong>, and a{' '}
        <strong>Preview</strong> / <strong>Edit</strong> toggle. An existing note opens in Preview, a new one in Edit.
      </p>
      <Bullets>
        <li><strong>Cmd/Ctrl+B</strong> bold, <strong>Cmd/Ctrl+I</strong> italic, <strong>Tab</strong> indents two spaces</li>
        <li><strong>Enter</strong> on a list line continues the list (numbers count up, checklist items start unticked)</li>
        <li><strong>Enter</strong> on an empty list item ends the list; <strong>Shift+Enter</strong> is a plain new line</li>
        <li>In Preview, clicking a checklist box ticks it in the text and saves it</li>
      </Bullets>
      <Tip>
        There is no Save button. Hive saves about 0.7 seconds after you stop typing, and before you switch note, search
        or leave the page. The toolbar shows <strong>Saving…</strong> then <strong>Saved</strong>.
      </Tip>
    </>
  )
}

const tutorials: Tutorial[] = [
  // Overview
  {
    id: 'dashboard',
    title: 'Dashboard',
    icon: BarChart3,
    description: 'What needs attention, the current sprint, and delivery history',
    category: 'Overview',
    subtitle: 'What needs you today, then how delivery has been going',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              The Dashboard is action first. The top half is about <strong>today</strong>: what needs you, the sprint in
              progress, your 1:1s, your to-dos and who is away. Below it, <strong>Delivery</strong> is about how the team
              has been delivering over the sprints you choose, and <strong>Team health</strong> about how people are
              doing.
            </p>
            <Tip>
              every widget can be hidden from <strong>Customize</strong>. Hiding a widget never changes what Needs
              attention counts.
            </Tip>
          </>
        )
      },
      {
        title: 'Needs attention',
        body: (
          <>
            <p>Every warning Hive can raise, people first. Click one to open the page that deals with it.</p>
            <Bullets>
              <li><strong>Overdue 1:1s</strong></li>
              <li><strong>Overloaded people</strong>, using the WIP limits in Settings</li>
              <li><strong>Unengaged members</strong>: direct reports with no task on any product, unless on leave today</li>
              <li><strong>Overdue tasks</strong>: open tasks past their due date, in any sprint</li>
              <li><strong>Products nobody has worked on</strong>, <strong>knowledge silos</strong>, <strong>scope creep</strong>, <strong>unattributed tasks</strong>, <strong>low utilization</strong> and <strong>falling velocity</strong></li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Current sprint',
        body: (
          <>
            <p>
              The sprint containing today; between sprints the next one to start, and after the last one the most
              recent. The bar shows the share of new story points done; the black marker shows how much of the sprint's
              working days have gone.
            </p>
            <Bullets>
              <li><strong>Behind pace</strong> when the share done trails the time gone by 15 points or more</li>
              <li><strong>Ahead of pace</strong> when it leads by 10 points or more</li>
            </Bullets>
            <p className="text-sm">
              Below it: the status mix (Blocked is always listed), scope creep against the committed capacity, points
              carried over from earlier sprints, and every blocked task with a link to Jira.
            </p>
          </>
        )
      },
      {
        title: '1:1s, to-dos and who\'s out',
        body: (
          <Bullets>
            <li>
              <strong>1:1s</strong> lists each direct report against your 1:1 cadence (set in Settings, 14 days by
              default), most urgent first. Click a person to open their 1:1s.
            </li>
            <li><strong>To-dos</strong> lists the bullets under the <em>hot</em> heading (for example <code>## hot 🔥</code>) of your latest journal entry: the note in the <strong>Journal</strong> folder whose title sorts last, so title entries by date (<code>2026-09-28</code>). Edit the note to change the list; click an item to open it.</li>
            <li><strong>Who's out</strong> lists leave today and starting in the next 14 days.</li>
            <li>
              <strong>Milestones</strong> appears only in a week when someone reaches six months at the company, or
              another full year, and lists them for the seven days up to the day.
            </li>
          </Bullets>
        )
      },
      {
        title: 'Delivery and sprint windows',
        body: (
          <>
            <p>
              Velocity, Capacity, Carry-over trend and Bug &amp; support inflow each choose their own sprints at their
              top right: <strong>Current</strong>, <strong>Last 3</strong>, <strong>Last 6</strong>,{' '}
              <strong>Last 12</strong> or <strong>All</strong> (the default). Changing one leaves the others alone.
              Workload and Work mix always cover every sprint and are marked <strong>All sprints</strong>, and
              Product engagement has its own period: <strong>3 months</strong> (the default),{' '}
              <strong>6 months</strong> or <strong>All time</strong>.
            </p>
            <Bullets>
              <li><strong>Velocity</strong>: completed story points per sprint</li>
              <li><strong>Capacity</strong>: committed against completed, and the next sprint's prediction, which is always shown</li>
              <li><strong>Carry-over trend</strong>: how much of each sprint was carried in from an earlier one, and how many tasks moved on unfinished</li>
              <li>
                <strong>Bug &amp; support inflow</strong>: new Bug and Support tickets (by Jira issue type) and maintenance
                tickets (by the Maintenance labels) per sprint, counted in tickets, not story points. A bug labelled
                maintenance shows in both columns; the share of new tasks counts it once
              </li>
              <li><strong>Workload</strong>: each person's tasks by status, where a warning icon marks anyone over the WIP limits; and each person's support and maintenance tickets, counted in tasks</li>
              <li><strong>Work mix</strong>: effort by type, label or component in story points or tasks, and by work category (Tech Maintenance, Tech Roadmap, Tech Debt, System Improvements, from the epic's name as set in Settings, or Productisation) as a share of issues and of story points</li>
              <li>Click a person, a bar or a sprint in these widgets to open the tasks behind it on the Tasks page</li>
              <li>
                <strong>Product engagement</strong>: how many direct reports have worked on each product, including products
                nobody has, and how many products each person has. Every task counts except epics; a task is in the period
                when one of its sprints is
              </li>
            </Bullets>
            <Note label="Next sprint prediction">
              the next sprint's commitment scaled by average utilization when it has one; otherwise the average points
              past sprints delivered, scaled by the people available in it after leave.
            </Note>
          </>
        )
      },
      {
        title: 'Team health',
        body: (
          <>
            <p>Team sentiment covers every sprint.</p>
            <Bullets>
              <li><strong>Team sentiment</strong>: the tone of recent 1:1s, when sentiment analysis is enabled in Settings. Hover a person to read why the model judged them as it did</li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Sync',
        body: (
          <p>
            <strong>Sync Jira</strong> pulls sprint dates, products, parents and tasks, then refreshes the page.
          </p>
        )
      }
    ]
  },
  {
    id: 'daily-planner',
    title: 'Daily Planner',
    icon: CalendarDays,
    description: 'Date-tagged notes, 1:1s and your Google Calendar in one month view',
    category: 'Overview',
    subtitle: 'Everything dated in Hive, and your Google Calendar, on one calendar',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              The <strong>Calendar</strong> entry in the navigation opens the Daily Planner: a month of working days
              (Monday to Friday) showing three kinds of item.
            </p>
            <Bullets>
              <li><strong className="text-amber-600">Notes</strong>: notes carrying a date tag</li>
              <li><strong className="text-purple-600">1:1s</strong>: shown on the meeting date as "1:1 · name"</li>
              <li><strong className="text-sky-600">Google Calendar</strong>: your events, when a calendar is connected in Settings</li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Putting a note on a day',
        body: (
          <>
            <p>
              Tag a note with a date written as <Code>#YYYYMMDD</Code>, for example <Code>#20261003</Code>. The tag can
              go in the note's tags or anywhere in its text, and must be a real date.
            </p>
            <Note>only the first 100 notes, in the Notes list's order, are checked for date tags.</Note>
          </>
        )
      },
      {
        title: 'Reading the month',
        body: (
          <>
            <Bullets>
              <li>A day shows up to three items, then "+N more". All-day items come first, then timed items by time</li>
              <li>Today has an amber ring. <strong>Today</strong> and the arrows move between months</li>
              <li>Click a day to open its items: title, date or time, and a preview, snippet or location</li>
              <li>All-day events appear on every day they cover; recurring events on each day they occur</li>
            </Bullets>
            <Tip>
              if the current month has no date-tagged notes, the planner opens on the month of the next upcoming one.
            </Tip>
          </>
        )
      },
      {
        title: 'Syncing Google Calendar',
        body: (
          <>
            <p>
              Opening the page never asks Google. Press <strong>Sync Google Calendar</strong> to load your events. The
              header says when the last sync ran.
            </p>
            <Bullets>
              <li>A sync is reused for 30 minutes: changing month or syncing again within that time is instant</li>
              <li>After 30 minutes the events disappear until the next sync</li>
              <li>Events are held in memory only: never stored, never in a backup, gone when Hive restarts</li>
              <li>If the calendar cannot be read, the error shows on the page and notes and 1:1s still show</li>
            </Bullets>
            <Note>to connect a calendar, see the Settings tutorial. Hive never writes to Google Calendar.</Note>
          </>
        )
      }
    ]
  },
  {
    id: 'notes',
    title: 'Notes',
    icon: StickyNote,
    description: 'Write markdown notes, file them in folders, pin and tag them',
    category: 'Overview',
    subtitle: 'Your own notes: write, file, pin and find them',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              Notes has three panes: folders on the left (on wide windows), the note list, and the editor. A note is a
              page to write on. There is no title field and no Save button.
            </p>
            <Bullets>
              <li>The title is the first non-empty line, with its markdown removed. An empty note is titled "New Note"</li>
              <li>Notes about a direct report belong on a 1:1, where they are part of that person's record</li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Creating a note',
        body: (
          <p>
            Press <strong>New</strong>, or <strong>Cmd/Ctrl+N</strong> anywhere on the page. The note is created empty in
            the folder you are viewing and opens ready to type. It exists from the first moment, so nothing you type is
            lost.
          </p>
        )
      },
      MARKDOWN_EDITOR_SECTION,
      {
        title: 'Pinning, tags and moving',
        body: (
          <Bullets>
            <li><strong>Pin note</strong> lists the note under "Pinned", above every other note in its folder</li>
            <li><strong>Note details</strong> (sliders icon) opens the tags field: comma separated, saved when you leave it</li>
            <li>The folder dropdown in the toolbar moves the note; "All Notes" means no folder</li>
            <li>You can also drag a note from the list onto a folder</li>
            <li><strong>Delete note</strong> asks first, and cannot be undone</li>
          </Bullets>
        )
      },
      {
        title: 'Folders',
        body: (
          <>
            <Bullets>
              <li><strong>New Folder</strong> at the bottom of the sidebar creates a top-level folder</li>
              <li>Hover a folder for <strong>New sub-folder</strong>, <strong>Rename folder</strong> and <strong>Delete folder</strong>. Folders nest as deep as you like</li>
              <li>Enter saves a name, Escape cancels</li>
              <li>"All Notes" lists every note, filed or not</li>
            </Bullets>
            <Tip label="Deleting a folder">
              never deletes what was written in it. Its notes and sub-folders move up to the folder that contained it.
            </Tip>
          </>
        )
      },
      {
        title: 'Finding notes',
        body: (
          <>
            <p>
              <strong>Search notes</strong> searches the folder you are viewing as you type. The list is always pinned
              notes on top, then by title from Z to A, so journal entries titled by date read newest first.
            </p>
            <Note>
              the list shows the first 100 notes; when there are more it says so. Narrow it with search.
            </Note>
          </>
        )
      }
    ]
  },

  // Delivery
  {
    id: 'quarterly-planning',
    title: 'Quarterly Planning',
    icon: TrendingUp,
    description: 'Turn the PM\'s options into a committed plan the team can deliver, and hold the quarter against it',
    category: 'Delivery',
    subtitle: 'Options, capacity, allocation, then the commitment and the review',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              The page follows the quarter in four steps, shown across the top. Each step says what it still needs, and turns
              green when it has it. <strong>Commit &amp; review</strong> lists what still stands in the way of committing.
            </p>
            <Steps>
              <li><strong>Options</strong>: add what the PM proposes, rank it, size it and write the solution design</li>
              <li><strong>Capacity</strong>: check who is on the team, their leave, the mix, and which options fit</li>
              <li><strong>Allocation</strong>: put people on options sprint by sprint, or let Draft plan propose it</li>
              <li><strong>Commit &amp; review</strong>: commit, share the summary, then follow each option against it</li>
            </Steps>
            <Note>the quarter's sprints must exist first; create them on the Sprints page or sync them from Jira.</Note>
          </>
        )
      },
      {
        title: 'Options',
        body: (
          <>
            <p>
              Add each candidate with a name and the status it starts in: In discovery, In Solution Design or Ready for commitment. An option is sized only once it is ready for commitment. Drag rows to set the PM's priority; leftovers from last quarter
              always come first and read <strong>0</strong>, and an option that arrived mid-quarter reads <strong>NEW</strong>.
              Click an option to open it:
            </p>
            <Bullets>
              <li><strong>Status and decision</strong>: In discovery, In Solution Design, Ready for commitment, then Committed or Dropped, with the reason</li>
              <li><strong>Solution design</strong>: size, work type, the design link, the due date, and notes</li>
              <li><strong>Products</strong>: they drive the people suggested from the knowledge matrix and past tasks</li>
              <li><strong>People</strong>: one lead (the crown) and contributors</li>
              <li><strong>Other teams</strong>: what it needs from CORE, CS, Research… and whether that is agreed</li>
              <li><strong>Carry over</strong> copies it to next quarter as a leftover, with what is still open</li>
            </Bullets>
            <p>
              <strong>Standing lanes</strong>, Maintenance and Tech roadmap, exist in every quarter and are sized by the mix.
            </p>
          </>
        )
      },
      {
        title: 'T-shirt sizes',
        body: (
          <>
            <p>
              Sizes are measured in <strong>dev-sprints</strong>: one person for one sprint. A size is S, M or L, widened with X
              (XL, XXL) and optionally a plus (L+). The defaults are S 1, M 2, L 4, XL 6 and XXL 12: XXL is an option that takes up the whole quarter.
            </p>
            <Tip>change the mapping in Settings, under T-Shirt Size to Sprints Mapping. A size without a mapping counts as nothing and is flagged.</Tip>
          </>
        )
      },
      {
        title: 'Capacity and the waterline',
        body: (
          <>
            <Bullets>
              <li><strong>Team this quarter</strong>: everyone starts at 100%; lower it for part-timers, add or remove people. Each sprint shows how available they are after leave and public holidays</li>
              <li><strong>Mix</strong>: the target split (50 / 20 / 30 by default) against the plan and last quarter, with how much of last quarter's tickets were bugs, support or maintenance. <strong>Use the team's average</strong> fills the target from the story points of all past work, split by the Dashboard's work categories; edit it and save</li>
              <li><strong>Waterline</strong>: options in priority order against what is left once the lanes take their share. Above the dashed line fits; drag to reprioritise</li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Allocation',
        body: (
          <>
            <p>People by sprint, as in the Plan-Catalogue sheet.</p>
            <Bullets>
              <li>Pick a <strong>brush</strong> (an option or lane), then click or drag across cells to give it the whole cell</li>
              <li>With <strong>Select</strong>, click a cell to set its shares exactly</li>
              <li>A red underline and percentage mark a sprint the person is partly away</li>
              <li><strong>By work type</strong> colours the grid by product, tech and maintenance to check the mix</li>
              <li><strong>Draft plan</strong> proposes allocations for the free cells; nothing changes until you apply it</li>
              <li><strong>What the plan delivers</strong> shows, for each option, what is planned against its size and the sprint it is covered by</li>
              <li>The <strong>Notes</strong> row keeps a note per sprint</li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Commit and review',
        body: (
          <>
            <p>
              <strong>Commit the plan</strong> records it as the baseline; commit again if it changes. Copy the summary for the PM.
              During the quarter:
            </p>
            <Bullets>
              <li><strong>Options against the commitment</strong>: each option then and now; mark it delivered, or carry it over</li>
              <li><strong>Estimation calibration</strong>: what each size really took, across quarters, to correct the mapping</li>
            </Bullets>
            <p><strong>Export</strong> downloads the plan as a Plan-Catalogue spreadsheet.</p>
          </>
        )
      }
    ]
  },
  {
    id: 'sprints',
    title: 'Sprints',
    icon: Zap,
    description: 'Create sprints, set their dates and committed points, sync dates from Jira',
    category: 'Delivery',
    subtitle: 'Sprints, their dates, and the capacity each one has',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              A sprint is identified by team, quarter, year and number. Its dates and committed points feed velocity,
              capacity and the current sprint on the Dashboard.
            </p>
            <Note label="Current sprint">
              the sprint containing today; between sprints the next to start, and after the last one the most recent.
              The Dashboard and the Tasks page's Current sprint filter both use this rule.
            </Note>
          </>
        )
      },
      {
        title: 'Sprint names',
        body: (
          <>
            <p>Every sprint name follows one pattern, which is also how the Jira sync and import recognise sprints:</p>
            <code className="block bg-slate-900 text-green-400 p-3 rounded text-xs">
              {'{TeamName}_{Quarter}Q{YearShort}_S{SprintNumber}'}
            </code>
            <p className="text-sm">
              Team "LP", Q4, 2025, sprint 6 → <Code>LP_4Q25_S6</Code>. Sprints are always ordered by year, quarter and
              number, never by name or date.
            </p>
          </>
        )
      },
      {
        title: 'Creating a sprint',
        body: (
          <Steps>
            <li>Press <strong>New Sprint</strong></li>
            <li>Enter the <strong>Team Name</strong>, <strong>Quarter</strong>, <strong>Year</strong> and <strong>Sprint Number</strong></li>
            <li>Check the <strong>Generated Sprint Name</strong> preview</li>
            <li>Press <strong>Create Sprint</strong>, then edit the sprint to add dates and committed points</li>
          </Steps>
        )
      },
      {
        title: 'Dates, committed points and team',
        body: (
          <>
            <p>The pencil on a sprint opens it for editing:</p>
            <Bullets>
              <li><strong>Start</strong> and <strong>End</strong> dates</li>
              <li><strong>Committed</strong>: the story points the team commits to, used for utilization and scope creep</li>
              <li>
                <strong>Team</strong>: people available, marked "(auto)". Hive works it out when you save: the team, less
                the working days lost to leave in the sprint
              </li>
            </Bullets>
            <Tip>keep leave up to date: the available team, and the next sprint's prediction, depend on it.</Tip>
          </>
        )
      },
      {
        title: 'Sprint Import Team Filter',
        body: (
          <>
            <p>
              At the top of Sprint Configuration. When set (for example "LP"), the Jira sync and CSV import only take
              that team's sprints; leave it empty to take every team.
            </p>
            <Note>set it when your Jira board carries sprints for several teams.</Note>
          </>
        )
      },
      {
        title: 'Syncing from Jira',
        body: (
          <Bullets>
            <li><strong>Sync Jira</strong> copies each sprint's planned start and end dates from the Jira board, matched by name</li>
            <li>It creates upcoming (future or active) sprints Hive does not have yet; never closed ones</li>
            <li>Jira's dates win over dates typed by hand; each change is recorded in the Activity Feed</li>
            <li>A sprint missing from the board, without both dates, or named twice on the board is left alone and reported</li>
            <li>It never deletes a sprint</li>
          </Bullets>
        )
      }
    ]
  },
  {
    id: 'products',
    title: 'Products',
    icon: Package,
    description: 'Products are Jira components: how work is attributed and tracked',
    category: 'Delivery',
    subtitle: 'The things the team builds and runs, and the work that names them',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              A product is a <strong>Jira component</strong>. A task or parent belongs to every product its components
              name, ignoring case, so one task can count toward several products. Nothing else links work to a product.
            </p>
            <Note>
              a task whose components name no product is <em>unattributed</em>. The Dashboard warns about these, because
              they are missing from every product breakdown.
            </Note>
          </>
        )
      },
      {
        title: 'Product tiles',
        body: (
          <Bullets>
            <li>The name, and whether it is a "Jira component" or "Made by hand"</li>
            <li><strong>Projects</strong>: the repositories that serve it, linked on the Projects page</li>
            <li>How many parents belong to it</li>
            <li><strong>Progress</strong>: resolved tasks over all tasks. Done and Won't Do both count as resolved</li>
          </Bullets>
        )
      },
      {
        title: 'Syncing from Jira',
        body: (
          <>
            <p>
              <strong>Sync Jira</strong> keeps one product for every component in the configured Jira space.
            </p>
            <Bullets>
              <li>Jira owns the name and description; renaming a component renames its product</li>
              <li>A product whose component is gone is removed, with its links to projects</li>
              <li>A component whose name another product already holds is reported, not applied</li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Products made by hand',
        body: (
          <>
            <p>
              <strong>New Product</strong> creates one when a component cannot be read from Jira. Use the component's
              exact name, since that is how work is matched. Names cannot contain a comma.
            </p>
            <Bullets>
              <li>Only products made by hand have the Edit and Delete menu</li>
              <li>A sync never removes them; the first sync that finds a component of the same name links the two</li>
            </Bullets>
          </>
        )
      }
    ]
  },
  {
    id: 'parents',
    title: 'Parents',
    icon: Layers,
    description: 'Epics and other Jira issues with children, and their progress',
    category: 'Delivery',
    subtitle: 'Jira parent issues that group tasks',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              A parent is any Jira issue with children: every epic, and any story, task or support ticket that has
              sub-tasks. It is not a manager or an organisational parent.
            </p>
            <Bullets>
              <li>Each card shows the name, the Jira key (a link when the Jira base URL is set) and the issue type</li>
              <li>Its labels, and the products its Jira components name</li>
              <li>Tasks and story points done over total, and progress by task count</li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Progress and done',
        body: (
          <Bullets>
            <li>A task is resolved when it is Done or Won't Do; a parent whose remaining tasks were all dropped reads 100%</li>
            <li>A parent is <strong>done</strong> when it has tasks and every one is resolved. A parent with no tasks is not done</li>
          </Bullets>
        )
      },
      {
        title: 'Filtering',
        body: (
          <>
            <Bullets>
              <li><strong>Search parents</strong> matches names and labels, on Enter</li>
              <li>Issue type chips: Epics first, then the other types</li>
              <li><strong>Hide done</strong> / <strong>Show done</strong>, remembered between visits</li>
              <li>Label chips, one at a time</li>
            </Bullets>
            <p className="text-sm">
              The Totals bar counts done over total per issue type, then tasks and story points, for the parents shown.
            </p>
          </>
        )
      },
      {
        title: 'Syncing from Jira',
        body: (
          <>
            <Bullets>
              <li><strong>Sync Jira</strong> keeps one parent for every parent issue in the configured space, done ones included</li>
              <li>Jira owns the name, the issue type and the components; the key is the parent's identity</li>
              <li>A parent whose issue is gone is removed, and its tasks are unlinked first</li>
              <li>An issue whose name another parent already holds is reported, never merged</li>
            </Bullets>
            <Tip label="Labels are yours">
              a parent's labels are only edited in Hive, and a sync never changes them.
            </Tip>
          </>
        )
      }
    ]
  },
  {
    id: 'tasks',
    title: 'Tasks',
    icon: CheckSquare,
    description: 'Find, filter and correct the team\'s tasks from Jira',
    category: 'Delivery',
    subtitle: 'The team\'s delivery work, kept in step with Jira',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              Tasks come from Jira: <strong>Sync Jira</strong> keeps them current, and <strong>Import CSV</strong> is
              for one-off corrections. There is no New Task button; a task's status always comes from Jira.
            </p>
            <Note>effort is measured in story points only. Hive does not track logged time or hours.</Note>
          </>
        )
      },
      {
        title: 'Reading a task card',
        body: (
          <Bullets>
            <li>The Jira link icon, the title, and "Overdue" or "Parent" badges where they apply</li>
            <li>Status, priority, type, assignee, project and due date</li>
            <li>
              Story points: <Code>5 SP</Code>, or <Code>3+2 SP</Code> for work carried over (3 new, 2 delivered in
              earlier sprints)
            </li>
            <li>The sprints it has been in, its labels, and its components (purple), which name its products</li>
            <li>A pin marks a field you overrode</li>
          </Bullets>
        )
      },
      {
        title: 'Filtering',
        body: (
          <>
            <Bullets>
              <li><strong>Search tasks</strong> filters on Enter; the X clears every filter</li>
              <li>Label and sprint chips, one of each at a time. <strong>Current sprint</strong> comes first</li>
              <li><strong>Exclude Parents</strong> hides tasks that are other tasks' parents</li>
              <li>Status chips with counts, plus <strong>All</strong> and <strong>Overdue</strong></li>
            </Bullets>
            <Note label="Sprints">
              a task is in every sprint it lists, like Jira's <Code>sprint = X</Code>, so filtering by a sprint includes
              work carried into or out of it.
            </Note>
            <p className="text-sm">
              The Totals bar adds up the story points of every matching task. Show 10, 20, 50 or 100 per page.
            </p>
          </>
        )
      },
      {
        title: 'Links into the page',
        body: (
          <Bullets>
            <li><Code>?sprint=current</Code> opens on the current sprint (the Dashboard's Current sprint card uses it)</li>
            <li><Code>?sprint=LP_4Q25_S6</Code> opens on a named sprint</li>
            <li><Code>?filter=overdue</Code> opens on overdue tasks (Needs attention uses it)</li>
            <li>
              <Code>?filter=stale</Code> opens on stale tasks: in flight with no status change for the days set under
              Settings, counted from the sync that saw the change
            </li>
            <li>
              <Code>?assignee=</Code>, <Code>?type=</Code>, <Code>?label=</Code> and <Code>?component=</Code> narrow the
              list; the Dashboard's drill-downs use them, and each shows as a chip you can clear
            </li>
          </Bullets>
        )
      },
      {
        title: 'Task actions',
        body: (
          <>
            <Bullets>
              <li><strong>Edit</strong>: title, description, priority, type, assignee, due date, story points, sprint, labels and components</li>
              <li><strong>Duplicate</strong>: a full copy of the task</li>
              <li><strong>Pin</strong>: override fields so Jira does not overwrite them (below)</li>
              <li><strong>Delete</strong>, or tick several and use <strong>Delete Selected</strong></li>
            </Bullets>
            <Warning>deleting is permanent. A task still in Jira comes back on the next sync.</Warning>
          </>
        )
      },
      {
        title: 'Overriding fields',
        body: (
          <>
            <p>
              <strong>Pin</strong> opens Override Task Fields. Tick a field and set its value; that field is then yours,
              and neither the sync nor the CSV import will overwrite it.
            </p>
            <Bullets>
              <li><strong>Assignee</strong>, <strong>Story Points</strong> and <strong>Sprint</strong></li>
              <li>
                <strong>Previous Sprints SP</strong>, for a task that has been in several sprints: the points already
                delivered, so New SP = Total − Previous
              </li>
              <li><strong>Clear Override</strong> hands a field back to Jira</li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Status lifecycle',
        body: (
          <>
            <p className="text-sm">
              Backlog → To Do → In Progress → In Review → In Test → PO Acceptance → Ready To Release → Done, with Blocked
              and Cancelled (Won't Do) alongside.
            </p>
            <Bullets>
              <li>Work cannot enter review without having been worked on: only from In Progress, Blocked or In Test</li>
              <li>A Done task cannot be cancelled, and a cancelled one cannot be completed</li>
              <li>Reopening always lands in To Do. A task Jira moves back from Done is reopened and moved on</li>
            </Bullets>
          </>
        )
      }
    ]
  },
  {
    id: 'projects',
    title: 'Projects',
    icon: FolderKanban,
    description: 'Code repositories and the products each one serves',
    category: 'Delivery',
    subtitle: 'Code repositories and the products they serve',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              A project is a code repository, such as a GitHub project. It serves one or more products, and a product
              can be served by several projects.
            </p>
            <Note>
              a project is never linked to tasks. Work reaches a project only through the products it serves.
            </Note>
          </>
        )
      },
      {
        title: 'Creating a project',
        body: (
          <Steps>
            <li>Press <strong>New Project</strong></li>
            <li>Enter a <strong>Name</strong>, and optionally a <strong>Description</strong> and <strong>URL</strong></li>
            <li>Under <strong>Products it serves</strong>, pick every product the repository serves</li>
            <li>Press <strong>Create</strong>. The project now appears on those products' tiles</li>
          </Steps>
        )
      },
      {
        title: 'Finding projects',
        body: (
          <Bullets>
            <li><strong>Search projects</strong> matches names, descriptions and product names, on Enter</li>
            <li>Product chips show only the projects serving that product</li>
            <li>Hover a card for its Edit and Delete menu</li>
          </Bullets>
        )
      },
      {
        title: 'Knowledge',
        body: (
          <p>
            Projects are the rows of the Knowledge Matrix, where you record how well each direct report knows each one.
          </p>
        )
      }
    ]
  },
  {
    id: 'knowledge-matrix',
    title: 'Knowledge Matrix',
    icon: Award,
    description: 'Record how well each person knows each project, and spot the gaps',
    category: 'Delivery',
    subtitle: 'Who knows which project, and where knowledge is thin',
    sections: [
      {
        title: 'Overview',
        body: (
          <p>
            The matrix has a row per project and a column per direct report. Every level is your own assessment, set by
            hand: Hive never suggests or changes a level.
          </p>
        )
      },
      {
        title: 'Knowledge levels',
        body: (
          <>
            <div className="space-y-3">
              {[
                { level: 1, label: 'No clue', color: 'bg-red-200 text-red-800', desc: 'Very little or no knowledge about the topic' },
                { level: 2, label: 'Limited', color: 'bg-orange-200 text-orange-800', desc: 'Some awareness, basic understanding' },
                { level: 3, label: 'Moderate', color: 'bg-yellow-200 text-yellow-800', desc: 'Reasonable understanding, familiar with key concepts' },
                { level: 4, label: 'Good', color: 'bg-lime-200 text-lime-800', desc: 'Solid understanding, can discuss with confidence' },
                { level: 5, label: 'Confident', color: 'bg-green-200 text-green-800', desc: 'Excellent comprehensive understanding, expert level' }
              ].map(({ level, label, color, desc }) => (
                <div key={level} className="flex items-start gap-3">
                  <span className={`w-10 h-10 rounded-full flex items-center justify-center text-sm font-medium flex-shrink-0 ${color}`}>
                    {level}
                  </span>
                  <div className="flex-1">
                    <h5 className="font-semibold text-slate-900">{label}</h5>
                    <p className="text-sm text-slate-600">{desc}</p>
                  </div>
                </div>
              ))}
            </div>
            <p className="text-sm">
              A <span className="text-slate-400">-</span> means not assessed. There is no level 0; level 1 is the
              starting point for someone who has touched the project at all.
            </p>
          </>
        )
      },
      {
        title: 'Setting a level',
        body: (
          <Steps>
            <li>Click the cell for the person and project</li>
            <li>Choose a level from 1 to 5, or <strong>-</strong> to remove the assessment</li>
            <li>The change is saved and recorded in the Activity Feed</li>
          </Steps>
        )
      },
      {
        title: 'Filtering and sorting',
        body: (
          <Bullets>
            <li><strong>Search projects</strong> filters rows, on Enter</li>
            <li>Select team member chips to show only their columns</li>
            <li><strong>Sort by Avg</strong> puts the best-known projects and people first and shows each average; <strong>Sort by Name</strong> goes back to A–Z</li>
            <li><strong>Legend</strong> explains the five levels</li>
          </Bullets>
        )
      },
      {
        title: 'Knowledge radar and gaps',
        body: (
          <>
            <p>
              Once any level is set, the radar shows the average and highest level for every assessed project, whatever
              the filters. Beside it are the overall average, the projects tracked, and the five weakest projects with
              how much of the team has been assessed on each.
            </p>
            <Tip>
              projects averaging below 3 are flagged as knowledge gaps. Pair someone who knows the project with someone
              who does not.
            </Tip>
          </>
        )
      }
    ]
  },
  {
    id: 'jira',
    title: 'Jira Sync & Import',
    icon: RefreshCw,
    description: 'Connect Jira Cloud, what each sync does, and the CSV import',
    category: 'Delivery',
    subtitle: 'How delivery data gets into Hive without typing it',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              Hive reads from Jira Cloud on demand and never writes back. <strong>Sync Jira</strong> on the Dashboard
              pulls everything, in order: sprint dates, products, parents, then tasks. The button on the Sprints,
              Products, Parents and Tasks pages pulls only that page's part.
            </p>
            <Note>
              the parts run independently: one that is not configured, or that Jira refuses, is reported without
              stopping the others.
            </Note>
          </>
        )
      },
      {
        title: 'Connecting Jira',
        body: (
          <>
            <p>In Settings, under Jira Integration:</p>
            <Bullets>
              <li><strong>Jira Base URL</strong>: your site, also used for links from tasks to Jira</li>
              <li><strong>Atlassian Account Email</strong> and an <strong>API Token</strong> (classic or scoped)</li>
              <li><strong>Board ID</strong>: the number in the board's URL, for sprint dates</li>
              <li><strong>Space Key</strong>, e.g. MAR: for products, parents and tasks</li>
            </Bullets>
            <Tip>
              a scoped token needs read access to Jira Software boards and sprints, and to the space's work items and
              components.
            </Tip>
          </>
        )
      },
      {
        title: 'Jira is the source of truth',
        body: (
          <>
            <Bullets>
              <li>A product, parent or task that came from Jira and is no longer returned is removed on the next sync</li>
              <li>Removal only follows a complete, non-empty answer, so a failed sync never empties Hive</li>
              <li>Anything made by hand in Hive, without a Jira key or component id, is never removed</li>
              <li>Fields you overrode on a task (Pin on the Tasks page) are never overwritten</li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Which tasks come across',
        body: (
          <Bullets>
            <li>Every work item in the space that is in at least one of the team's sprints, done ones included</li>
            <li>A sprint counts when its name follows <Code>Team_QQYY_SN</Code> and matches the Sprint Import Team Filter, if set</li>
            <li>Backlog items and queues such as "REFINEMENT QUEUE" are left out; epics become parents, not tasks</li>
            <li>Assignees match team members by email, or by full name when Jira hides the email</li>
            <li>Story points are rounded up; descriptions come across as plain text</li>
          </Bullets>
        )
      },
      {
        title: 'CSV import',
        body: (
          <>
            <p>
              For one-off corrections, <strong>Import CSV</strong> on the Tasks page takes a Jira CSV export. It maps
              fields exactly as the sync does.
            </p>
            <Steps>
              <li>Choose the file and press <strong>Preview Import</strong>: row counts, detected columns and the first ten rows</li>
              <li>Choose whether to <strong>update existing tasks</strong> or skip them, and to match them by <strong>Issue Key</strong> or <strong>Title</strong></li>
              <li>Press <strong>Import</strong> and review imported, skipped and error counts</li>
            </Steps>
          </>
        )
      }
    ]
  },

  // People
  {
    id: 'team',
    title: 'Team',
    icon: Users,
    description: 'Add direct and indirect reports, import them from CSV, track tenure',
    category: 'People',
    subtitle: 'The people you are responsible for',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              The Team page lists everyone you manage. Once added, people can be assigned tasks, linked to 1:1s, given
              leave and assessed in the Knowledge Matrix.
            </p>
            <Tip label="Email matters">
              Jira assignees are matched to team members by email, so use the address they have in Jira.
            </Tip>
          </>
        )
      },
      {
        title: 'Direct and indirect reports',
        body: (
          <Bullets>
            <li><strong>Direct</strong>: people who report to you. They are measured against the 1:1 cadence, appear in the Knowledge Matrix and can be flagged as unengaged</li>
            <li><strong>Indirect</strong>: skip-level or dotted-line reports. Tracked and reportable, but left out of those team analyses</li>
          </Bullets>
        )
      },
      {
        title: 'Adding a team member',
        body: (
          <>
            <Steps>
              <li>Press <strong>Add Team Member</strong></li>
              <li>Enter <strong>First Name</strong>, <strong>Last Name</strong>, <strong>Email</strong> and <strong>Hire Date</strong>; optionally <strong>Job Title</strong> and <strong>Department</strong></li>
              <li>Set the switch to <strong>Direct Report</strong> or <strong>Indirect Report</strong></li>
              <li>Press <strong>Add</strong></li>
            </Steps>
            <p className="text-sm">Hover a card and use its menu to Edit or Delete.</p>
            <Warning>
              when someone leaves, consider keeping their record: it is the history of their tasks, 1:1s and leave.
            </Warning>
          </>
        )
      },
      {
        title: 'Importing from CSV',
        body: (
          <>
            <p>
              <strong>Import CSV</strong> adds many people at once. <strong>Download sample CSV template</strong> gives
              the format:
            </p>
            <code className="block bg-slate-900 text-green-400 p-3 rounded text-xs overflow-x-auto">
              FirstName,LastName,Email,JobTitle,Department,HireDate,IsDirect
            </code>
            <p className="text-sm">
              FirstName, LastName and Email are required. With <strong>Skip duplicate emails</strong> ticked (the
              default), people already on the team are skipped; otherwise they are reported as errors. The results
              list each row as Created, Skipped or Error.
            </p>
          </>
        )
      },
      {
        title: 'Finding people',
        body: (
          <Bullets>
            <li><strong>Search team members</strong> matches name, email, job title and department, on Enter</li>
            <li>Department chips show one department at a time</li>
            <li><strong>All</strong>, <strong>Direct Reports</strong> and <strong>Indirect Reports</strong> filter by report type</li>
            <li>The X clears all of them at once</li>
          </Bullets>
        )
      },
      {
        title: 'Tenure',
        body: (
          <p>
            Each card shows tenure from the hire date, counted in whole months: "8 months" in the first year, "2 years"
            on an anniversary, otherwise "2y 3m". It is always current.
          </p>
        )
      }
    ]
  },
  {
    id: 'one-on-ones',
    title: '1:1 Meetings',
    icon: MessageSquare,
    description: 'Write each 1:1 as a markdown note, linked to the person by a tag',
    category: 'People',
    subtitle: 'One markdown note per 1:1, linked by its tags',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              A 1:1 is a single markdown note, written while the meeting happens. There is no agenda form and no action
              items. The title comes from the first line; a blank 1:1 is titled "New 1:1".
            </p>
            <Bullets>
              <li>The left pane lists <strong>All 1:1s</strong>, each team member with their count, and <strong>Unlinked</strong> when there are any</li>
              <li>The middle pane lists 1:1s newest first, with <strong>Search 1:1s</strong></li>
              <li>The right pane is the editor</li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Writing a 1:1',
        body: (
          <>
            <p>
              Press <strong>+ New</strong>. The 1:1 is created straight away, dated today. When you are viewing one
              person, it is already tagged with their name.
            </p>
            <Tip>
              the Dashboard's 1:1s card opens this page on the person you click.
            </Tip>
          </>
        )
      },
      {
        title: 'Linking by tags',
        body: (
          <>
            <p>
              You never pick the person from a list: the tags decide. Type tags in the bar under the toolbar.
            </p>
            <Bullets>
              <li>
                A tag names a person by first name, last name, or both joined in either order, ignoring case and
                punctuation: for Jane Doe, <Code>#jane</Code>, <Code>#doe</Code> and <Code>#janedoe</Code> all match
              </li>
              <li><Code>#YYYYMMDD</Code> sets the date, for a 1:1 written up later; the date picker does the same</li>
              <li>
                A 1:1 whose tags name nobody, or more than one person, is kept as <strong>Not linked to anyone</strong>.
                Hive never guesses; re-tag it to link it
              </li>
            </Bullets>
          </>
        )
      },
      MARKDOWN_EDITOR_SECTION,
      {
        title: '1:1 cadence',
        body: (
          <>
            <p>
              Each direct report is due a 1:1 every cadence period (14 days by default, set in Settings) after the last
              one held.
            </p>
            <Bullets>
              <li><strong>Due soon</strong> in the two days before, <strong>overdue</strong> after, <strong>never met</strong> with no linked 1:1</li>
              <li>A 1:1 dated in the future does not count until it has happened</li>
              <li>Someone on leave today is not flagged as overdue</li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Sentiment',
        body: (
          <Note label="Opt-in">
            with sentiment analysis enabled in Settings, the bodies of a person's recent 1:1s are sent to Claude and the
            result appears in Team sentiment on the Dashboard. Nothing is sent while it is off.
          </Note>
        )
      }
    ]
  },
  {
    id: 'leaves',
    title: 'Leaves',
    icon: Palmtree,
    description: 'Record leave and public holidays on the leave calendar',
    category: 'People',
    subtitle: 'Who is away, and when',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              Leave in Hive is a record, not a request: by the time it is entered it is already decided, so there is no
              approval step.
            </p>
            <Bullets>
              <li><strong className="text-green-600">Vacation</strong>, <strong className="text-orange-600">Sick Leave</strong>, <strong className="text-blue-600">Other</strong> and <strong className="text-purple-600">Public Holiday</strong></li>
              <li>Leave reduces a sprint's available team, and so its capacity and the next sprint's prediction</li>
              <li>It feeds Who's out on the Dashboard, and someone on leave today is not flagged for an overdue 1:1</li>
            </Bullets>
          </>
        )
      },
      {
        title: 'Adding leave',
        body: (
          <>
            <Steps>
              <li>Press <strong>Add Leave</strong></li>
              <li>Pick the <strong>Team Member</strong> and <strong>Leave Type</strong></li>
              <li>Set <strong>Start Date</strong> and <strong>End Date</strong> (both days included) and optional notes</li>
              <li>Press <strong>Add Leave</strong></li>
            </Steps>
            <p className="text-sm">The end cannot be before the start, and one leave cannot exceed 365 days.</p>
          </>
        )
      },
      {
        title: 'Public holidays',
        body: (
          <p>
            Choosing <strong>Public Holiday</strong> replaces the team member with a <strong>Holiday Name</strong>, and
            creates the leave for every active team member. Anyone who already has leave on those days is skipped, and
            the message says who.
          </p>
        )
      },
      {
        title: 'The calendar',
        body: (
          <Bullets>
            <li>A day shows up to four people, then "+N more"; hover a day for one second to see everyone and their notes</li>
            <li>Click a person's chip to edit or delete that leave; the person cannot be changed</li>
            <li>The arrows and <strong>Today</strong> move between months</li>
          </Bullets>
        )
      },
      {
        title: 'Summary and trend',
        body: (
          <>
            <p className="text-sm">
              Cards above the calendar count <strong>Total Records</strong>, <strong>On Leave Today</strong>,{' '}
              <strong>This Week</strong> and <strong>Upcoming</strong>. The chart plots people on leave per day for the
              month shown.
            </p>
            <Tip>log leave as soon as it is known, and add the year's public holidays early, so capacity is right.</Tip>
          </>
        )
      }
    ]
  },

  // Resources
  {
    id: 'activity-feed',
    title: 'Activity Feed',
    icon: Activity,
    description: 'The audit trail of everything that changed in Hive',
    category: 'Resources',
    subtitle: 'What changed, and when',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              Every change in Hive is recorded here, newest first: what was affected, a coloured badge for its kind
              (Task, Leave, Team member, Note, Sprint, Product, Parent, Project knowledge and more), what happened and
              when.
            </p>
            <Note>entries are never edited or deleted. The feed is the history.</Note>
          </>
        )
      },
      {
        title: 'Finding an entry',
        body: (
          <Bullets>
            <li><strong>Search activities</strong> filters on Enter</li>
            <li>Show 25, 50 or 100 per page</li>
            <li>Recent times read "Just now" or "N hours ago"; after a week the date is shown</li>
          </Bullets>
        )
      }
    ]
  },
  {
    id: 'settings',
    title: 'Settings',
    icon: SettingsIcon,
    description: 'Thresholds, T-shirt sizes, labels, Jira, Google Calendar, Claude and backups',
    category: 'Resources',
    subtitle: 'Everything you can change while Hive is running',
    sections: [
      {
        title: 'Overview',
        body: (
          <p>
            Settings sits at the bottom of the sidebar. Each card saves on its own button, and everything is stored in
            Hive's database, so there is nothing to edit on disk.
          </p>
        )
      },
      {
        title: 'Dashboard warning thresholds',
        body: (
          <Bullets>
            <li><strong>Max In Progress / Blocked / In Review Tasks</strong> per person (defaults 2, 1, 1): above them, Workload warns</li>
            <li><strong>Minimum Product Members</strong> (default 2): fewer people on a product is a knowledge silo</li>
            <li><strong>Days between 1:1s</strong> (default 14, 1 to 90): the 1:1 cadence</li>
          </Bullets>
        )
      },
      {
        title: 'T-shirt sizes and labels',
        body: (
          <Bullets>
            <li><strong>T-Shirt Size to Sprints Mapping</strong>: how many sprints S, M, L and XL initiatives take (defaults ½, 1, 2, 4)</li>
            <li><strong>Maintenance Labels</strong>: tasks carrying these labels count as maintenance in Workload and the Bug &amp; support inflow; support is recognised by its Jira issue type, Support</li>
            <li><strong>Work Categories</strong>: the epic names that make a task Tech Maintenance, Tech Roadmap, Tech Debt or System Improvements on Work mix; every other task is Productisation</li>
          </Bullets>
        )
      },
      {
        title: 'Jira Integration',
        body: (
          <p>
            The Jira base URL, account email, API token, board ID and space key. See the Jira Sync &amp; Import
            tutorial for what each one enables.
          </p>
        )
      },
      {
        title: 'Google Calendar',
        body: (
          <>
            <Steps>
              <li>In Google Calendar, open <strong>Settings</strong> and pick your calendar</li>
              <li>Under <strong>Integrate calendar</strong>, copy the <strong>Secret address in iCal format</strong></li>
              <li>Paste it here and press <strong>Save</strong></li>
            </Steps>
            <Tip label="Treat it like a password">
              it is never shown again once saved. If it leaks, reset it in Google Calendar and save the new one. Some
              Google Workspace admins turn it off.
            </Tip>
          </>
        )
      },
      {
        title: 'AI Sentiment Analysis',
        body: (
          <>
            <Steps>
              <li>Paste your <strong>Claude API Key</strong> and press <strong>Validate</strong></li>
              <li>Turn on <strong>Enable Sentiment Analysis</strong></li>
              <li>Choose the <strong>Analysis Period</strong> (90 days recommended) and <strong>Save Settings</strong></li>
            </Steps>
            <Note>
              off by default. Only the bodies of 1:1s inside the period are sent to Anthropic. Removing the key also
              turns analysis off.
            </Note>
          </>
        )
      },
      {
        title: 'Backup & Restore',
        body: (
          <>
            <Bullets>
              <li><strong>Export Backup</strong> downloads everything as <Code>hive-backup-YYYY-MM-DD.json</Code></li>
              <li><strong>Choose Backup File</strong> restores one: it adds what is missing, keeps the records you already have, and takes the backup's settings</li>
              <li>Hive also copies its whole database when it starts and every hour while it runs, when something changed, next to <Code>hive.db</Code>: a day of copies, then one a day for a month. Settings shows the last one</li>
            </Bullets>
            <Note>
              a backup holds everything: every table and every field, and your settings. The Claude key, Jira token
              and Google Calendar address are never in it, and a restore leaves the ones you have in place.
            </Note>
          </>
        )
      }
    ]
  },
  {
    id: 'logs',
    title: 'Logs',
    icon: Terminal,
    description: 'Find what went wrong and send an error report',
    category: 'Resources',
    subtitle: 'Find what went wrong and send it to the developers',
    sections: [
      {
        title: 'Overview',
        body: (
          <>
            <p>
              Logs sits at the bottom of the sidebar, above Settings. A red badge on it counts errors you have not seen
              yet. Entries come from four sources: the <strong>Renderer</strong> (the app window), failed{' '}
              <strong>API</strong> calls, the <strong>Backend</strong> and the <strong>Main</strong> process.
            </p>
            <Note>
              when nothing has gone wrong the page says so: "No errors or warnings since …".
            </Note>
          </>
        )
      },
      {
        title: 'Finding a problem',
        body: (
          <Bullets>
            <li>Level chips (<strong>Errors</strong>, <strong>Warnings</strong>, <strong>Info</strong>, <strong>Debug</strong>) with counts; errors and warnings are on by default</li>
            <li>Source chips narrow to one or more sources; <strong>Search messages</strong> searches messages and details</li>
            <li>Repeats of the same message are grouped, with a count and when it was last seen</li>
            <li>Click a row to expand it: the full details, such as a stack trace or an API response</li>
          </Bullets>
        )
      },
      {
        title: 'Reporting an error',
        body: (
          <>
            <Steps>
              <li>Tick the problems to report, or tick none to report every error and warning shown</li>
              <li><strong>Copy report</strong> copies it; the file icon saves it as a text file to attach</li>
              <li>Send it to the developers. Reported problems are marked <strong>Reported</strong></li>
            </Steps>
            <p className="text-sm">
              The report includes the app version and operating system, and the entries leading up to each problem.
            </p>
          </>
        )
      },
      {
        title: 'Earlier sessions and the log folder',
        body: (
          <>
            <Bullets>
              <li>Every run writes its own log file; the last 10 are kept, so the log of a run that crashed survives it</li>
              <li>The session dropdown opens an earlier run; <strong>Back to this session</strong> returns</li>
              <li><strong>Log folder</strong> opens the folder holding the files</li>
              <li>The download icon saves every entry of the session; the trash icon clears the current one, after asking</li>
            </Bullets>
          </>
        )
      }
    ]
  }
]

export default function Tutorials() {
  const [selectedTutorial, setSelectedTutorial] = useState<Tutorial | null>(null)
  const [searchInput, setSearchInput] = useState('')
  const [searchQuery, setSearchQuery] = useState('')

  // Filter tutorials based on search query
  const filteredTutorials = tutorials.filter(tutorial => {
    if (!searchQuery.trim()) return true
    const query = searchQuery.toLowerCase()
    return (
      tutorial.title.toLowerCase().includes(query) ||
      tutorial.description.toLowerCase().includes(query) ||
      tutorial.category.toLowerCase().includes(query)
    )
  })

  if (selectedTutorial) {
    return <TutorialContent tutorial={selectedTutorial} onBack={() => setSelectedTutorial(null)} />
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title="Tutorials"
        subtitle="A guide to every page in Hive: what it shows, what you can do, and the rules behind it"
      />

      {/* Search Bar */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
        <input
          type="text"
          placeholder="Search tutorials... (press Enter)"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter') setSearchQuery(searchInput) }}
          className="w-full pl-10 pr-10 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
        />
        {searchInput && (
          <button
            onClick={() => { setSearchInput(''); setSearchQuery('') }}
            className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600"
          >
            <X className="w-4 h-4" />
          </button>
        )}
      </div>

      {/* Tutorial Cards */}
      {filteredTutorials.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-slate-500">
              No tutorials match your search "{searchQuery}"
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredTutorials.map((tutorial) => {
            const Icon = tutorial.icon
            return (
              <div
                key={tutorial.id}
                className="cursor-pointer hover:shadow-lg transition-shadow"
                onClick={() => setSelectedTutorial(tutorial)}
              >
                <Card>
                  <CardContent>
                  <div className="flex items-start gap-4">
                    <div className="p-3 bg-amber-100 rounded-lg">
                      <Icon className="w-6 h-6 text-amber-600" />
                    </div>
                    <div className="flex-1">
                      <h3 className="font-semibold text-slate-900 mb-1">
                        {tutorial.title}
                      </h3>
                      <p className="text-xs text-amber-600 mb-2">
                        {tutorial.category}
                      </p>
                      <p className="text-sm text-slate-600">
                        {tutorial.description}
                      </p>
                    </div>
                    <ChevronRight className="w-5 h-5 text-slate-400 flex-shrink-0" />
                  </div>
                </CardContent>
                </Card>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

interface TutorialContentProps {
  tutorial: Tutorial
  onBack: () => void
}

function TutorialContent({ tutorial, onBack }: TutorialContentProps) {
  const [tutorialSearchQuery, setTutorialSearchQuery] = useState('')
  const [currentMatchIndex, setCurrentMatchIndex] = useState(0)
  const [totalMatches, setTotalMatches] = useState(0)
  const contentRef = useRef<HTMLDivElement>(null)

  // Clear highlights and search when tutorial changes
  useEffect(() => {
    setTutorialSearchQuery('')
    setCurrentMatchIndex(0)
    setTotalMatches(0)
  }, [tutorial.id])

  // Perform search and highlight
  useEffect(() => {
    if (!contentRef.current) return

    // Remove existing highlights
    const existingHighlights = contentRef.current.querySelectorAll('.tutorial-search-highlight')
    existingHighlights.forEach(el => {
      const parent = el.parentNode
      if (parent) {
        parent.replaceChild(document.createTextNode(el.textContent || ''), el)
        parent.normalize()
      }
    })

    if (!tutorialSearchQuery.trim()) {
      setTotalMatches(0)
      setCurrentMatchIndex(0)
      return
    }

    // Find and highlight matches
    const walker = document.createTreeWalker(
      contentRef.current,
      NodeFilter.SHOW_TEXT,
      {
        acceptNode: (node) => {
          // Skip script and style elements
          const parent = node.parentElement
          if (!parent || parent.tagName === 'SCRIPT' || parent.tagName === 'STYLE') {
            return NodeFilter.FILTER_REJECT
          }
          // Skip if already highlighted
          if (parent.classList.contains('tutorial-search-highlight')) {
            return NodeFilter.FILTER_REJECT
          }
          return NodeFilter.FILTER_ACCEPT
        }
      }
    )

    const textNodes: Text[] = []
    let node: Node | null
    while ((node = walker.nextNode())) {
      textNodes.push(node as Text)
    }

    const query = tutorialSearchQuery.toLowerCase()
    const highlights: HTMLElement[] = []

    textNodes.forEach(textNode => {
      const text = textNode.textContent || ''
      const lowerText = text.toLowerCase()
      let lastIndex = 0
      const indices: number[] = []

      let index = lowerText.indexOf(query, lastIndex)
      while (index !== -1) {
        indices.push(index)
        lastIndex = index + query.length
        index = lowerText.indexOf(query, lastIndex)
      }

      if (indices.length > 0) {
        const parent = textNode.parentNode
        if (!parent) return

        const fragment = document.createDocumentFragment()
        let currentPos = 0

        indices.forEach(matchIndex => {
          // Add text before match
          if (matchIndex > currentPos) {
            fragment.appendChild(document.createTextNode(text.substring(currentPos, matchIndex)))
          }

          // Add highlighted match
          const mark = document.createElement('mark')
          mark.className = 'tutorial-search-highlight'
          mark.style.backgroundColor = '#fef08a' // yellow-200
          mark.style.padding = '2px'
          mark.style.borderRadius = '2px'
          mark.textContent = text.substring(matchIndex, matchIndex + query.length)
          fragment.appendChild(mark)
          highlights.push(mark)

          currentPos = matchIndex + query.length
        })

        // Add remaining text
        if (currentPos < text.length) {
          fragment.appendChild(document.createTextNode(text.substring(currentPos)))
        }

        parent.replaceChild(fragment, textNode)
      }
    })

    setTotalMatches(highlights.length)
    if (highlights.length > 0) {
      setCurrentMatchIndex(0)
      // Highlight first match
      highlights[0].style.backgroundColor = '#fbbf24' // amber-400
      highlights[0].style.fontWeight = 'bold'
      highlights[0].scrollIntoView({ behavior: 'smooth', block: 'center' })
    }
  }, [tutorialSearchQuery])

  // Navigate to next/previous match
  const navigateMatch = (direction: 'next' | 'prev') => {
    if (!contentRef.current || totalMatches === 0) return

    const highlights = Array.from(contentRef.current.querySelectorAll('.tutorial-search-highlight')) as HTMLElement[]
    if (highlights.length === 0) return

    // Reset current highlight to default
    highlights[currentMatchIndex].style.backgroundColor = '#fef08a' // yellow-200
    highlights[currentMatchIndex].style.fontWeight = 'normal'

    // Calculate new index
    let newIndex = currentMatchIndex
    if (direction === 'next') {
      newIndex = (currentMatchIndex + 1) % totalMatches
    } else {
      newIndex = (currentMatchIndex - 1 + totalMatches) % totalMatches
    }

    // Highlight new match
    highlights[newIndex].style.backgroundColor = '#fbbf24' // amber-400
    highlights[newIndex].style.fontWeight = 'bold'
    highlights[newIndex].scrollIntoView({ behavior: 'smooth', block: 'center' })

    setCurrentMatchIndex(newIndex)
  }

  return (
    <div className="space-y-4">
      {/* Search Bar */}
      <div className="sticky top-0 z-10 bg-white pb-4">
        <div className="flex items-center gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
            <input
              type="text"
              placeholder="Search within tutorial..."
              value={tutorialSearchQuery}
              onChange={(e) => setTutorialSearchQuery(e.target.value)}
              className="w-full pl-10 pr-10 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500 text-sm"
            />
            {tutorialSearchQuery && (
              <button
                onClick={() => setTutorialSearchQuery('')}
                className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600"
              >
                <X className="w-4 h-4" />
              </button>
            )}
          </div>
          {totalMatches > 0 && (
            <>
              <div className="flex items-center gap-2 px-3 py-2 bg-slate-100 rounded-lg text-sm text-slate-600">
                <span>{currentMatchIndex + 1} / {totalMatches}</span>
              </div>
              <div className="flex items-center gap-1">
                <button
                  onClick={() => navigateMatch('prev')}
                  className="p-2 hover:bg-slate-100 rounded-lg text-slate-600"
                  title="Previous match"
                >
                  <ArrowUp className="w-4 h-4" />
                </button>
                <button
                  onClick={() => navigateMatch('next')}
                  className="p-2 hover:bg-slate-100 rounded-lg text-slate-600"
                  title="Next match"
                >
                  <ArrowDown className="w-4 h-4" />
                </button>
              </div>
            </>
          )}
        </div>
      </div>

      {/* Tutorial Content */}
      <div ref={contentRef}>
        <TutorialGuide tutorial={tutorial} onBack={onBack} />
      </div>
    </div>
  )
}

function TutorialGuide({ tutorial, onBack }: TutorialContentProps) {
  const sectionId = (index: number) => `${tutorial.id}-section-${index + 1}`

  return (
    <div className="space-y-6 max-w-5xl">
      <button
        onClick={onBack}
        className="flex items-center gap-2 text-amber-600 hover:text-amber-700 transition-colors"
      >
        <Home className="w-4 h-4" />
        Back to Tutorials
      </button>

      <PageHeader title={`${tutorial.title} Tutorial`} subtitle={tutorial.subtitle} />

      <Card>
        <CardHeader title="Table of Contents" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {tutorial.sections.map((section, index) => (
              <button
                key={section.title}
                onClick={() => {
                  const element = document.getElementById(sectionId(index))
                  element?.scrollIntoView({ behavior: 'smooth', block: 'start' })
                }}
                className="flex items-center gap-2 px-3 py-2 text-left text-sm text-slate-700 hover:bg-slate-100 rounded transition-colors"
              >
                <ChevronRight className="w-4 h-4 text-amber-500" />
                {section.title}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {tutorial.sections.map((section, index) => (
        <div key={section.title} id={sectionId(index)}>
          <Card>
            <CardHeader title={`${index + 1}. ${section.title}`} />
            <CardContent>
              <div className="space-y-4 text-slate-700">{section.body}</div>
            </CardContent>
          </Card>
        </div>
      ))}
    </div>
  )
}
