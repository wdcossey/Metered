# How work runs

From the developer's shared ways of working (`~/.claude/ways-of-working`, adopted 2026-11-16). A unit of
work that needs no hands-on step runs from issue to merge without a check-in. Anything that needs the
developer stops and waits for them.

```
 ISSUE → WORKTREE → BUILD → SELF-REVIEW ×2 → PROVE THE TESTS → PR → GATE → MERGE → TIDY
```

## 1. Which issues run on their own

| Label | Meaning |
|---|---|
| `self-drive` | Can be taken from issue to merge without a hands-on step. |
| `hands-on` | Needs the developer; the reason is in an issue comment. |
| `blocked` | Waiting on an answer; not started. |

Priority `P0` to `P3`; type `feature`, `bug`, `chore`, `documentation`. The developer approves the
triage. A new issue is `hands-on` until triaged. An issue that is mostly self-drive with one hands-on step
is `self-drive`: the step is split into a follow-up issue when the work reaches it.

**Pick:** the highest-priority unblocked `self-drive` issue whose blockers are closed. For a batch, two or
three that don't touch the same code.

## 2. Ask first, always

Never without the developer's go-ahead in the conversation, whatever the label, a batch or away mode says:

- **Anything that takes over the developer's machine:** running the sample in a real window, a
  real-window driver, UI automation, screen capture of a real window, anything on the Pixel over ADB. When
  it is allowed, it goes through the device lease (`~/.claude/device-lease/lease.js`), which other
  projects' sessions share.
- **Tools that need a human:** debugger MCPs (x32dbg, x64dbg), Figma (its Base Gallery file blocks
  automated fetches), a real browser session (Claude in Chrome), VMs, phones.
- **Anything that leaves the repo's branches:** tags, releases, NuGet packages and ID reservations,
  publishing anything, repo settings, branch protection, deleting remote branches, rewriting history.
- **Commands that change the system:** installs (beyond `npm install`, `npm ci` and `dotnet restore` in a
  worktree or a clone of this repository), registry, services, stopping processes Claude didn't start.
- **Decisions that aren't Claude's:** a change to an accepted ADR's design (for example a Button theme
  pruned by kind, size and shape: `docs/performance/README.md`), a deviation from Base Web that Avalonia
  doesn't force, a new dependency, the public API's names where Base Web's don't map.
- **Hard rules no go-ahead lifts:**
  - Uber Move, or any other proprietary font, is never downloaded, saved, committed, embedded or copied;
    it is only measured as baseweb.design renders it.
  - No Uber or Base mark in Metered's own names; Base Web's names stay only where they are its API.
  - Requests to outside services carry nothing about the developer or the project: a generic user agent.

When a unit of work runs into one of these it doesn't try it: it finishes the rest, files the step as a
`hands-on` follow-up and lists it under **Hands-on follow-ups** in the PR.

## 3. Build

1. **Worktree** from an up-to-date `main`, one per branch, in `Metered-wt/` beside the main clone, outside
   the repository. From the main clone (the paths are relative to it):
   `git fetch origin && git worktree add --no-track ../Metered-wt/<slug> -b <type>/<slug> origin/main`,
   then `npm install` there; first push `git push -u origin HEAD`.
   **Every command runs in the worktree by absolute path:** a Bash call's working directory can reset to
   the main clone, so start each call with `cd <worktree> &&` or use `git -C <worktree>`; check
   `git rev-parse --show-toplevel` before a commit.
2. **Data CI doesn't have:** none is needed by the tests. The tools that measure baseweb.design
   (`tools/reference/precedence.js`, `fonts.js`) need the network and a headless Chromium; CI doesn't run
   them.
3. **Plan** in a comment on the issue before code when the work is more than a small change: the
   behaviours (they become the tests), Base Web's props and style functions they come from, what's left
   out and why, the files touched, any hands-on step. No human gate: carry on.
4. **Build** in small commits. Before every commit: `dotnet build Metered.sln` and `dotnet test
   Metered.sln`; when `tools/` or anything generated changes, also `node tools/tokens/gen.js --check` and
   `node tools/reference/cases.js --check`. `CLAUDE.md`'s rules hold throughout (generated files, the
   oracle, literal colours, the style budget).
5. **A control** (the inventory): one control per PR. Its plan lists Base Web's props and the style
   functions behind them; its oracle cases come from those functions (`tools/reference/`); it is compared
   with Base Web in headless frames beside baseweb.design's; and it gets `docs/controls/<Name>/` with what
   matches, what differs and why.
6. **Document** in the same PR: `CLAUDE.md` (it states only what exists), `docs/ROADMAP.md`, an ADR in
   `docs/decisions/` for a design decision, `docs/controls/<Name>/` for a control, and any deviation from
   Base Web with its measurement. A PR that lands a control or changes the sample visibly redraws the
   preview (`METERED_PREVIEW`, `tests/Metered.Tests/Sample/PreviewGallery.cs`) into `docs/preview/` and
   updates `PREVIEW.md`, in the same PR or its batch's roll-up.

