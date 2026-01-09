import { exec } from 'child_process';
import { promisify } from 'util';

const execAsync = promisify(exec);

export interface AppleCalendarEvent {
  id: string;
  title: string;
  startDate: string;
  endDate: string;
  location: string;
  calendar: string;
}

/**
 * Fetches events from Apple Calendar for the next 30 days from a specific calendar
 * @param calendarEmail The email of the calendar to query (e.g., "michael.geranis@orfium.com")
 * @returns Array of calendar events
 */
export async function fetchAppleCalendarEvents(
  calendarEmail: string
): Promise<AppleCalendarEvent[]> {
  const today = new Date();
  const endDate = new Date();
  endDate.setDate(today.getDate() + 10);

  // Use JavaScript for Automation (JXA) which has better permission handling
  const safeEmail = calendarEmail.replace(/"/g, '\\"').replace(/'/g, "\\'");

  const jxaScript = `
    const app = Application('Calendar');
    app.includeStandardAdditions = true;

    const today = new Date();
    const endDate = new Date();
    endDate.setDate(today.getDate() + 10);

    const targetEmail = "${safeEmail}";

    // Find calendar by name
    let foundCalendar = null;
    const calendars = app.calendars();

    for (let i = 0; i < calendars.length; i++) {
      if (calendars[i].name().includes(targetEmail)) {
        foundCalendar = calendars[i];
        break;
      }
    }

    if (!foundCalendar) {
      return JSON.stringify({ error: "Calendar not found for email: " + targetEmail });
    }

    // Get events
    const events = foundCalendar.events.whose({
      _and: [
        { startDate: { _greaterThanEquals: today } },
        { startDate: { _lessThanEquals: endDate } }
      ]
    })();

    const eventData = [];
    for (let i = 0; i < events.length; i++) {
      const event = events[i];
      eventData.push({
        id: event.uid(),
        title: event.summary(),
        startDate: event.startDate().toISOString(),
        endDate: event.endDate().toISOString(),
        location: event.location() || "",
        calendar: foundCalendar.name()
      });
    }

    return JSON.stringify(eventData);
  `;

  try {
    // Use JXA (JavaScript for Automation) instead of AppleScript
    const { stdout, stderr } = await execAsync(`osascript -l JavaScript -e '${jxaScript}'`);

    if (stderr) {
      console.error('JXA stderr:', stderr);
    }

    // Parse JSON output
    const result = JSON.parse(stdout);

    if (result.error) {
      throw new Error(result.error);
    }

    return result;
  } catch (error: any) {
    console.error('Error fetching Apple Calendar events:', error);

    // Check for specific error codes
    const errorStr = error.toString();

    if (errorStr.includes('-609') || errorStr.includes('Connection is invalid')) {
      throw new Error('PERMISSION_ERROR: Hive needs Automation permission. Try building the app (npm run electron:build:mac) instead of running in dev mode, as dev mode may not trigger macOS permission dialogs correctly.');
    }

    if (errorStr.includes('-1743') || errorStr.includes('not authorized')) {
      throw new Error('PERMISSION_ERROR: Calendar access denied. Please grant Hive access in System Settings → Privacy & Security → Calendar.');
    }

    if (errorStr.includes('SyntaxError') || errorStr.includes('JSON')) {
      throw new Error(`Failed to parse calendar data: ${error.message}`);
    }

    throw new Error(`Failed to fetch calendar events: ${error.message || error}`);
  }
}


/**
 * Extract direct report name from meeting title using pattern "[FirstName] / [ManagerName]"
 * @param title The meeting title
 * @returns The first name of the direct report, or null if pattern doesn't match
 */
export function extractDirectReportName(title: string): string | null {
  // Pattern: "FirstName / ManagerName" (e.g., "George / Michail")
  const pattern = /^([^\/]+)\s*\/\s*[^\/]+$/;
  const match = title.trim().match(pattern);

  if (match && match[1]) {
    return match[1].trim();
  }

  return null;
}

/**
 * Check if Calendar access is granted
 * This uses a lightweight check that doesn't force Calendar to launch
 */
export async function checkCalendarAccess(): Promise<boolean> {
  try {
    const { stdout } = await execAsync(`osascript -l JavaScript -e '
      const app = Application("Calendar");
      try {
        app.calendars.length;
        true;
      } catch (e) {
        false;
      }
    '`);
    return stdout.trim() === 'true';
  } catch (error) {
    console.error('Calendar access check error:', error);
    return false;
  }
}
