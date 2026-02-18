#!/bin/bash

# 1. Ensure we are on the main branch first
git checkout main

2. Pull latest changes
git pull --rebase

# 3. Update local list of remote branches (optional but recommended)
git fetch -p

# 4. Delete all local branches except 'main'
# The grep -v command excludes the 'main' pattern
git branch | grep -v "main" | xargs -r git branch -d

echo "Cleanup complete. Only the 'main' branch remains."