// Exports the wizard step definitions of the OpenAstroTech Firmware Config website
// (https://github.com/OpenAstroTech/OpenAstroTracker-Firmware-Config-Web) to a single JSON file
// that OATControl embeds, so both use the same source of truth.
//
// Every "condition" string is also exported as "conditionTree", parsed by the website's own
// parser.js, so OATControl only has to evaluate the trees and never re-implements the parser.
//
// Usage: node export-steps.mjs <path-to-config-web-repo> <output.json>

import fs from 'fs';
import path from 'path';
import { pathToFileURL } from 'url';

const [webRoot, outFile] = process.argv.slice(2);
if (!webRoot || !outFile) {
    console.error('Usage: node export-steps.mjs <path-to-config-web-repo> <output.json>');
    process.exit(1);
}

const load = (rel) => import(pathToFileURL(path.join(webRoot, 'src', 'modules', rel)).href);

const { getStepsForTracker } = await load('configurations/index.js');
const { Defaults } = await load('configurations/base/sharedDefaults.js');
const { parseExpression } = await load('parser.js');

const toPlain = (expr) => (typeof expr === 'object' && expr !== null)
    ? { lhs: toPlain(expr.lhs), op: expr.op, rhs: toPlain(expr.rhs) }
    : expr;

const addConditionTrees = (node) => {
    if (Array.isArray(node)) {
        node.forEach(addConditionTrees);
    } else if (typeof node === 'object' && node !== null) {
        for (const value of Object.values(node)) {
            addConditionTrees(value);
        }
        if (typeof node.condition === 'string') {
            node.conditionTree = toPlain(parseExpression(node.condition));
        }
    }
};

const trackers = {};
for (const tracker of ['OAT', 'OAM', 'OAE']) {
    const steps = getStepsForTracker(tracker);
    addConditionTrees(steps);
    trackers[tracker] = steps;
}

let sourceVersion = '';
try {
    sourceVersion = JSON.parse(fs.readFileSync(path.join(webRoot, 'package.json'), 'utf8')).version;
} catch (e) {
    // Version is informational only.
}

const result = { sourceVersion, defaults: Defaults, trackers };
fs.writeFileSync(outFile, JSON.stringify(result, null, 2) + '\n');
console.log(`Wrote ${outFile}: ` + Object.entries(trackers).map(([k, v]) => `${k}=${v.length} steps`).join(', '));
