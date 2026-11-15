#!/bin/sh
# The file name of a frontend bundle, without its extension.
#
# A scheduled release names the version it publishes (chimera-csx-1.4.1), so
# the download matches the tag. The rolling dev build names its commit
# (chimera-csx-5f58a8fa), because its assets are replaced on every push and a
# version there would name something that is not a release.
#
# Usage: bundle-name.sh dev|release <short-commit>
set -eu
kind="$1"
short="$2"
case "$kind" in
release) echo "chimera-csx-$(tools/release-version.sh next)" ;;
dev) echo "chimera-csx-$short" ;;
*)
	echo "kind must be dev or release" >&2
	exit 2
	;;
esac
