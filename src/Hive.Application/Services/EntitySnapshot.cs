using System.Reflection;
using System.Text.Json;

namespace Hive.Application.Services;

/// <summary>
/// An entity as it is stored: every property it keeps (those with a setter, private or not),
/// as JSON. Computed properties are left out. Restoring sets the same properties on an entity
/// built through its parameterless constructor, the way the database materialises a row, so
/// a backup brings back exactly what was saved without replaying the domain's methods.
/// </summary>
public static class EntitySnapshot
{
    private static readonly JsonSerializerOptions Options = new();

    /// <summary>
    /// The stored properties of an entity, by name.
    /// </summary>
    /// <param name="entity">The entity to capture.</param>
    /// <param name="omit">Property names to leave out, such as secrets.</param>
    public static JsonElement Capture(object entity, IReadOnlySet<string>? omit = null)
    {
        var values = StoredProperties(entity.GetType())
            .Where(p => omit == null || !omit.Contains(p.Name))
            .ToDictionary(p => p.Name, p => p.GetValue(entity));
        return JsonSerializer.SerializeToElement(values, Options);
    }

    /// <summary>
    /// Rebuilds an entity from its stored properties. A property missing from the snapshot
    /// keeps the value the entity starts with.
    /// </summary>
    public static T Restore<T>(JsonElement snapshot) where T : class
    {
        var entity = (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
        Apply(entity, snapshot);
        return entity;
    }

    /// <summary>
    /// Sets the stored properties a snapshot carries onto an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="snapshot">The snapshot to read.</param>
    /// <param name="skip">Property names to leave as they are.</param>
    public static void Apply(object entity, JsonElement snapshot, IReadOnlySet<string>? skip = null)
    {
        foreach (var property in StoredProperties(entity.GetType()))
        {
            if (skip != null && skip.Contains(property.Name))
                continue;
            if (!snapshot.TryGetProperty(property.Name, out var value))
                continue;

            property.SetValue(entity, value.Deserialize(property.PropertyType, Options));
        }
    }

    /// <summary>
    /// The id a snapshot carries.
    /// </summary>
    public static Guid IdOf(JsonElement snapshot) => snapshot.GetProperty("Id").GetGuid();

    private static IEnumerable<PropertyInfo> StoredProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetSetMethod(nonPublic: true) != null && p.GetIndexParameters().Length == 0);
}
