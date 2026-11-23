#!/usr/bin/env python3
"""Measure and repair the shipped themes' contrast (docs/theming.md).

Every pair here is a place the frontend draws something on something else, and
the rule is the one ThemeContrastTests holds the built-in Dark theme to, raised
to WCAG AA for text:

    4.5:1  text a person reads
    3.0:1  icons, and text that is there but cannot be used

Run without arguments it reports every shipped theme that misses the rule and
exits non-zero. With --fix it moves the colours that miss it, and writes the
files back.

A colour keeps its hue and moves only in lightness, and only as far as the rule
needs. Where the thing behind the text is what should move - a row state, a
warning banner - that is what moves instead.

The themes named in EXEMPT are left exactly as they are (user-decided,
2026-11-23), and so are Light and Dark, which are compiled in and not files.
Keep the pair list and the floors in step with ThemeContrastTests.
"""

import glob
import json
import os
import re
import sys

FLOOR_TEXT = 4.5
FLOOR_OTHER = 3.0

# text that does not have to read as normal text: an icon's ink and shadow, and
# text that says it cannot be used
OTHER_TEXT = {"DisabledText", "GlyphForeground", "GlyphShadow", "GlyphWarningInk"}

EXEMPT = {
    "Dark", "Argos", "Earl Grey", "Engage", "Espresso", "Evergreen", "Hedgehog",
    "MARPAT", "Nebula", "Pint of Bitter", "Spring Leaves", "Tie-Dye",
}

# the colours that define what a theme is: text moves to suit these
FIXED_GROUNDS = {
    "WindowBackground", "InputBackground", "MenuBackground", "RollBackground",
    "HexBackground", "MeterTrough", "ShadedBackground",
}

PAIRS = [
    # window chrome
    ("WindowText", "WindowBackground"),
    ("DisabledText", "WindowBackground"),
    ("DisabledText", "DisabledBackground"),
    ("MutedText", "WindowBackground"),
    ("LinkText", "WindowBackground"),
    ("GlyphForeground", "WindowBackground"),
    ("GlyphShadow", "WindowBackground"),
    # fields, buttons, strips
    ("InputText", "InputBackground"),
    ("MutedText", "ReadOnlyBackground"),
    ("InputText", "InputAwaitingBackground"),
    ("InputText", "AlternateRowBackground"),
    ("ButtonText", "ButtonBackground"),
    ("MenuText", "MenuBackground"),
    ("MenuSelectedText", "MenuSelectedBackground"),
    ("ToolStripText", "ToolStripBackground"),
    ("StatusBarText", "StatusBarBackground"),
    ("HeaderText", "HeaderBackground"),
    # a chosen row
    ("SelectionText", "Selection"),
    ("InactiveSelectionText", "InactiveSelection"),
    # a hovered row keeps its text colour
    ("InputText", "HoverBackground"),
    ("MutedText", "HoverBackground"),
    ("DisabledText", "HoverBackground"),
    ("AccentGood", "HoverBackground"),
    ("AccentReady", "HoverBackground"),
    ("AccentWarning", "HoverBackground"),
    ("AccentError", "HoverBackground"),
    # colours that say something
    ("AccentGood", "WindowBackground"),
    ("AccentGood", "InputBackground"),
    ("AccentReady", "WindowBackground"),
    ("AccentReady", "InputBackground"),
    ("AccentWarning", "WindowBackground"),
    ("AccentWarning", "InputBackground"),
    ("AccentError", "WindowBackground"),
    ("AccentError", "InputBackground"),
    ("WindowText", "AccentWarningBackground"),
    ("GlyphWarningInk", "GlyphWarning"),
    # the piano roll, and every row state it draws text on
    ("RollText", "RollBackground"),
    ("RollText", "RollColumnBackground"),
    ("RollHintText", "RollBackground"),
    ("RollSelectionText", "RollSelection"),
    # the hex editor and the level meter
    ("HexText", "HexBackground"),
    ("HexText", "HexFreeze"),
    ("HexText", "HexHighlight"),
    ("HexText", "HexHighlightFreeze"),
    ("MeterText", "MeterTrough"),
    ("MeterText", "ShadedBackground"),
]

