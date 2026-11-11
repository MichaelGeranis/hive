using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

internal static class DbSetExtensions
{
    /// <summary>
    /// Marks an entity as modified. Reads are untracked, so the instance being saved may not be
    /// the one this context tracks for the same row (from an earlier GetById or save); that
    /// stale instance is let go first, since Update would otherwise throw.
    /// </summary>
    public static void UpdateUntracked<T>(this DbSet<T> set, T entity) where T : class
    {
        var key = set.EntityType.FindPrimaryKey()!.Properties;
        var keyValues = key.Select(p => p.PropertyInfo!.GetValue(entity));
        var tracked = set.Local.FindEntryUntyped(keyValues);
        if (tracked is not null && !ReferenceEquals(tracked.Entity, entity))
            tracked.State = EntityState.Detached;

        set.Update(entity);
    }
}
