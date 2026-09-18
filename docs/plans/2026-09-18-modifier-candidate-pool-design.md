# Modifier Candidate Pool Design

## Selection behavior

Keep the existing outer title and surname rolls. For each successful outer roll, inspect every modifier that applies to the item and independently roll its `chance`. Collect the successful modifiers, return no modifier when the pool is empty, and otherwise select one candidate uniformly. Titles and surnames remain separate pools, so an item can receive at most one of each.

The `chance` field represents the probability that a modifier enters the candidate pool, not its final probability of being selected. A modifier with chance `1.0` always enters the pool but does not prevent other modifiers from entering or winning.

## Verification

Add focused tests showing that a single partial-chance modifier can produce both a modifier and no result, and that multiple guaranteed candidates can each win while only one is applied per pool.

Repair the pre-existing help integration tests by deriving expected visible section counts from the real command registry and player privileges instead of hardcoded totals. This preserves their access-filtering assertions while allowing legitimate command additions.
