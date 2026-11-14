using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Hive.Infrastructure.Persistence;

/// <summary>
/// Encryption of the SQLite database at rest, with SQLite3 Multiple Ciphers.
/// </summary>
/// <remarks>
/// The desktop app keeps a random 256-bit key in the operating system's keystore (Electron
/// <c>safeStorage</c>) and hands it to the backend in <c>HIVE_DATABASE_KEY</c>. With a key, the
/// database and its automatic backups are encrypted; a database written before encryption
/// existed is encrypted in place on the first start that has a key, and so are its backups.
/// Without a key (running the backend from source) the database is plain SQLite, as before.
/// </remarks>
public static class DatabaseEncryption
{
    /// <summary>The environment variable that carries the key.</summary>
    public const string KeyVariable = "HIVE_DATABASE_KEY";

    private static readonly byte[] PlainHeader = Encoding.ASCII.GetBytes("SQLite format 3\0");

    /// <summary>
    /// A key is 64 hexadecimal characters (256 bits). Anything else is refused, which also keeps
    /// it safe to place inside a PRAGMA.
    /// </summary>
    public static bool IsValidKey(string? key) =>
        key is { Length: 64 } && key.All(Uri.IsHexDigit);

    /// <summary>
    /// The connection string for the database, opening it with the key when there is one.
    /// </summary>
    public static string WithKey(string connectionString, string? key)
    {
        if (key is null)
        {
            return connectionString;
        }

        EnsureValid(key);
        return new SqliteConnectionStringBuilder(connectionString) { Password = key }.ToString();
    }

    /// <summary>
    /// True when the file is an unencrypted SQLite database.
    /// </summary>
    public static bool IsPlaintext(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        using var file = File.OpenRead(path);
        if (file.Length < PlainHeader.Length)
        {
            return false;
        }

        Span<byte> header = stackalloc byte[16];
        file.ReadExactly(header);
        return header.SequenceEqual(PlainHeader);
    }

    /// <summary>
    /// Encrypts the database, and every backup of it, that is still unencrypted.
    /// </summary>
    /// <returns>How many files were encrypted</returns>
    public static int EncryptPlaintextFiles(string databasePath, string key, ILogger logger)
    {
        EnsureValid(key);
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath)) ?? ".";
        var name = Path.GetFileName(databasePath);
        var candidates = new[] { Path.GetFullPath(databasePath) }
            .Concat(Directory.Exists(directory) ? Directory.GetFiles(directory, $"{name}.backup.*") : []);

        var encrypted = 0;
        foreach (var path in candidates.Where(IsPlaintext))
        {
            EncryptInPlace(path, key);
            encrypted++;
            logger.LogInformation("Encrypted {DatabaseFile}", Path.GetFileName(path));
        }

        return encrypted;
    }

    /// <summary>
    /// Encrypts an unencrypted database file in place.
    /// </summary>
    public static void EncryptInPlace(string path, string key)
    {
        EnsureValid(key);
        var plain = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();
        bool wasWal;
        using (var connection = new SqliteConnection(plain))
        {
            connection.Open();
            wasWal = string.Equals(Scalar(connection, "PRAGMA journal_mode;"), "wal", StringComparison.OrdinalIgnoreCase);
            // Re-keying needs a rollback journal: this also folds the write-ahead log into the file
            Scalar(connection, "PRAGMA journal_mode=DELETE;");
            Scalar(connection, $"PRAGMA rekey='{key}';");
        }

        if (wasWal)
        {
            using var connection = new SqliteConnection(WithKey(plain, key));
            connection.Open();
            Scalar(connection, "PRAGMA journal_mode=WAL;");
        }

        if (IsPlaintext(path))
        {
            throw new InvalidOperationException($"{Path.GetFileName(path)} is still unencrypted after re-keying.");
        }
    }

    private static string? Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar()?.ToString();
    }

    private static void EnsureValid(string key)
    {
        if (!IsValidKey(key))
        {
            throw new ArgumentException($"{KeyVariable} must be 64 hexadecimal characters.", nameof(key));
        }
    }
}