for _row in [
    "TasDefaultRow", "TasCurrentFrame", "TasGreenZone", "TasGreenZoneInput",
    "TasGreenZoneInputStated", "TasGreenZoneInputInvalidated", "TasLagZone",
    "TasLagZoneInput", "TasLagZoneInputStated", "TasLagZoneInputInvalidated",
    "TasMarker", "TasPermanentMarker", "TasAnalogEdit", "RowDefault", "RowInvalid",
    "RowActive", "RowPaused", "RowExcluded", "RowExcludedActive",
]:
    PAIRS.append(("RollText", _row))


def channel(v):
    c = v / 255.0
    return c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4


def luminance(rgb):
    return 0.2126 * channel(rgb[0]) + 0.7152 * channel(rgb[1]) + 0.0722 * channel(rgb[2])


def over(top, under):
    if len(top) == 3 or top[3] == 255:
        return top[:3]
    a = top[3] / 255.0
    return tuple(round(top[i] * a + under[i] * (1 - a)) for i in range(3))


def ratio(ink, ground):
    a = luminance(over(ink, ground))
    b = luminance(ground[:3])
    return (a + 0.05) / (b + 0.05) if a > b else (b + 0.05) / (a + 0.05)


def parse(text):
    text = text.lstrip("#")
    if len(text) == 8:
        return (int(text[0:2], 16), int(text[2:4], 16), int(text[4:6], 16), int(text[6:8], 16))
    return (int(text[0:2], 16), int(text[2:4], 16), int(text[4:6], 16))


def format_color(rgb):
    return "#{:02X}{:02X}{:02X}".format(*rgb[:3])


def with_luminance(rgb, target):
    """The same colour, moved along its own line towards black or white until it
    has this luminance: hue stays, saturation hardly moves."""
    start = luminance(rgb)
    target = max(0.0, min(1.0, target))
    towards_white = target > start

    def at(t):
        if towards_white:
            return tuple(round(c + (255 - c) * t) for c in rgb[:3])
        return tuple(round(c * (1 - t)) for c in rgb[:3])

    lo, hi = 0.0, 1.0
    for _ in range(40):
        mid = (lo + hi) / 2
        if (luminance(at(mid)) < target) == towards_white:
            lo = mid
        else:
            hi = mid
    return at((lo + hi) / 2)


def adjust(rgb, needs, flip=True):
    """The nearest colour to this one that reads on every colour in needs.

    needs is a list of (ground, floor). A colour has to be far enough from all of
    them, which in practice means above them all or below them all; both are
    tried and the nearer one is taken. With flip false the colour may not cross
    to the other side of what it was, so light text stays light: a theme is
    recognised by which way round it is, and dark text on a mid blue selection is
    a different theme, not a more readable one. None when nothing fits.
    """
    start = luminance(rgb)
    grounds = [c[:3] for c, _ in needs]
    if all(ratio(rgb, g) >= f for (g, f) in needs):
        return rgb[:3]
    lightest = max(luminance(g) for g in grounds)
    darkest = min(luminance(g) for g in grounds)
    # aim past the floor, further each time: the colour is rounded to whole
    # channels and the ground's own luminance is what it is, so the exact
    # ratio asked for can land a shade short
    for slack in (1.0, 1.01, 1.02, 1.03, 1.05, 1.08, 1.12):
        above = max(slack * f * (luminance(g) + 0.05) - 0.05 for (g, f) in needs)
        below = min((luminance(g) + 0.05) / (slack * f) - 0.05 for (g, f) in needs)
        candidates = []
        if above > lightest and above <= 1.0:
            candidates.append(above)
        if 0.0 <= below < darkest:
            candidates.append(below)
        if not flip:
            if start >= lightest:
                candidates = [c for c in candidates if c > start]
            elif start <= darkest:
                candidates = [c for c in candidates if c < start]
        for target in sorted(candidates, key=lambda t: abs(t - start)):
            colour = with_luminance(rgb, target)
            if all(ratio(colour, g) >= f - 1e-6 for (g, f) in needs):
                return colour
    return None


