using Maenifold.Tools;
using NUnit.Framework;
using System;

namespace Maenifold.Tests;

public class ConceptGraphTests
{
    private static readonly string[] TestTags = new[] { "ai", "ml", "test" };

    [SetUp]
    public void SetUp()
    {
        // Ensure database is initialized before any Sync operations
        GraphDatabase.InitializeDatabase();
    }

    [Test]
    public void ConceptGraphSmokeTest()
    {
        Console.WriteLine("Testing Ma Core Concept Graph\n");

        // Test WriteMemory
        Console.WriteLine("1. Writing test memory...");
        var result = MemoryTools.WriteMemory(
            "AI Concepts Test",
            "This document explores [[Artificial Intelligence]] and its relationship to [[Machine Learning]] and [[Deep Learning]].\n\n" +
            "[[Deep Learning]] is a subset of [[Machine Learning]], which itself is a subset of [[Artificial Intelligence]].",
            folder: "test",
            tags: TestTags
        );
        Console.WriteLine(result);

        // Test Sync
        Console.WriteLine("\n2. Syncing concepts to graph database...");
        var syncResult = GraphTools.Sync();
        Console.WriteLine(syncResult);

        // Test BuildContext
        Console.WriteLine("\n3. Building context for 'Machine Learning'...");
        var contextResult = GraphTools.BuildContext("Machine Learning", 2);
        Console.WriteLine(contextResult);

        Console.WriteLine("\nTest complete!");
    }

    [Test]
    public void BuildContextWithoutContentTest()
    {
        Console.WriteLine("\nTesting BuildContext with includeContent=false\n");

        // Test WriteMemory
        Console.WriteLine("1. Writing test memory...");
        var result = MemoryTools.WriteMemory(
            "Machine Learning Concepts for Content Test",
            "This document discusses [[Machine Learning]] and its applications in modern systems.",
            folder: "test",
            tags: TestTags
        );
        Console.WriteLine(result);

        // Test Sync
        Console.WriteLine("\n2. Syncing concepts to graph database...");
        var syncResult = GraphTools.Sync();
        Console.WriteLine(syncResult);

        // Test BuildContext WITHOUT content
        Console.WriteLine("\n3. Building context WITHOUT content preview...");
        var contextResult = GraphTools.BuildContext("Machine Learning", depth: 2, maxEntities: 20, includeContent: false);
        Console.WriteLine(contextResult);

        // Verify content is NOT included
        Assert.That(contextResult.ToString(), Does.Not.Contain("Content preview"),
            "Output should not contain content previews when includeContent=false");

        Console.WriteLine("\nTest passed - no content included!");
    }

    [Test]
    public void BuildContextWithContentTest()
    {
        Console.WriteLine("\nTesting BuildContext with includeContent=true\n");

        // Create MULTIPLE files with SHARED concepts to create relationships
        Console.WriteLine("1. Writing test memories with shared concepts...");

        // File 1: Deep Learning + Machine Learning + Neural Networks
        var result1 = MemoryTools.WriteMemory(
            "Deep Learning Guide",
            "Comprehensive guide to [[Deep Learning]] using [[Neural Networks]]. [[Deep Learning]] is part of [[Machine Learning]].",
            folder: "test",
            tags: TestTags
        );

        // File 2: Machine Learning + Deep Learning + AI
        var result2 = MemoryTools.WriteMemory(
            "Machine Learning Overview",
            "Overview of [[Machine Learning]] and [[Deep Learning]] in [[AI]]. [[Machine Learning]] uses [[Neural Networks]].",
            folder: "test",
            tags: TestTags
        );

        // File 3: Neural Networks + Deep Learning
        var result3 = MemoryTools.WriteMemory(
            "Neural Networks Tutorial",
            "Tutorial on [[Neural Networks]] for [[Deep Learning]]. [[Neural Networks]] power modern [[AI]].",
            folder: "test",
            tags: TestTags
        );

        Console.WriteLine(result1);

        // Test Sync to create relationships
        Console.WriteLine("\n2. Syncing concepts to graph database...");
        var syncResult = GraphTools.Sync();
        Console.WriteLine(syncResult);

        // Test BuildContext WITH content
        Console.WriteLine("\n3. Building context WITH content preview...");
        var contextResult = GraphTools.BuildContext("Deep Learning", depth: 2, maxEntities: 20, includeContent: true);
        Console.WriteLine(contextResult);

        // Verify content IS included when relationships exist
        Assert.That(contextResult.DirectRelations.Count, Is.GreaterThan(0),
            "Should have direct relations from shared concepts");
        Assert.That(contextResult.ToString(), Does.Contain("Content preview"),
            "Output should contain content previews when includeContent=true AND relationships exist");

        Console.WriteLine("\nTest passed - content included with proper formatting!");
    }

    [Test]
    public void BuildContextIncludeContentDefaultTest()
    {
        Console.WriteLine("\nTesting BuildContext with default includeContent parameter\n");

        // Test WriteMemory
        Console.WriteLine("1. Writing test memory...");
        var result = MemoryTools.WriteMemory(
            "AI Overview for Default Test",
            "Overview of [[Artificial Intelligence]] in modern systems.",
            folder: "test",
            tags: TestTags
        );
        Console.WriteLine(result);

        // Test Sync
        Console.WriteLine("\n2. Syncing concepts to graph database...");
        var syncResult = GraphTools.Sync();
        Console.WriteLine(syncResult);

        // Test BuildContext with default parameter (should not include content)
        Console.WriteLine("\n3. Building context with default includeContent...");
        var contextResult = GraphTools.BuildContext("Artificial Intelligence");
        Console.WriteLine(contextResult);

        // Default should be false, so no content preview
        Assert.That(contextResult.ToString(), Does.Not.Contain("Content preview"),
            "Default includeContent should be false - no content previews");

        Console.WriteLine("\nTest passed - default behavior maintains backwards compatibility!");
    }
}
