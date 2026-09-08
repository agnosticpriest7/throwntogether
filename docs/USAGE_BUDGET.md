# Thrown Together — AI Usage Budget

## Goal
Approximately **50% of the owner's monthly OpenAI/Codex development allowance** may be dedicated to Thrown Together.

Treat this as a soft project budget. Do not assume the agent can directly read account-wide monthly usage.

## Allocation
- **45%** normal project development
- **5%** emergency / late-month reserve

The human owner may explicitly override this policy.

## Budget bands
- **0–30%:** normal development.
- **30–40%:** optimize aggressively; reserve Astra for high-value work.
- **40–45%:** high-value work only; avoid broad exploration/refactors.
- **45–50%:** emergency reserve for blockers, regressions, build failures, or finishing nearly complete milestones.
- **50%+:** stop non-critical project work until reset unless explicitly overridden.

## Model routing
Use the least expensive model/reasoning level likely to succeed reliably.

### GPT-6 Astra
Use for major architecture, employee/customer AI, complex Unity scene work, difficult debugging, major refactors, visual/spatial judgment, and multi-system integration.

Reasoning:
- Light/Low: inspection and trivial MCP operations.
- Medium: normal substantive development.
- High: only when complexity or repeated failure justifies it.

### Cheaper capable models
Prefer for repetitive data/content, straightforward UI wiring, ScriptableObject creation, routine tests, file moves/renames, simple refactors, and documentation cleanup.

## Efficiency rules
Before substantial work:
1. Define narrow acceptance criteria.
2. Inspect before editing.
3. Reuse existing systems.
4. Avoid speculative scope.
5. Do not rebuild working systems without strong reason.
6. Stop when acceptance criteria are met.

After major milestones, record actual known usage in `USAGE_LEDGER.md`. Never invent usage values.