def floor_of(ink):
    return FLOOR_OTHER if ink in OTHER_TEXT else FLOOR_TEXT


def repair(colors):
    """Moves what has to move until every pair reads. Returns the roles changed."""
    changed = {}
    grounds_by_ink = {}
    for ink, ground in PAIRS:
        grounds_by_ink.setdefault(ink, []).append(ground)

    for _ in range(12):
        moved = False
        # text first, kept on its own side of the background; a ground moves
        # rather than let text turn from light to dark
        for ink, grounds in grounds_by_ink.items():
            needs = [(colors[g], floor_of(ink)) for g in grounds]
            if all(ratio(colors[ink], g) >= f for g, f in needs):
                continue
            colour = adjust(colors[ink], needs, flip=False)
            if colour is None:
                # the text may turn if the fixed backgrounds alone leave it no
                # choice; a movable one moves instead
                fixed = [(colors[g], floor_of(ink)) for g in grounds if g in FIXED_GROUNDS]
                if any(ratio(colors[ink], ground) < floor for ground, floor in fixed):
                    colour = adjust(colors[ink], needs, flip=True)
            if colour is not None and colour != colors[ink][:3]:
                colors[ink] = colour
                changed[ink] = colour
                moved = True
        for ground in sorted({g for _, g in PAIRS}):
            if ground in grounds_by_ink or ground in FIXED_GROUNDS:
                continue
            pair_inks = [(ink, floor_of(ink)) for ink, g in PAIRS if g == ground]
            if all(ratio(colors[ink], colors[ground]) >= floor for ink, floor in pair_inks):
                continue
            needs = [(colors[ink], floor) for ink, floor in pair_inks]
            colour = adjust(colors[ground], needs)
            if colour is not None and colour != colors[ground][:3]:
                colors[ground] = colour
                changed[ground] = colour
                moved = True
        if not moved:
            break
    return changed


def failing(colors):
    return [
        (ink, ground, ratio(colors[ink], colors[ground]), floor_of(ink))
        for ink, ground in PAIRS
        if ratio(colors[ink], colors[ground]) < floor_of(ink) - 1e-9
    ]


def rewrite(path, text, colors, changed):
    for role, rgb in changed.items():
        pattern = re.compile(r'("' + re.escape(role) + r'"\s*:\s*")(#[0-9A-Fa-f]{6,8})(")')
        replacement = r"\g<1>" + format_color(rgb) + r"\g<3>"
        text, n = pattern.subn(replacement, text)
        if n != 1:
            raise SystemExit(f"{path}: {role} appears {n} times, expected once")
    with open(path, "w", encoding="utf-8", newline="") as handle:
        handle.write(text)


def main():
    fix = "--fix" in sys.argv[1:]
    root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
    files = sorted(glob.glob(os.path.join(root, "Themes", "*.chimeraTheme.json")))
    if not files:
        raise SystemExit("no themes found")

    total = 0
    touched = 0
    for path in files:
        with open(path, encoding="utf-8-sig", newline="") as handle:
            text = handle.read()
        theme = json.loads(text)
        if theme["name"] in EXEMPT:
            continue
        colors = {role: parse(value) for role, value in theme["colors"].items()}
        bad = failing(colors)
        if not bad:
            continue
        touched += 1
        total += len(bad)
        if not fix:
            print(f"{theme['name']}: {len(bad)} pair(s) below the floor")
            for ink, ground, value, floor in bad:
                print(f"  {ink} on {ground} is {value:.2f}:1, needs {floor:.1f}:1")
            continue
        changed = repair(colors)
        left = failing(colors)
        if left:
            print(f"{theme['name']}: {len(left)} pair(s) could not be fixed:")
            for ink, ground, value, floor in left:
                print(f"  {ink} on {ground} is {value:.2f}:1, needs {floor:.1f}:1")
        rewrite(path, text, colors, changed)
        print(f"{theme['name']}: {len(bad)} pair(s) below the floor, {len(changed)} colour(s) moved")

    print(f"\n{total} pair(s) below the floor in {touched} theme(s)")
    return 1 if total and not fix else 0


if __name__ == "__main__":
    sys.exit(main())
