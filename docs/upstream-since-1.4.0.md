# Upstream since Chimera CSX 1.4.0

2026-11-16. Chimera CSX 1.4.0 carries the cores batch. The earlier assessment, [upstream-since-1.0.0.md](outdated/upstream-since-1.0.0.md), judged upstream through 1241bff3 on 2026-11-14. Upstream rebased main after that, so the last ten commits it judged have new SHAs and the same content (the table below). Main is now c98a3565 (2026-11-15), 17 commits after that point, and the official roster has grown from 63 cores to 72.

## The short answer

The 17 commits add nine cores and a gamepad model.

- **Portable: five commits, nine cores.** Gearboy, Gearsystem, Gearcoleco, Geargrafx, Gearlynx, Pokket, Geartowns, blueMSX and Sonic 3 A.I.R. join the upstream roster in five commits. They change `official-cores.json`, the README table, `docs/platforms.md` and the design log. The nine packages are published and pass this fork's package contract (below). The 1.4.1 batch is those nine roster rows, nine README rows and the cores set; no engine or window change.
- **Engine candidate: one commit.** The TAS Studio long seek adds `ce_session_measured_fps`, an export that says how fast a replay is running so the window can estimate a seek. The rest of that feature is Qt. A C# window would need a progress dialog and a cancel, and the export is a new declaration, so this is a separate decision rather than part of the cores batch.
- **Held: one commit.** Gamepads move from SDL 2 to SDL 3. That replaces an extern and the Qt window's gamepad model; in the C# window it would be an input rewrite, not a port.
- **Skip: ten commits.** The Qt window's own: four gamepad features and their settings document, two menus, a project-scripts fix, a greenzone status line, and the long seek's threshold setting. `source/ui` does not exist here.

## The commits since the last assessment

