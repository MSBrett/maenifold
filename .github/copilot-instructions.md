# maenifold - GitHub Copilot Instructions

**ALWAYS follow these instructions first. Only search or explore further if information here is incomplete or found to be incorrect.**

maenifold is a test-time adaptive reasoning system for AI agents that enables persistent knowledge building through markdown memories, WikiLinks, and vector-backed concept graphs. Built with C# .NET 9.0, it operates as both a CLI tool and MCP (Model Context Protocol) server.

## Working Effectively

### Prerequisites and Setup
- **Install .NET 9.0 SDK** (REQUIRED - .NET 8.0 will NOT work):
  ```bash
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version latest --channel 9.0
  export PATH="$HOME/.dotnet:$PATH"
  ```
- SQLite 3 (vector extension loads automatically)
- Environment: Linux, macOS, or Windows

### Core Build Commands
- **Restore dependencies**: `dotnet restore src/Maenifold.csproj` -- takes ~15 seconds
- **Build project**: `dotnet build src/Maenifold.csproj --configuration Release --no-restore` -- takes ~11 seconds. NEVER CANCEL.
- **Run tests**: `dotnet test tests/Maenifold.Tests/Maenifold.Tests.csproj --configuration Release --verbosity normal` -- takes ~34 seconds. NEVER CANCEL. Some tests fail (expected, see CI config).
- **Build + test combined**: Run build commands sequentially -- total ~26 seconds for build, ~34 seconds for tests.

### Running the Application
- **CLI help**: `dotnet run --project src/Maenifold.csproj --configuration Release -- --help`
- **CLI mode**: `dotnet run --project src/Maenifold.csproj --configuration Release -- --tool <name> --payload '<json>'`
- **MCP server mode**: `dotnet run --project src/Maenifold.csproj --configuration Release -- --mcp`

### Build Timing - CRITICAL
- **NEVER CANCEL builds or tests** - They complete in reasonable time:
  - Restore: 15 seconds
  - Build: 11 seconds  
  - Tests: 34 seconds
- **Set timeouts**: Use 180+ seconds for build commands, 60+ seconds for tests
- **Expect verbose output** during restore/build - this is normal
- **Subsequent runs faster**: First build downloads packages, subsequent builds use cache

## Validation Scenarios

### Always Test These Workflows After Changes
1. **Memory Creation and Search**:
   ```bash
   # Set test environment
   export MAENIFOLD_ROOT="/tmp/ma-test-$(date +%s)"
   mkdir -p "$MAENIFOLD_ROOT"
   
   # Create memory with WikiLink
   dotnet run --project src/Maenifold.csproj --configuration Release -- --tool WriteMemory --payload '{"title":"Test Memory","content":"Testing [[WikiLink]] functionality"}'
   
   # Sync to build graph
   dotnet run --project src/Maenifold.csproj --configuration Release -- --tool Sync --payload '{}'
   
   # Search memories
   dotnet run --project src/Maenifold.csproj --configuration Release -- --tool SearchMemories --payload '{"query":"test memory"}'
   ```

2. **MCP Server Startup**:
   ```bash
   # Test MCP mode starts without errors (timeout after 10s)
   timeout 10 dotnet run --project src/Maenifold.csproj --configuration Release -- --mcp
   ```

3. **Graph Operations** (can be slow with large data):
   ```bash
   # Test graph visualization (limit depth to avoid timeouts)
   dotnet run --project src/Maenifold.csproj --configuration Release -- --tool Visualize --payload '{"conceptName":"WikiLink","depth":2,"maxNodes":15}'
   ```

### Performance Expectations
- **Memory operations**: < 2 seconds
- **Sync operations**: 1-5 seconds for small datasets  
- **Vector search**: 1-3 seconds (ONNX model loading adds ~400ms first time)
- **Graph operations**: 2-15 seconds (depth 1-3), may timeout at depth 4+
- **Application startup**: 1-2 seconds
- **First run setup**: Additional time for asset initialization and database creation

## Common Issues and Workarounds

### Build Issues
- **".NET 9.0 not supported"**: Must install .NET 9.0 SDK, not 8.0
- **"assets not found"**: Assets copy automatically during build
- **Test failures**: Expected - some tests are disabled due to performance issues

### Runtime Issues  
- **"Vector extension failed"**: SQLite vector extension loads from assets/native/[platform]/
- **Graph timeouts**: Use `depth=2`, `maxNodes=15` for large datasets
- **Memory path issues**: Set `MAENIFOLD_ROOT` environment variable

