import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const editor = join(here, '..', 'DataEditor');
const dist = join(here, 'dist');

const SOURCES = [
  { key: 'illutia', label: 'Illutia (released)', sheetId: '1Ig7u4XHc1Vjk4Y1502bwHEVEDba3JTCUcrKwrcOPWyQ',
    gids: { items: '1321358734', classes: '1321498466' } },
  { key: 'aspereta', label: 'Aspereta', sheetId: '1O2mbze7WGIt2JLeqDctR1zFSL6CdaNhf7iZlaqE4ieU',
    gids: { items: '1018454407', classes: '2002802832' } },
];

const { Sets } = await import('../DataEditor/src/sets.js');

function parseCsv(text) {
  const rows = [];
  let row = [];
  let cell = '';
  let quoted = false;
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (quoted) {
      if (ch === '"' && text[i + 1] === '"') { cell += '"'; i++; }
      else if (ch === '"') quoted = false;
      else cell += ch;
    } else if (ch === '"') quoted = true;
    else if (ch === ',') { row.push(cell); cell = ''; }
    else if (ch === '\n' || ch === '\r') {
      if (ch === '\r' && text[i + 1] === '\n') i++;
      row.push(cell); rows.push(row); row = []; cell = '';
    } else cell += ch;
  }
  if (cell !== '' || row.length) { row.push(cell); rows.push(row); }
  return rows;
}

function records(text) {
  const [header, ...rows] = parseCsv(text);
  const names = header.map((h) => h.replace(/\s*\(.*\)\s*$/, '').trim());
  return rows.map((r) => Object.fromEntries(names.map((h, i) => [h, r[i] ?? ''])));
}

async function sheet(source, name) {
  const url = `https://docs.google.com/spreadsheets/d/${source.sheetId}/export?format=csv&gid=${source.gids[name]}`;
  const res = await fetch(url);
  if (!res.ok) throw new Error(`fetching ${name}: HTTP ${res.status} from ${url}`);
  return res.text();
}

const missing = ['parts'].filter((n) => !existsSync(join(editor, `sprites-${n}.html`)));
if (missing.length) {
  console.error(
    'ERROR: tools/DataEditor/sprites-parts.html is missing. Build it against a client checkout:\n\n' +
    '    dotnet run --project tools/SpriteBundle -- <client-assets-dir> tools/DataEditor\n\n' +
    'See tools/README.md ("SpriteBundle").');
  process.exit(1);
}

async function load(source) {
  const classes = Object.fromEntries(
    records(await sheet(source, 'classes'))
      .filter((c) => parseInt(c.id, 10) > 0)
      .map((c) => [parseInt(c.id, 10), c.name]));

  const itemRows = records(await sheet(source, 'items'))
    .filter((r) => parseInt(r.id, 10) > 0)
    .map((r) => ({
      id: r.id, name: r.name, slot: r.slot, graphic: r['equip display'],
      r: r.r, g: r.g, b: r.b, a: r.a,
      classes: r.classes, minLevel: r['min lvl'],
    }));

  const autoSets = Sets.detect(itemRows, classes).map((s) => ({ ...s, id: `${source.key}:${s.id}` }));
  for (const s of autoSets) s.variants = s.variants.map((v) => `${source.key}:${v}`);
  const items = itemRows.map(Sets.normalizeItem).filter(Boolean).map(({ words, ...rest }) => rest);

  return {
    key: source.key,
    label: source.label,
    url: `https://docs.google.com/spreadsheets/d/${source.sheetId}`,
    classes,
    items,
    autoSets,
  };
}

const sources = [];
for (const source of SOURCES) sources.push(await load(source));

const data = { generatedAt: new Date().toISOString(), sources };

const wrap = (code) => `<script>\n${code.replaceAll('</script', '<\\/script')}\n</script>\n`;

const modules = [];
for (const f of ['equipped', 'appearance', 'sprites', 'preview', 'sets']) {
  modules.push(wrap(await readFile(join(editor, 'src', `${f}.js`), 'utf8')));
}
modules.push(wrap(`var SET_DATA = ${JSON.stringify(data)};`));
modules.push(wrap(await readFile(join(here, 'src', 'app.js'), 'utf8')));

const template = await readFile(join(here, 'page.template.html'), 'utf8');
const bundle = await readFile(join(editor, 'sprites-parts.html'), 'utf8');
const html = template
  .replace('<!-- SPRITES -->', () => bundle)
  .replace('<!-- MODULES -->', () => modules.join(''));

await mkdir(dist, { recursive: true });
await writeFile(join(dist, 'set-browser.html'), html);
await writeFile(join(dist, 'sets.json'), JSON.stringify(
  sources.map(({ key, label, url, classes, autoSets }) => ({ key, label, url, classes, autoSets })), null, 2));

for (const s of sources) console.log(`${s.label}: ${s.items.length} wearable items, ${s.autoSets.length} detected sets`);
console.log('-> dist/set-browser.html, dist/sets.json');