| Commit | Subject | Verdict |
| --- | --- | --- |
| 266cfb1a | fix(qt): a project's scripts are recognised as listed on Windows too (#587) | skip (Qt) |
| cd79b207 | feat(cores): Gearboy, Gearsystem, Gearcoleco, Geargrafx and Gearlynx join (#581) | portable (roster) |
| 559edb41 | feat(cores): Pokket joins the official cores (#575) | portable (roster) |
| 72a45c2a | feat(cores): Geartowns joins the official cores (#581) | portable (roster) |
| 74789237 | build(gamepads): read gamepads through SDL 3 instead of SDL 2 | hold (extern and C# input) |
| 8256593a | feat(gamepads): face buttons kept by place, shown as the pad prints them | skip (Qt) |
| bead0292 | fix(gamepads): a pad's labels show as soon as a bindings window opens | skip (Qt) |
| 3ca43d8e | feat(gamepads): a pad's face buttons can be swapped and relabelled by hand | skip (Qt) |
| 3e426635 | fix(gamepads): pads plugged in and out are followed again | skip (Qt) |
| 3060b792 | docs(gamepads): the owner decided the face buttons and the two pad settings | docs only (the Qt model) |
| 443f3597 | feat(qt): menus are windows of their own and reach past the window (#593) | skip (Qt) |
| c3e2716a | test(qt): the menus test checks a menu the way Qt shows it (#593) | skip (Qt) |
| 63bd69b0 | feat(cores): blueMSX joins the official cores (#591) | portable (roster) |
| ca01fe69 | feat(tastudio): a long seek asks first, shows its progress and can be cancelled (#595) | ported (this window's own seek; the export is not) |
| f6598133 | feat(tastudio): a long seek's threshold is a setting | skip (Qt) |
| 9cd20fb4 | feat(cores): Sonic 3 A.I.R. joins the official cores, a game core (#594) | portable (roster) |
| c98a3565 | fix(greenzone): a greenzone thrown away at close is said in the status line | skip (Qt) |

## The rebase

The ten commits the earlier assessment judged last now have these SHAs. The content and the verdicts are unchanged.

| Old | Now on main | Subject |
| --- | --- | --- |
| ced1d661 | 9ea9143b | docs(agents): AGENTS.md says who the owner is (#580) |
| 6ba3049f | 47475179 | fix(tastudio): a double click on a button's cell changes it twice (#582) |
| 08c40487 | d356af53 | fix(greenzone): Clear Greenzone gives back a budget halved for lack of memory (#577) |
| 9de4c502 | 1bd017c0 | test(project): a slot with no formats takes any file (#567) |
| f2a3293b | 7be82a7b | feat(qt): a long menu has arrows at its ends that scroll its rows (#588) |
| 9b1e0da9 | 7d54075d | feat(lua): the Lua Console's Script column can be widened (#586) |
| a4ecc4e5 | 80504307 | feat(greenzone): a marker on the machine's frame keeps its state at once (#578) |
| 93b881b8 | d9eea61b | feat(project): a project can reopen its tool windows and their files (#587) |
| c9d78d33 | 1d1cc953 | fix(greenzone): a greenzone anchor no longer makes a frame late on Windows (#548) |
| 1241bff3 | 28396378 | Merge pull request #589 from P-AS/fix/light-theme-on-kde |

## The nine cores

All nine are published. The package column is the nightly tested here (the newest when this was written) and its x86_64 package; each except Sonic 3 A.I.R. also publishes an aarch64 package.

| Core | Systems | Package | What it needs | Terms |
| --- | --- | --- | --- | --- |
| Gearboy | Game Boy, Game Boy Color, Super Game Boy, Game Boy Link | nightly-2026-11-15, 7aaf5a77, 1.1 MB | Boot ROMs are optional and on by default; an SGB ROM for the Super Game Boy | GPL-3.0 |
| Gearsystem | Master System, Game Gear, SG-1000, Game Gear Link, Mark III Link | nightly-2026-11-15, fbd5a45f, 0.8 MB | A BIOS is optional; games run without one | GPL-3.0; its FM sound chip is non-commercial, as Genesis Plus GX's is |
| Gearcoleco | ColecoVision, Coleco ADAM | nightly-2026-11-16, a61de165, 0.8 MB | The ColecoVision OS7 ROM; the ADAM's EOS and SmartWriter ROMs for the ADAM | GPL-3.0 |
| Geargrafx | PC Engine / TurboGrafx-16, SuperGrafx, PC Engine CD | nightly-2026-11-15, 040d7547, 1.8 MB | A System Card ROM for a CD; HuCards need none | GPL-3.0 |
| Gearlynx | Atari Lynx, Atari Lynx Link | nightly-2026-11-15, 0c7ad05d, 1.1 MB | The Lynx boot ROM, unless the game is `.o` homebrew; one per linked Lynx | GPL-3.0 |
| Pokket | Sony PocketStation | nightly-2026-11-15, d1d44136, 87 KB | Sony's PocketStation BIOS, 16 KiB, pinned to revision J110 | MIT (the emulation core) |
| Geartowns | FM Towns, FM Towns II, FM Towns Marty | nightly-2026-11-15, ce607094, 1.8 MB | The chosen model's system and OS ROMs, or the free TOWNSROM set through `freeTownsRoms` | GPL-3.0-or-later |
| blueMSX | MSX, MSX2, MSX2+, MSX turbo R | nightly-2026-11-15, 72c20df7, 1.3 MB | The chosen machine's BIOS; C-BIOS is built in for MSX, MSX2 and MSX2+ cartridges | GPL-2.0-or-later; the V9938 command engine's own terms allow any MSX emulator, and nothing else |
| Sonic 3 A.I.R. | Sonic 3 A.I.R. (a game core) | nightly-2026-11-15, 24e19755, 8.2 MB, x86_64 only | The Sonic & Knuckles + Sonic 3 ROM; the remastered soundtrack is an optional slot | GPL-3.0; its Mega Drive sound chips are non-commercial, and the package is declared non-commercial |

The cores' own repositories measured each one: its test script, the frontend test script and Chimera's package checks, on x86_64 and aarch64; and games and firmware played natively, in the sandbox, and with a savestate saved and loaded. The details are in each `chimera-core-<id>` repository's `docs/PLAN.md`. Geartowns' Marty and Gearcoleco's ADAM were the least proved: the Marty was not booted, and the ADAM ran on stand-in ROMs until the owner's EOS and SmartWriter arrived. Upstream's own log says so.

## What the fork's contract measured

On 2026-11-16 the nine x86_64 packages were fetched from those nightlies into a folder and the four contract classes were run with `CHIMERA_CORES_DIR` pointing at it: `InstalledCorePackagesTests` 17 of 17 passed, `MnemonicUniquenessTests` 9 of 9, `CoreLanguageTableTests` 6 of 6, and `RosterNamesTests` 2 passed with 2 inconclusive because the roster does not name these cores yet. In all, 36 tests, 0 failed, 2 skipped.

That says the packages are read, their factories build, and their settings, file slots, firmware conditions, language tables and controller names hold in this window's engine. Nothing new is needed. Two details already have support here: Gearlynx writes `optionLabels` as an array, the form Mesen needed in 1.4.0, and the packages gate slots and firmware with nested `all`, `any`, `not` and `slot` conditions, which the engine evaluates and the wizard asks it about.

The systems the nine declare have no name conflicts. The systems already in the roster - GB, GBC, SGB, GBL, SMS, GG, SG, CV, PCE, SGX, PCECD, LYNX, MSX, MSX2 - carry the same names these packages do, so the one-name test stays as it is, with its Mesen/Snes9x SNES split. GGL, M3L, ADAM, LYNXLINK, POCKETSTATION, FMTowns, MSX2P, MSXTR and S3AIR are new rows only.

Not run: a game in each core, a project made through the C# wizard, and the window with these packages. The packages' own repositories measured gameplay; this fork has not.

## The 1.4.1 batch (user-decided, 2026-11-16)

The owner took the batch as 1.4.1: nine cores are a patch, not a minor, so 1.5.0 was not used. The batch is:

- The nine rows in `official-cores.json`, in upstream's order.
- The nine README rows, with Sonic 3 A.I.R. under Game cores and the other eight in their systems' places.
- The nine x86_64 packages in the cores set, which becomes 72 cores; Cemu and ScummVM stay in the oversize release.
- The design log section for the batch, adapted from upstream's five entries with this fork's measurements added.

No engine, C# or miniBox change is needed. The release is 1.4.1, cut by hand when the batch is in.

The README's licence note now names the new non-commercial terms too: Gearsystem's FM sound chip, Sonic 3 A.I.R.'s sound chips and its declared package, beside Genesis Plus GX and the others already there.

## Asked for, not built

Seven open `core-request` issues were on upstream on 2026-11-16. None has a repository yet, so none is a core to assess:

| Issue | Request | Opened |
| --- | --- | --- |
| 608 | SMBX Legacy and TheXTech (SMBX port) | 2026-11-16 |
| 605 | Switch 2 emulators | 2026-11-16 |
| 604 | OpenGMK (GameMaker) | 2026-11-16 |
| 603 | Sonic 1 Forever and Sonic 2 Absolute | 2026-11-16 |
| 601 | Heroes of Might and Magic II and III game engines | 2026-11-16 |
| 596 | CPS2 modded games | 2026-11-15 |
| 592 | Unleashed Recompiled and Marathon Recompiled | 2026-11-14 |

## The other changes

The long seek's engine export is the only engine work. It adds one function to `engine.h`, `ce_session_measured_fps`, and counts the frames of a replay in `session.cpp`; the ABI stays version 1. The Qt window uses it to estimate a seek and show a progress dialog with a cancel. The C# window already has `ce_session_seek_stop` in the engine but does not bind it, so this would be an engine export plus a C# binding and dialog, and the engine's export list is the owner's to change.

The gamepad commits would need SDL 3 in place of SDL 2 and a C# version of the face-button-by-place model. They are held, not taken: the C# window's controller configuration is its own and the fork's goal is the behaviour its projects were made with.

The rest is the Qt window. There is nothing in them for this fork.

## The second batch: upstream after 1.4.2 (2026-11-19)

Upstream main is 99adbca8 (2026-11-19), 17 commits after c98a3565, the tip the first assessment judged; main was not rebased. The official roster is 74 cores, up from 72: fheroes2 and TheXTech, both game cores.

The new cores are one commit; the other 16 are four groups.

- **A core's windows (#610), 8 commits.** A core draws QML windows of its own from its package (`windows/`, declared in `waterbox.config`) and may hold its frame while a window is open. Chimera loses its own Export Save Data, CPU Registers, Trace Logger and Surface Viewer; miniBox's kit (`waterbox_windows.h`, `add-windows.py`) gives the cores ready-made ones from the exports they already have. This is the Qt window's architecture: QML, a view model and a package window host. It is not a port for the C# window: the host would have to be written in WinForms, the tools removed and re-provided by the packages, and the miniBox kit is an extern bump that changes replay determinism. The C# window keeps its own tools and ignores a package's `windows/` QML. Held or skipped, not in a batch.
- **Input headers (#607), 2 commits.** A button's header may be any printable Unicode character, one to eight characters counted as characters and not ASCII only. The engine half is a small port and it is the fix for the current published-cores failure: the newest RPCS3 package (70d2a537) declares the PlayStation symbols by code point (`U+25B3` triangle, `U+25CB` circle, `U+2715` cross and `U+25A1` square), and this fork's engine and the C# test that mirrors it still take ASCII only, so the test refuses the package. The C# test must adopt the same rule. The other commit shows a column's header to a script in the Qt view model; skip.
- **New project large files (#611), 3 commits.** A large file is hashed away from the window with a progress bar and Cancel, through two new engine exports (`ce_sha1_file_progress` and `ce_sha1_file_known`). Taken on 2026-11-19: both exports are in the engine, and the C# wizard uses them - a file of 16 MB or more is read with the bar and a Cancel, a stopped read leaves the file in its slot shown "(not read)", Compile reads it again, and the firmware scan and a hand-picked firmware file go through the same bar. The third commit only fixes upstream's own view-model test on Windows and is not ported.
- **Three single commits.** The Windows OpenAL build takes only Windows's sound backends (`meson.build`; a portable build fix), Dolphin's roster row lists Triforce (portable roster and README), and `--measure` with `--play` counts from the machine's start (Qt; skip).

### The two new cores

Both are game cores, and both packages passed the four contract classes here: 34 passed, 2 inconclusive because the roster does not name them yet, 0 failed. Each is x86_64 only and carries a `windows/` SaveData QML from miniBox's kit, which the C# window ignores; its own Export Save Data remains.

| Core | Game | Package | What it needs | Terms |
| --- | --- | --- | --- | --- |
| fheroes2 | Heroes of Might and Magic II | nightly-2026-11-19, 0645d327, 5.6 MB | `HEROES2.AGG` from the game, or its free demo | GPL-2.0 |
| TheXTech | Super Mario Bros. X (SMBX 1.3) | nightly-2026-11-19, 5372b92b, 4.4 MB | An SMBX asset pack in the file slots | GPL-3.0 |

The roster row for Dolphin gains Triforce, the arcade board its package already runs.

### What was proposed, and what was done

- **1.4.3, a patch**: port #607's engine rule and update the C# test that mirrors it. No roster change. Done and published on 2026-11-19 from `798543b0`; the published-cores job is green again, and the RPCS3 package was not pinned or skipped.
- **The cores batch**: fheroes2, TheXTech and Dolphin's Triforce row were taken on 2026-11-19. The roster is 74 cores and the README's game-core table has the two new rows. The 74-package set is published in [chimera-csx-cores](https://github.com/CloakTheLurker/chimera-csx-cores) now, ahead of the release it is named for: `1.5.0Cores` with `cores-1.5.0.json` (published as 1.4.4Cores in c768c8a, renamed in 8acf0eb). No engine or C# change is needed for these packages; the `windows/` QML is ignored and the C# window's tools remain.
- **#611, the large-file wizard**: taken on 2026-11-19, on the owner's word. The two engine exports and the C# wizard's bar and Cancel are in; a stopped read leaves its file in the slot, marked "(not read)", and Compile reads it again. Why it was first held: the C# window's progress dialog had no Cancel, and the two exports are declarations that are the owner's to add; both are here now. The engine exports change what a frontend may ask of the engine, so the next release carries them.
- **The rest of the ports, taken on 2026-11-19**: the Windows OpenAL backends (e778d80d), the greenzone's raw copies freed on the packer's thread (1d1cc953), the budget a clear gives back (08c40487) and a marker storing the machine's frame at once (a4ecc4e5), chimera-run's `--movie` with its two fixes (8e888145, 6afea84e, 530b5591), and the long seek in this window's own shape (ca01fe69's behaviour, without its engine export, which this window has nothing to measure with). `readsDeadStack` (4629dcfd) needed miniBox's compare-writes, so the submodule moved to `ba7cbc54`; the witness then passed 117 legs with RAM and VRAM byte-identical. The design log has what each changes and what was not proved.
- **Not taken**: the core windows architecture. It is the Qt window's: the host would have to be written in WinForms and the miniBox kit is an extern bump.

Measured here on 2026-11-19 against the 74-row roster: the 1.2.0, 1.4.0 and 1.4.1 set files with the two new x86_64 packages (72 files; the two oversize, Cemu and ScummVM, were not downloaded) passed 39 tests with 0 failed and 0 skipped, where the same two packages alone had left two tests inconclusive for the roster rows they lacked.

### Asked for, not built (2026-11-19)

Three open `core-request` issues were on upstream. Two of the earlier seven are now cores: TheXTech for 608, and fheroes2 for 601's HoMM II part; 601 stays open for HoMM III.

| Issue | Request |
| --- | --- |
| 604 | OpenGMK (GameMaker) |
| 601 | Heroes of Might and Magic II and III (II is fheroes2; III remains) |
| 592 | Unleashed Recompiled and Marathon Recompiled |

The current CI: witness level A and the full frontend pass; published cores fails on the RPCS3 package's Unicode headers, which the #607 port fixes.

### The 17 commits

| Commit | Subject | Verdict |
| --- | --- | --- |
| 46d9b817 | feat(cores): a core may add windows of its own and hold its frame (#610) | skip (Qt) and hold (miniBox kit) |
| c9b68913 | docs(readme): Dolphin lists Triforce among its systems (#606) | portable (roster and README) |
| ad7c30ac | fix(cores): a core's window reaches a file only through Chimera's dialog (#610) | skip (Qt) |
| e7ebf0c9 | feat(cores): Chimera has no tools of its own for a core (#610) | skip (Qt) |
| e778d80d | fix(build): the Windows build's OpenAL takes only Windows's own sound backends | ported (the CMake backend flags) |
| faa98d77 | fix(cores): a core's save exported in windows reaches the zip whole (#610) | hold (miniBox kit) |
| 4a2afeb2 | fix(cores): a core's window writes the file Chimera gave it (#610) | hold (miniBox kit) |
| 07463056 | fix(cores): every core's windows link, whatever its code model (#610) | hold (miniBox kit) |
| cc7fda63 | feat(input): a button's header may be any printable characters (#607) | engine candidate (fixes RPCS3) |
| e5734a6e | feat(tastudio): a script sees a column's header, not only its mnemonic (#607) | skip (Qt) |
| bf3fe228 | feat(new-project): a large file is read away from the window, with progress and Cancel (#611) | ported (engine exports and the C# wizard) |
| f38820fc | feat(new-project): Cancel keeps the file, and a firmware scan reads large files away (#611) | ported (the C# wizard's rule; the Qt half is not) |
| 9e5dae9a | fix(cores): a core's windows kit compiles in a C core built with -std=c11 (#610) | hold (miniBox kit) |
| 49ecb065 | test(new-project): the large-file test makes its files right on Windows (#611) | skip (Qt test) |
| e2b51565 | test(cores): a core window's refused pictures are checked on a Qt without its network module (#610) | skip (Qt test) |
| 53341840 | fix(window): --measure with --play counts from when the machine starts | skip (Qt) |
| 99adbca8 | feat(cores): fheroes2 and TheXTech join the official cores (#601, #608) | portable (roster) |