### Performance Issues
- **Deep graph traversal timeouts**: Limit `depth` to 2-3 and `maxNodes` to 10-15
- **Large sync operations**: Can take 30+ seconds with many files - DO NOT CANCEL
- **First vector search slow**: ONNX model loading adds ~400ms initial overhead

## Key Projects and Structure

### Source Structure
```
src/
├── Maenifold.csproj          # Main project file (.NET 9.0)
├── Tools/                 # MCP tool implementations
│   ├── MemoryTools.cs     # WriteMemory, ReadMemory, EditMemory
│   ├── MemorySearchTools.cs # SearchMemories, FindSimilarConcepts  
│   ├── GraphTools.cs      # Sync, Visualize, BuildContext
│   └── [other tools]
├── Utils/                 # Core utilities
│   ├── SqliteExtensions.cs # Vector extension loading
│   ├── VectorTools.cs     # ONNX embeddings
│   └── Config.cs          # Environment configuration
└── assets/               # Models, workflows, native libraries
    ├── models/           # ONNX models for embeddings
    ├── native/           # Platform-specific SQLite extensions
    └── workflows/        # 30+ reasoning methodologies
```

### Test Structure
```
tests/Maenifold.Tests/       # NUnit tests (some disabled/failing)
```

### Key Configuration
- **Memory path**: `MAENIFOLD_ROOT` env var (default: `~/maenifold`)  
- **Database**: `{MAENIFOLD_ROOT}/memory.db`
- **Memory files**: `{MAENIFOLD_ROOT}/memory/*.md`
- **Assets**: Copy from `src/assets/` to output directory

## Development Workflows

### Making Changes
1. **Always build first**: `dotnet build src/Maenifold.csproj --configuration Release`
2. **Test core functionality**: Run memory creation/search validation
3. **Test MCP integration**: Verify server starts correctly
4. **Check graph operations**: Test with small datasets first

### Before Committing  
- **Run validation**: Use `scripts/validate-ma-core.sh` for comprehensive checks
- **Test edge cases**: Large graphs, empty queries, invalid paths
- **Performance check**: Ensure no operations timeout unexpectedly

### CI Integration
- **GitHub Actions**: `.github/workflows/build.yml` - tests currently disabled
- **Branch protection**: Changes require PR review
- **Build verification**: Must compile and basic functionality work

### Security & Compliance Workflows
- **Security pipelines**: `.github/workflows/codeql.yml`, `security.yml`, and `dependency-review.yml`
- **When GitHub Advanced Security is disabled**: Workflows emit warnings but continue; enable features under **Settings → Security → Code security and analysis** for full enforcement
- **Secret scanning**: Requires the workflow token to have `pull-requests: read`; already configured in `security.yml`
- **Manual verification**: Run `dotnet list src/Maenifold.csproj package --vulnerable --include-transitive` to mirror the dependency scan locally

## Common Tasks - Quick Reference

### Building and Testing
```bash
# Complete build cycle
dotnet restore src/Maenifold.csproj                    # ~15s
dotnet build src/Maenifold.csproj --configuration Release --no-restore  # ~11s  
dotnet test tests/Maenifold.Tests/Maenifold.Tests.csproj --configuration Release --verbosity normal  # ~34s

# Quick validation  
./scripts/validate-ma-core.sh
```

### Development Testing
```bash
# Test CLI tools
dotnet run --project src/Maenifold.csproj --configuration Release -- --help

# Test memory workflow
export MAENIFOLD_ROOT="/tmp/test"
mkdir -p "$MAENIFOLD_ROOT"
dotnet run --project src/Maenifold.csproj --configuration Release -- --tool WriteMemory --payload '{"title":"Test","content":"[[Test]] content"}'
dotnet run --project src/Maenifold.csproj --configuration Release -- --tool Sync --payload '{}'
dotnet run --project src/Maenifold.csproj --configuration Release -- --tool SearchMemories --payload '{"query":"test"}'

# Test MCP mode  
timeout 10 dotnet run --project src/Maenifold.csproj --configuration Release -- --mcp
```

### Repository Navigation
```bash
# Key files for development
src/Tools/           # All MCP tool implementations
src/Utils/           # Core utilities and extensions  
src/assets/models/   # ONNX embedding models
docs/KNOWN_ISSUES.md # Performance limitations
README.md           # User documentation
DISTRIBUTION.md     # Deployment options
```

**Remember**: This is production-quality test-time reasoning infrastructure. The system handles 1.1M+ graph relationships and provides 30+ reasoning methodologies. Treat performance constraints seriously and always validate changes against real usage scenarios.
