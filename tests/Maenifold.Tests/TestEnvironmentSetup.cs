using System;
using System.IO;
using Maenifold.Utils;
using NUnit.Framework;

namespace Maenifold.Tests;

[SetUpFixture]
public class TestEnvironmentSetup
{
    private string? _originalRootEnv;
    private string? _originalDatabaseEnv;
    private string _testRootPath = string.Empty;

    [OneTimeSetUp]
    public void Initialize()
    {
        _originalRootEnv = Environment.GetEnvironmentVariable("MAENIFOLD_ROOT");
        _originalDatabaseEnv = Environment.GetEnvironmentVariable("MAENIFOLD_DATABASE_PATH");

        // Use system temp directory for CI compatibility (GitHub Actions, etc.)
        // This ensures the path is always writable regardless of runner environment
        _testRootPath = Path.Combine(Path.GetTempPath(), "maenifold-test-root", Guid.NewGuid().ToString("N"));

        // Ensure we're NOT using production paths
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var prodPath = Path.Combine(userHome, "maenifold");
        if (_testRootPath.StartsWith(prodPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"TEST ISOLATION FAILURE: Test path '{_testRootPath}' would use production directory '{prodPath}'!");
        }

        if (Directory.Exists(_testRootPath))
        {
            try
            {
                // Ensure directory is writable before deletion
                var dirInfo = new DirectoryInfo(_testRootPath);
                dirInfo.Attributes = FileAttributes.Normal;

                // Recursively make all files writable
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    file.Attributes = FileAttributes.Normal;
                }

                Directory.Delete(_testRootPath, recursive: true);
            }
            catch
            {
                // If cleanup fails, try to continue with a different path
                _testRootPath = Path.Combine(Path.GetTempPath(), "maenifold-test-root", Guid.NewGuid().ToString("N"));
            }
        }

        Directory.CreateDirectory(_testRootPath);

        // Explicitly ensure directory is writable
        var testDir = new DirectoryInfo(_testRootPath);
        testDir.Attributes = FileAttributes.Normal;

        Environment.SetEnvironmentVariable("MAENIFOLD_ROOT", _testRootPath);
        var testDatabasePath = Path.Combine(_testRootPath, "memory.db");
        Environment.SetEnvironmentVariable("MAENIFOLD_DATABASE_PATH", testDatabasePath);

        Config.OverrideRoot(_testRootPath);
        Config.SetDatabasePath(testDatabasePath);
        Config.EnsureDirectories();

        // Ensure all created directories are writable
        foreach (var dir in Directory.GetDirectories(_testRootPath, "*", SearchOption.AllDirectories))
        {
            new DirectoryInfo(dir).Attributes = FileAttributes.Normal;
        }

        // Log test isolation verification (goes to test output, not production logs)
        Console.WriteLine($"[TEST ISOLATION] Test root: {_testRootPath}");
        Console.WriteLine($"[TEST ISOLATION] Test database: {testDatabasePath}");
        Console.WriteLine($"[TEST ISOLATION] Production path would be: {prodPath}");
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        Config.ResetOverrides();

        Environment.SetEnvironmentVariable("MAENIFOLD_ROOT", _originalRootEnv);
        Environment.SetEnvironmentVariable("MAENIFOLD_DATABASE_PATH", _originalDatabaseEnv);

        if (!string.IsNullOrEmpty(_testRootPath) && Directory.Exists(_testRootPath))
        {
            try
            {
                Directory.Delete(_testRootPath, recursive: true);
            }
            catch
            {
                // ignore cleanup errors
            }
        }
    }
}
