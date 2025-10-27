#!/bin/bash
set -e

# Build GitHub Pages documentation site
# Generates HTML from existing markdown without duplicating source files

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DOCS_DIR="$PROJECT_ROOT/docs-site"
MAENIFOLD_DOCS="$PROJECT_ROOT/maenifold-docs"
ASSETS_USAGE="$PROJECT_ROOT/src/assets/usage"

echo "Building GitHub Pages site in /docs..."

# Clean and create docs directory
rm -rf "$DOCS_DIR"
mkdir -p "$DOCS_DIR"

# Create landing page
cat > "$DOCS_DIR/index.html" << 'EOF'
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Maenifold - Extended Mind MCP Server</title>
    <style>
        :root {
            --primary: #00ff88;
            --bg: #0a0a0a;
            --surface: #151515;
            --text: #e0e0e0;
            --muted: #888;
            --border: #333;
        }
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: var(--bg);
            color: var(--text);
            line-height: 1.6;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            padding: 2rem;
        }
        header {
            border-bottom: 1px solid var(--border);
            padding-bottom: 2rem;
            margin-bottom: 3rem;
        }
        h1 {
            color: var(--primary);
            font-size: 3rem;
            margin-bottom: 0.5rem;
        }
        .tagline {
            font-size: 1.2rem;
            color: var(--muted);
        }
        .section {
            margin-bottom: 3rem;
        }
        h2 {
            color: var(--primary);
            margin-bottom: 1.5rem;
        }
        .cards {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 1.5rem;
        }
        .card {
            background: var(--surface);
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 1.5rem;
            transition: border-color 0.2s;
        }
        .card:hover {
            border-color: var(--primary);
        }
        .card h3 {
            color: var(--primary);
            margin-bottom: 0.5rem;
        }
        .card p {
            color: var(--muted);
            margin-bottom: 1rem;
        }
        a {
            color: var(--primary);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
        .tools-list {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 0.5rem;
            margin-top: 1rem;
        }
        .tool-link {
            padding: 0.5rem;
            background: var(--surface);
            border: 1px solid var(--border);
            border-radius: 4px;
            transition: all 0.2s;
        }
        .tool-link:hover {
            background: var(--border);
            border-color: var(--primary);
        }
        .graph-viz {
            background: var(--surface);
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 2rem;
            text-align: center;
            margin: 2rem 0;
        }
        .graph-viz h3 {
            color: var(--primary);
            margin-bottom: 1rem;
        }
        .stats {
            display: flex;
            justify-content: center;
            gap: 3rem;
            margin: 1rem 0;
        }
        .stat {
            text-align: center;
        }
        .stat-value {
            font-size: 2rem;
            color: var(--primary);
            font-weight: bold;
        }
        .stat-label {
            color: var(--muted);
        }
    </style>
</head>
<body>
    <div class="container">
        <header>
            <h1>Maenifold</h1>
            <p class="tagline">Extended Mind MCP Server - Your persistent reasoning layer</p>
        </header>

        <div class="section">
            <h2>Documentation</h2>
            <div class="cards">
                <div class="card">
                    <h3>📚 Main Documentation</h3>
                    <p>Complete documentation for Maenifold, including installation, configuration, and usage.</p>
                    <a href="README.html">Read Documentation →</a>
                </div>
                <div class="card">
                    <h3>🛠️ Development Guide</h3>
                    <p>Contributing guidelines, development setup, and architectural details.</p>
                    <a href="DEVELOPMENT.html">Development Guide →</a>
                </div>
                <div class="card">
                    <h3>🚀 Quick Start</h3>
                    <p>Get started with Maenifold in minutes with our bootstrap guide.</p>
                    <a href="llm-bootstrap.html">Bootstrap Guide →</a>
                </div>
                <div class="card">
                    <h3>🤖 LLM Integration</h3>
                    <p>Complete guide for integrating Maenifold with your LLM workflow.</p>
                    <a href="llm-guide.html">LLM Guide →</a>
                </div>
            </div>
        </div>

        <div class="graph-viz">
            <h3>Knowledge Graph Visualization</h3>
            <div class="stats">
                <div class="stat">
                    <div class="stat-value">889</div>
                    <div class="stat-label">Concepts</div>
                </div>
                <div class="stat">
                    <div class="stat-value">8,985</div>
                    <div class="stat-label">Relationships</div>
                </div>
            </div>
            <a href="graph.html">Explore Knowledge Graph →</a>
        </div>

        <div class="section">
            <h2>Tool Documentation</h2>
            <p style="color: var(--muted); margin-bottom: 1rem;">
                Complete reference for all 26 Maenifold tools:
            </p>
            <div class="tools-list">
EOF

# Generate tool links
for tool in "$ASSETS_USAGE/tools"/*.md; do
    if [ -f "$tool" ]; then
        basename=$(basename "$tool" .md)
        # Convert filename to readable name (e.g., buildcontext -> Build Context)
        readable_name=$(echo "$basename" | sed 's/\([a-z]\)\([A-Z]\)/\1 \2/g' | sed 's/^./\U&/')
        echo "                <a href=\"tools/${basename}.html\" class=\"tool-link\">${readable_name}</a>" >> "$DOCS_DIR/index.html"
    fi
done

# Complete the landing page
cat >> "$DOCS_DIR/index.html" << 'EOF'
            </div>
        </div>

        <footer style="margin-top: 4rem; padding-top: 2rem; border-top: 1px solid var(--border); text-align: center; color: var(--muted);">
            <p>Maenifold - Extended Mind MCP Server</p>
            <p><a href="https://github.com/MSBrett/maenifold">GitHub</a> |
               <a href="https://www.npmjs.com/package/maenifold">NPM</a></p>
        </footer>
    </div>
</body>
</html>
EOF

# Function to convert markdown to basic HTML
convert_md_to_html() {
    local md_file="$1"
    local html_file="$2"
    local title="$3"

    cat > "$html_file" << EOF
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>${title} - Maenifold</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/github-markdown-css@5/github-markdown-dark.min.css">
    <style>
        body {
            background: #0a0a0a;
            padding: 2rem;
        }
        .markdown-body {
            max-width: 1200px;
            margin: 0 auto;
            background: #0d1117;
            padding: 2rem;
            border-radius: 8px;
            border: 1px solid #30363d;
        }
        .nav {
            max-width: 1200px;
            margin: 0 auto 2rem;
        }
        .nav a {
            color: #00ff88;
            text-decoration: none;
            margin-right: 1rem;
        }
    </style>
</head>
<body>
    <div class="nav">
        <a href="index.html">← Home</a>
        <a href="README.html">Documentation</a>
        <a href="graph.html">Graph</a>
    </div>
    <div class="markdown-body">
        <pre><code>$(cat "$md_file" | sed 's/&/\&amp;/g; s/</\&lt;/g; s/>/\&gt;/g')</code></pre>
    </div>
</body>
</html>
EOF
}

# Convert main documentation files
echo "Converting documentation files..."
convert_md_to_html "$MAENIFOLD_DOCS/README.md" "$DOCS_DIR/README.html" "Documentation"
convert_md_to_html "$MAENIFOLD_DOCS/DEVELOPMENT.md" "$DOCS_DIR/DEVELOPMENT.html" "Development"
convert_md_to_html "$ASSETS_USAGE/llm-bootstrap.md" "$DOCS_DIR/llm-bootstrap.html" "Bootstrap Guide"
convert_md_to_html "$ASSETS_USAGE/llm-guide.md" "$DOCS_DIR/llm-guide.html" "LLM Guide"

# Convert tool documentation
mkdir -p "$DOCS_DIR/tools"
for tool in "$ASSETS_USAGE/tools"/*.md; do
    if [ -f "$tool" ]; then
        basename=$(basename "$tool" .md)
        readable_name=$(echo "$basename" | sed 's/\([a-z]\)\([A-Z]\)/\1 \2/g' | sed 's/^./\U&/')
        convert_md_to_html "$tool" "$DOCS_DIR/tools/${basename}.html" "$readable_name"
    fi
done

# Copy graph data if it exists
if [ -f "$PROJECT_ROOT/docs/graph.json" ]; then
    echo "Graph data already exists in docs/"
else
    echo "Generating graph data..."
    "$PROJECT_ROOT/src/bin/Release/net9.0/osx-arm64/publish/Maenifold" export-graph "$DOCS_DIR/graph.json" 2>/dev/null || echo "Graph export skipped"
fi

# Create graph visualization page
cat > "$DOCS_DIR/graph.html" << 'EOF'
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Knowledge Graph - Maenifold</title>
    <style>
        body {
            margin: 0;
            padding: 0;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: #0a0a0a;
            color: #e0e0e0;
        }
        .nav {
            position: absolute;
            top: 1rem;
            left: 1rem;
            z-index: 100;
        }
        .nav a {
            color: #00ff88;
            text-decoration: none;
            margin-right: 1rem;
            background: rgba(10, 10, 10, 0.9);
            padding: 0.5rem 1rem;
            border-radius: 4px;
            border: 1px solid #333;
            display: inline-block;
        }
        #graph-container {
            width: 100vw;
            height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            color: #00ff88;
            font-size: 1.2rem;
        }
    </style>
</head>
<body>
    <div class="nav">
        <a href="index.html">← Home</a>
    </div>
    <div id="graph-container">
        <div>Knowledge Graph Visualization Coming Soon</div>
    </div>
</body>
</html>
EOF

echo "GitHub Pages site built successfully in $DOCS_DIR"
echo "Total files created: $(find "$DOCS_DIR" -type f | wc -l)"