// Tests strays.js on real processes: `node tools/breakage/strays.test.js`. Exits 1 on a failure. CI runs it on Linux.
//
// It makes a folder standing in for a breakage run's clone, `clone.v1+x`, and on Linux and macOS a symbolic link to it, and starts a chain: two
// links with the path given (the symbolic link's) on their command lines, as run.js has and a shell that started it may, then a runner. The
// runner starts two strays, one with the path given and one with the clone's real path, and two bystanders: `cloneXv11x`, which
// `clone.v1+x` matches as a pattern, and `clone.v1+x2`, which it holds. It kills the strays and reports. Every link has to survive, and
// the runner; both strays have to go; the bystanders have to stay. On Windows the branch spares only the runner, not its ancestors, and
// matches a prefix, so the chain there has no links and no `clone.v1+x2`.
const { execFileSync, spawn } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');

const WAIT = 20000;
const [mode, ...rest] = process.argv.slice(2);

// A process that waits until it's killed, with `where` on its command line. It says when it has started.
const idle = (where) => spawn(process.execPath, ['-e', 'console.log("ready"); setInterval(() => {}, 1000)', path.join(where, 'testhost.dll')], { stdio: ['ignore', 'pipe', 'ignore'] });

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

const bystanders = (real) => {
    const named = { 'a pattern would match it': path.join(path.dirname(real), 'cloneXv11x') };
    if (process.platform !== 'win32') named['its path starts with the clone\'s'] = `${real}2`;
    return named;
};

const runner = async (given, real) => {
    const { killStrays } = require('./strays');
    const strays = [idle(given), idle(real)];
    const named = bystanders(real);
    const spared = Object.values(named).map(idle);
    const all = [...strays, ...spared];
    try {
        await Promise.all(all.map(ready));
        killStrays(given);
        const [killed, stayed] = await Promise.all([
            Promise.all(strays.map((child) => gone(child, 5000))),
            // Long enough for a wrongly killed bystander to be seen to exit.
            Promise.all(spared.map(async (child) => !(await gone(child, 1000)))),
        ]);
        console.log(JSON.stringify({ survived: 'runner', killed, spared: Object.fromEntries(Object.keys(named).map((name, at) => [name, stayed[at]])) }));
    } finally {
        for (const child of all) child.kill('SIGKILL');
    }
};

// Whatever of the chain is still running, should a broken strays.js have killed the runner before it stopped what it started. On Linux
// and macOS, everything with the test's folder on its command line, as its name, from mkdtemp, is this run's alone and holds nothing a
// pattern reads specially; on Windows, where the runner is the chain's only process, its tree.
const stopLeftovers = (root, child) => {
    try {
        if (process.platform === 'win32') {
            if (child.exitCode === null && child.signalCode === null) execFileSync('taskkill', ['/T', '/F', '/PID', String(child.pid)], { stdio: 'ignore' });
            return;
        }
        const listed = execFileSync('pgrep', ['-f', '--', path.basename(root)], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });
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
    const real = path.join(root, 'clone.v1+x');
    fs.mkdirSync(real);
    const posix = process.platform !== 'win32';
    const given = posix ? path.join(root, 'linked') : real;
    if (posix) fs.symlinkSync(real, given);
    const depth = posix ? 2 : 0;
    const start = depth > 0 ? ['link', String(depth - 1)] : ['runner'];
    const child = spawn(process.execPath, [__filename, ...start, given, fs.realpathSync(real)], { stdio: ['ignore', 'pipe', 'inherit'] });
    let text = '';
    child.stdout.on('data', (chunk) => {
        text += chunk;
    });
    const timer = setTimeout(() => {
        stopLeftovers(root, child);
        child.kill('SIGKILL');
    }, WAIT);
    // On close, not exit, so that all it printed has been read.
    child.on('close', (code, signal) => {
        clearTimeout(timer);
        stopLeftovers(root, child);
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
            ['the stray with the path given was killed', Boolean(report) && report.killed[0]],
            [`the stray with the ${posix ? 'real path (through the link)' : 'path again'} was killed`, Boolean(report) && report.killed[1]],
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
