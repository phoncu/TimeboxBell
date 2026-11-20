
<!-- BACKLOG.MD GUIDELINES START -->
<!-- backlog.md-instructions-version: 1.55.3 -->
<CRITICAL_INSTRUCTION>

## Backlog.md Workflow

This project uses Backlog.md for task and project management.

**At the beginning of each conversation in this project, run `backlog instructions overview` before answering or taking action. Re-read it only if you have not read it yet in the current conversation.**

Use the overview to decide whether to search, read, create, or update Backlog tasks.

Before task lifecycle actions, read the matching detailed guide:
- `backlog instructions task-creation` before creating or splitting tasks
- `backlog instructions task-execution` before planning, changing status or assignee, adding a plan or implementation notes, or implementing task work
- `backlog instructions task-finalization` before checking acceptance criteria, writing final summaries, or moving tasks to terminal statuses

Use `backlog <command> --help` before running unfamiliar commands. Help shows options, fields, and examples.

Do not edit Backlog task, draft, document, decision, or milestone markdown files directly. Use the `backlog` CLI so metadata, relationships, and history stay consistent.

</CRITICAL_INSTRUCTION>
<!-- BACKLOG.MD GUIDELINES END -->

## Project rules

Rules the user asked for live here (not in agent memory), so they apply on every machine and to every agent.

- **Fast-forward only.** No merge commits. Integrate `topic` into `main` only with `git merge --ff-only topic`; if the branches diverged, rebase `topic` onto `main` first (or ask). Local `git config merge.ff only` / `pull.ff only` is recommended.
- **Keep README.md in sync.** Whenever a change affects user-facing behavior, usage, settings (`appsettings.json` keys), CLI switches, exit codes or build/publish/test commands, update README.md in the same change without being asked, and mention it in the summary. README is in English; code comments, commits and Backlog stay Czech.
- **New rules go here.** When the user asks you to remember a rule or preference for this project, add it to this section (in both CLAUDE.md and AGENTS.md), not to agent memory.
