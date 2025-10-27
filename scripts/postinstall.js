#!/usr/bin/env node

const fs = require('fs');
const path = require('path');
const os = require('os');

console.log('Setting up Maenifold...');

// Ensure binary has correct permissions on Unix-like systems
if (os.platform() !== 'win32') {
  const platform = os.platform();
  const arch = os.arch();

  let subDir = '';
  if (platform === 'darwin') {
    subDir = arch === 'arm64' ? 'osx-arm64' : 'osx-x64';
  } else if (platform === 'linux') {
    subDir = 'linux-x64';
  }

  const binaryPath = path.join(__dirname, '..', 'bin', subDir, 'Maenifold');

  if (fs.existsSync(binaryPath)) {
    try {
      fs.chmodSync(binaryPath, '755');
      console.log('✓ Binary permissions set');
    } catch (err) {
      console.warn('Warning: Could not set binary permissions:', err.message);
    }
  }
}

// Create default Maenifold directory if it doesn't exist
const defaultRoot = path.join(os.homedir(), 'maenifold');
if (!fs.existsSync(defaultRoot)) {
  try {
    fs.mkdirSync(defaultRoot, { recursive: true });
    fs.mkdirSync(path.join(defaultRoot, 'memory'), { recursive: true });
    console.log(`✓ Created default directory: ${defaultRoot}`);
  } catch (err) {
    console.warn('Warning: Could not create default directory:', err.message);
  }
}

console.log('\nMaenifold installation complete!');
console.log('\nUsage:');
console.log('  CLI mode:  npx maenifold --tool memorystatus --payload \'{}\'');
console.log('  MCP mode:  npx maenifold --mcp');
console.log('\nFor MCP configuration, add to your Claude Desktop config:');
console.log(JSON.stringify({
  "mcpServers": {
    "maenifold": {
      "command": "npx",
      "args": ["maenifold", "--mcp"],
      "env": {
        "MAENIFOLD_ROOT": "~/maenifold"
      }
    }
  }
}, null, 2));
