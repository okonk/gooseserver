import fs from 'node:fs';
import vm from 'node:vm';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const here = path.dirname(fileURLToPath(import.meta.url));

// The REAL bundles, loaded the same way the Apps Script HTML service will: the fragments are
// plain <script> tags that assign into GOOSE_SPRITES. A toy fixture cannot tell us whether the
// clip names in clipCandidates exist, or that the icon index really does group 4,827 keys into
// 125 sheet files, so the tests that ask those questions run against these.
//
// NULL WHEN THEY ARE NOT BUILT, rather than throwing. The bundles embed the client's art and are
// gitignored, so a fresh clone has none — and a top-level readFileSync would then fail at IMPORT
// time, taking down the ~90 tests in these two files that need no bundle at all, with a stack
// trace rather than a reason. Callers pass `skipWithoutBundles` to the tests that do need them.
export const realBundles = (() => {
  const ctx = vm.createContext({});
  for (const name of ['icons', 'parts', 'effects']) {
    const file = path.join(here, '..', `sprites-${name}.html`);
    if (!fs.existsSync(file)) return null;
    vm.runInContext(fs.readFileSync(file, 'utf8').replace(/<\/?script>/g, ''), ctx);
  }
  return ctx.GOOSE_SPRITES;
})();

// Spread into a test's options: `test('...', { ...skipWithoutBundles }, () => {…})`. Skipping is
// the honest outcome — the assertion is about atlas contents this checkout does not have — and it
// says so by name in the runner output instead of passing silently.
export const skipWithoutBundles = realBundles
  ? {}
  : { skip: 'sprite bundles not built in this checkout (gitignored; see tools/README.md)' };
