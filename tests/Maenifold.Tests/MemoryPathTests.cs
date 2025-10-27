using System.IO;
using Maenifold.Tools;
using Maenifold.Utils;
using NUnit.Framework;

namespace Maenifold.Tests;

/// <summary>
/// Tests for MEM-009 nested paths functionality including path validation, 
/// directory creation, path traversal protection, and cross-platform compatibility.
/// </summary>
public class MemoryPathTests
{
    private const string TestFolder = "memory-path-tests";
    private string _testFolderPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        Config.EnsureDirectories();
        _testFolderPath = Path.Combine(Config.MemoryPath, TestFolder);
        Directory.CreateDirectory(_testFolderPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (string.IsNullOrEmpty(_testFolderPath))
            return;

        var directory = new DirectoryInfo(_testFolderPath);
        if (directory.Exists)
        {
            directory.Delete(true);
        }
    }

    #region RTM-018: Test nested directory creation works

    [Test]
    public void WriteMemoryCreatesNestedDirectoriesAutomatically()
    {
        // Arrange: Create a deeply nested path
        var title = "Deep Nested Test";
        var content = "Testing [[nested directory]] creation in deep folders.";
        var nestedFolder = $"{TestFolder}/level1/level2/level3";

        // Act: Write memory with nested folder path
        var result = MemoryTools.WriteMemory(title, content, folder: nestedFolder);

        // Assert: Memory should be created successfully
        Assert.That(result, Does.StartWith("Created memory FILE:"));
        Assert.That(result, Does.Contain($"memory://{TestFolder}/level1/level2/level3/deep-nested-test"));

        // Verify: Directory structure was created
        var expectedDir = Path.Combine(_testFolderPath, "level1", "level2", "level3");
        Assert.That(Directory.Exists(expectedDir), Is.True, "Nested directories should be created");

        // Verify: File exists and is readable
        var readResult = MemoryTools.ReadMemory($"memory://{TestFolder}/level1/level2/level3/deep-nested-test");
        Assert.That(readResult, Does.Not.StartWith("ERROR:"));
        Assert.That(readResult, Does.Contain("Testing [[nested directory]] creation in deep folders."));
    }

    [Test]
    public void MoveMemoryCreatesNestedDestinationDirectories()
    {
        // Arrange: Create a source memory file
        var sourceTitle = "Move Source Test";
        var content = "Testing [[move to nested]] directory functionality.";

        var writeResult = MemoryTools.WriteMemory(sourceTitle, content, folder: TestFolder);
        Assert.That(writeResult, Does.StartWith("Created memory FILE:"));

        // Act: Move to nested destination that doesn't exist yet
        var nestedDestination = $"{TestFolder}/categories/subcategory/moved-file";
        var moveResult = MemoryTools.MoveMemory($"memory://{TestFolder}/move-source-test", nestedDestination);

        // Assert: Move should succeed
        Assert.That(moveResult, Does.StartWith("Moved memory FILE:"));
        Assert.That(moveResult, Does.Contain($"memory://{nestedDestination}"));

        // Verify: Nested directory structure was created
        var expectedDir = Path.Combine(_testFolderPath, "categories", "subcategory");
        Assert.That(Directory.Exists(expectedDir), Is.True, "Nested destination directories should be created");

        // Verify: File is accessible at new location
        var readResult = MemoryTools.ReadMemory($"memory://{nestedDestination}");
        Assert.That(readResult, Does.Not.StartWith("ERROR:"));
        Assert.That(readResult, Does.Contain("Testing [[move to nested]] directory functionality."));
    }

    [Test]
    public void ExtractConceptsFromFileWorksWithNestedPaths()
    {
        // Arrange: Create a file with concepts in a nested directory
        var title = "Concept Test";
        var content = "This contains [[machine learning]] and [[data science]] concepts for testing.";
        var nestedFolder = $"{TestFolder}/concepts/nested";

        var writeResult = MemoryTools.WriteMemory(title, content, folder: nestedFolder);
        Assert.That(writeResult, Does.StartWith("Created memory FILE:"));

        // Act: Extract concepts from nested file
        var extractResult = MemoryTools.ExtractConceptsFromFile($"memory://{TestFolder}/concepts/nested/concept-test");

        // Assert: Concepts should be extracted successfully
        Assert.That(extractResult, Does.Not.StartWith("ERROR:"));
        Assert.That(extractResult, Does.Contain("machine-learning"));
        Assert.That(extractResult, Does.Contain("data-science"));
    }

