// What a breakage run leaves running from its clone, and killing it (for run.js). A timeout kills only `dotnet`, not the test host it
// started, which would keep the clone's binaries locked and make every later break NO BUILD.
//
// A process is a stray when its executable or command line holds the clone's path, but never this process or one of its ancestors:
// run.js has the clone's path on its own command line, and so may whatever started it (a shell, a script). On Linux and macOS the
// candidates come from `pgrep -f`, which matches the full command line and leaves itself out; the ancestors from `ps`, walked up from
// this process. Neither runs in a shell, which would hold the path in its own command line. `node tools/breakage/strays.test.js` tests it.
const { execFileSync, execSync } = require('child_process');
const path = require('path');

// `pgrep` reads its pattern as an extended regular expression, and a path can hold `.`, `+` or brackets.
const literal = (text) => text.replace(/[\\^$.|?*+()[\]{}]/g, '\\$&');

// This process and every process above it, up to the first that has no parent to read.
const ancestry = () => {
    const pids = new Set([process.pid]);
    let pid = process.ppid;
    while (pid > 1 && !pids.has(pid)) {
        pids.add(pid);
        try {
            pid = Number(execFileSync('ps', ['-o', 'ppid=', '-p', String(pid)], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }).trim());
        } catch {
            break;
        }
    }
    return pids;
};

// The strays' process IDs, on Linux and macOS.
const strays = (clone) => {
    let listed;
    try {
        listed = execFileSync('pgrep', ['-f', '--', literal(path.resolve(clone))], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });
    } catch {
        // pgrep exits 1 when nothing matches.
        return [];
    }
    const spared = ancestry();
    return listed.split('\n').map(Number).filter((pid) => pid > 0 && !spared.has(pid));
};

const killStrays = (clone) => {
    const where = path.resolve(clone);
    if (process.platform === 'win32') {
        try {
            const quoted = where.replace(/'/g, "''");
            execSync(`powershell -NoProfile -Command "Get-CimInstance Win32_Process | Where-Object { $_.ProcessId -ne ${process.pid} -and ($_.ExecutablePath -like '${quoted}*' -or $_.CommandLine -like '*${quoted}*') } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }"`, { stdio: 'ignore' });
        } catch {
            // Nothing left to kill.
        }
        return;
    }
    for (const pid of strays(where)) {
        try {
            process.kill(pid, 'SIGTERM');
        } catch {
            // It has gone already.
        }
    }
};

module.exports = { killStrays, strays };
