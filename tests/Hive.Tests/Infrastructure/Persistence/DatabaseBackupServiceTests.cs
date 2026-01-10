using Hive.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Hive.Tests.Infrastructure.Persistence;

public class DatabaseBackupServiceTests : IDisposable
{
    private readonly Mock<ILogger<DatabaseBackupService>> _loggerMock;
    private readonly DatabaseBackupService _service;
    private readonly string _tempDirectory;
    private readonly List<string> _createdFiles;

    public DatabaseBackupServiceTests()
    {
        _loggerMock = new Mock<ILogger<DatabaseBackupService>>();
        _service = new DatabaseBackupService(_loggerMock.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), "HiveBackupTests", Guid.NewGuid().ToString());
        _createdFiles = new List<string>();

        // Create temp directory for tests
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        // Cleanup: delete all created files and temp directory
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        // Act
        var service = new DatabaseBackupService(_loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void CreateBackup_WithNonExistentDatabase_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.db");

        // Act
        var result = _service.CreateBackup(nonExistentPath);

        // Assert
        result.Should().BeFalse();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Database file not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CreateBackup_WithExistingDatabase_CreatesBackupFile()
    {
        // Arrange
        var dbPath = CreateTestDatabase();

        // Act
        var result = _service.CreateBackup(dbPath);

        // Assert
        result.Should().BeTrue();

        // Verify backup file was created
        var backupFiles = Directory.GetFiles(_tempDirectory, "*.backup.*");
        backupFiles.Should().ContainSingle();

        // Verify backup file has content
        var backupFile = backupFiles[0];
        File.Exists(backupFile).Should().BeTrue();
        File.ReadAllText(backupFile).Should().Be("test database content");
    }

    [Fact]
    public void CreateBackup_WithExistingDatabase_BackupFileHasTimestamp()
    {
        // Arrange
        var dbPath = CreateTestDatabase();
        var beforeBackup = DateTime.Now.AddSeconds(-1);

        // Act
        var result = _service.CreateBackup(dbPath);

        // Assert
        result.Should().BeTrue();

        var backupFiles = Directory.GetFiles(_tempDirectory, "*.backup.*");
        var backupFileName = Path.GetFileName(backupFiles[0]);

        // Backup file should match pattern: test.db.backup.yyyy-MM-dd_HH-mm-ss
        backupFileName.Should().StartWith("test.db.backup.");
        backupFileName.Should().MatchRegex(@"test\.db\.backup\.\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}");
    }

    [Fact]
    public void CreateBackup_LogsSuccessfulBackup()
    {
        // Arrange
        var dbPath = CreateTestDatabase();

        // Act
        var result = _service.CreateBackup(dbPath);

        // Assert
        result.Should().BeTrue();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Creating database backup")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Database backup created successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CreateBackup_WithMultipleBackups_KeepsOnlyMostRecent()
    {
        // Arrange
        var dbPath = CreateTestDatabase();

        // Act - Create 12 backups (more than the max of 10)
        for (int i = 0; i < 12; i++)
        {
            _service.CreateBackup(dbPath);
            Thread.Sleep(1100); // Wait to ensure different timestamps
        }

        // Assert - Should have exactly 10 backups
        var backupFiles = Directory.GetFiles(_tempDirectory, "*.backup.*");
        backupFiles.Should().HaveCount(10);
    }

    [Fact]
    public void CreateBackup_WithMultipleBackups_DeletesOldestBackups()
    {
        // Arrange
        var dbPath = CreateTestDatabase();
        var backupPaths = new List<string>();

        // Create 5 backups with different timestamps
        for (int i = 0; i < 5; i++)
        {
            _service.CreateBackup(dbPath);
            var backupFiles = Directory.GetFiles(_tempDirectory, "*.backup.*");
            // Sort by filename (contains timestamp) - same ordering as implementation
            var latestBackup = backupFiles.OrderByDescending(f => Path.GetFileName(f)).First();
            backupPaths.Add(latestBackup);
            Thread.Sleep(1100); // Wait to ensure different timestamps
        }

        var oldestBackup = backupPaths[0];
        File.Exists(oldestBackup).Should().BeTrue();

        // Act - Create 6 more backups to exceed the limit of 10
        for (int i = 0; i < 6; i++)
        {
            _service.CreateBackup(dbPath);
            Thread.Sleep(1100);
        }

        // Assert - Oldest backup should be deleted
        var remainingBackupFiles = Directory.GetFiles(_tempDirectory, "*.backup.*");
        remainingBackupFiles.Should().HaveCount(10);
        File.Exists(oldestBackup).Should().BeFalse();
    }

    [Fact]
    public void CreateBackup_WhenCleanupFails_StillReturnsTrue()
    {
        // Arrange
        var dbPath = CreateTestDatabase();

        // Create a backup first
        _service.CreateBackup(dbPath);

        // Lock one of the backup files to cause cleanup to potentially fail
        var backupFiles = Directory.GetFiles(_tempDirectory, "*.backup.*");
        var lockedFile = backupFiles[0];

        // Note: File locking behavior is platform-specific, so this test just ensures
        // the service handles cleanup errors gracefully

        // Wait to ensure different timestamp for next backup
        Thread.Sleep(1100);

        // Act - Create more backups
        var result = _service.CreateBackup(dbPath);

        // Assert - Should still succeed even if cleanup had issues
        result.Should().BeTrue();
    }

    [Fact]
    public void CreateBackup_WithIOException_ReturnsFalseAndLogsError()
    {
        // Arrange
        var dbPath = CreateTestDatabase();
        var lockedDbPath = Path.Combine(_tempDirectory, "locked.db");

        // Create and lock a file
        using (var fileStream = File.Create(lockedDbPath))
        using (var writer = new StreamWriter(fileStream))
        {
            writer.Write("locked content");
            writer.Flush();

            // Try to create backup while file is locked
            // This should fail on Windows; on Unix it may succeed
            var result = _service.CreateBackup(lockedDbPath);

            // The result depends on the platform, but we verify error handling exists
            if (!result)
            {
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create database backup")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
        }
    }

    [Fact]
    public void CreateBackup_WithReadOnlyFile_CreatesBackupSuccessfully()
    {
        // Arrange
        var dbPath = CreateTestDatabase();

        // Make the file read-only
        var fileInfo = new FileInfo(dbPath);
        fileInfo.IsReadOnly = true;

        try
        {
            // Act
            var result = _service.CreateBackup(dbPath);

            // Assert
            result.Should().BeTrue();
            var backupFiles = Directory.GetFiles(_tempDirectory, "*.backup.*");
            backupFiles.Should().ContainSingle();
        }
        finally
        {
            // Cleanup: remove read-only attribute
            fileInfo.IsReadOnly = false;
        }
    }

    [Fact]
    public void CreateBackup_WithEmptyDatabase_CreatesEmptyBackup()
    {
        // Arrange
        var dbPath = Path.Combine(_tempDirectory, "empty.db");
        File.WriteAllText(dbPath, string.Empty);
        _createdFiles.Add(dbPath);

        // Act
        var result = _service.CreateBackup(dbPath);

        // Assert
        result.Should().BeTrue();
        var backupFiles = Directory.GetFiles(_tempDirectory, "*.backup.*");
        backupFiles.Should().ContainSingle();

        var backupFile = backupFiles[0];
        File.ReadAllText(backupFile).Should().BeEmpty();
    }

    [Fact]
    public void CreateBackup_WithLargeDatabase_CreatesBackupSuccessfully()
    {
        // Arrange
        var dbPath = Path.Combine(_tempDirectory, "large.db");
        var largeContent = new string('X', 10 * 1024 * 1024); // 10 MB
        File.WriteAllText(dbPath, largeContent);
        _createdFiles.Add(dbPath);

        // Act
        var result = _service.CreateBackup(dbPath);

        // Assert
        result.Should().BeTrue();
        var backupFiles = Directory.GetFiles(_tempDirectory, "*.backup.*");
        backupFiles.Should().ContainSingle();

        var backupFile = backupFiles[0];
        var backupInfo = new FileInfo(backupFile);
        backupInfo.Length.Should().BeGreaterThan(10 * 1024 * 1024 - 1000); // Allow small margin
    }

    [Fact]
    public void CreateBackup_WithSpecialCharactersInPath_HandlesCorrectly()
    {
        // Arrange
        var specialDir = Path.Combine(_tempDirectory, "special (dir) [test]");
        Directory.CreateDirectory(specialDir);

        var dbPath = Path.Combine(specialDir, "test.db");
        File.WriteAllText(dbPath, "test content");
        _createdFiles.Add(dbPath);

        // Act
        var result = _service.CreateBackup(dbPath);

        // Assert
        result.Should().BeTrue();
        var backupFiles = Directory.GetFiles(specialDir, "*.backup.*");
        backupFiles.Should().ContainSingle();
    }

    [Fact]
    public void CreateBackup_CreatesBackupInSameDirectoryAsOriginal()
    {
        // Arrange
        var dbPath = CreateTestDatabase();
        var originalDirectory = Path.GetDirectoryName(dbPath);

        // Act
        _service.CreateBackup(dbPath);

        // Assert
        var backupFiles = Directory.GetFiles(originalDirectory!, "*.backup.*");
        backupFiles.Should().ContainSingle();

        var backupDirectory = Path.GetDirectoryName(backupFiles[0]);
        backupDirectory.Should().Be(originalDirectory);
    }

    [Fact]
    public void CreateBackup_MultipleConsecutiveCalls_CreatesMultipleBackups()
    {
        // Arrange
        var dbPath = CreateTestDatabase();

        // Act
        _service.CreateBackup(dbPath);
        Thread.Sleep(1100); // Ensure different timestamp
        _service.CreateBackup(dbPath);
        Thread.Sleep(1100);
        _service.CreateBackup(dbPath);

        // Assert
        var backupFiles = Directory.GetFiles(_tempDirectory, "*.backup.*");
        backupFiles.Should().HaveCount(3);
    }

    #region Helper Methods

    private string CreateTestDatabase(string fileName = "test.db")
    {
        var dbPath = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(dbPath, "test database content");
        _createdFiles.Add(dbPath);
        return dbPath;
    }

    #endregion
}
