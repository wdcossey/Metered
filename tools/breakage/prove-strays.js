// TEMPORARY (removed before merge): breaks strays.js in a copy, one break at a time, and checks strays.test.js goes red.
const { spawnSync } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');

const source = fs.readFileSync(path.join(__dirname, 'strays.js'), 'utf8');
const test = fs.readFileSync(path.join(__dirname, 'strays.test.js'), 'utf8');

// The code before #18's fix, word for word.
const pkill = [
    '        try {',
    "            execSync(`pkill -f -- '${where.replace(/'/g, \"'\\\\''\")}'`, { stdio: 'ignore' });",
    '        } catch {',
    '            // Nothing left to kill.',
    '        }',
    '        return;',
].join('\n');

const breaks = [
    ['the bug in place: pkill -f through a shell', '        killPosix(where);\n        return;', pkill],
    ['spares nothing', '!spared.has(pid)', 'true'],
    ['spares only this process', 'const spared = ancestry();', 'const spared = new Set([process.pid]);'],
    ['spares only this process and its parent', 'for (let pid = process.pid; pid > 0 && !pids.has(pid); pid = parents.get(pid) || 0) pids.add(pid);', 'pids.add(process.pid).add(process.ppid);'],
    ['the path as a pattern', '.map(literal);', ';'],
    ['the path unbounded', "(${paths.join('|')})(/| |$)", "(${paths.join('|')})"],
    ['the real path left out', '[where, real(where)]', '[where]'],
    ['kills nothing', "process.kill(pid, 'SIGTERM');", 'process.kill(pid, 0);'],
];

const baseline = spawnSync(process.execPath, [path.join(__dirname, 'strays.test.js')], { encoding: 'utf8' });
console.log(`baseline: exit ${baseline.status}\n${baseline.stdout}`);
let caught = 0;
for (const [name, from, to] of breaks) {
    if (!source.includes(from)) {
        console.log(`NOT APPLIED  ${name}\n`);
        continue;
    }
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'prove-strays-'));
    fs.writeFileSync(path.join(dir, 'strays.js'), source.replace(from, () => to));
    fs.writeFileSync(path.join(dir, 'strays.test.js'), test);
    const result = spawnSync(process.execPath, [path.join(dir, 'strays.test.js')], { encoding: 'utf8', timeout: 60000 });
    fs.rmSync(dir, { recursive: true, force: true });
    const red = result.status !== 0;
    if (red) caught += 1;
    console.log(`${red ? 'caught' : 'MISSED'}       ${name}: exit ${result.status}\n${result.stdout}${result.stderr}`);
}
console.log(`${caught} of ${breaks.length} caught on ${process.platform}`);
process.exitCode = baseline.status === 0 && caught === breaks.length ? 0 : 1;
