// Generate a concept graph from [[WikiLinks]] in Markdown/MDX
import { readdirSync, readFileSync, writeFileSync, statSync, mkdirSync } from 'fs';
import { resolve, relative } from 'path';

const siteDir = resolve(process.cwd());
const pagesDir = resolve(siteDir, 'src/pages');
const outDir = resolve(siteDir, 'public');
const outFile = resolve(outDir, 'graph-data.json');

function walk(dir) {
  const files = [];
  for (const name of readdirSync(dir)) {
    const p = resolve(dir, name);
    const st = statSync(p);
    if (st.isDirectory()) files.push(...walk(p));
    else if (/\.(md|mdx|markdown)$/i.test(name)) files.push(p);
  }
  return files;
}

function parseWikiLinks(text) {
  const links = new Set();
  const re = /\[\[([^\]]+)\]\]/g;
  let m;
  while ((m = re.exec(text))) {
    const concept = m[1].trim();
    if (concept) links.add(concept);
  }
  return [...links];
}

const files = walk(pagesDir);
const nodeSet = new Map(); // concept -> { id, count }
const linkMap = new Map(); // key a||b -> weight

for (const file of files) {
  const raw = readFileSync(file, 'utf8');
  const concepts = parseWikiLinks(raw);
  // Count nodes
  for (const c of concepts) {
    const cur = nodeSet.get(c) || { id: c, count: 0 };
    cur.count += 1;
    nodeSet.set(c, cur);
  }
  // Fully connect co-mentioned concepts within this file
  for (let i = 0; i < concepts.length; i++) {
    for (let j = i + 1; j < concepts.length; j++) {
      const a = concepts[i];
      const b = concepts[j];
      const [x, y] = a < b ? [a, b] : [b, a];
      const k = x + '||' + y;
      linkMap.set(k, (linkMap.get(k) || 0) + 1);
    }
  }
}

const nodes = [...nodeSet.values()].map(n => ({ id: n.id, val: Math.min(6, 2 + Math.log2(1 + n.count)) }));
const links = [...linkMap.entries()].map(([k, w]) => {
  const [source, target] = k.split('||');
  return { source, target, value: w };
});

mkdirSync(outDir, { recursive: true });
writeFileSync(outFile, JSON.stringify({ nodes, links }, null, 2));

console.log(`Generated ${nodes.length} nodes and ${links.length} links from ${files.length} pages -> ${relative(siteDir, outFile)}`);
