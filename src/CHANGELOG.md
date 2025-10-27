# Changelog

All notable changes to maenifold MCP Server will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Sprint 20250128 – Test Coverage and Hardening
- Tests expanded to 114 total (up from 31); 99.1% pass (113/114)
- Security hardening: fixed path traversal and command injection; retained real infra ([[SQLite]] + filesystem) tests
- [[Ma Protocol]] compliance: TestRunner split to ≤250 lines with no behavior changes
- Metadata enhancements: richer workflow metadata (agentic-slc.json), structured responses, improved reporting
- CI: CLI workflow testing automated using compiled binary (no `dotnet run`), multi-OS matrix, artifacts in test-outputs/

### Added
- Enhanced workflow system with research-based metadata
  - Structured JSON responses for programmatic tool access
  - Enhanced agentic-dev.json with comprehensive metadata for enhanced thinking steps
  - Added toolHints, reasoning_effort, stop_conditions, and guardrails metadata
- Comprehensive test execution framework (foundational)
  - Isolated test environments with preserved artifacts

### Changed
- Workflow tool responses now return complete JSON step objects instead of text-only
- Test suite documentation updated with execution results and automation roadmap
- Three-phase automation roadmap created for comprehensive testing

### Fixed
- Path traversal and command injection mitigations
- Nullability warning in WorkflowTools.cs for queue array handling
- Workflow system exposes step metadata for programmatic access

### Documentation
- Updated CHANGELOG for Sprint 20250128 – test coverage expansion and security fixes
- Created comprehensive test execution summary; automation roadmap (Phase 1–3)
- Enhanced project documentation following [[Ma Protocol]] principles

## Historical Test Execution Results (earlier snapshot)
- Memory Tools: 14/52 tests (35% coverage) - 87.5% success rate
- Graph Tools: 2/21 tests (13% coverage) - 100% success rate
- System Tools: 10/10 tests (100% coverage) - 100% success rate
- Workflow Tools: 3/12 tests (25% coverage) - 66.7% success rate
- Thinking Tools: 2/8 tests (25% coverage) - 50% success rate
- Total at that time: 31/118 tests implemented with 87.1% success rate

**Performance Metrics (historical snapshot):**
- Memory operations: ~800-850ms
- Search operations: ~1400ms (large datasets)
- Graph sync: ~800-1600ms
- System tools: <500ms
