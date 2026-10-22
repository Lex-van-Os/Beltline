# Beltline

# Code style and workflow

### Think Before Coding
Don't assume. Don't hide confusion. Surface tradeoffs.

Before implementing code:
- If available: Read and understand the linked issue and understand its acceptance criteria.
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.
- Refer to the documentation of the technologies that you work with when given a task.
- Verify that you've got the most up-to-date context on a specific technology that you work with, by consulting the documentation.
- Refer to the documentation when working with a specific framework / technology. Adhere to the mentioned best practice.
- Refer to the documentation of the Beltline project (README.md) to properly understand the project context and workflow.

### Simplicity First
Minimum code that solves the problem. Nothing extra that is speculative.

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.
- Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.
- Make use of the KISS principle.

### Required conventions
Make use of the following required conventions when implementing code:

- No one letter variables. Prefer writing out variable names, instead of shorthands (i.e.: ex -> exception).

### Surgical Changes
Touch only what you must. Clean up only your own mess.

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.
- Prerequisites for changes: Every changed line should trace directly to the request that you were given.

### Goal-Driven Execution
Define success criteria. Loop until verified.

Transform tasks into verifiable goals. Examples:
- Input: "Add validation" → "Write tests for invalid inputs, then make them pass".
- Input: Fix the bug" → "Write a test that reproduces it, then make it pass".
- Input: "Refactor X" → "Ensure tests pass before and after".

For multi-step tasks, state a brief plan:
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

### Architectural decisions
Beltline records significant decisions as ADRs. The index and the rules for what warrants one are in [`docs/architecture/architecture-decisions.md`](docs/architecture/architecture-decisions.md); the records live in [`docs/architecture/adr/`](docs/architecture/adr/).

- Before proposing an architecturally-significant change (project structure, dependencies, hosting/deployment topology, cross-cutting qualities), read the index and any relevant ADR.
- For a decision that warrants one, propose a new ADR (copy `adr/0000-madr-template.md`) — don't make the decision silently.
- Already defined constraints can be found in the `docs/architecture/adr/` folder.

### Completing work
Make use of best practices regarding finishing work and source control actions when you finish work. Everything should be clear and concise.

Before committing code:
- Before committing and pushing code: Verify that no new issues arise and execute the necessary CI commands to verify the correctness of implemented code.
- After creating a pull request: Verify that the CI script passes, and fix any recurring problems before completing a task in the case that CI scripts fail.
- Check if any changes have to be made to project documentation. README.md files should serve to make the context of projects understandable to both humans and agents.

When using source control:
- Don't just blindly commit changes. Commit when you have a coherent, reviewable unit of work.
- Make use of the Conventional Commits specification.
- When creating a pull request, make use of the pull request template that is found in the .github folder.
- Link the created pull request to the GitHub issue, and populate the pull request with a fitting fixes or closing statement.

### Project specific instructions
Beltline consists of multiple .NET projects in `src/` and `tests/`. Before working in one of these projects, check whether it contains its own CLAUDE.md file. If it does, consult it and follow its project specific instructions in addition to this file.

---
These guidelines are working if: fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

#### Credits
This CLAUDE.md file structure is based and expanded on the Karpathy-inspired CLAUDE.md file as defined by `forrestchang`.

[The original CLAUDE.md file](https://github.com/multica-ai/andrej-karpathy-skills/blob/main/CLAUDE.md)
