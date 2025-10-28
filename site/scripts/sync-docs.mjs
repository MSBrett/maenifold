// Sync top-level README and docs/assets into the Astro site structure
import { promises as fs, constants as FS_CONSTANTS } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const siteRoot = resolve(__dirname, '..'); // /site
const repoRoot = resolve(siteRoot, '..');  // repo root

const mappings = [
  { from: resolve(repoRoot, 'README.md'), to: resolve(siteRoot, 'src/pages/start.mdx'), transform: 'landing' },
  { from: resolve(repoRoot, 'assets'), to: resolve(siteRoot, 'public/assets') },
  { from: resolve(repoRoot, 'src/assets/usage'), to: resolve(siteRoot, 'src/pages/usage') },
];

async function existsReadable(p) {
  try { await fs.access(p, FS_CONSTANTS.R_OK); return true; } catch { return false; }
}

async function copyRecursive(src, dest) {
  if (fs.cp) {
    await fs.cp(src, dest, { recursive: true, force: true });
    return;
  }
  const st = await fs.stat(src);
  if (st.isDirectory()) {
    await fs.mkdir(dest, { recursive: true });
    for (const entry of await fs.readdir(src)) {
      await copyRecursive(resolve(src, entry), resolve(dest, entry));
    }
  } else {
    await fs.mkdir(dirname(dest), { recursive: true });
    await fs.copyFile(src, dest);
  }
}

function selfCloseVoidTags(html) {
  // Protect fenced code blocks
  const codeBlocks = [];
  const protectedHtml = html.replace(/```[\s\S]*?```/g, (block) => {
    codeBlocks.push(block);
    return `__CODEBLOCK_PLACEHOLDER_${codeBlocks.length - 1}__`;
  });

  const voidTags = [
    'area','base','br','col','embed','hr','img','input',
    'link','meta','param','source','track','wbr'
  ];
  const re = new RegExp(`<(${voidTags.join('|')})(\\b[^>]*)?>`, 'gi');
  let out = protectedHtml.replace(re, (match, tag, attrs = '') => {
    if (/\/\s*>$/.test(match)) return match; // already self-closed
    return `<${tag}${attrs} />`;
  });

  // Restore code blocks
  out = out.replace(/__CODEBLOCK_PLACEHOLDER_(\d+)__/g, (_, idx) => codeBlocks[Number(idx)]);
  return out;
}

async function run() {
  for (const m of mappings) {
    if (!(await existsReadable(m.from))) {
      console.warn(`[sync-docs] Skipping missing source: ${m.from}`);
      continue;
    }

    if (m.transform === 'landing') {
      let raw = await fs.readFile(m.from, 'utf8');
      raw = selfCloseVoidTags(raw);
      const transformed = `---\nlayout: ../layouts/Landing.astro\n---\n\n${raw}`;
      await fs.mkdir(dirname(m.to), { recursive: true });
      await fs.writeFile(m.to, transformed, 'utf8');
      continue;
    }

    await copyRecursive(m.from, m.to);
  }
}

run().catch(err => { console.error('[sync-docs] Failed:', err); process.exit(1); });
