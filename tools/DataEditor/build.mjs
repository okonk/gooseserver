import { readdir, readFile, writeFile, mkdir, copyFile, rm } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { dirname, join, basename } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const src = join(here, 'src');
const dist = join(here, 'dist');

// Rebuild from scratch. Nothing here is incremental — every output is rewritten
// unconditionally — and a stale dist/foo.html from a deleted src/foo.js would go
// unnoticed, since dist/ is gitignored, until it double-defined a global at runtime.
await rm(dist, { recursive: true, force: true });
await mkdir(dist, { recursive: true });

// An HTML parser ends a <script> block at the first literal `</script`, wherever it
// appears — including inside a JS string or comment, where it would silently truncate
// the module. `\/` collapses to `/` in string and regex literals, so the escape is
// invisible there. The two exceptions are String.raw templates, which preserve the
// backslash, and a `<` immediately followed by a regex literal starting `script`.
const wrap = (code) => `<script>\n${code.replaceAll('</script', '<\\/script')}\n</script>\n`;

// 1. Wrap each pure-JS module in <script> tags. Apps Script has no .js file type.
let modules = [];
if (existsSync(src)) modules = (await readdir(src)).filter((f) => f.endsWith('.js')).sort();
else console.warn('WARNING: src/ missing — no modules to build');

for (const file of modules) {
  const code = await readFile(join(src, file), 'utf8');
  const name = basename(file, '.js');
  const banner = `// Built from src/${file}. Do not edit.`;
  await writeFile(join(dist, `${name}.html`), wrap(`${banner}\n${code}`));
}

// 2. Part 2 emits schema.js, which also needs wrapping.
const schema = join(here, 'schema.js');
if (existsSync(schema)) {
  await writeFile(join(dist, 'schema.html'), wrap(await readFile(schema, 'utf8')));
} else {
  console.warn('WARNING: schema.js missing — run tools/SchemaGen first');
}

// 3. Sprite bundles are already <script>-wrapped by Part 2.
for (const name of ['icons', 'parts', 'effects']) {
  const from = join(here, `sprites-${name}.html`);
  if (existsSync(from)) await copyFile(from, join(dist, `sprites-${name}.html`));
  else console.warn(`WARNING: sprites-${name}.html missing — run tools/SpriteBundle first`);
}

// 4. Static files pass through.
for (const f of ['Code.gs', 'Editor.html', 'appsscript.json']) {
  if (existsSync(join(here, f))) await copyFile(join(here, f), join(dist, f));
}

console.log(`Built ${modules.length} modules into dist/`);
