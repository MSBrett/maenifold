#pragma warning disable CA1861
using System.Collections.Generic;
using System.IO;
using Maenifold.Tools;
using Maenifold.Utils;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using System.Text.Json;

namespace Maenifold.Tests;

[NonParallelizable] // Database operations need sequential execution in CI
public class VectorSearchTests
{
    private const string TestFolder = "vector-search-tests";
    private string _testFolderPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _testFolderPath = Path.Combine(Config.MemoryPath, TestFolder);

        Config.EnsureDirectories();
        GraphDatabase.InitializeDatabase(); // Ensure database and vector tables exist
        Directory.CreateDirectory(_testFolderPath);
        Directory.CreateDirectory(Path.Combine(_testFolderPath, "ai"));
        Directory.CreateDirectory(Path.Combine(_testFolderPath, "tutorials"));
        Directory.CreateDirectory(Path.Combine(_testFolderPath, "projects"));

        // NO database isolation - use shared database which already has vector tables
        // Vector extension requires special loading that doesn't work in isolated test databases
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up test folder
        var directory = new DirectoryInfo(_testFolderPath);
        if (directory.Exists)
        {
            foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.IsReadOnly = false;
                file.Delete();
            }
            foreach (var sub in directory.EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                sub.Delete(true);
            }
            directory.Delete(true);
        }
    }

    [Test]
    [Ignore("Disabled due to SQLite permission errors in CI - needs investigation")]
    public void FindSimilarConceptsReturnsExpectedResults()
    {
        var content = "This note discusses [[Machine Learning]] and [[Artificial Intelligence]] techniques.";
        var title = "AI Concepts";

        var writeResult = MemoryTools.WriteMemory(title, content, folder: TestFolder);
        TestContext.Out.WriteLine(writeResult);

        var syncResult = GraphTools.Sync();
        TestContext.Out.WriteLine(syncResult);

        var result = VectorSearchTools.FindSimilarConcepts("machine learning");
        TestContext.Out.WriteLine(result);

        Assert.That(result, Contains.Substring("Similar concepts"));
        Assert.That(result, Contains.Substring("machine-learning"));
    }

    [Test]
    [Ignore("Disabled due to SQLite permission errors in CI - needs investigation")]
    public void SyncPopulatesVectorTablesWithBlobs()
    {
        var conceptName = "VectorBlobConcept";
        var fileName = Path.Combine(_testFolderPath, "vector-blob.md");
        var frontmatter = new Dictionary<string, object>
        {
            ["title"] = "Vector Blob Note",
            ["tags"] = new[] { "vector", "test" },
            ["status"] = "saved"
        };
        var content = $"Exploring [[{conceptName}]] storage for semantic search.";

        MarkdownIO.WriteMarkdown(fileName, frontmatter, content);
        var fileUri = MarkdownIO.PathToUri(fileName, Config.MemoryPath);

        var syncResult = GraphTools.Sync();
        TestContext.Out.WriteLine(syncResult);

        using var connection = new SqliteConnection($"Data Source={Config.DatabasePath}");
        connection.Open();
        connection.LoadVectorExtension();

        var expectedBytes = VectorTools.EmbeddingLength * sizeof(float);
        const double SelfDistanceTolerance = 1e-5;

        using (var conceptCommand = connection.CreateCommand())
        {
            conceptCommand.CommandText = @"
                SELECT typeof(embedding), length(embedding), vec_distance_cosine(embedding, embedding)
                FROM vec_concepts
                WHERE concept_name = @concept
                LIMIT 1";
            conceptCommand.Parameters.AddWithValue("@concept", MarkdownIO.NormalizeConcept(conceptName));

            using var reader = conceptCommand.ExecuteReader();
            Assert.That(reader.Read(), Is.True, "Concept embedding row should exist");
            Assert.That(reader.GetString(0), Is.EqualTo("blob"), "Concept embedding should be stored as BLOB");
            Assert.That(reader.GetInt32(1), Is.EqualTo(expectedBytes), "Concept embedding should have expected byte length");
            var selfDistance = reader.IsDBNull(2) ? double.NaN : reader.GetDouble(2);
            Assert.That(selfDistance, Is.LessThan(SelfDistanceTolerance),
                "vec_distance_cosine(embedding, embedding) should evaluate close to zero for valid vectors");
        }

        using (var fileCommand = connection.CreateCommand())
        {
            fileCommand.CommandText = @"
                SELECT typeof(embedding), length(embedding), vec_distance_cosine(embedding, embedding)
                FROM vec_memory_files
                WHERE file_path = @path
                LIMIT 1";
            fileCommand.Parameters.AddWithValue("@path", fileUri);

            using var reader = fileCommand.ExecuteReader();
            Assert.That(reader.Read(), Is.True, "File embedding row should exist");
            Assert.That(reader.GetString(0), Is.EqualTo("blob"), "File embedding should be stored as BLOB");
            Assert.That(reader.GetInt32(1), Is.EqualTo(expectedBytes), "File embedding should have expected byte length");
            var selfDistance = reader.IsDBNull(2) ? double.NaN : reader.GetDouble(2);
            Assert.That(selfDistance, Is.LessThan(SelfDistanceTolerance),
                "vec_distance_cosine(embedding, embedding) should evaluate close to zero for valid vectors");
        }
    }

    [Test]
    [Ignore("Disabled due to SQLite permission errors in CI - needs investigation")]
    public void SemanticSearchRespectsTagFilters()
    {
        // Create test files with tags
        var pythonTags = new[] { "programming", "python", "tutorial" };
        CreateTestFileWithTags("python-tutorial.md",
            "Python Programming Tutorial",
            "Learn [[Python]] programming with practical examples and [[data structures]]. This comprehensive guide covers Python fundamentals, advanced concepts, and best practices for writing clean, maintainable code.",
            pythonTags);

        var javaTags = new[] { "programming", "java", "guide" };
        CreateTestFileWithTags("java-guide.md",
            "Java Development Guide",
            "Master [[Java]] programming and [[object-oriented]] design patterns. Learn enterprise Java development, Spring Framework, and modern Java features.",
            javaTags);

        var cookingTags = new[] { "cooking", "tutorial" };
        CreateTestFileWithTags("cooking-basics.md",
            "Cooking Basics",
            "Learn basic [[cooking]] techniques and [[recipes]] for beginners. Master essential kitchen skills and create delicious meals with confidence.",
            cookingTags);

        // Sync to ensure database is populated
        GraphTools.Sync();

        // Test 1: Search with tag filter for "python"
        var pythonResult = MemorySearchTools.SearchMemories(
            query: "programming language tutorial",
            mode: SearchMode.Semantic,
            pageSize: 10,
            page: 1,
            folder: TestFolder,
            tags: new string[] { "python" });

        Assert.That(pythonResult, Contains.Substring("Python Programming Tutorial"),
            "Should find Python tutorial when filtering by python tag");
        Assert.That(pythonResult, Does.Not.Contain("Java Development Guide"),
            "Should not include Java guide when filtering by python tag");
        Assert.That(pythonResult, Does.Not.Contain("Cooking Basics"),
            "Should not include cooking content when filtering by python tag");

        // Test 2: Search with multiple tag filters
        var programmingResult = MemorySearchTools.SearchMemories(
            query: "programming concepts",
            mode: SearchMode.Semantic,
            pageSize: 10,
            page: 1,
            folder: TestFolder,
            tags: new string[] { "programming", "tutorial" });

        Assert.That(programmingResult, Contains.Substring("Python Programming Tutorial"),
            "Should find Python tutorial with programming+tutorial tags");
        Assert.That(programmingResult, Does.Not.Contain("Java Development Guide"),
            "Java guide lacks 'tutorial' tag so should be filtered out");

        // Test 3: Semantic search without tag filter should find all relevant
        var unFilteredResult = MemorySearchTools.SearchMemories(
            query: "programming",
            mode: SearchMode.Semantic,
            pageSize: 10,
            page: 1,
            folder: TestFolder,
            tags: null);

        Assert.That(unFilteredResult, Contains.Substring("memory://"),
            "Should return results with proper URIs");

        // Verify that results include actual content snippets (not empty)
        Assert.That(unFilteredResult, Contains.Substring("Python"),
            "Should have real content from files, not empty snippets");
    }

    [Test]
    [Ignore("Disabled due to SQLite permission errors in CI - needs investigation")]
    public void HybridSearchBlendsTextAndSemanticScoresWithTags()
    {
        // Create test files with varying relevance
        var mlIntroTags = new[] { "ai", "ml", "tutorial" };
        CreateTestFileWithTags("ml-intro.md",
            "Machine Learning Introduction",
            "An introduction to [[machine learning]], [[neural networks]], and [[deep learning]] concepts.",
            mlIntroTags);

        var mlAdvancedTags = new[] { "ai", "ml", "advanced" };
        CreateTestFileWithTags("ml-advanced.md",
            "Advanced Machine Learning",
            "Advanced topics in [[machine learning]] including [[reinforcement learning]] and [[GANs]].",
            mlAdvancedTags);

        var statsTags = new[] { "math", "statistics", "tutorial" };
        CreateTestFileWithTags("statistics-basics.md",
            "Statistics Fundamentals",
            "Understanding [[statistics]], [[probability]], and [[data analysis]] for beginners.",
            statsTags);

        // Sync to populate database
        GraphTools.Sync();

        // Test hybrid search with tag filter - use TestFolder to isolate search
        var result = MemorySearchTools.SearchMemories(
            query: "machine learning",
            mode: SearchMode.Hybrid,
            pageSize: 10,
            page: 1,
            folder: TestFolder,
            tags: new string[] { "ml" });

        // Verify hybrid search combines text and semantic scores
        Assert.That(result, Contains.Substring("Hybrid Search"),
            "Should indicate hybrid search mode");
        Assert.That(result, Contains.Substring("Text results:"),
            "Should show text result count");
        Assert.That(result, Contains.Substring("Semantic results:"),
            "Should show semantic result count");

        // Verify scores are shown for hybrid results
        Assert.That(result, Contains.Substring("Fused:"),
            "Should show fused score");
        Assert.That(result, Contains.Substring("Text:"),
            "Should show text score");
        Assert.That(result, Contains.Substring("Semantic:"),
            "Should show semantic score");

        // Verify tag filtering works
        Assert.That(result, Does.Not.Contain("Statistics Fundamentals"),
            "Statistics file should be filtered out by ml tag requirement");
    }

    [Test]
    public void SemanticSearchHandlesEmptyQueryGracefully()
    {
        var emptyResult = MemorySearchTools.SearchMemories(
            query: string.Empty,
            mode: SearchMode.Semantic,
            pageSize: 5,
            page: 1);

        Assert.That(emptyResult, Does.Contain("ERROR"),
            "Empty query should return a helpful error message.");
    }

    [Test]
    public void SemanticSearchHandlesWhitespaceOnlyQueryGracefully()
    {
        var whitespaceResult = MemorySearchTools.SearchMemories(
            query: "     ",
            mode: SearchMode.Semantic,
            pageSize: 5,
            page: 1);

        Assert.That(whitespaceResult, Does.Contain("ERROR"),
            "Whitespace-only query should return a helpful error message.");
    }

    [Test]
    public void SemanticSearchHandlesStopwordOnlyQueryGracefully()
    {
        var stopwordResult = MemorySearchTools.SearchMemories(
            query: "the and of",
            mode: SearchMode.Semantic,
            pageSize: 5,
            page: 1);

        Assert.That(stopwordResult, Does.Contain("ERROR"),
            "Stopword-only query should return a helpful error message.");
    }

    private void CreateTestFileWithTags(string fileName, string title, string content, string[] tags)
    {
        var path = Path.Combine(_testFolderPath, fileName);
        var fullContent = $"# {title}\n\n{content}";

        var frontmatter = new Dictionary<string, object>
        {
            ["title"] = title,
            ["tags"] = tags,
            ["type"] = "memory",
            ["status"] = "saved"
        };

        MarkdownIO.WriteMarkdown(path, frontmatter, fullContent);
    }
}
#pragma warning restore CA1861
