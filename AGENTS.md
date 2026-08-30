# AGENTS.md

Instructions for AI coding agents contributing to LeafSweeper — a Godot 4.7
(C# / .NET 8) Android puzzle game. The full workflow rationale lives in
[`docs/agentic-development.md`](docs/agentic-development.md); this file is the
operational summary.

## Validate after every change (in this order)

```sh
dotnet build                                   # 1. compile
godot --headless --import                      # 2. import assets/scenes
godot --headless --quit-after 180              # 3. boot smoke test
LEAF_AUTOPLAY=1 godot --headless --quit-after 300   # 4. gameplay self-test
```

- The binary on this machine is `godot-mono` (NixOS wrapper), not plain `godot`.
- A fresh worktree needs `--import` before level 4, and autoplay **silently
  exits 0 with no output** without it — always grep for `AUTOPLAY` in the
  output; never trust the exit code alone.
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

1. Plan first, code second — planned work tracked as todos; one todo = one
   vertical slice. No drive-by refactors of unrelated code.
2. Implement each slice in a dedicated git worktree on a short-lived branch,
   never the main checkout (it carries human's in-progress edits):
   ```sh
   git worktree add ../LeafSweeper-<slice> -b <slice>
   # validate + atomic commits there, then:
   git merge <slice>
   git worktree remove ../LeafSweeper-<slice>; git branch -d <slice>
   ```
   Merged branches are never left behind.
3. Commit atomically: one logical change per commit; message leads with the
   change, body explains the *why*; `.uid`/`.import` metadata in its own
   commit.
4. Docs live with code: behavior changes update `README.md` and `docs/*` in
   the same slice (numbers in prose drift fast).
5. Stop at "buildable and headlessly verified" and hand off to the human for
   playtesting; their findings become new todos.

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