    #endregion

    #region RTM-019: Test path traversal ".." is rejected

    [Test]
    public void WriteMemoryRejectsPathTraversalAttempts()
    {
        // Arrange: Attempt various path traversal patterns in folder parameter
        var maliciousFolders = new[]
        {
            "../outside-memory",
            "../../etc",
            "folder/../../../escape",
            "valid/../../invalid",
            $"{TestFolder}/../escape-test-folder"
        };

        foreach (var maliciousFolder in maliciousFolders)
        {
            // Act & Assert: Each attempt should be rejected
            var result = MemoryTools.WriteMemory("Test Title", "Content with [[required concept]] for testing.", folder: maliciousFolder);

            // Should get an error from folder validation
            Assert.That(result, Does.StartWith("ERROR:"),
                $"Path traversal should be rejected for folder: {maliciousFolder}");
        }
    }

    [Test]
    public void ReadMemoryRejectsPathTraversalAttempts()
    {
        // Arrange: Attempt to read files outside memory directory
        var maliciousUris = new[]
        {
            "memory://../outside-memory",
            "memory://../../etc/hosts",
            "memory://folder/../../../system-file"
        };

        foreach (var maliciousUri in maliciousUris)
        {
            // Act & Assert: Each read attempt should be rejected with exception or error
            try
            {
                var result = MemoryTools.ReadMemory(maliciousUri);
                // If no exception, should still be an error
                Assert.That(result, Does.StartWith("ERROR:"),
                    $"Path traversal read should be rejected for URI: {maliciousUri}");
            }
            catch (ArgumentException)
            {
                // Expected - path traversal protection threw exception
                Assert.Pass($"Path traversal correctly rejected with exception for URI: {maliciousUri}");
            }
        }
    }

    [Test]
    public void MoveMemoryRejectsPathTraversalInDestination()
    {
        // Arrange: Create a valid source file
        var sourceTitle = "Valid Source";
        var content = "Content to move with [[required concept]] for validation.";
        var writeResult = MemoryTools.WriteMemory(sourceTitle, content, folder: TestFolder);
        Assert.That(writeResult, Does.StartWith("Created memory FILE:"));
        var sourcePath = $"memory://{TestFolder}/valid-source";

        // Act: Attempt to move to paths with traversal
        var maliciousDestinations = new[]
        {
            "../outside-memory",
            "../../escape-folder/file",
            $"{TestFolder}/../escape-test-folder/file"
        };

        foreach (var maliciousDestination in maliciousDestinations)
        {
            // Act & Assert: Move should be rejected
            var moveResult = MemoryTools.MoveMemory(sourcePath, maliciousDestination);

            Assert.That(moveResult, Does.StartWith("ERROR:").Or.Contains("Invalid").Or.Contains("escape"),
                $"Path traversal move should be rejected for destination: {maliciousDestination}");
        }
    }

    [Test]
    public void UriToPathDirectlyRejectsPathTraversal()
    {
        // Arrange: Test the underlying URI conversion method directly
        var maliciousUris = new[]
        {
            "memory://../outside",
            "memory://../../system",
            "memory://folder/../../../escape"
        };

        foreach (var maliciousUri in maliciousUris)
        {
            // Act & Assert: Direct URI conversion should throw
            Assert.Throws<ArgumentException>(() => MarkdownIO.UriToPath(maliciousUri, Config.MemoryPath),
                $"UriToPath should reject path traversal for: {maliciousUri}");
        }
    }

    #endregion

    #region RTM-020: Test cross-platform path handling

