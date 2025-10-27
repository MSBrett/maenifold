using System.Globalization;
using Microsoft.Data.Sqlite;
using System.Text;
using Maenifold.Utils;

namespace Maenifold.Tools;

public static class GraphAnalyzer
{
    public static List<string> RandomWalk(
            SqliteConnection conn,
            string startConcept,
            int walkLength = 10)
    {
        var walk = new List<string> { startConcept };
        var current = startConcept;
        var random = new Random();

        for (int i = 1; i < walkLength; i++)
        {

            var neighbors = conn.Query<string>(
                            @"SELECT DISTINCT 
                    CASE 
                        WHEN concept_a = @concept THEN concept_b 
                        ELSE concept_a 
                    END as neighbor
                FROM concept_graph 
                WHERE concept_a = @concept OR concept_b = @concept",
                            new { concept = current }).ToList();


            if (neighbors.Count == 0) break;


            current = neighbors[random.Next(neighbors.Count)];
            walk.Add(current);
        }

        return walk;
    }

    public static string Visualize(
            string conceptName,
            int depth = 2,
            int maxNodes = 30)
    {

        if (string.IsNullOrWhiteSpace(conceptName))
            return "ERROR: conceptName is required";

        if (depth < 1 || depth > 5)
            return "ERROR: depth must be between 1 and 5";

        if (maxNodes < 5 || maxNodes > 100)
            return "ERROR: maxNodes must be between 5 and 100";


        conceptName = MarkdownIO.NormalizeConcept(conceptName);

        using var conn = new SqliteConnection(Config.DatabaseConnectionString);
        conn.OpenReadOnly();


        var exists = conn.QuerySingle<bool>("SELECT COUNT(*) > 0 FROM concepts WHERE concept_name = @name", new { name = conceptName });
        if (!exists)
            return $"CONCEPT '{conceptName}' not found. Run sync first.";


        var relations = conn.Query<(string a, string b, int count)>(
                    @"WITH RECURSIVE search(depth, from_concept, to_concept, count, path) AS (
                -- Seed: direct neighbors of the root concept
                SELECT
                    1 as depth,
                    @name as from_concept,
                    CASE WHEN concept_a = @name THEN concept_b ELSE concept_a END as to_concept,
                    co_occurrence_count as count,
                    '|' || @name || '|' || (CASE WHEN concept_a = @name THEN concept_b ELSE concept_a END) || '|' as path
                FROM concept_graph
                WHERE concept_a = @name OR concept_b = @name
                UNION ALL
                -- Recursive step: expand from the last 'to_concept' to its neighbors
                SELECT
                    s.depth + 1,
                    s.to_concept as from_concept,
                    CASE WHEN g.concept_a = s.to_concept THEN g.concept_b ELSE g.concept_a END as to_concept,
                    g.co_occurrence_count as count,
                    s.path || (CASE WHEN g.concept_a = s.to_concept THEN g.concept_b ELSE g.concept_a END) || '|' as path
                FROM search s
                JOIN concept_graph g ON (g.concept_a = s.to_concept OR g.concept_b = s.to_concept)
                WHERE s.depth < @depth
                  AND instr(s.path, '|' || (CASE WHEN g.concept_a = s.to_concept THEN g.concept_b ELSE g.concept_a END) || '|') = 0
            )
            SELECT DISTINCT from_concept as concept_a, to_concept as concept_b, count
            FROM search
            ORDER BY count DESC
            LIMIT @maxNodes;",
                    new { name = conceptName, depth, maxNodes });


        var mermaid = new StringBuilder("graph TD\n");
        foreach (var (a, b, count) in relations)
        {
            var nodeA = a.Replace(" ", "_").Replace("-", "_");
            var nodeB = b.Replace(" ", "_").Replace("-", "_");
            mermaid.AppendLine(CultureInfo.InvariantCulture, $"    {nodeA} -->|{count}| {nodeB}");
        }

        if (!relations.Any())
            mermaid.AppendLine(CultureInfo.InvariantCulture, $"    {conceptName.Replace(" ", "_")} [No connections found]");

        return mermaid.ToString();
    }
}