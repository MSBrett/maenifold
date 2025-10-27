using System.Diagnostics;
using System.Globalization;
using System.Text;
using Maenifold.Utils;
using Microsoft.Data.Sqlite;

namespace Maenifold.Tools;

public static partial class PerformanceBenchmark
{
    private static string BenchmarkGraphTraversal(int iterations)
    {
        var results = new StringBuilder();
        results.AppendLine("GRPH-009: CTE vs N+1 Pattern Benchmark");
        results.AppendLine("Claim: 34% faster than CTE");
        results.AppendLine();

        using var conn = new SqliteConnection(Config.DatabaseConnectionString);
        conn.OpenWithWAL();


        var testConcepts = conn.Query<(string concept, int relations)>(
                    @"SELECT concept_name, relation_count
              FROM (
                  SELECT concept_name,
                         (SELECT COUNT(*) FROM concept_graph WHERE concept_a = c.concept_name OR concept_b = c.concept_name) as relation_count
                  FROM concepts c
              )
              WHERE relation_count > 5
              ORDER BY relation_count DESC
              LIMIT 10").ToList();

        if (!testConcepts.Any())
        {
            return "No test concepts with sufficient connectivity found. Run sync first.";
        }

        var cteTimings = new List<long>();
        var n1Timings = new List<long>();

        foreach (var (concept, relationCount) in testConcepts.Take(3))
        {
            results.AppendLineInvariant($"Testing concept: {concept} (relations: {relationCount})");


            for (int i = 0; i < iterations; i++)
            {
                var timer = Stopwatch.StartNew();
                var cteResult = GraphAnalyzer.Visualize(concept, depth: 3, maxNodes: 50);
                timer.Stop();
                cteTimings.Add(timer.ElapsedMilliseconds);
            }


            for (int i = 0; i < iterations; i++)
            {
                var timer = Stopwatch.StartNew();
                var n1Result = BenchmarkN1Pattern(conn, concept, depth: 3, maxNodes: 50);
                timer.Stop();
                n1Timings.Add(timer.ElapsedMilliseconds);
            }
        }


        var cteAvg = cteTimings.Average();
        var n1Avg = n1Timings.Average();
        var percentDiff = ((cteAvg - n1Avg) / cteAvg) * 100;

        results.AppendLineInvariant($"Results ({iterations * testConcepts.Count} iterations each):");
        results.AppendLineInvariant($"CTE Average: {cteAvg:F1}ms");
        results.AppendLineInvariant($"N+1 Average: {n1Avg:F1}ms");
        results.AppendLineInvariant($"Performance difference: {percentDiff:F1}% {(percentDiff > 0 ? "N+1 FASTER" : "CTE FASTER")}");
        results.AppendLineInvariant($"Claim verification: {(Math.Abs(percentDiff) >= 34 ? "CONFIRMED" : "UNCONFIRMED")} (target: 34%)");

        return results.ToString();
    }

    private static string BenchmarkN1Pattern(SqliteConnection conn, string conceptName, int depth, int maxNodes)
    {
        var visited = new HashSet<string>();
        var relations = new List<(string a, string b, int count)>();
        var queue = new Queue<(string concept, int currentDepth)>();

        queue.Enqueue((conceptName, 0));
        visited.Add(conceptName);

        while (queue.Count > 0 && relations.Count < maxNodes)
        {
            var (current, currentDepth) = queue.Dequeue();

            if (currentDepth >= depth) continue;


            var neighbors = conn.Query<(string neighbor, int count)>(
                            @"SELECT 
                    CASE WHEN concept_a = @concept THEN concept_b ELSE concept_a END as neighbor,
                    co_occurrence_count as count
                FROM concept_graph 
                WHERE concept_a = @concept OR concept_b = @concept
                ORDER BY co_occurrence_count DESC",
                            new { concept = current }).ToList();

            foreach (var (neighbor, count) in neighbors)
            {
                relations.Add((current, neighbor, count));

                if (!visited.Contains(neighbor) && relations.Count < maxNodes)
                {
                    visited.Add(neighbor);
                    queue.Enqueue((neighbor, currentDepth + 1));
                }
            }
        }


        var mermaid = new StringBuilder("graph TD\n");
        foreach (var (a, b, count) in relations.Take(maxNodes))
        {
            var nodeA = a.Replace(" ", "_").Replace("-", "_");
            var nodeB = b.Replace(" ", "_").Replace("-", "_");
            mermaid.AppendLine(CultureInfo.InvariantCulture, $"    {nodeA} -->|{count}| {nodeB}");
        }

        return mermaid.ToString();
    }

    private static string BenchmarkComplexTraversal(int iterations)
    {
        var results = new StringBuilder();
        results.AppendLine("Complex Traversal Bottleneck Analysis");
        results.AppendLine("Target: Identify 5-8s complex traversal scenarios");
        results.AppendLine();

        using var conn = new SqliteConnection(Config.DatabaseConnectionString);
        conn.OpenWithWAL();


        var hubConcepts = conn.Query<(string concept, int connections)>(
                    @"SELECT concept_name, connection_count
              FROM (
                  SELECT concept_name,
                         (SELECT COUNT(*) FROM concept_graph WHERE concept_a = c.concept_name OR concept_b = c.concept_name) as connection_count
                  FROM concepts c
              )
              ORDER BY connection_count DESC
              LIMIT 5").ToList();

        if (!hubConcepts.Any())
        {
            return "No concepts found for traversal testing. Run sync first.";
        }

        var complexTimings = new List<long>();

        foreach (var (concept, connections) in hubConcepts)
        {
            results.AppendLineInvariant($"Testing hub concept: {concept} ({connections} connections)");


            for (int i = 0; i < iterations; i++)
            {
                var timer = Stopwatch.StartNew();
                var result = GraphAnalyzer.Visualize(concept, depth: 5, maxNodes: 200);
                timer.Stop();
                complexTimings.Add(timer.ElapsedMilliseconds);

                if (timer.ElapsedMilliseconds > 1000)
                {
                    results.AppendLineInvariant($"  Slow traversal detected: {timer.ElapsedMilliseconds}ms");
                }
            }
        }

        var avgComplexTime = complexTimings.Average();
        var maxTime = complexTimings.Max();
        var slowCount = complexTimings.Count(t => t >= 5000);

        results.AppendLine();
        results.AppendLine("Complex Traversal Results:");
        results.AppendLineInvariant($"Average time: {avgComplexTime:F1}ms");
        results.AppendLineInvariant($"Maximum time: {maxTime:F1}ms");
        results.AppendLineInvariant($"Queries >=5s: {slowCount}/{complexTimings.Count}");
        results.AppendLineInvariant($"Bottleneck confirmed: {(slowCount > 0 ? "YES" : "NO")} (target: 5-8s scenarios)");

        return results.ToString();
    }
}
