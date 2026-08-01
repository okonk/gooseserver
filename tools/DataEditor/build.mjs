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
//
// FAILS, where every other missing input here only warns. The bundles are no longer committed
// (they embed the client's art), so "absent" went from meaning "something is badly wrong with
// your checkout" to being the state of every fresh clone — which makes a warning exactly the
// wrong shape. Editor.html include()s all three unconditionally, so a build that skipped them
// produces a dist/ that deploys and then fails in the sidebar, and the warning that said so
// scrolled past minutes earlier.
const missing = ['icons', 'parts', 'effects'].filter(
  (name) => !existsSync(join(here, `sprites-${name}.html`)));
if (missing.length) {
  console.error(
    `ERROR: missing sprite bundle(s): ${missing.map((n) => `sprites-${n}.html`).join(', ')}\n` +
    'They are gitignored — the client art they embed is not ours to redistribute — so a fresh\n' +
    'checkout has to build them against a client sprite tree:\n\n' +
    '    dotnet run --project tools/SpriteBundle -- <client-assets-dir> tools/DataEditor\n\n' +
    'See tools/README.md ("SpriteBundle").');
  process.exit(1);
}
for (const name of ['icons', 'parts', 'effects']) {
  await copyFile(join(here, `sprites-${name}.html`), join(dist, `sprites-${name}.html`));
}

// 4. Static files pass through.
for (const f of ['Code.gs', 'Editor.html', 'appsscript.json']) {
  if (existsSync(join(here, f))) await copyFile(join(here, f), join(dist, f));
}

console.log(`Built ${modules.length} modules into dist/`);
