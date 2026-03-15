---
name: create-branch-and-set-upstream
description: Creates or switches to a Git branch, refreshes `main` before branching from it, and configures upstream tracking. Use when the user wants a new branch created, an existing branch checked out, or a branch's remote tracking set or fixed.
---

When invoked, follow this workflow:

1. Gather context with `git branch --show-current`, `git status --short`, `git remote -v`, and `git branch -vv`.
2. Resolve missing inputs before changing anything:
   - If the branch name is not provided, ask for it.
   - Default the base branch to the current branch unless the user names another base branch.
   - Default the remote to `origin` when it exists; otherwise ask which remote to use.
3. Protect the worktree:
   - If there are uncommitted changes and switching branches may carry them into the new branch, explain the risk and ask before switching.
   - Do not stash, reset, clean, or discard changes unless the user explicitly asks.
4. Determine whether the branch already exists:
   - Check local branches with `git branch --list <branch-name>`.
   - Check remote-tracking branches with `git branch -r --list <remote>/<branch-name>`.
5. Refresh `main` before creating a new branch from it:
   - If the branch does not already exist and it will be created from `main`, make sure local `main` matches the latest remote `main` before branching.
   - Verify local `main` exists. If it does not, ask before creating it from the remote `main` branch.
   - Determine `main`'s upstream with `git rev-parse --abbrev-ref main@{upstream}`.
   - If `main` has no upstream, prefer `<remote>/main` when it exists; otherwise ask which remote branch should be treated as canonical `main`.
   - Fetch the remote `main` branch, switch to local `main`, and fast-forward it with `git merge --ff-only <upstream>`.
   - If the fetch/auth/network step is blocked by the sandbox, request escalation and retry.
   - If `main` cannot be fast-forwarded cleanly, stop and explain the problem instead of creating the new branch from a stale base.
6. Choose the correct branch command:
   - New local branch from the current/base branch: `git switch -c <branch-name>` or `git switch -c <branch-name> <base-branch>`.
   - Existing local branch: `git switch <branch-name>`.
   - Remote-tracking branch exists but the local branch does not: `git switch --track <remote>/<branch-name>`.
7. Configure upstream:
   - For a new branch that needs to be published: `git push -u <remote> <branch-name>`.
   - For an existing local branch with a matching remote branch but no upstream: `git branch --set-upstream-to=<remote>/<branch-name> <branch-name>`.
   - If the branch already tracks a different upstream, show the current tracking target and ask before changing it.
8. Confirm the result with `git branch -vv` and report the current branch, upstream branch, and whether the push or upstream command succeeded.
9. Use non-interactive Git commands only.