    [Test]
    public void PathHandlingWorksWithUnixStyleSeparators()
    {
        // Arrange: Use Unix-style path separators
        var title = "Unix Style Path";
        var content = "Testing [[unix paths]] with forward slashes in nested directories.";
        var unixStyleFolder = $"{TestFolder}/unix/style";

        // Act: Write memory using nested folder structure
        var writeResult = MemoryTools.WriteMemory(title, content, folder: unixStyleFolder);

        // Assert: Should work regardless of platform
        Assert.That(writeResult, Does.StartWith("Created memory FILE:"));
        Assert.That(writeResult, Does.Contain($"memory://{TestFolder}/unix/style/unix-style-path"));

        // Verify: File should be readable
        var readResult = MemoryTools.ReadMemory($"memory://{TestFolder}/unix/style/unix-style-path");
        Assert.That(readResult, Does.Not.StartWith("ERROR:"));
        Assert.That(readResult, Does.Contain("Testing [[unix paths]] with forward slashes in nested directories."));

        // Verify: Physical directory structure exists (with platform-appropriate separators)
        var expectedDir = Path.Combine(_testFolderPath, "unix", "style");
        Assert.That(Directory.Exists(expectedDir), Is.True, "Directory should exist with correct platform separators");
    }

    [Test]
    public void PathToUriProducesConsistentUriFormat()
    {
        // Arrange: Create files in nested directories
        var testCases = new[]
        {
            Path.Combine(_testFolderPath, "simple.md"),
            Path.Combine(_testFolderPath, "nested", "file.md"),
            Path.Combine(_testFolderPath, "deep", "nesting", "levels", "file.md")
        };

        foreach (var testPath in testCases)
        {
            // Ensure directory exists
            var dir = Path.GetDirectoryName(testPath);
            if (dir != null) Directory.CreateDirectory(dir);

            // Create test file
            File.WriteAllText(testPath, "# Test File\n\nContent");

            // Act: Convert to URI
            var uri = MarkdownIO.PathToUri(testPath, Config.MemoryPath);

            // Assert: URI should always use forward slashes regardless of platform
            Assert.That(uri, Does.StartWith("memory://"));
            Assert.That(uri, Does.Not.Contain("\\"), "URI should not contain backslashes");
            Assert.That(uri, Does.Not.EndWith(".md"), "URI should not include .md extension");

            // Verify: URI can be converted back to valid path
            var convertedPath = MarkdownIO.UriToPath(uri, Config.MemoryPath);
            Assert.That(File.Exists(convertedPath), Is.True, $"Converted path should exist: {convertedPath}");
        }
    }

    [Test]
    public void UriToPathHandlesPlatformSpecificSeparators()
    {
        // Arrange: Test URIs that should work on all platforms
        var testUris = new[]
        {
            $"memory://{TestFolder}/simple-file",
            $"memory://{TestFolder}/nested/subdirectory/file",
            $"memory://{TestFolder}/deep/multi/level/nesting/file"
        };

        foreach (var uri in testUris)
        {
            // Act: Convert URI to platform path
            var path = MarkdownIO.UriToPath(uri, Config.MemoryPath);

            // Assert: Path should use platform-appropriate separators
            var expectedSeparator = Path.DirectorySeparatorChar;
            if (uri.Contains('/') && expectedSeparator == '\\')
            {
                // On Windows, converted path should use backslashes for nested paths
                Assert.That(path.Contains('\\') || !uri.Contains('/'), Is.True,
                    "Windows paths should use backslashes for nested directories");
            }

            // Verify: Path should be within memory directory
            var fullPath = Path.GetFullPath(path);
            var baseFullPath = Path.GetFullPath(Config.MemoryPath);
            Assert.That(fullPath, Does.StartWith(baseFullPath),
                "Converted path should be within memory directory");

            // Verify: Path should end with .md extension
            Assert.That(path, Does.EndWith(".md"), "Converted path should have .md extension");
        }
    }

