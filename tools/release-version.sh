#!/bin/bash
# The frontend's release tags are semantic versions: 0.1.0 is the first,
# 1.0.0 is the fork cut, and a minor version marks a major feature. A release
# is dispatched by hand when its batch is in (user-decided, 2026-11-15).
#
#   release-version.sh last   the newest release tag, or nothing
#   release-version.sh next   the tag this build publishes: the newest tag when
#                             it already names HEAD (a minor tagged by hand, or
#                             a rerun finishing a release that exists empty),
#                             the next patch otherwise
#
# A major feature bumps the minor by hand: tag the commit, then dispatch.
set -eu

last=$(git tag -l '[0-9]*' --sort=-v:refname | head -1)

case "${1:-last}" in
last)
	printf '%s\n' "$last"
	;;
next)
	if [ -z "$last" ]; then
		echo 0.1.0
		exit 0
	fi
	if [ "$(git rev-parse "$last^{commit}")" = "$(git rev-parse HEAD)" ]; then
		echo "$last"
		exit 0
	fi
	major=${last%%.*}
	rest=${last#*.}
	minor=${rest%%.*}
	patch=${rest#*.}
	case "$major$minor$patch" in
		'' | *[!0-9]*) echo "the newest release tag ($last) is not a semantic version" >&2; exit 1 ;;
	esac
	echo "$major.$minor.$((10#$patch + 1))"
	;;
*)
	echo "usage: release-version.sh last|next" >&2
	exit 2
	;;
esac
