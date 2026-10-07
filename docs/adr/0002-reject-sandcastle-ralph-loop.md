# Reject sandcastle and the RALPH autonomous loop, for now

We tried running an unattended, multi-iteration agent loop against this repo: `sandcastle`
(added in 2df846a, adapted to this .NET solution in 409efd4) drove a two-phase
implement-then-review cycle, letting an agent pick an open issue, implement it on its own
branch, and hand it to a second agent for review, repeating for up to ten issues per run
with no human in the loop between iterations. 0576bcf is the output of one such iteration.

That run's token consumption was disproportionate to the size of the change it produced.
The output itself wasn't the problem — the commit was fine — the cost of getting it was.
On a solo project with no token budget to spend experimenting, that's enough to stop here:
we reverted the sandcastle setup (91d19c3) rather than keep testing it further.

## Considered Options

Running the loop again with tighter guardrails (a lower `MAX_ITERATIONS`, a per-run token
cap) was an option, but that's more tuning work on top of a result that already looked
expensive for what it returned. Simpler to stop, work issues one at a time through an
interactive Claude Code session as usual, and park the idea.

## Scope

This rejects *unattended, multi-iteration* agent loops specifically (sandcastle/RALPH-style:
an agent that keeps going from issue to issue without a human checking in between).
It says nothing against using Claude Code itself, which remains how this project gets agent
assistance — one task at a time, with a human reviewing along the way.

## Revisiting

This is a decision about today's conditions, not a permanent ban. If token economics change
(cheaper models, better caching) or cost controls get added (a hard per-run spend cap), it's
worth trying again.
