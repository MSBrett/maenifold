#!/bin/bash
# Export maenifold graph data to sigma.js JSON format

DB_PATH="memory/.graph.db"
OUTPUT_DIR="docs/demo-artifacts/graph-data"

echo "Exporting graph data from: $DB_PATH"

mkdir -p "$OUTPUT_DIR"

# Export to JSON format compatible with sigma.js
sqlite3 "$DB_PATH" <<'EOF' > "$OUTPUT_DIR/graph-data.json"
.mode json
.output /dev/stdout

WITH
-- Get top concepts by occurrence (limit to reasonable size for visualization)
top_concepts AS (
  SELECT concept_name, occurrence_count
  FROM concepts
  WHERE occurrence_count > 1
  ORDER BY occurrence_count DESC
  LIMIT 500
),
-- Get nodes
nodes AS (
  SELECT json_object(
    'id', concept_name,
    'label', concept_name,
    'size', occurrence_count,
    'color', '#3B82F6'
  ) as node
  FROM top_concepts
),
-- Get edges between top concepts
edges AS (
  SELECT json_object(
    'id', concept_a || '-' || concept_b,
    'source', concept_a,
    'target', concept_b,
    'size', co_occurrence_count,
    'color', '#94A3B8'
  ) as edge
  FROM concept_graph
  WHERE concept_a IN (SELECT concept_name FROM top_concepts)
    AND concept_b IN (SELECT concept_name FROM top_concepts)
    AND co_occurrence_count > 1
  ORDER BY co_occurrence_count DESC
  LIMIT 1000
)
SELECT json_object(
  'nodes', (SELECT json_group_array(json(node)) FROM nodes),
  'edges', (SELECT json_group_array(json(edge)) FROM edges)
);
EOF

echo "Graph data exported to: $OUTPUT_DIR/graph-data.json"

# Print stats
echo ""
echo "Statistics:"
sqlite3 "$DB_PATH" "SELECT COUNT(*) || ' nodes' FROM concepts WHERE occurrence_count > 1 LIMIT 500;"
sqlite3 "$DB_PATH" "SELECT COUNT(*) || ' edges' FROM concept_graph WHERE co_occurrence_count > 1 LIMIT 1000;"
