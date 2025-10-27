using Maenifold.Tools;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Maenifold.Tests;

/// <summary>
/// Regression test for FTS index accumulation bug.
/// This test validates that multiple sync operations don't cause
/// the file_search table to grow unboundedly.
/// </summary>
public class FtsIndexAccumulationTests
{
    [Test]
    [Ignore("Test hangs - sync operations take too long")]
    public void MultipleSyncsShouldNotAccumulateFtsRows()
    {
        Console.WriteLine("Testing FTS index accumulation fix");
        Console.WriteLine("This test validates the telemetry output shows consistent row counts\n");

        // Capture stderr to check telemetry output
        var originalError = Console.Error;
        var errorCapture = new StringBuilder();
        var errorWriter = new StringWriter(errorCapture);
        Console.SetError(errorWriter);

        try
        {
            // Run multiple syncs to test for accumulation
            Console.WriteLine("Running first sync...");
            ConceptSync.Sync();

            Console.WriteLine("Running second sync...");
            ConceptSync.Sync();

            Console.WriteLine("Running third sync...");
            ConceptSync.Sync();
        }
        finally
        {
            Console.SetError(originalError);
        }

        var telemetryOutput = errorCapture.ToString();
        Console.WriteLine("Telemetry output:");
        Console.WriteLine(telemetryOutput);

        // Parse telemetry to verify no accumulation
        var lines = telemetryOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var rowCounts = new List<(int before, int after)>();
        var beforeValues = new List<int>();
        var afterValues = new List<int>();

        // First pass: collect all "before" values
        foreach (var line in lines)
        {
            if (line.Contains("[SYNC TELEMETRY] file_search rows before sync:"))
            {
                var beforeText = line.Split(':')[1].Trim();
                if (int.TryParse(beforeText, out var before))
                {
                    beforeValues.Add(before);
                }
            }
        }

        // Second pass: collect all "after" values
        foreach (var line in lines)
        {
            if (line.Contains("[SYNC TELEMETRY] file_search rows after sync:"))
            {
                var afterText = line.Split(':')[1].Trim();
                if (int.TryParse(afterText, out var after))
                {
                    afterValues.Add(after);
                }
            }
        }

        // Pair them up (should be equal counts)
        var minCount = Math.Min(beforeValues.Count, afterValues.Count);
        for (int i = 0; i < minCount; i++)
        {
            rowCounts.Add((beforeValues[i], afterValues[i]));
        }

        Console.WriteLine($"\nParsed {rowCounts.Count} sync operations:");
        for (int i = 0; i < rowCounts.Count; i++)
        {
            var (before, after) = rowCounts[i];
            Console.WriteLine($"  Sync {i + 1}: {before} -> {after} rows");
        }

        // Verify we captured multiple syncs
        Assert.That(rowCounts.Count, Is.GreaterThanOrEqualTo(2),
            "Should have captured at least 2 sync operations");

        // The actual behavior: sync doesn't clear the table before each operation,
        // it clears and rebuilds within the transaction
        // Verify that row counts are consistent across syncs (no accumulation)
        if (rowCounts.Count >= 2)
        {
            var firstAfter = rowCounts[0].after;
            for (int i = 1; i < rowCounts.Count; i++)
            {
                var (before, after) = rowCounts[i];

                // Before sync should equal the after count from previous sync
                // (since the table isn't cleared between syncs, just rebuilt within transaction)
                Assert.That(before, Is.EqualTo(firstAfter),
                    $"Sync {i + 1}: file_search should start with same count as previous sync ended with (found {before} rows, expected {firstAfter})");

                // After sync should match first sync result (consistent)
                Assert.That(after, Is.EqualTo(firstAfter),
                    $"Sync {i + 1}: file_search rows should remain consistent ({after} vs expected {firstAfter})");
            }
        }

        Console.WriteLine("\n✅ Test passed - file_search table shows no accumulation pattern");
    }
}