    [Test]
    public void FileOperationsWorkWithMixedPathStyles()
    {
        // Arrange: Create a file using platform paths
        var platformPath = Path.Combine(_testFolderPath, "mixed", "platform", "file.md");
        var dir = Path.GetDirectoryName(platformPath);
        if (dir != null) Directory.CreateDirectory(dir);

        var content = "# Mixed Path Test\n\nTesting [[cross platform]] compatibility.";
        File.WriteAllText(platformPath, content);

        // Act: Convert to URI and back
        var uri = MarkdownIO.PathToUri(platformPath, Config.MemoryPath);
        var convertedPath = MarkdownIO.UriToPath(uri, Config.MemoryPath);

        // Assert: Paths should be equivalent
        var originalFull = Path.GetFullPath(platformPath);
        var convertedFull = Path.GetFullPath(convertedPath);
        Assert.That(convertedFull, Is.EqualTo(originalFull),
            "Round-trip conversion should preserve path identity");

        // Verify: Memory tools should work with the URI
        var readResult = MemoryTools.ReadMemory(uri);
        Assert.That(readResult, Does.Not.StartWith("ERROR:"));
        Assert.That(readResult, Does.Contain("Testing [[cross platform]] compatibility."));
    }

    #endregion

    #region Additional Edge Cases

    [Test]
    public void NestedPathsHandleSpecialCharacters()
    {
        // Arrange: Test paths with various special characters (that are valid in file names)
        var testCases = new[]
        {
            (title: "File With Hyphens", folder: $"{TestFolder}/special-chars", uri: "file-with-hyphens"),
            (title: "File With Underscores", folder: $"{TestFolder}/special_chars", uri: "file-with-underscores"),
            (title: "File With Numbers456", folder: $"{TestFolder}/numbers123", uri: "file-with-numbers456"),
            (title: "Final File", folder: $"{TestFolder}/mixed-chars_123", uri: "final-file")
        };

        foreach (var testCase in testCases)
        {
            // Act: Write and read memory with special characters
            var content = $"Testing [[special characters]] in path: {testCase.folder}";
            var writeResult = MemoryTools.WriteMemory(testCase.title, content, folder: testCase.folder);

            // Assert: Should work without errors
            Assert.That(writeResult, Does.StartWith("Created memory FILE:"),
                $"Special character path should work: {testCase.folder}");

            var readResult = MemoryTools.ReadMemory($"memory://{testCase.folder}/{testCase.uri}");
            Assert.That(readResult, Does.Not.StartWith("ERROR:"),
                $"Reading special character path should work: {testCase.folder}");
            Assert.That(readResult, Does.Contain($"Testing [[special characters]] in path: {testCase.folder}"));
        }
    }

    [Test]
    public void DeepNestingLimitsAreRespected()
    {
        // Arrange: Create a very deeply nested folder path
        var deepFolder = TestFolder;
        for (int i = 1; i <= 10; i++)
        {
            deepFolder += $"/level{i}";
        }

        var title = "Deep File";
        var content = "Testing [[very deep]] nesting in directory structures.";

        // Act: Write memory with deep nesting
        var writeResult = MemoryTools.WriteMemory(title, content, folder: deepFolder);

        // Assert: Should work (filesystem should handle reasonable nesting)
        Assert.That(writeResult, Does.StartWith("Created memory FILE:"));

        // Verify: File should be readable
        var expectedUri = $"memory://{deepFolder}/deep-file";
        var readResult = MemoryTools.ReadMemory(expectedUri);
        Assert.That(readResult, Does.Not.StartWith("ERROR:"));
        Assert.That(readResult, Does.Contain("Testing [[very deep]] nesting in directory structures."));
    }

    [Test]
    public void EmptyDirectoryNamesAreHandledGracefully()
    {
        // Arrange: Test folder paths with empty directory segments
        var problematicFolders = new[]
        {
            $"{TestFolder}//double-slash",
            $"{TestFolder}/normal//double-middle",
            $"{TestFolder}/trailing-slash//"
        };

        foreach (var problematicFolder in problematicFolders)
        {
            // Act: Attempt to write memory
            var content = "Testing [[empty segments]] in folder paths for validation.";
            var writeResult = MemoryTools.WriteMemory("Empty Directory Test", content, folder: problematicFolder);

            // Assert: Should either work or fail gracefully with clear error
            // (The exact behavior depends on how the system handles empty segments)
            Assert.That(writeResult, Does.Not.Contain("Exception"),
                $"Empty directory segments should be handled gracefully: {problematicFolder}");
        }
    }

    #endregion
}
