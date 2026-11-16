// Tests strays.js on real processes: `node tools/breakage/strays.test.js`. Exits 1 on a failure. CI runs it on Linux.
//
// It makes a folder standing in for a breakage run's clone, `clone.v1+x`, and a sibling its name matches as a pattern, `cloneXv11x`, and
// starts a chain: two links, each with the clone's path on its command line (as run.js has, and a shell that started it may), then a
// runner. The runner starts two strays with the clone's path on their command lines and a bystander with the sibling's, kills the strays
// and reports. Every link has to survive, and the runner; both strays have to go; the bystander has to stay, as the path is matched
// literally. On Windows the branch spares only the runner, not its ancestors, so the chain there has no links.
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

const alive = (pid) => {
    try {
        process.kill(pid, 0);
        return true;
    } catch {
        return false;
    }
};

// Starts the next link, or the runner, and passes on what it prints; then says it survived.
const link = (depth, clone, sibling) => {
    const next = depth > 0 ? ['link', String(depth - 1), clone, sibling] : ['runner', clone, sibling];
    const child = spawn(process.execPath, [__filename, ...next], { stdio: ['ignore', 'pipe', 'inherit'] });
    child.stdout.pipe(process.stdout, { end: false });
    child.on('close', (code) => {
        console.log(JSON.stringify({ survived: `link ${depth}` }));
        process.exitCode = code === null ? 1 : code;
    });
};

const runner = async (clone, sibling) => {
    const { killStrays } = require('./strays');
    const strays = [idle(clone), idle(clone)];
    const bystander = idle(sibling);
    const all = [...strays, bystander];
    try {
        await Promise.all(all.map(ready));
        console.log(JSON.stringify({ started: all.map((child) => child.pid) }));
        killStrays(clone);
        const killed = await Promise.all(strays.map((child) => gone(child, 5000)));
        console.log(JSON.stringify({ survived: 'runner', killed, bystander: alive(bystander.pid) }));
    } finally {
        for (const child of all) {
            try {
                child.kill('SIGKILL');
            } catch {
                // It has gone already.
            }
        }
    }
};

// When the runner was killed before it could stop what it started: those of its processes still running from the test's folder, so
// that a process ID taken since by another is left alone. The folder's name, from mkdtemp, holds nothing a pattern reads specially.
const stopLeftovers = (root, started) => {
    if (process.platform === 'win32' || started.length === 0) return;
    let listed = '';
    try {
        listed = execFileSync('pgrep', ['-f', '--', path.basename(root)], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });
    } catch {
        // None left.
    }
    const running = new Set(listed.split('\n').map(Number));
    for (const pid of started.filter((pid) => running.has(pid))) {
        try {
            process.kill(pid, 'SIGKILL');
        } catch {
            // It has gone already.
        }
    }
};

const main = () => {
    const root = fs.mkdtempSync(path.join(os.tmpdir(), 'metered-strays-'));
    const clone = path.join(root, 'clone.v1+x');
    const sibling = path.join(root, 'cloneXv11x');
    fs.mkdirSync(clone);
    fs.mkdirSync(sibling);
    const depth = process.platform === 'win32' ? 0 : 2;
    const links = depth > 0 ? ['link', String(depth - 1)] : ['runner'];
    const child = spawn(process.execPath, [__filename, ...links, clone, sibling], { stdio: ['ignore', 'pipe', 'inherit'] });
    let text = '';
    child.stdout.on('data', (chunk) => {
        text += chunk;
    });
    const timer = setTimeout(() => child.kill('SIGKILL'), WAIT);
    // On close, not exit, so that all it printed has been read.
    child.on('close', (code, signal) => {
        clearTimeout(timer);
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
        if (!report) stopLeftovers(root, lines.flatMap((line) => line.started || []));
        fs.rmSync(root, { recursive: true, force: true });
        const checks = [
            ['the runner survived', Boolean(report)],
            ...Array.from({ length: depth }, (_, at) => [`link ${at} survived (an ancestor with the path)`, survived.has(`link ${at}`)]),
            ['the chain exited cleanly', code === 0 && signal === null],
            ['both strays were killed', Boolean(report) && report.killed.every(Boolean)],
            ['the bystander was spared (the path is literal)', Boolean(report) && report.bystander],
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
