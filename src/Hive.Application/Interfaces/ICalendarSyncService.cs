using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

public interface ICalendarSyncService
{
    Task<CalendarSyncResultDto> SyncCalendarEventsAsync(IEnumerable<AppleCalendarEventDto> events);
}
