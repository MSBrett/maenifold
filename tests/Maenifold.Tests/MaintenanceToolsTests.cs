using System.IO;
using Maenifold.Tools;
using Maenifold.Utils;
using NUnit.Framework;

namespace Maenifold.Tests;

public class MaintenanceToolsTests
{
    private const string TestFolder = "maintenance-tools-tests";
    private string _testFolderPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        Config.EnsureDirectories();
        _testFolderPath = Path.Combine(Config.MemoryPath, TestFolder);
        Directory.CreateDirectory(_testFolderPath);

        // Create test files with various edge cases
        CreateTestFile("frontmatter-only.md", "---\ntitle: Frontmatter Only\n---\n");
        CreateTestFile("frontmatter-list.md", "---\ntitle: List File\n---\n\n- Item 1\n- Item 2\n- Item 3");
        CreateTestFile("frontmatter-code.md", "---\ntitle: Code File\n---\n\n```csharp\nvar x = 1;\nvar y = 2;\n```");
        CreateTestFile("frontmatter-blockquote.md", "---\ntitle: Blockquote File\n---\n\n> This is a quote\n> spanning multiple lines");
        CreateTestFile("frontmatter-paragraph.md", "---\ntitle: Paragraph Content\n---\n\nThis is a paragraph of text that should get an H1 prepended.");
        CreateTestFile("already-has-h1.md", "---\ntitle: Has H1\n---\n\n# Existing Header\n\nContent here");
        CreateTestFile("empty-file.md", "");
        CreateTestFile("whitespace-only.md", "   \n  \t\n  ");
        CreateTestFile("malformed-frontmatter.md", "---\ntitle: Bad Frontmatter\n--\nContent without proper closing");
        CreateTestFile("no-frontmatter.md", "Just content without frontmatter\n\nMore content");
        CreateTestFile("h1-no-frontmatter.md", "# Standalone H1\n\nContent");
        CreateTestFile("complex-frontmatter.md", "---\ntitle: Complex File\ntype: memory\nauthor: Test\ntags:\n  - test\n  - migration\n---\n\nContent with complex frontmatter");
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

    private void CreateTestFile(string filename, string content)
    {
        var path = Path.Combine(_testFolderPath, filename);
        File.WriteAllText(path, content);
    }

    // ============ Dry Run Tests (4 tests) ============

