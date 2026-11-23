# Credits

## Chimera

- **Sergio Martin** ([SergioMartin86](https://github.com/SergioMartin86)): author and maintainer.
- Development assisted by **Claude** (Anthropic); every assisted commit is marked as such in the history.

Emulation cores are separate projects with their own authors and licenses; each core package carries its licenses and cites its source repository.

## Themes

The sixty colour themes in [Themes](Themes) were made by **Nelson/Geferulf**, who gave them to the project to ship with Chimera.

## Icons

The interface's command icons are [Feather](https://feathericons.com), by Cole Bemis and the Feather contributors, under the MIT license. The SVG files the window uses are in `source/gui/Chimera.Client.GUI/images/feather`, with the license beside them; `theming/FeatherIcons.cs` draws them. The gamepad is [Gamepadicon.svg](https://commons.wikimedia.org/wiki/File:Gamepadicon.svg) from Wikimedia Commons, public domain, in `images/glyphs` because Feather has no gamepad. The Chimera logo and the coloured status lights and arrows are the project's own.

## BizHawk

Chimera is a derivative fork of [BizHawk](https://github.com/TASEmulators/BizHawk). The frontend architecture, the TAS toolchain, the emulation service interfaces, and years of accumulated correctness are the work of the BizHawk team, and this project would not exist without them.

Every BizHawk contributor is credited through the upstream contributors list: [github.com/TASEmulators/BizHawk/graphs/contributors](https://github.com/TASEmulators/BizHawk/graphs/contributors).

This repository's history begins at the point of separation from BizHawk; the full pre-separation ancestry, including the BizHawk contributors' commit history, is preserved in [BizHawk](https://github.com/TASEmulators/BizHawk) and in the [transitional fork](https://github.com/SergioMartin86/BizHawk) this repository was extracted from.

## Third-party libraries

The native libraries built from `extern/` (SDL2, Lua, zstd, SQLite, OpenAL Soft, cimgui, miniz, cJSON, and supporting projects) belong to their respective authors, under their respective licenses; see each project's license file, and [LICENSE](LICENSE) for how they relate to this repository.
