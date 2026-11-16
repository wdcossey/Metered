// Tests strays.js on real processes: `node tools/breakage/strays.test.js`. Exits 1 on a failure. CI runs it on Linux.
//
// It makes a folder standing in for a breakage run's clone, `clone.v1+x`, and on Linux and macOS a symbolic link to it, and starts a
// chain: two links with the path given (the symbolic link's) on their command lines, as run.js has and a shell that started it may,
// then a runner. The runner starts strays and bystanders, each a process with one argument, kills the strays and reports. Every link
// has to survive, and the runner; every stray has to go; every bystander has to stay. On Windows the branch spares only the runner, not
// its ancestors, and matches the path anywhere (#24), so the chain there has no links, and the bystanders only the pattern's.
const { execFileSync, spawn } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');

const WAIT = 20000;
const posix = process.platform !== 'win32';
const [mode, ...rest] = process.argv.slice(2);

// Each with the argument it runs with.
const strays = (given, real) => ({
    'the path given': path.join(given, 'testhost.dll'),
    [posix ? 'the real path (through the link)' : 'the path again']: path.join(real, 'testhost.dll'),
    'the path in an option, before a `;`': `-p:Root=${given};x`,
});
const bystanders = (real) => ({
    'a pattern would match it (`cloneXv11x`)': path.join(path.dirname(real), 'cloneXv11x', 'testhost.dll'),
    ...(posix ? {
        'its path starts with the clone\'s (`clone.v1+x2`)': path.join(`${real}2`, 'testhost.dll'),
        'its path ends with the clone\'s': path.join(path.dirname(real), 'outer') + path.join(real, 'testhost.dll'),
    } : {}),
});

// A process that waits until it's killed, with `argument` on its command line. It says when it has started.
const idle = (argument) => spawn(process.execPath, ['-e', 'console.log("ready"); setInterval(() => {}, 1000)', '--', argument], { stdio: ['ignore', 'pipe', 'ignore'] });

const ready = (child) => new Promise((resolve, reject) => {
    child.stdout.once('data', () => resolve());
    child.once('exit', () => reject(new Error('it exited before it started')));
});

const gone = (child, within) => new Promise((resolve) => {
    if (child.exitCode !== null || child.signalCode !== null) return resolve(true);
    const timer = setTimeout(() => resolve(false), within);
    child.once('exit', () => {
        clearTimeout(timer);
        resolve(true);
    });
});

// Starts the next link, or the runner, and passes on what it prints; then says it survived.
const link = (depth, given, real) => {
    const next = depth > 0 ? ['link', String(depth - 1), given, real] : ['runner', given, real];
    const child = spawn(process.execPath, [__filename, ...next], { stdio: ['ignore', 'pipe', 'inherit'] });
    child.stdout.pipe(process.stdout, { end: false });
    child.on('close', (code) => {
        console.log(JSON.stringify({ survived: `link ${depth}` }));
        process.exitCode = code === null ? 1 : code;
    });
};

const runner = async (given, real) => {
    const { killStrays } = require('./strays');
    const start = (named) => Object.entries(named).map(([name, argument]) => [name, idle(argument)]);
    const doomed = start(strays(given, real));
    const spared = start(bystanders(real));
    const all = [...doomed, ...spared].map(([, child]) => child);
    try {
        await Promise.all(all.map(ready));
        killStrays(given);
        const [killed, stayed] = await Promise.all([
            Promise.all(doomed.map(([, child]) => gone(child, 5000))),
            // Long enough for a wrongly killed bystander to be seen to exit.
            Promise.all(spared.map(async ([, child]) => !(await gone(child, 1000)))),
        ]);
        const named = (list, results) => Object.fromEntries(list.map(([name], at) => [name, results[at]]));
        console.log(JSON.stringify({ survived: 'runner', killed: named(doomed, killed), spared: named(spared, stayed) }));
    } finally {
        for (const child of all) child.kill('SIGKILL');
    }
};

// Whatever of the chain is still running, should a broken strays.js have killed the runner before it stopped what it started:
// everything with the test's folder on its command line. Its name, from mkdtemp, is this run's alone and holds nothing a pattern or a
// wildcard reads specially.
const stopLeftovers = (root) => {
    const name = path.basename(root);
    try {
        if (!posix) {
            execFileSync('powershell', ['-NoProfile', '-Command', `Get-CimInstance Win32_Process | Where-Object { $_.ProcessId -ne $PID -and $_.CommandLine -like '*${name}*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }`], { stdio: 'ignore' });
            return;
        }
        const listed = execFileSync('pgrep', ['-f', '--', name], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });
        for (const pid of listed.split('\n').map(Number).filter((pid) => pid > 0 && pid !== process.pid)) {
            try {
                process.kill(pid, 'SIGKILL');
            } catch {
                // It has gone already.
            }
        }
    } catch {
        // None left.
    }
};

const main = () => {
    const root = fs.mkdtempSync(path.join(os.tmpdir(), 'metered-strays-'));
    const made = path.join(root, 'clone.v1+x');
    fs.mkdirSync(made);
    const real = fs.realpathSync(made);
    const given = posix ? path.join(root, 'linked') : made;
    if (posix) fs.symlinkSync(made, given);
    const depth = posix ? 2 : 0;
    const start = depth > 0 ? ['link', String(depth - 1)] : ['runner'];
    const child = spawn(process.execPath, [__filename, ...start, given, real], { stdio: ['ignore', 'pipe', 'inherit'] });
    let text = '';
    child.stdout.on('data', (chunk) => {
        text += chunk;
    });
    const timer = setTimeout(() => {
        stopLeftovers(root);
        child.kill('SIGKILL');
    }, WAIT);
    // On close, not exit, so that all it printed has been read.
    child.on('close', (code, signal) => {
        clearTimeout(timer);
        stopLeftovers(root);
        fs.rmSync(root, { recursive: true, force: true });
        // A link killed mid-write leaves half a line.
        const lines = text.split('\n').flatMap((line) => {
            try {
                return [JSON.parse(line)];
            } catch {
                return [];
            }
        });
        const survived = new Set(lines.map((line) => line.survived));
        const report = lines.find((line) => line.survived === 'runner');
        const checks = [
            ['the runner survived', Boolean(report)],
            ...Array.from({ length: depth }, (_, at) => [`link ${at} survived (an ancestor with the path)`, survived.has(`link ${at}`)]),
            ['the chain exited cleanly', code === 0 && signal === null],
            ...Object.keys(strays(given, real)).map((name) => [`a stray was killed: ${name}`, Boolean(report) && report.killed[name] === true]),
            ...Object.keys(bystanders(real)).map((name) => [`a bystander was spared: ${name}`, Boolean(report) && report.spared[name] === true]),
        ];
        for (const [name, ok] of checks) console.log(`${ok ? 'ok    ' : 'FAILED'}  ${name}`);
        const failed = checks.filter(([, ok]) => !ok).length;
        console.log(failed === 0 ? `${checks.length} of ${checks.length} passed on ${process.platform}` : `${failed} of ${checks.length} failed on ${process.platform}`);
        process.exitCode = failed === 0 ? 0 : 1;
    });
};

if (mode === 'link') link(Number(rest[0]), rest[1], rest[2]);
else if (mode === 'runner') runner(rest[0], rest[1]).catch((error) => {
    console.error(error);
    process.exitCode = 1;
});
else main();
