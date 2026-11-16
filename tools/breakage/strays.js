// What a breakage run leaves running from its clone, and killing it (for run.js). A timeout kills only `dotnet`, not the test host it
// started, which would keep the clone's binaries locked and make every later break NO BUILD.
//
// A process is a stray when its executable or command line holds the clone's path. On Linux and macOS, never this process or one of its
// ancestors: run.js has the clone's path on its own command line, and so may whatever started it (a shell, a script). The candidates
// come from `pgrep -f`, which matches the full command line and leaves itself out; the ancestors from one `ps` snapshot. Neither runs in
// a shell, which would hold the path in its own command line. The path is matched literally, as itself or its real path, and only
// whole: `/tmp/clone` isn't `/tmp/clone2`. On Windows only this process is spared. `node tools/breakage/strays.test.js` tests it.
const { execFileSync, execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const read = (command, args) => execFileSync(command, args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });

// `pgrep` reads its pattern as an extended regular expression, and a path can hold `.`, `+` or brackets.
const literal = (text) => text.replace(/[\\^$.|?*+()[\]{}]/g, '\\$&');

// This process and every process above it, or null if the process table can't be read.
const ancestry = () => {
    let table;
    try {
        table = read('ps', ['-A', '-o', 'pid=', '-o', 'ppid=']);
    } catch {
        return null;
    }
    const parents = new Map(table.split('\n').map((line) => line.trim().split(/\s+/).map(Number)).filter(([pid]) => pid > 0));
    const pids = new Set();
    for (let pid = process.pid; pid > 0 && !pids.has(pid); pid = parents.get(pid) || 0) pids.add(pid);
    return pids;
};

const real = (where) => {
    try {
        return fs.realpathSync(where);
    } catch {
        return where;
    }
};

const killPosix = (where) => {
    const paths = [...new Set([where, real(where)])].map(literal);
    let listed;
    try {
        listed = read('pgrep', ['-f', '--', `(${paths.join('|')})(/| |$)`]);
    } catch (error) {
        // pgrep exits 1 when nothing matches.
        if (error.status !== 1) console.error(`Strays left running: pgrep failed (${error.code || `exit ${error.status}`}).`);
        return;
    }
    const spared = ancestry();
    if (!spared) {
        console.error('Strays left running: ps failed, so run.js could not tell its own ancestors from them.');
        return;
    }
    for (const pid of listed.split('\n').map(Number).filter((pid) => pid > 0 && !spared.has(pid))) {
        try {
            process.kill(pid, 'SIGTERM');
        } catch {
            // It has gone already.
        }
    }
};

const killStrays = (clone) => {
    const where = path.resolve(clone);
    if (process.platform !== 'win32') {
        killPosix(where);
        return;
    }
    try {
        const quoted = where.replace(/'/g, "''");
        execSync(`powershell -NoProfile -Command "Get-CimInstance Win32_Process | Where-Object { $_.ProcessId -ne ${process.pid} -and ($_.ExecutablePath -like '${quoted}*' -or $_.CommandLine -like '*${quoted}*') } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }"`, { stdio: 'ignore' });
    } catch {
        // Nothing left to kill.
    }
};

module.exports = { killStrays };
