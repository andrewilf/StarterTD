#!/bin/bash
set -e

FEATURE_BRANCH=$(git branch --show-current)

git checkout main
git pull
git checkout "$FEATURE_BRANCH"
git rebase main
