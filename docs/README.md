# Chimera CSX documentation

Chimera CSX is the C# WinForms fork of Chimera. It keeps the frontend that upstream shipped in nightly-2026-10-27 (e9b74216) and the C++ engine under it; upstream replaced that window with a Qt one after the cut. The documents here describe the fork. Where a document is a record of an earlier decision, or no longer describes this fork, it is filed under `history/` or `outdated/` and named so below.

Start with [new-core.md](new-core.md) if you are building a core, [porting-a-core.md](porting-a-core.md) for the craft, and [gates.md](gates.md) for what proves a change. The [project charter and design log](history/design-principles.md) is the record behind every rule.

## Current

These describe the C# fork as it is.

| Document | What it covers |
|---|---|
| [new-core.md](new-core.md) | From a core request to a published core: what to answer, which existing core to copy, the gates, and the publishing checklist. |
| [porting-a-core.md](porting-a-core.md) | The craft: the guest toolchain, the picture, savestates, package declarations, the optional exports, and CI. |
| [gates.md](gates.md) | How a gate goes green on a broken thing, and the ways a green tick lies. |
| [game-cores.md](game-cores.md) | A core that is one game, run from the game's own files. |
| [multi-file.md](multi-file.md) | The `.chimeraMultiFile` descriptor for games that come as several files. |
| [save-data.md](save-data.md) | Save data: the core keeps it, the user takes it out. |
| [core-manager.md](core-manager.md) | How cores reach a Chimera install, and what the Core Manager lists. |
| [cache-manager.md](cache-manager.md) | What Chimera keeps on disk that it could work out again, and what may be removed. |
| [compile-cache.md](compile-cache.md) | The cache of code a core compiled, kept between sessions. |
| [encode-video.md](encode-video.md) | Encoding a run to a video. |
| [media-maker.md](media-maker.md) | The Reproducible Media Maker: a folder becomes one file. |
| [project.md](project.md) | The Chimera project: TAS-only refocus, the project file, and its editing rules. |
| [state-manager.md](state-manager.md) | The state history: the greenzone, its budgets, its files, and its rules. |
| [gpu-bridge.md](gpu-bridge.md) | A core's renderer on a real GPU, and the states it may keep. |
| [theming.md](theming.md) | Themes: the colours Chimera paints with, and how to choose or write one. |
| [folder-picker.md](folder-picker.md) | The file and folder pickers, including the desktop portal on Linux. |
| [mcp-server.md](mcp-server.md) | The MCP server: an AI agent's tools for the project, the machine, and the Lua Console. |

## History

These are records of decisions and work that led to the fork. They are kept as written; where they describe the upstream Qt window or the C# window's removal, that is upstream history, not this fork.

| Document | What it records |
|---|---|
| [history/design-principles.md](history/design-principles.md) | The project charter and design log: every dated decision, what was measured, and why. It is the record behind every rule, including the fork's own 1.x decisions. |
| [history/engine-migration.md](history/engine-migration.md) | The 2026-08-24 decision to move everything functional into C++ and keep C# for the WinForms interface. |
| [history/waterbox-analysis.md](history/waterbox-analysis.md) | The 2026-08-11 analysis of BizHawk's waterbox, the sandbox every core runs in. |
| [history/graphics-settings.md](history/graphics-settings.md) | The 2026-09-19 analysis of graphics settings for the sixth- and seventh-generation cores: what could be granted, what could not, and why. |
| [history/issue-intake.md](history/issue-intake.md) | The 2026-09-19 agreed plan for issue intake and handling on the upstream repository and the core repositories. |

## Outdated

These no longer describe the fork's current state. They are kept so a reader can see what was believed or planned at the time.

| Document | Why it is here |
|---|---|
| [outdated/upstream-since-1.0.0.md](outdated/upstream-since-1.0.0.md) | The commit-by-commit assessment of upstream since the 1.0.0 cut, and the batch plans built on it. The batches through 1.4.0 are done; the document describes upstream's Qt line, not this fork's current code. |

The upstream Qt window's own documentation is not carried here: it describes a frontend this fork does not use.