## 4. Self-review (the gate before a PR is ready)

1. **Review, pass 1:** the `code-review` skill at high effort on the branch, uncapped:
   `high origin/main...<branch> --max-findings all`, run by an Opus agent. Fix each finding or waive it
   with a reason. **A finding can be wrong:** check it against the code and Base Web before acting.
2. **Review, pass 2:** the same on the fixed branch. Docs-only and chore PRs get one pass.
3. **Prove the tests can fail.** For each new test, break the behaviour it covers (on committed work, so a
   restore is safe), run it, see it go red, restore. `tools/breakage/run.js` does this in a fresh clone.
   A test for a bug must fail with the bug in place. Report the numbers.
4. **Clean run:** in a fresh clone of the pushed branch: `npm ci`, `dotnet build Metered.sln -c Release`,
   `dotnet test Metered.sln -c Release`, both `--check` runs.
5. **Screenshots** for anything visible: the headless frames (`METERED_FRAMES=1`), said to be headless.

## 5. The PR

- From the template, with `Closes #<issue>` under **What and why**.
- **Review:** what each pass found, fixed and waived, with reasons; the breakage numbers.
- **Screenshots:** pushed to the `pr-assets` branch (`pr/<issue>/<name>.png`), linked pinned to its commit:
  `https://github.com/wdcossey/Metered/blob/<sha>/pr/<issue>/<name>.png?raw=true`. It is an orphan branch
  that is never merged and holds only those pictures. If `origin/pr-assets` doesn't exist yet, make it in a
  separate worktree with `git switch --orphan pr-assets`, commit only the pictures, and push. Agents share
  that branch: fetch, `git rebase origin/pr-assets` (only the unpushed commit moves), push, and repeat on a
  rejected push.
- **For you to decide:** questions settled with the conservative choice, each with the alternative.
- **Hands-on follow-ups:** issues filed for the steps this PR couldn't do.
- **Proposed for shared files** (batches): exact changes wanted in files the agent mustn't touch.
- Not a draft: check `isDraft` after `gh pr create`; `gh pr ready` if needed.

The developer can dive in at any point and ask for changes or a live preview; that comes before merging.

## 6. Batches

Two or three units of work at once, each by its own agent in its own worktree; the session that starts the
batch coordinates and merges.

- **Agents run on Opus** (`model: "opus"` on every Agent call), briefed with `docs/agent-brief.md` and the
  issue, each with its own temp folder. They push and open their PR; they never merge, never ask the
  developer, and stop at an ask-first step.
- **The coordinator checks each PR** before merging: CI on the exact head commit, the files touched, the
  test and breakage numbers, the screenshots.
- **Report per batch:** PR, what changed, tokens (measured from the agent's usage, or an estimate said as
  one).

## 7. Merging

Claude merges its own PRs once **all** of these hold:

- The required checks (`build-test`, `generated`) are green on the PR's **head commit**
  (`gh pr view <n> --json headRefOid,statusCheckRollup`; `gh pr checks` alone can report an older run).
- The review passes §4 asks for (two, or one for docs and chores) are done and the PR says what they
  found.
- It isn't a draft, it touches only what its issue needs, and the developer hasn't asked to hold it.

**One at a time:** merge, then bring the next PR up to date (`gh pr update-branch <n>`, or merge
`origin/main` into it and push; no force-push); in its worktree `git pull` and check `git rev-parse HEAD`
equals the PR's `headRefOid`; wait for its checks on that head, merge. `main` is protected (since 2026-11-16): a PR, `build-test`
and `generated` green, the branch up to date, squash only, linear history, admins included; head
branches are deleted on merge. Still read `statusCheckRollup` on the head commit before merging, merge
with `gh pr merge <n> --squash --match-head-commit <sha>`, and don't use `gh pr merge --auto`: it stays
armed through `gh pr update-branch` and merges before any local re-run. **Counters** two PRs both bump from one base (the limits in
`StyleBudgetTests`) merge "cleanly" but wrong: re-bump on top. **Shared docs** (`CLAUDE.md`,
`docs/ROADMAP.md`) conflict the same way: merge `origin/main` in and fold both changes. After each merge:
`main`'s CI is green, the worktree is removed (`git worktree remove`), then its local branch
(`git branch -D` once the PR shows MERGED).
Avoid stacked PRs in a batch.

## 8. When the developer is away

Only when they say so. Batches go on, green PRs merge, open questions get the conservative choice, each
logged (what, options, choice, why) in one GitHub issue titled `Away <date>`, with an overview for their
return. §2 still holds.

## 9. Keeping the next session going

At the end of a session: a short project memory with what's in flight (branches, PRs, worktrees, agents)
and what's next. Traps worth keeping for every project go in `~/.claude/ways-of-working/LESSONS.md`, with
the project and date. The repo is the record: check `gh pr list`, `gh issue list` and `git worktree list`
before trusting a note.
