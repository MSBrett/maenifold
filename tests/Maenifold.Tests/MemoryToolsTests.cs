using System.IO;
using Maenifold.Tools;
using Maenifold.Utils;
using NUnit.Framework;

namespace Maenifold.Tests;

public class MemoryToolsTests
{
    private const string TestFolder = "memory-tools-tests";
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

        MemoryTools.DeleteMemory("memory://regression-test/moved-extension-test", confirm: true);
        MemoryTools.DeleteMemory("memory://simple-move-renamed", confirm: true);
        MemoryTools.DeleteMemory("memory://simple-move-test", confirm: true);
        MemoryTools.DeleteMemory("memory://move-extension-test", confirm: true);

        var regressionTestDir = Path.Combine(Config.MemoryPath, "regression-test");
        if (Directory.Exists(regressionTestDir) && !Directory.EnumerateFileSystemEntries(regressionTestDir).Any())
        {
            Directory.Delete(regressionTestDir);
        }
    }

    [Test]
    public void MoveMemoryWithPathBasedDestinationPreservesMarkdownExtension()
    {
        // Arrange: Create a test memory file
        var testTitle = "Move Extension Test";
        var testContent = "# Move Extension Test\n\nTesting [[move operation]] preserves .md extension.";

        var writeResult = MemoryTools.WriteMemory(testTitle, testContent);
        Assert.That(writeResult, Does.StartWith("Created memory FILE: memory://move-extension-test"));

        // Act: Move with path-based destination (contains slash)
        var moveResult = MemoryTools.MoveMemory("memory://move-extension-test", "regression-test/Moved Extension Test");

        // Assert: Move should succeed (destination gets slugified like WriteMemory)
        Assert.That(moveResult, Does.StartWith("Moved memory FILE: memory://move-extension-test"));
        Assert.That(moveResult, Does.Contain("memory://regression-test/moved-extension-test"));

        // Verify: File should be readable at new location (proving .md extension exists)
        var readResult = MemoryTools.ReadMemory("memory://regression-test/moved-extension-test");
        Assert.That(readResult, Does.Not.StartWith("ERROR:"), "File should be readable, proving .md extension was added");
        Assert.That(readResult, Does.Contain("Testing [[move operation]] preserves .md extension."));
    }

    [Test]
    public void MoveMemoryWithSimpleDestinationPreservesExistingBehavior()
    {
        // Arrange: Create a test memory file
        var testTitle = "Simple Move Test";
        var testContent = "# Simple Move Test\n\nTesting [[simple move]] functionality.";

        var writeResult = MemoryTools.WriteMemory(testTitle, testContent);
        Assert.That(writeResult, Does.StartWith("Created memory FILE: memory://simple-move-test"));

        // Act: Move with simple destination (no slash)
        var moveResult = MemoryTools.MoveMemory("memory://simple-move-test", "Simple Move Renamed");

        // Assert: Move should succeed
        Assert.That(moveResult, Does.StartWith("Moved memory FILE: memory://simple-move-test"));
        Assert.That(moveResult, Does.Contain("memory://simple-move-renamed"));

        // Verify: File should be readable at new location
        var readResult = MemoryTools.ReadMemory("memory://simple-move-renamed");
        Assert.That(readResult, Does.Not.StartWith("ERROR:"));
        Assert.That(readResult, Does.Contain("Testing [[simple move]] functionality."));
    }

    [Test]
    public void ReadMemoryHandlesMissingTitleInFrontmatter()
    {
        // Arrange: Create a memory file manually without a title field in frontmatter
        var testFileName = "no-title-test.md";
        var testFilePath = Path.Combine(_testFolderPath, testFileName);

        // Create file with frontmatter that has no title field
        var fileContent = @"---
type: memory
status: saved
---

# Test Content

This is a [[test file]] without a title in frontmatter.";

        Directory.CreateDirectory(_testFolderPath);
        File.WriteAllText(testFilePath, fileContent);

        // Act: Read the memory file using its URI
        var uri = $"memory://{TestFolder}/{Path.GetFileNameWithoutExtension(testFileName)}";
        var readResult = MemoryTools.ReadMemory(uri);

        // Assert: Should not throw exception and should use filename as title
        Assert.That(readResult, Does.Not.StartWith("ERROR:"));
        Assert.That(readResult, Does.Contain("# no-title-test")); // Filename used as title
        Assert.That(readResult, Does.Contain("URI: memory://memory-tools-tests/no-title-test"));
        Assert.That(readResult, Does.Contain("This is a [[test file]] without a title in frontmatter."));
    }

    [Test]
    public void ReadMemoryHandlesEmptyFrontmatter()
    {
        // Arrange: Create a memory file with no frontmatter at all
        var testFileName = "no-frontmatter-test.md";
        var testFilePath = Path.Combine(_testFolderPath, testFileName);

        var fileContent = @"# Test Without Frontmatter

This is a [[test file]] without any frontmatter.";

        Directory.CreateDirectory(_testFolderPath);
        File.WriteAllText(testFilePath, fileContent);

        // Act: Read the memory file
        var uri = $"memory://{TestFolder}/{Path.GetFileNameWithoutExtension(testFileName)}";
        var readResult = MemoryTools.ReadMemory(uri);

        // Assert: Should not throw exception and should use filename as title
        Assert.That(readResult, Does.Not.StartWith("ERROR:"));
        Assert.That(readResult, Does.Contain("# no-frontmatter-test")); // Filename used as title
        Assert.That(readResult, Does.Contain("This is a [[test file]] without any frontmatter."));
    }

    [Test]
    public void WriteMemoryDoesNotWriteEmbeddingFields()
    {
        // Arrange
        var testTitle = "Embedding Removal Test";
        var testContent = "Content with [[embedding]] concept to ensure write path works.";

        // Act
        var writeResult = MemoryTools.WriteMemory(testTitle, testContent, folder: TestFolder);
        Assert.That(writeResult, Does.StartWith("Created memory FILE:"));

        // Extract URI and resolve to path
        var uriLine = writeResult.Split('\n').FirstOrDefault() ?? string.Empty;
        var uriStart = uriLine.IndexOf("memory://", StringComparison.Ordinal);
        Assert.That(uriStart, Is.GreaterThanOrEqualTo(0), "WriteMemory should return a memory:// URI");
        var uri = uriLine.Substring(uriStart).Trim();
        var path = MarkdownWriter.UriToPath(uri, Config.MemoryPath);

        // Read raw file content
        var raw = File.ReadAllText(path);

        // Assert: frontmatter must not contain any embedding_* keys
        Assert.That(raw, Does.Not.Contain("embedding_base64"));
        Assert.That(raw, Does.Not.Contain("embedding_model"));
        Assert.That(raw, Does.Not.Contain("embedding_date"));
    }

}
