#!/usr/bin/env node

/**
 * Maenifold NPM wrapper script
 * Detects platform and runs the appropriate binary
 */

const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');
const os = require('os');

function getBinaryPath() {
  const platform = os.platform();
  const arch = os.arch();

  let binaryName = 'Maenifold';
  let subDir = '';

  if (platform === 'darwin') {
    subDir = arch === 'arm64' ? 'osx-arm64' : 'osx-x64';
  } else if (platform === 'linux') {
    subDir = 'linux-x64';
  } else if (platform === 'win32') {
    subDir = 'win-x64';
    binaryName = 'Maenifold.exe';
  } else {
    console.error(`Unsupported platform: ${platform}`);
    process.exit(1);
  }

  const binaryPath = path.join(__dirname, '..', 'bin', subDir, binaryName);

  // Fallback to generic binary if platform-specific not found
  if (!fs.existsSync(binaryPath)) {
    const fallbackPath = path.join(__dirname, '..', 'bin', binaryName);
    if (fs.existsSync(fallbackPath)) {
      return fallbackPath;
    }
    console.error(`Binary not found: ${binaryPath}`);
    console.error('Please run: npm run build');
    process.exit(1);
  }

  return binaryPath;
}

function main() {
  const binaryPath = getBinaryPath();
  const args = process.argv.slice(2);

  const child = spawn(binaryPath, args, {
    stdio: 'inherit',
    env: process.env
  });

  child.on('error', (err) => {
    console.error('Failed to start maenifold:', err);
    process.exit(1);
  });

  child.on('exit', (code) => {
    process.exit(code ?? 0);
  });
}

main();
