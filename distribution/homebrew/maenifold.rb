class Maenifold < Formula
  desc "Persistent Graph-of-Thoughts for AI agents and multi-agent systems"
  homepage "https://github.com/MSBrett/maenifold"
  version "1.0.0"
  license "MIT"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/MSBrett/maenifold/releases/download/v1.0.0/maenifold-osx-arm64.tar.gz"
      sha256 "PLACEHOLDER_ARM64_SHA256"
    else
      url "https://github.com/MSBrett/maenifold/releases/download/v1.0.0/maenifold-osx-x64.tar.gz"
      sha256 "PLACEHOLDER_X64_SHA256"
    end
  end

  on_linux do
    if Hardware::CPU.intel?
      url "https://github.com/MSBrett/maenifold/releases/download/v1.0.0/maenifold-linux-x64.tar.gz"
      sha256 "PLACEHOLDER_LINUX_SHA256"
    end
  end

  def install
    bin.install "Maenifold" => "maenifold"
  end

  test do
    # Test that the binary runs and returns valid output
    output = shell_output("#{bin}/maenifold --tool MemoryStatus --payload '{}'")
    assert_match(/memory root/i, output)
  end

  def caveats
    <<~EOS
      To use maenifold, set the memory root directory:
        export maenifold_ROOT=~/maenifold

      For MCP integration with Claude Code, add to ~/.claude/config.json:
        {
          "mcpServers": {
            "maenifold": {
              "command": "maenifold",
              "args": ["--mcp"],
              "env": {"maenifold_ROOT": "~/maenifold"}
            }
          }
        }

      Documentation: https://github.com/MSBrett/maenifold
    EOS
  end
end
