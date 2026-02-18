#!/bin/bash

# 1. Ensure we are on the main branch first
# Note: change 'main' to 'master' if your repo uses the older naming convention
git checkout main

# 2. Update local list of remote branches (optional but recommended)
git fetch -p

# 3. Delete all local branches except 'main'
# The grep -v command excludes the 'main' pattern
git branch | grep -v "main" | xargs -r git branch -d

echo "Cleanup complete. Only the 'main' branch remains."