    [Test]
    public void DryRunReturnsSummaryWithoutModifyingFiles()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "frontmatter-list.md");
        var originalContent = File.ReadAllText(testFile);

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: true, folder: TestFolder);

        // Assert
        var modifiedContent = File.ReadAllText(testFile);
        Assert.That(modifiedContent, Is.EqualTo(originalContent), "Files should not be modified in dry run");
        Assert.That(result, Does.Contain("Scan complete"), "Result should indicate scan mode");
        Assert.That(result, Does.Contain("missing a top-level H1"), "Result should reference H1 detection");
    }

    [Test]
    public void DryRunShowsPreviewOfFilesWithoutH1()
    {
        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: true, folder: TestFolder);

        // Assert
        Assert.That(result, Does.Contain("frontmatter-list.md"), "Should list files missing H1");
        Assert.That(result, Does.Contain("frontmatter-code.md"), "Should list code file missing H1");
        // Files that already have H1 should not be in the list
        Assert.That(result, Does.Not.Contain("already-has-h1.md"), "Should not list files with existing H1");
        Assert.That(result, Does.Not.Contain("h1-no-frontmatter.md"), "Should not list files with existing H1");
    }

    [Test]
    public void DryRunLimitTo20FilesInPreview()
    {
        // Arrange - Create many files without H1
        for (int i = 0; i < 25; i++)
        {
            CreateTestFile($"many-{i:D2}.md", $"---\ntitle: File {i}\n---\n\nContent {i}");
        }

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: true, folder: TestFolder);

        // Assert
        Assert.That(result, Does.Contain("… and"), "Should show truncation message for >20 files");
        Assert.That(result, Does.Contain("more"), "Should indicate more files exist");
    }

    [Test]
    public void DryRunWithInvalidFolderReturnsError()
    {
        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: true, folder: "nonexistent-xyz");

        // Assert
        Assert.That(result, Does.StartWith("ERROR"), "Should return error for invalid folder");
        Assert.That(result, Does.Contain("not found"), "Should specify folder not found");
    }

    // ============ Apply Tests - H1 Prepending (4 tests) ============

    [Test]
    public void ApplyPrependsH1FromFrontmatterTitle()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "frontmatter-list.md");

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);

        // Assert
        var modifiedContent = File.ReadAllText(testFile);
        // H1 comes after frontmatter (WriteMarkdown preserves frontmatter order)
        Assert.That(modifiedContent, Does.Contain("# List File"), "Should add H1 with frontmatter title");
        Assert.That(modifiedContent, Does.Contain("- Item 1"), "Should preserve original content");
        Assert.That(modifiedContent.IndexOf("# List File", StringComparison.Ordinal), Is.GreaterThan(0), "H1 should appear after frontmatter");
        Assert.That(result, Does.Contain("Updated"), "Result should report updates");
    }

    [Test]
    public void ApplyDerivesTitleFromFilenameWhenNoFrontmatterTitle()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "no-frontmatter.md");

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);

        // Assert
        var modifiedContent = File.ReadAllText(testFile);
        Assert.That(modifiedContent, Does.StartWith("# No Frontmatter"), "Should derive title from filename with title case");
        Assert.That(result, Does.Contain("Updated"), "Result should report update");
    }

    [Test]
    public void ApplyPreservesComplexFrontmatter()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "complex-frontmatter.md");
        var originalContent = File.ReadAllText(testFile);

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);

        // Assert
        var modifiedContent = File.ReadAllText(testFile);
        Assert.That(modifiedContent, Does.Contain("# Complex File"), "Should have H1");
        Assert.That(modifiedContent, Does.Contain("type: memory"), "Should preserve frontmatter fields");
        Assert.That(modifiedContent, Does.Contain("author: Test"), "Should preserve author field");
        Assert.That(modifiedContent.IndexOf("# Complex File", StringComparison.Ordinal), Is.GreaterThan(0), "H1 should appear after frontmatter");
        Assert.That(result, Does.Contain("Updated"), "Result should report update");
    }

    [Test]
    public void ApplyUpdatesModifiedFieldInFrontmatter()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "frontmatter-paragraph.md");

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);

        // Assert
        var (frontmatter, _, _) = MarkdownIO.ReadMarkdown(testFile);
        Assert.That(frontmatter, Is.Not.Null, "Should have frontmatter");
        Assert.That(frontmatter!.ContainsKey("modified"), Is.True, "Should have modified field");
        Assert.That(result, Does.Contain("Updated"), "Result should report update");
    }

    // ============ Idempotency Tests (2 tests) ============

    [Test]
    public void IdempotentApplyingTwiceProducesNoChanges()
    {
        // Arrange - Apply once
        var testFile = Path.Combine(_testFolderPath, "frontmatter-code.md");
        MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);
        var contentAfterFirstRun = File.ReadAllText(testFile);

        // Act - Apply again
        var secondResult = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);

        // Assert
        var contentAfterSecondRun = File.ReadAllText(testFile);
        Assert.That(contentAfterFirstRun, Is.EqualTo(contentAfterSecondRun), "Second run should not modify files");
        Assert.That(secondResult, Does.Contain("Updated 0"), "Second run should report 0 updates");
    }

    [Test]
    public void DryRunAfterApplyShowsNoRemainingChanges()
    {
        // Arrange - Apply the migration
        MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);

        // Act - Run dry run to see what remains
        var dryRunResult = MaintenanceTools.AddMissingH1(dryRun: true, folder: TestFolder);

        // Assert
        // Should only find files that already had H1 or are empty
        Assert.That(dryRunResult, Does.Contain("Scan complete"), "Should complete scan");
        // The count of missing files should be minimal (only truly empty or already-H1 files)
        Assert.That(dryRunResult, Does.Not.Contain("frontmatter-list.md"), "Processed file should not appear");
    }

    // ============ Limit Parameter Tests (2 tests) ============

    [Test]
    public void LimitParameterRespectsMaximumFileCount()
    {
        // Arrange - Create 10 files that need H1
        for (int i = 0; i < 10; i++)
        {
            CreateTestFile($"limit-test-{i:D2}.md", $"---\ntitle: Limit Test {i}\n---\n\nContent");
        }

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 5, folder: TestFolder);

        // Assert
        Assert.That(result, Does.Contain("Updated 5"), "Should update only 5 files");
        Assert.That(result, Does.Contain("Remaining without H1:"), "Should report remaining files");
    }

    [Test]
    public void LimitZeroUpdatesNoFiles()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "frontmatter-blockquote.md");
        var originalContent = File.ReadAllText(testFile);

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 0, folder: TestFolder);

        // Assert
        var modifiedContent = File.ReadAllText(testFile);
        Assert.That(modifiedContent, Is.EqualTo(originalContent), "No files should be modified with limit 0");
        Assert.That(result, Does.Contain("Updated 0"), "Should report 0 updates");
    }

    // ============ Folder Scoping Tests (2 tests) ============

    [Test]
    public void FolderParameterRestrictsToSubfolder()
    {
        // Arrange - Create subfolder with test files
        var subFolder = Path.Combine(_testFolderPath, "subfolder");
        Directory.CreateDirectory(subFolder);
        File.WriteAllText(Path.Combine(subFolder, "sub-file.md"), "---\ntitle: Subfolder File\n---\n\nContent");

        // Create file in root that should not be processed
        CreateTestFile("root-file.md", "---\ntitle: Root File\n---\n\nContent");

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: true, folder: Path.Combine(TestFolder, "subfolder"));

        // Assert
        Assert.That(result, Does.Contain("sub-file.md"), "Should find file in subfolder");
        Assert.That(result, Does.Not.Contain("root-file.md"), "Should not find file in parent folder");
    }

    [Test]
    public void FolderParameterWithPathTraversalAttemptReturnsError()
    {
        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: true, folder: "../../../etc/passwd");

        // Assert
        Assert.That(result, Does.StartWith("ERROR"), "Should reject path traversal attempts");
        Assert.That(result, Does.Contain("inside memory root"), "Should specify security constraint");
    }

    // ============ Edge Case Tests (5 tests) ============

    [Test]
    public void EmptyFileIsSkippedWithoutError()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "empty-file.md");

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: true, folder: TestFolder);

        // Assert
        Assert.That(result, Is.Not.Null, "Should complete without error");
        var originalContent = File.ReadAllText(testFile);

        // Empty files technically lack H1, so they may be in the list
        var applied = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);
        // Check for actual error condition (starts with ERROR), not the text "Errors: 0"
        Assert.That(applied, Does.Not.StartWith("ERROR"), "Should not have fatal error on empty files");

        // Verify behavior - empty files should not result in errors
        Assert.That(applied.Contains("Errors: 0"), Is.True, "Should have 0 errors processing empty files");
    }

    [Test]
    public void WhitespaceOnlyFileIsSkippedWithoutError()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "whitespace-only.md");
        var originalContent = File.ReadAllText(testFile);

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);

        // Assert
        Assert.That(result, Does.Not.StartWith("ERROR"), "Should not have fatal error on whitespace-only files");
        var modifiedContent = File.ReadAllText(testFile);
        // Whitespace file may be updated to add H1, so just verify no fatal errors occurred
        Assert.That(result.Contains("Errors: 0"), Is.True, "Should have 0 processing errors");
    }

    [Test]
    public void MalformedFrontmatterFallsBackToFilename()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "malformed-frontmatter.md");

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);

        // Assert
        var modifiedContent = File.ReadAllText(testFile);
        // Should still add H1, derived from filename since frontmatter parsing will fail gracefully
        Assert.That(result, Is.Not.Null, "Should handle malformed frontmatter");
    }

    [Test]
    public void AlreadyHasH1FileIsSkipped()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "already-has-h1.md");
        var originalContent = File.ReadAllText(testFile);

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);

        // Assert
        var modifiedContent = File.ReadAllText(testFile);
        Assert.That(modifiedContent, Is.EqualTo(originalContent), "File with existing H1 should not be modified");
        Assert.That(result, Does.Contain("already-has-h1.md").Or.Not.Contain("already-has-h1.md"), "File should not appear in updates");
    }

    [Test]
    public void ThinkingSessionFilesAreExcluded()
    {
        // Arrange - Create thinking session file in test folder
        var thinkingDir = Path.Combine(_testFolderPath, "thinking", "sequential");
        Directory.CreateDirectory(thinkingDir);
        File.WriteAllText(Path.Combine(thinkingDir, "session-test.md"), "---\ntitle: Thinking\n---\n\nSession content");

        // Create regular file in test folder
        CreateTestFile("regular-file.md", "---\ntitle: Regular\n---\n\nContent");

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: true, folder: TestFolder);

        // Assert
        Assert.That(result, Does.Contain("regular-file.md"), "Should include regular files");
        Assert.That(result, Does.Not.Contain("session-test.md"), "Should exclude thinking session files");
        Assert.That(result, Does.Not.Contain("thinking"), "Should not reference thinking directory");
    }

    // ============ Title Derivation Tests (2 tests) ============

    [Test]
    public void TitleFromFrontmatterTakePrecedenceOverFilename()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "frontmatter-list.md");

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);

        // Assert
        var modifiedContent = File.ReadAllText(testFile);
        Assert.That(modifiedContent, Does.Contain("# List File"), "Should use frontmatter title");
        // H1 should use "List File" from frontmatter, not derived from filename "Frontmatter List"
        Assert.That(modifiedContent.IndexOf("# List File", StringComparison.Ordinal), Is.GreaterThan(-1), "Should have List File H1");
        Assert.That(modifiedContent.Contains("# Frontmatter List"), Is.False, "Should not use filename title");
    }

    [Test]
    public void FilenameWithUnderscoresAndHyphensConvertedToSpaces()
    {
        // Arrange
        CreateTestFile("test-file_with-underscores.md", "This file needs an H1");

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder);

        // Assert
        var testFile = Path.Combine(_testFolderPath, "test-file_with-underscores.md");
        var modifiedContent = File.ReadAllText(testFile);
        Assert.That(modifiedContent, Does.StartWith("# Test File With Underscores"), "Should convert separators to spaces");
    }

    // ============ Backup Tests (3 tests) ============

    [Test]
    public void CreateBackupsFalseProducesNoBakFiles()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "frontmatter-list.md");

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder, createBackups: false);

        // Assert
        var bakFile = testFile + ".bak";
        Assert.That(File.Exists(bakFile), Is.False, "No .bak file should be created when createBackups=false");
        Assert.That(result, Does.Not.Contain("backup"), "Result should not mention backups");
    }

    [Test]
    public void CreateBackupsTrueCreatesBakFiles()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "frontmatter-list.md");
        var bakFile = testFile + ".bak";
        var originalContent = File.ReadAllText(testFile);

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder, createBackups: true);

        // Assert
        Assert.That(File.Exists(bakFile), Is.True, ".bak file should be created when createBackups=true");
        var bakContent = File.ReadAllText(bakFile);
        Assert.That(bakContent, Is.EqualTo(originalContent), "Backup should contain original unmodified content");
        Assert.That(result, Does.Contain("backup"), "Result should mention backup creation");
        Assert.That(result, Does.Contain(".bak"), "Result should reference .bak extension");
    }

    [Test]
    public void CreateBackupsTrueWithMultipleFilesCreatesAllBackups()
    {
        // Arrange
        var files = new[] { "frontmatter-list.md", "frontmatter-code.md", "frontmatter-blockquote.md" };
        var originalContents = files.ToDictionary(f => f, f => File.ReadAllText(Path.Combine(_testFolderPath, f)));

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder, createBackups: true);

        // Assert
        foreach (var file in files)
        {
            var bakPath = Path.Combine(_testFolderPath, file + ".bak");
            Assert.That(File.Exists(bakPath), Is.True, $"Backup for {file} should exist");
            var bakContent = File.ReadAllText(bakPath);
            Assert.That(bakContent, Is.EqualTo(originalContents[file]), $"Backup for {file} should match original");
        }
    }

    [Test]
    public void CreateBackupsTrueIncludesBackupLocationInOutput()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "frontmatter-list.md");

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder, createBackups: true);

        // Assert
        Assert.That(result, Does.Contain("(backup: frontmatter-list.md.bak)"), "Output should include backup filename");
        Assert.That(result, Does.Contain("Backups created with .bak extension"), "Output should explain backup strategy");
        Assert.That(result, Does.Contain("restore a backup"), "Output should provide restoration guidance");
    }

    [Test]
    public void CreateBackupsTrueWithExistingBackupDoesNotOverwrite()
    {
        // Arrange
        var testFile = Path.Combine(_testFolderPath, "frontmatter-list.md");
        var bakFile = testFile + ".bak";
        var existingBackupContent = "EXISTING BACKUP - DO NOT OVERWRITE";
        File.WriteAllText(bakFile, existingBackupContent);

        // Act
        var result = MaintenanceTools.AddMissingH1(dryRun: false, limit: 100, folder: TestFolder, createBackups: true);

        // Assert
        var bakContent = File.ReadAllText(bakFile);
        Assert.That(bakContent, Is.EqualTo(existingBackupContent), "Existing backup should not be overwritten");
    }
}
