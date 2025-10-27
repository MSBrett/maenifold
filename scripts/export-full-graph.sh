#!/bin/bash
# Export full maenifold graph data to sigma.js JSON format

DB_PATH="${MAENIFOLD_ROOT:-$HOME/maenifold}/memory.db"
OUTPUT_DIR="docs/demo-artifacts/graph-data"

echo "Exporting full graph data from: $DB_PATH"

mkdir -p "$OUTPUT_DIR"

# Get stats
TOTAL_CONCEPTS=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM concepts;")
TOTAL_EDGES=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM concept_graph;")

echo "Total concepts in database: $TOTAL_CONCEPTS"
echo "Total edges in database: $TOTAL_EDGES"
echo ""
echo "Exporting all concepts with relationships..."

# Export to JSON format compatible with sigma.js
# All concepts with relationships (highest degree in graph)
sqlite3 "$DB_PATH" << 'SQLEOF' > "$OUTPUT_DIR/graph-data-full.json"
.mode list
WITH
-- Calculate degree (relationship count) for each concept
concept_degrees AS (
  SELECT concept_name,
         COUNT(*) as degree
  FROM (
    SELECT concept_a as concept_name FROM concept_graph
    UNION ALL
    SELECT concept_b as concept_name FROM concept_graph
  )
  GROUP BY concept_name
),
-- Get mention counts for sizing
concept_stats_temp AS (
  SELECT
    cm.concept_name,
    SUM(cm.mention_count) as total_mentions
  FROM concept_mentions cm
  GROUP BY cm.concept_name
),
-- Get all concepts with relationships (filter only isolated concepts with < 2 connections)
top_concepts AS (
  SELECT
    cd.concept_name,
    cd.degree as relationship_count,
    COALESCE(cs.total_mentions, 0) as total_mentions,
    ROW_NUMBER() OVER (ORDER BY cd.degree DESC) as rank
  FROM concept_degrees cd
  LEFT JOIN concept_stats_temp cs ON cd.concept_name = cs.concept_name
  WHERE cd.degree >= 2
  ORDER BY cd.degree DESC
),
-- Get all edges between concepts
edges_temp AS (
  SELECT
    concept_a,
    concept_b,
    co_occurrence_count
  FROM concept_graph
  WHERE concept_a IN (SELECT concept_name FROM top_concepts)
    AND concept_b IN (SELECT concept_name FROM top_concepts)
    AND co_occurrence_count >= 1
  ORDER BY co_occurrence_count DESC
),
-- Filter nodes to only those that appear in at least one edge
connected_nodes AS (
  SELECT DISTINCT concept_name
  FROM (
    SELECT concept_a as concept_name FROM edges_temp
    UNION
    SELECT concept_b as concept_name FROM edges_temp
  )
),
-- Final nodes list (only connected nodes)
nodes AS (
  SELECT json_object(
    'id', t.concept_name,
    'label', t.concept_name,
    'size', CAST(5 + (t.relationship_count - (SELECT MIN(relationship_count) FROM top_concepts WHERE concept_name IN (SELECT concept_name FROM connected_nodes))) * 25.0 /
                     ((SELECT MAX(relationship_count) FROM top_concepts WHERE concept_name IN (SELECT concept_name FROM connected_nodes)) -
                      (SELECT MIN(relationship_count) FROM top_concepts WHERE concept_name IN (SELECT concept_name FROM connected_nodes))) AS INTEGER),
    'degree', t.relationship_count,
    'mentions', t.total_mentions,
    'relationships', t.relationship_count
  ) as node
  FROM top_concepts t
  WHERE t.concept_name IN (SELECT concept_name FROM connected_nodes)
),
-- Final edges with json formatting
edges AS (
  SELECT json_object(
    'id', concept_a || '->' || concept_b,
    'source', concept_a,
    'target', concept_b,
    'size', CASE
      WHEN co_occurrence_count > 50 THEN 5
      WHEN co_occurrence_count > 20 THEN 3
      WHEN co_occurrence_count > 10 THEN 2
      ELSE 1
    END,
    'weight', co_occurrence_count,
    'color', '#94A3B8'
  ) as edge
  FROM edges_temp
)
SELECT json_object(
  'nodes', (SELECT json_group_array(json(node)) FROM nodes),
  'edges', (SELECT json_group_array(json(edge)) FROM edges),
  'metadata', json_object(
    'total_concepts', (SELECT COUNT(*) FROM concepts),
    'total_edges', (SELECT COUNT(*) FROM concept_graph),
    'filtered_concepts', (SELECT COUNT(*) FROM nodes),
    'filtered_edges', (SELECT COUNT(*) FROM edges),
    'generated', datetime('now')
  )
);
SQLEOF

echo ""
echo "Full graph data exported to: $OUTPUT_DIR/graph-data-full.json"

# Print stats from JSON
NODE_COUNT=$(jq -r '.nodes | length' "$OUTPUT_DIR/graph-data-full.json" 2>/dev/null || echo "0")
EDGE_COUNT=$(jq -r '.edges | length' "$OUTPUT_DIR/graph-data-full.json" 2>/dev/null || echo "0")

echo ""
echo "Exported Statistics:"
echo "  Nodes: $NODE_COUNT"
echo "  Edges: $EDGE_COUNT"
echo "  Filtered from: $TOTAL_CONCEPTS concepts, $TOTAL_EDGES edges"
