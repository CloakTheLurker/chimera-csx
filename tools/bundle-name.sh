#!/bin/sh
# The file name of a frontend bundle, without its extension:
# chimera-csx-<version>, the version the release's tag names.
set -eu
echo "chimera-csx-$(tools/release-version.sh next)"
