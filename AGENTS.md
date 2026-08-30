# AGENTS.md

Instructions for AI coding agents contributing to LeafSweeper — a Godot 4.7
(C# / .NET 8) Android puzzle game. This file is the operating manual: it holds
everything an agent needs every session. For the rationale behind the workflow,
the validation levels and a troubleshooting table, read
[`docs/agentic-development.md`](docs/agentic-development.md) once before the
first dev task; when the two disagree, this file wins.

## Validate after every change (in this order)

```sh
dotnet build                                   # 1. compile
godot --headless --import                      # 2. import assets/scenes
godot --headless --quit-after 180              # 3. boot smoke test
LEAF_AUTOPLAY=1 godot --headless --quit-after 4700   # 4. gameplay self-test
```

- The binary on this machine is `godot-mono` (NixOS wrapper), not plain `godot`.
- A fresh worktree needs `--import` before level 4, and autoplay **silently
  exits 0 with no output** without it — so does a `--quit-after` frame
  budget that runs out before the ~2.5s settle finishes (autoplay prints
  its first line only once the floor is dressed). Always grep for
  `AUTOPLAY` in the output; never trust the exit code alone; on missing
  lines rerun with a bigger budget — autoplay self-quits on pass or fail.
- `LEAF_AUTOPLAY` resets the save and asserts a full round-trip; exit 0 on
  pass only after `AUTOPLAY` output is seen.
- Visual slices: headless is blind. Temporarily add a `LEAF_SHOT=<path>` hook
  in `Main._Ready`, run **windowed** (headless has no framebuffer), inspect
  the PNG, then remove the hook before committing.

## Cleanup after every Godot run (the tree dirties itself)

```sh
git checkout -- LeafSweeper.csproj; rm -f LeafSweeper.csproj.old
git diff --ignore-all-space --stat   # must be empty before committing; discard whitespace-only churn (repo standard is 4-space indent)
```

A new script's generated `.cs.uid` is committed together with the script.

## Workflow

Start each dev task here; the *why* behind every step lives in
[`docs/agentic-development.md`](docs/agentic-development.md).

1. Plan first, code second — planned work tracked as todos; one todo = one
   vertical slice. No drive-by refactors of unrelated code.
2. Implement each slice in a dedicated git worktree on a short-lived branch,
   never the main checkout (it carries human's in-progress edits):
   ```sh
   git worktree add ../LeafSweeper-<slice> -b <slice>
   # validate + atomic commits there, then push the branch and open a PR
   # (push/PR only when the human asks — see rule 3):
   git -c credential.helper= -c http.sslVerify=false push \
     "https://nickmartin1ee7:$(gh auth token)@github.com/<owner>/<repo>.git" <slice>
   gh pr create --base main --head <slice>
   # once the PR is merged on GitHub (pull/fetch may need the same token URL):
   git pull; git worktree remove ../LeafSweeper-<slice>; git branch -d <slice>
   ```
   **Hard rule — PRs only:** `main` advances exclusively through GitHub PRs.
   Never `git merge` a slice branch into `main` locally and never push
   directly to `main`.
   **Hard rule for agent sessions:** even when your session's working
   directory *is* the main checkout, do not edit repo files there —
   create the worktree before the first edit and work only inside it.
   If edits already landed in the main checkout, recover before
   continuing: dump the changed files to a patch in a persistent path
   (`git diff -- <files> > ~/persistent/slice.patch`), `git worktree
   add` a slice worktree, `git apply` the patch there, then
   `git checkout -- <files>` in the main checkout so it returns to its
   pre-session state. Verify with `git status` — only the human's own
   pre-existing edits may remain.
   Merged branches are never left behind.
3. Commit atomically — and only when the human asks: an agent session never
   commits, pushes or opens a PR on its own initiative; it hands off the
   slice green and uncommitted on its branch. When a commit is requested: one
   logical change per commit; message leads with the change, body explains
   the *why*; `.uid`/`.import` metadata in its own commit.
4. Docs live with code: behavior changes update `README.md` and `docs/*` in
   the same slice (numbers in prose drift fast).
5. Stop at "buildable and headlessly verified" and hand off to the human for
   playtesting; their findings become new todos. Every handoff must end by
   asking the human whether to open a PR and merge it — don't make them ask.
6. The final steps of every slice are to create a PR and merge it: push the
   slice branch, open a PR against `main` (`gh pr create --base main --head
   <slice>`), and merge that PR on GitHub (`gh pr merge` or the GitHub UI) —
   never locally. A slice is only finished once its PR is merged and the
   worktree and branch are cleaned up; `main` never advances any other way
   (PR-only hard rule in step 2). Push, PR and merge happen only on the
   human's explicit yes to the rule-5 handoff question (rule 3).

## Code practices

- Scene tree is code-built: `scenes/Main.tscn` is a one-node shell; all nodes
  are constructed in `Main.BuildTree()`. Don't hand-edit `.tscn` state.
- Difficulty tuning is pure functions of level number in
  `scripts/RoundConfig.cs` — playtest findings map to one named constant.
- Feel tunables (sweep radius, friction, etc.) are named `const`s with
  comments explaining the feel they produce.
- Reused nodes must clear old children before adding new ones:
  `Setup()` = teardown + build.
- Z-order is an explicit ladder declared in `Main.BuildTree`, never
  tree-order luck; UI lives on a `CanvasLayer`, which `ZIndex` cannot beat.

## Environment constraints

- .NET target is pinned to `net8.0` (local SDK is 8.0.x). Don't bump it
  alone.
- `git-remote-https` is broken on this machine (`CURL_OPENSSL_4` error).
  Push by injecting the `gh` token into the URL:
  ```sh
  git -c credential.helper= -c http.sslVerify=false push \
    "https://nickmartin1ee7:$(gh auth token)@github.com/<owner>/<repo>.git" <branch>
  ```
- `/tmp` is volatile between agent tool calls — keep scratch artifacts in
  persistent paths.
- Keystores, Android SDK and JDK paths stay out of the repo (editor settings
  or `keystore.env`, which is gitignored).

  ## Agentic workflow (mandatory, TODO-tracked)

  **CRITICAL: Every workflow step below MUST be tracked in the session SQL todos table. No step is considered complete until its TODO is marked `done`.**

  The following workflow complements the repository-specific guidance above. Each step corresponds to a session TODO that enforces progress tracking and audibility:

  1) **PRE-REQ: Track todos** — Create a session TODO for every step below. Update todos as work progresses.
     - TODO id: `agents-01-track-todos`
     - This step is meta: you are doing it now by reading this file.

  2) **PRE-REQ: Work from worktree** — Create a dedicated git worktree branch before editing.
     - TODO id: `agents-02-use-worktree`
     - Never edit the main checkout; commit only in the worktree.

  3) **ORIENT: Inventory & docs** — List the project tree and read docs/ to identify touch points.
     - TODO id: `agents-03-orient-inventory`
     - Capture affected files and required doc updates.

  4) **ORIENT: Verify tooling** — Ensure all required tools (dotnet, godot, gh, tests) work.
     - TODO id: `agents-04-verify-tooling`
     - Confirm versions and pinning; document any constraints.

  5) **DESIGN: Write a plan** — Commit a short plan and test strategy.
     - TODO id: `agents-05-plan`
     - Include acceptance tests and rollback approach.

  6) **DESIGN: Research & verify** — Search authoritative docs; cite and verify all claims.
     - TODO id: `agents-06-research`
     - No assumptions; all facts linked to online references.

  7) **DEVELOP: Atomic commits** — Implement changes with small, focused commits; update docs inline.
     - TODO id: `agents-07-develop-atomic`
     - One logical change per commit; messages explain the *why*.

  8) **DEVELOP: Run tests** — Execute and pass all tests that exercise the change.
     - TODO id: `agents-08-test`
     - Document test commands and outcomes in the PR.

  9) **REVIEW: Subagent review** — Launch a high-reasoning review agent for independent inspection.
     - TODO id: `agents-09-review-subagent`
     - The reviewer must NEVER attempt to execute the Godot engine (no
       `godot`/`godot-mono` runs — no import, boot, or autoplay). Runtime and
       build validation is already done by the validating session; trust it.
     - Review focus: code quality, maintainability, and reliability only —
       correctness of logic, readability, structure, edge cases, and error
       handling as visible in the diff and surrounding code.
     - Reviewer reports PASS/FAIL with summary of critical findings.

  10) **GATE: Iterate on FAIL** — If reviewer returns FAIL, loop back to #7, address issues, update TODOs.
      - TODO id: `agents-10-gate-on-fail`
      - Do not proceed until PASS (#GATE#).

  11) **GATE: Proceed on PASS** — If reviewer returns PASS, prepare for merge.
      - TODO id: `agents-11-gate-on-pass`
      - Unblock merge actions (#GATE#).

  12) **CLEANUP: Create and merge PR** — Open a GitHub PR from the worktree branch and merge via GitHub (not locally).
      - TODO id: `agents-12-merge`
      - PR title and description include test results and review summary.
      - **REQUIRED: Merge via GitHub UI or `gh` CLI, never with `git merge` locally.**
      - After GitHub merge, sync the worktree branch locally: `git fetch origin main && git rebase origin/main`.

  13) **CLEANUP: Delete worktree** — Remove the local worktree and slice branch after remote merge is confirmed.
      - TODO id: `agents-13-delete-worktree`
      - Clean: `git worktree remove <path> && git branch -d <slice>`.
      - Verify on GitHub that the slice branch is deleted by GitHub's post-merge cleanup, or delete it manually: `git push origin --delete <slice>`.

  14) **IMPROVE: Update docs** — Capture improvements back into AGENTS.md or docs/.
      - TODO id: `agents-14-improve-docs`
      - Share lessons with future agentic sessions.

  ### Subtasks and discoveries

  As work progresses, new TODOs may be discovered and added to the session todos table under the guided workflow. All subtasks must:
  - Be linked to a parent workflow step (via todo_deps if dependencies exist).
  - Use clear, actionable titles (gerund form: "Fixing authentication").
  - Include enough detail that the task can be executed without external context.

  ### NixOS git push guidance

  When pushing from this machine, inject the gh auth token into the URL to avoid libcurl compatibility failures:

  ```sh
  git -c credential.helper= -c http.sslVerify=false push \
    "https://nickmartin1ee7:$(gh auth token)@github.com/owner/repo.git" branch
  ```

  Do not commit tokens; use gh CLI at push time.

