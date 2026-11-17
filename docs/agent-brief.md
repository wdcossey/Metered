# Brief for an agent in a batch

Give each agent this file, its issue number, its worktree and its temp folder (`docs/PROCESS.md` §6).
Keep it up to date as mistakes are found, so that they aren't repeated.

You're building one Metered issue while one or two other agents build others. Read `CLAUDE.md` and
`docs/PROCESS.md` first, then follow PROCESS from §3 to §5. **You open the PR. You never merge.**

## Setup

1. Your worktree's absolute path is in your prompt. **Start every Bash call with `cd <worktree> &&`** (or
   `git -C <worktree>`): the working directory can reset to the developer's main clone between calls.
   Check `git rev-parse --show-toplevel` before each commit. Run `npm install` once. First push:
   `git push -u origin HEAD`.
2. No local-only data is needed. Don't run the tools that measure baseweb.design unless your issue says so.
3. Read the issue and its comments. Post a plan as an issue comment when the work is more than a small
   change.

## Rules, because others work at the same time

- **Your files are your issue's.** Stage them by name; never `git add -A` or `git add .`. Check
  `git status` before each commit.
- **Shared files:** `docs/PROCESS.md`, this brief, `.github/`,
  `Metered.sln`, `Directory.Build.*`, `Directory.Packages.props`, `global.json`, `package.json` and
  `package-lock.json`, `tools/tokens/gen.js`, `tools/reference/cases.js`, `tools/README.md`,
  `tests/Metered.Tests/Harness/`, `tests/Metered.Tests/Performance/StyleBudgetTests.cs`,
  `docs/spikes/README.md`, `docs/performance/README.md`. Don't change them unless your issue can't be
  done without it; then change only what it needs and say so. A change you'd *like* goes under
  **Proposed for shared files** in the PR, with the exact text.
- **Docs everyone edits** (`CLAUDE.md`, `docs/ROADMAP.md`): PROCESS §3 asks you to keep them true for your
  change. Change only your own rows and paragraphs, in their natural place, never appended at the end of a
  table or list, where two agents collide; the coordinator folds the rest at merge.
- **Temp files** in your own temp folder, never in the repo or a shared scratchpad.
- **Processes:** don't stop anything you didn't start. Other projects run on this machine.
- **Never** do anything on the ask-first list (PROCESS §2): no real window, no phone, no publishing. If your
  issue needs it, stop that part, file a `hands-on` follow-up with what's needed, and finish the rest.
- **Never run git in the main clone,** only in your worktree.
- **Don't ask the developer anything.** Take the conservative choice and list it under **For you to
  decide**.

## Getting it right

- **Read Base Web before writing a line:** its props, its style functions in
  `node_modules/baseui/<component>/styled-components.js`, and its scenarios, which the npm package
  doesn't ship: `src/<component>/__tests__/*.scenario.tsx` in `github.com/uber/baseweb` at commit `3adb0478c9`
  (`3adb0478c96569dc4e593ab39c24f36dc59e39f5`, the commit that set `package.json` from 18.1.0 to 18.2.0; the
  repository has no `v18.2.0` tag). Every value a
  test expects comes from Base Web: the oracle (`tools/reference/`), or a value read from its source with
  the file named (CLAUDE.md, rule 3). Never re-record an expected value from Avalonia's output.
- **Build the component's v2 if Base Web has one.** Metered reproduces Base Web's v2 controls (the developer,
  2026-11-17). Before you choose, read the component's docs page at that commit
  (`documentation-site/pages/components/`) for a deprecation notice, and look for a `<component>-v2` folder in
  `node_modules/baseui/`. #40 built v1 Checkbox, whose page says it's "about to be deprecated" in favour of
  Checkbox-v2 and Switch, and it had to be redone (#42). Also check the page for deprecated props.
- **Never hand-edit a generated file** (`CLAUDE.md`, rule 1): change the generator and run it, then both
  `--check` runs.
- **A part's look lives only in its own `Styled{Part}` theme** (ADR 0001); a pseudoclass style outranks a
  plain setter whatever the order.
- **No literal colours under `src/`** outside `src/Metered.Themes/Tokens/`; global style rules stay inside
  the budget, and a raise is said in the commit with the new total.
- **Uber Move is never downloaded, saved or embedded** (PROCESS §2).
- **LF line endings** everywhere; scripts in Node or `pwsh`, portable to Linux and macOS.
- **Traps already hit** in the developer's other projects: `~/.claude/ways-of-working/LESSONS.md`.
- **Numbers in the PR are measured,** not estimated: test counts, breakage counts, timings.
- **Review findings can be wrong.** Check each against the code and Base Web before acting.
- **A breakage list goes stale when review fixes change the code.** Run it again on the final head. A
  NOT APPLIED, NO BUILD, NO RESULT or TIMED OUT result proves nothing: fix that break or the run, and
  run it again before you report.
- **Finish PROCESS §4 before you hand back** (the merge in §7 is the coordinator's). Don't leave CI on
  your head, the clean run, the breakage run or a placeholder in the PR body for later. If you can't finish, say exactly what's left.
- **Never restore a file over uncommitted work:** commit first, then break things to prove the tests.
- **A build step skipped runs stale code** (`--no-build` after an edit): build, then test.

## Review and PR

- PROCESS §4 in full: two `code-review` passes (one for a docs-only or chore PR), proof the tests can fail, a
  clean run, screenshots.
- **Write the review level in the command,** with your branch's name:
  `<level> origin/main...<branch> --max-findings all`. The levels come from PROCESS §4's table:
  - `high` for pass 1 on a control, a generator or the oracle;
  - `high` for pass 1 on other behaviour, or `medium` for a small bug fix;
  - `medium` for pass 2, and for the one pass on a docs-only or chore PR.

  Record each pass's level and token count in the PR, or "not measured" if you can't see it. Never guess. The
  review-level trial is the coordinator's: don't run extra passes for it.
- **Preview pictures:** `PreviewGallery` draws every page. Point `METERED_PREVIEW` at a new folder in your temp
  folder, not at `docs/preview/`, and copy only your own page's `{light,dark}__<page>-*.png` into `docs/preview/`.
  Write your page's section of `PREVIEW.md`, in page order. Don't commit the `*__list-*.png` pictures or any other
  page's: every new page changes the list, so the coordinator redraws it after the batch.
- Push, `gh pr create` with the template and `Closes #<issue>`. Check it isn't a draft.
- **CI must be green on your exact commit:** `gh run list --commit <sha>`. Fix it if it's red.
- **Be honest in the PR:** the true test count, what wasn't tried, known gaps, every waived finding with its
  reason.

## Report back

Short: the PR link, the head commit, CI's result, the test count, the breakage numbers, each review pass's level,
findings and tokens ("not measured" if not visible), **For you to decide**, **Hands-on follow-ups** and
**Proposed for shared files**.
