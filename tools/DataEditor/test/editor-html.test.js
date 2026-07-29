import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

// Editor.html is the one file nothing else can test: it is only ever evaluated by Apps Script.
// The failure mode it invites is silent — a module added to src/ but not to the include list
// simply never loads, and the first symptom is a ReferenceError in a deployed sidebar.

const here = dirname(fileURLToPath(import.meta.url));
const root = join(here, '..');
const html = readFileSync(join(root, 'Editor.html'), 'utf8');

const included = [...html.matchAll(/include\('([^']+)'\)/g)].map((m) => m[1]);

test('every src module is included, and every include exists', () => {
  const modules = readdirSync(join(root, 'src'))
    .filter((f) => f.endsWith('.js'))
    .map((f) => f.replace(/\.js$/, ''));

  const missing = modules.filter((m) => !included.includes(m));
  assert.deepEqual(missing, [], 'src modules absent from Editor.html — they would never load');

  // The generated files build.mjs copies through alongside the wrapped modules.
  const generated = ['schema', 'sprites-icons', 'sprites-parts', 'sprites-effects'];
  const stale = included.filter((n) => !modules.includes(n) && !generated.includes(n));
  assert.deepEqual(stale, [], 'Editor.html includes a file build.mjs does not emit');
});

test('no module is included twice', () => {
  assert.equal(new Set(included).size, included.length);
});

test('includes are ordered so a module never precedes the data it reads at load time', () => {
  // Modules resolve each other as free globals at CALL time, so their relative order is free.
  // The generated data files are different: they assign GOOSE_SCHEMA and GOOSE_SPRITES at
  // parse time, and App.init() reads GOOSE_SCHEMA synchronously, so both must precede app.
  assert.ok(included.indexOf('schema') < included.indexOf('app'));
  assert.ok(included.indexOf('sprites-icons') < included.indexOf('app'));
});

test('every element id app.js looks up exists in the markup', () => {
  const app = readFileSync(join(root, 'src', 'app.js'), 'utf8');
  const ids = [...app.matchAll(/getElementById\('([^']+)'\)/g)].map((m) => m[1]);
  assert.ok(ids.length >= 9, 'expected app.js to look up the shell elements');

  const declared = new Set([...html.matchAll(/id="([^"]+)"/g)].map((m) => m[1]));
  const absent = [...new Set(ids)].filter((id) => !declared.has(id));
  assert.deepEqual(absent, [], 'app.js reads an element Editor.html does not declare');
});
