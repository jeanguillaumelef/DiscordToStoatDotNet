# Channel Links are stored in Stoat, not in local state

The Bridge records each Channel Link on the Stoat side: the Discord ID is the first line of a Mirror Channel's description, and the ID is part of a Mirror Category's title. The Bridge keeps no database or state file, so a redeploy loses nothing and any Stoat admin can see which Source a Mirror belongs to.

## Considered Options

A local database or state file would be more robust, because an admin editing a description or title cannot break a Link. We accepted that risk to keep the Bridge stateless. Reconcile treats Discord as the source of truth, so a broken Link is repaired by creating a new Mirror.
