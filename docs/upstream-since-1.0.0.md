# Upstream since Chimera CSX 1.0.0

2026-11-14. Chimera CSX 1.0.0 is e9b74216, the last nightly before upstream
replaced the WinForms window with Qt. Upstream main is now 1241bff3, 541
commits later, 525 of them non-merge. This document judges every one of those
commits and says what can come across.

## The short answer

Most of it cannot. 343 commits touch the Qt window or things that exist only
for it. 54 add engine code and Qt code together, so only their engine half is
a candidate, and only by hand. 23 touch the engine alone, but a probe of four
of the safer engine fixes found three that conflict with the 1.0.0 tree: the
engine has drifted, so those are ports, not cherry-picks. 85 touch neither the
frontend nor the engine (docs, tools, CI, the core roster) and can be taken.
16 bump miniBox or another submodule and are held for replay determinism. Four
are C#-era commits that the Qt line made obsolete.

The engine's C ABI is still version 1, so the frozen C# client can load a
newer libchimera at the symbol level. That is not enough: the project file
format changed twice after the cut, and the C# client owns parts of the project
and movie code. A batch that changes those formats needs matching C# work
before it can be called compatible.

## Verdicts

Every non-merge commit is one of these:

| Verdict | Commits | Meaning |
| --- | --- | --- |
| skip (Qt) | 343 | touches only the Qt window; source/ui does not exist here |
| engine half only | 54 | engine and Qt in one commit; the engine part is a hand port |
| engine candidate | 23 | engine only; port and test before it can land |
| portable | 85 | docs, tools, CI, roster, tests: no frontend or engine change |
| hold (sandbox) | 16 | miniBox or extern bump: would change replay determinism |
| obsolete (C# era) | 4 | C# changes from before the removal; superseded or reverted |

## What cannot come across

- The Qt window and its view models. source/ui does not exist at 1.0.0, and the C# window is the fork.
- The removal commit c1ac3562, which deletes source/gui and the C#-only parts of extern.
- Qt-only packaging, translations for the Qt window, Qt build files, and the macOS and arm64 Qt bundles.
- The four C#-era commits: the removal itself, the hotkey rename, the project-only change and its revert. The fork keeps the pre-removal C# window and the reverted behaviour, so none of them applies.

## Engine candidates

These 23 commits touch the engine and neither the Qt nor the C# frontend. They are the engine half of the Qt line, not a maintained C# branch: most still need a port, a build and the engine tests.

| Commit | Subject |
| --- | --- |
| c9d78d33 | fix(greenzone): a greenzone anchor no longer makes a frame late on Windows (#548) |
| 9de4c502 | test(project): a slot with no formats takes any file, and the docs say so (#567) |
| 4629dcfd | feat(engine): a core whose guest reads its dead stack says so (#555) |
| a9f77da3 | test(state history): spill folders are removed after the history lets go of its file |
| 84312d64 | fix(windows): what the first run of the engine's tests on Windows on arm64 found |
| 19cd8d31 | test: the media and state history tests read their files as Windows has them |
| 1586ed46 | ci(windows): Chimera's tests run on Windows, x64 and arm64 |
| f0cc3a1e | perf(engine): a full greenzone thins a little every frame, its merges on a helper thread (#482) |
| 6277b09c | fix(aarch64): the engine's build info names the CPU it was built for |
| 55307a92 | feat(macos): the engine and miniBox build for x86-64 macOS and pass their tests |
| af99c84b | fix(engine): chimera-run in a bundle gives its texts as sentences, not keys |
| 8ee74abf | feat(languages): the engine's table check holds proper nouns |
| 25848631 | fix(engine): a history of another machine leaves the greenzone as it was |
| f621249b | fix(languages): a core's texts come from any table it has |
| d460b63b | feat(languages): a reason inside a core's text has its own arguments, and a report is a text a line |
| 1bb84087 | test(history): the state history tests hold the near band's stride |
| c8ae2b47 | feat(project): a project's input is its columns and its entries |
| 8e888145 | feat(chimera-run): a made project takes its movie with --movie |
| 6afea84e | fix(chimera-run): a made project's files are checked against its settings |
| 530b5591 | fix(chimera-run): a made project's machine follows its game's kind of file |
| 627eba8c | feat(chimera-run): a made project takes its movie with --movie |
| b5954008 | fix(chimera-run): a made project's files are checked against its settings |
| 27a10f98 | fix(chimera-run): a made project's machine follows its game's kind of file |

Notes: 6277b09c applied cleanly in a probe; c9d78d33, 25848631 and af99c84b conflicted. Three pairs are duplicates left by the project-only revert (8e888145/627eba8c, 6afea84e/b5954008, 530b5591/27a10f98), so take one of each. The macOS, arm64 and aarch64 commits matter only if the fork builds on those hosts.

## Engine and Qt together

These 54 commits change the engine and the Qt window in one commit. The window half cannot apply. The engine half is a hand port; the themes are Qt (10), MCP (9), UI (8), greenzone (4), engine (3), project (3), chimera-run (2), tools (2), languages (2), Lua (2), and single fixes for audio, TAStudio, macOS, memory, input, controls and Windows, plus one engine/ui and one other.

| Commit | Subject |
| --- | --- |
| a4ecc4e5 | feat(greenzone): a marker on the machine's frame keeps its state at once (#578) |
| 08c40487 | fix(greenzone): Clear Greenzone gives back a budget halved for lack of memory (#577) |
| 81725c0a | feat(mcp): run_lua with frames: the agent's code may wait for frames, played for it (#571) |
| 1c17c46a | feat(mcp): chimera-mcp lists the window's own tools, says another build at once, and shows in Tools > MCP Server... (#571) |
| 911d41f0 | feat(mcp): trace_memory and trace_cpu; cores may say which instruction made an access, and their registers' roles (#571) |
| ab4ccfb2 | feat(mcp): try_input, snapshot and restore, seek with extend, length changes said (#571) |
| 07e95e11 | feat(mcp): the picture with what Lua draws, and the Lua Console read: lua_console, lua_scripts, errors (#571) |
| ed50f989 | feat(mcp): screenshot crops and colours, diff_memory, advance_until on a range, a shorter status (#571) |
| cc15df81 | feat(mcp): the agent's RAM search does what the engine's does, and several rounds in one call (#571) |
| bea85f29 | feat(mcp): an agent is told when its memory writes are undone; freeze tools; hex addresses (#571) |
| 49361067 | feat(mcp): an agent is told when chimera-mcp and the window are different builds; run_lua stops code that does not end (#571) |
| 38fccbeb | fix(engine): Clear Greenzone gives memory back and leaves the machine where it is (#564) |
| 5f58a8fa | fix(engine): a step of rewind with the greenzone off can be stopped (#559) |
| d5d3ebd6 | fix(ui): the window no longer slows down as the movie gets longer (#548) |
| f86bbb02 | feat(greenzone): every marker's frame is kept in the greenzone, unless it is off |
| 7103e435 | fix(greenzone): a budget changed applies to the open project at once (#543) |
| 06066f3c | feat(lua): memory functions get flags and may change a value; branches keep userdata; event.onloadstate (#534) |
| 59ae78b3 | feat(engine): each core package says which systems it runs on |
| 52dea77b | feat(lua): a core may call back at the memory a script watches (#496) |
| f55300df | build(windows): Chimera's engine and tools build for Windows on arm64 |
| ad34a3a7 | fix(macos): the build asks for macOS 14.4, not for the version of the Mac that made it |
| 08ee9de1 | feat(project): a project is a zip of its JSON and its input's lines; the greenzone is written when it closes |
| d43bb214 | feat(tools): a core's tool is given no game file, so any name but tool and settings is its own |
| 6862ee93 | fix(qt): readable menus and lists in dark mode, the Hex Editor's rows and columns, 8-byte values (#471 #472 #473 #474) |
| cdb708a4 | fix(qt): the menu bar keeps its words after full screen; the project description is gone (#451 #440) |
| 685d8031 | feat(tastudio): Header Info, Comments and Subtitles are gone (#439 #440) |
| 972a7cd0 | fix(memory): buses are memory domains again, and RAM Search searches when it opens (#456) |
| 14037f6e | feat(tools): a core's own tools, under Tools > Core-Specific Tools |
| 3f8ea7e2 | feat(qt): View > Display Real Time |
| 4216be00 | fix(qt): Report an Issue names the graphics card (#427) |
| 23ef2de0 | fix(input): every button reaches the movie, not only the first 64 (#411) |
| 9a162f31 | fix(project): a project saved under another name gets an id of its own |
| 999f79dd | feat(languages): every text of Chimera is a key; a core's texts are keys of its own tables |
| 7013a849 | feat(languages): the window's texts are keys in one table per language |
| 66d6a944 | refactor(project): the old "input" string is not read |
| a0692bfd | feat(controls): the core says which player each control belongs to; the roll draws lines between players |
| d6ac5398 | fix(audio): a core's sound is converted to 44100 Hz again (#372) |
| 74022e7c | refactor: the code and documents describe only Chimera as it is (owner) |
| 7f202eb9 | fix(ui): controls are the chosen machine's, and an unplayable movie ends a headless run |
| f6cd8a04 | feat(chimera-run): a project is the only way to a machine |
| 8b561c78 | feat(chimera-run): a project is the only way to a machine |
| 9a61f1e4 | build(qt): the Windows folder holds the program; DLLs in lib, tools in bin |
| 7c302c3e | fix(qt): Flash pictures keep their shape, right button + wheel seeks, Chimera's icon |
| f83e0d21 | feat(qt): New Project compiles a game's code first, as Chimera's (#142) |
| e3612ea6 | feat(qt): New Project asks when the machine's clock starts (#142) |
| f64d5751 | feat(qt): the session journals its own movie against a crash (#142) |
| a4ba7da7 | feat(qt): frozen addresses for this session only, kept by the engine (#142) |
| 00c21226 | feat(ui): New Project takes a game's own settings and firmware from the core |
| 1728163d | feat(ui): TAStudio's editing: frames picked, put in, taken out, cloned, cleared, copied and pasted |
| 39ec6a1e | feat(ui): rerecords are counted, by the engine, as Chimera's TAStudio counts them |
| 1a6937cf | feat(ui): the greenzone is kept with the project, and the machine's identity is the engine's |
| 065ea818 | feat(ui): a branch keeps its state in a file, so loading it is a state load |
| 187c5561 | feat(engine,ui): the engine owns the movie the piano roll edits (#142, prototype) |
| aa1e6698 | feat(ui): projects in the Qt window - open, play, edit, save (#142, prototype) |

## Portable commits

These 85 touch neither the frontend nor the engine. They can be applied by hand or cherry-picked, with the old names and paths adjusted where the fork renamed things.

| Paths | Commits |
| --- | --- |
| docs | 26 |
| ci,docs,tools | 10 |
| ci,docs | 9 |
| docs,roster | 9 |
| tools | 8 |
| docs,tools | 7 |
| ci | 6 |
| ci,tools | 6 |
| build | 2 |
| docs,themes,tools | 1 |
| tests | 1 |

The nine roster commits are the only product changes among them: bsnes, ScummVM, Cemu, Mesen (and its Pocket Challenge V2 entry), 86Box, ZEsarUX, RSDKv5U, Supermodel, and the Godot version name. A core in the roster still has to run on the 1.0.0 engine and be published for the fork; see the core package repository linked from the README.

## Held: sandbox and extern

These 16 bump miniBox or another submodule. A sandbox change can change how a core runs, which is the one thing this fork promises not to do; they are held until a batch is measured against recorded movies.

| Commit | Subject |
| --- | --- |
| fec47a21 | build(minibox): aarch64 trampolines fit any image size; aarch64 for emulator cores |
| b9c65cd3 | build(minibox): main takes miniBox 3848e4e, so cores can be built for an Intel Mac |
| 61e70400 | test: the Windows tests get the packages they open; Apple silicon's test core has no fault handler |
| 2fdcbad9 | build: miniBox 23fa1c3, Windows on arm64's halts, wild calls and x18 |
| 471b316b | build: miniBox 2f20665, the TEB's stack bounds on Windows on arm64 |
| f6743848 | build: miniBox b68ad43, an Intel Mac mapping a guest's RWX pages as asked |
| 0c0ba0cf | build(windows): the Windows arm64 bundle, and the release builds all six platforms |
| dc151be8 | build(macos): Chimera for Apple silicon, beside the Intel build |
| e3169985 | build(aarch64): miniBox c03a0da, upstream main with the aarch64 port merged |
| e3001e8d | build(aarch64): miniBox 5032bac, the aarch64 branch on miniBox 0266ed5 |
| ef61382d | build(aarch64): miniBox c3ca2e9, whose aarch64 guests read their thread pointer from memory |
| 44e6e9dc | build(aarch64): miniBox 2817e29, whose build info names the CPU it was built for |
| a267f105 | build(aarch64): miniBox e734c62, whose aarch64 guest finds no host values in v0-v31 |
| d6968dd6 | build(aarch64): miniBox's host sources name x86-64 and aarch64, and refuse any other |
| 6b68f541 | build: miniBox 8e2c... the gs block helpers are macOS's only, so Linux builds again |
| 87d0f81a | build(aarch64): the native libraries build on an aarch64 host, with miniBox's aarch64 host |

## Obsolete: C# era

| Commit | Subject |
| --- | --- |
| c1ac3562 | feat(ui): the Qt window is Chimera's only window; the C# one is removed |
| bcf26d87 | feat(hotkeys): Open, Close and Load Last Project replace the ROM ones |
| 981b2343 | feat(gui): a game opens only in a project, back on the way to main |
| 4fa77daa | Revert "revert: "feat(gui): a game opens only in a project"" |


## Port batches and the tags they carry

The historical rule applies: a major feature starts a minor version and a smaller feature or fix is a patch. A version tag points at a commit in this fork, so it is made when its batch is ported, built and tested. The project format batch is deliberately not implemented (owner, 2026-11-14): it would change the files the C# client writes, and the fork keeps those stable.

| Tag | Kind | Batch | What lands | State |
| --- | --- | --- | --- | --- |
| 1.0.1 | patch | engine fixes | 6277b09c (build info) and 25848631 (a history of another machine leaves the greenzone as it was). c9d78d33, the Windows anchor fix, is held back: it needs later threading work from upstream and fails the state history test on this tree | done: state history passes and the Windows cross-build passes; C# builds. media and sha1 fail on this case-insensitive, 9p checkout for filesystem reasons, not from these changes |
| 1.1.0 | minor | player names | ed955dfb, made for chimera-core-unrealspeccyp: a package's input declaration may name its player groups, and TAStudio's column menu and Show toggle use those names instead of "Player n" | done: C# builds; the declaration reaches ControllerDefinition; not opened in a window |
| 1.2.0 | minor | languages (core configuration strings) | a package built after nightly-2026-10-27 names settings, file slots and firmware text by key; the window reads the package's languages/en.json and shows the words, and an older package is unchanged. The engine's own text-key refactor (999f79dd, 7013a849) is held: it rewrites every engine text and the Qt window, which this C# window cannot show | done: five new tests pass; the unrealspeccyp package resolves Machine, its 128K option label, and the slot title and help |
| 1.3.0 | minor | MCP and Lua | event.onloadstate, the engine's memory-callback ABI and its C# surface, and the waterbox core's IDebuggable wiring are done: scopes, watches, kind/size flags, a replaceable read or write, and event.can_replace_value. Next: a branch's scripts' userdata in the project, then the C# MCP tool surface | in progress; no tag until the batch is whole |
| 1.4.0 | minor | cores | the nine roster commits, once each core has a package built against the 1.0.x engine | not started |

## Appendix: every commit

Newest first. The subject is upstream's own.

| Commit | Verdict | Subject |
| --- | --- | --- |
| c9d78d33 | engine candidate | fix(greenzone): a greenzone anchor no longer makes a frame late on Windows (#548) |
| 93b881b8 | skip (Qt) | feat(project): a project can reopen its tool windows and their files (#587) |
| a4ecc4e5 | engine half only | feat(greenzone): a marker on the machine's frame keeps its state at once (#578) |
| 9b1e0da9 | skip (Qt) | feat(lua): the Lua Console's Script column can be widened, and its width is kept (#586) |
| f2a3293b | skip (Qt) | feat(qt): a long menu has arrows at its ends that scroll its rows (#588) |
| 9de4c502 | engine candidate | test(project): a slot with no formats takes any file, and the docs say so (#567) |
| 08c40487 | engine half only | fix(greenzone): Clear Greenzone gives back a budget halved for lack of memory (#577) |
| 6ba3049f | skip (Qt) | fix(tastudio): a double click on a button's cell changes it twice (#582) |
| ced1d661 | portable | docs(agents): AGENTS.md says who the owner is (#580) |
| 13ccd57e | skip (Qt) | fix(qt): the Light theme stays light under a dark KDE colour scheme |
| a0b446f8 | skip (Qt) | test: two checks no longer fail on a fast machine or a mixed build |
| 858d386b | portable | ci(linux): x86_64 Linux builds stay on Ubuntu 24.04 (owner, 2026-11-12) |
| 882ca20e | portable | feat(cores): bsnes v115 joins the official cores (#572) |
| f76a8cca | skip (Qt) | fix(qt): a bound Alt held down no longer hands the menus the keyboard (#573) |
| 81725c0a | engine half only | feat(mcp): run_lua with frames: the agent's code may wait for frames, played for it (#571) |
| 1c17c46a | engine half only | feat(mcp): chimera-mcp lists the window's own tools, says another build at once, and shows in Tools > MCP Server... (#571) |
| 911d41f0 | engine half only | feat(mcp): trace_memory and trace_cpu; cores may say which instruction made an access, and their registers' roles (#571) |
| ab4ccfb2 | engine half only | feat(mcp): try_input, snapshot and restore, seek with extend, length changes said (#571) |
| 07e95e11 | engine half only | feat(mcp): the picture with what Lua draws, and the Lua Console read: lua_console, lua_scripts, errors (#571) |
| ed50f989 | engine half only | feat(mcp): screenshot crops and colours, diff_memory, advance_until on a range, a shorter status (#571) |
| cc15df81 | engine half only | feat(mcp): the agent's RAM search does what the engine's does, and several rounds in one call (#571) |
| bea85f29 | engine half only | feat(mcp): an agent is told when its memory writes are undone; freeze tools; hex addresses (#571) |
| 05075e8f | skip (Qt) | fix(ui): the window says a project is opening while its core loads it (#566) |
| 49361067 | engine half only | feat(mcp): an agent is told when chimera-mcp and the window are different builds; run_lua stops code that does not end (#571) |
| 37d6f615 | skip (Qt) | fix(ui): the frame counter follows edits, and a sharp picture is not grown first (#568) |
| bf809703 | skip (Qt) | perf(qt): the window copies each frame once, into an image it keeps (#568) |
| 8e1fc415 | skip (Qt) | perf(ui): TAStudio skips columns past its edge, RAM Search copies only pages still shown, run markers move in place (#568) |
| c511049a | skip (Qt) | fix: a memory function put back at once is called; the window follows the core's rate; chimera-run starts from bin\ (#570, #569, #565) |
| 38fccbeb | engine half only | fix(engine): Clear Greenzone gives memory back and leaves the machine where it is (#564) |
| a0f59b38 | skip (Qt) | feat(qt): scripts dropped on the Lua Console are opened (#561) |
| 11c5f0ae | skip (Qt) | fix(lua): a script's extra padding goes once no script runs (#562) |
| 5f58a8fa | engine half only | fix(engine): a step of rewind with the greenzone off can be stopped (#559) |
| 1a6890d5 | skip (Qt) | fix(ui): Recent Projects lists a project once however its path is spelt (#556) |
| 4629dcfd | engine candidate | feat(engine): a core whose guest reads its dead stack says so (#555) |
| 509682de | skip (Qt) | feat(qt): RAM Search and RAM Watch pick rows with a drag (#554) |
| d5d3ebd6 | engine half only | fix(ui): the window no longer slows down as the movie gets longer (#548) |
| d5ed6f9e | portable | Point at runsOn necessity in porting-a-core.md |
| 2fb4bb2c | skip (Qt) | fix(cores): New Project offers only the packages this Chimera can run (#549) |
| 701904c5 | skip (Qt) | fix(qt): a menu closed gives the keyboard back to the game (#552) |
| 0602a4da | portable | docs(cores): list NP2kai, ShinobiWannabe's PC-98 core, as a third-party core (#550) |
| f86bbb02 | engine half only | feat(greenzone): every marker's frame is kept in the greenzone, unless it is off |
| 9dbe8903 | portable | test(engine): the state history test has 120 s on a CI runner, not 30 |
| b0ef2916 | skip (Qt) | feat(encode): Encode Video can write a frame rate of its own (#547) |
| 06aa19f7 | portable | docs(issues): the bug report asks whether the upstream core has the issue too |
| 7ccdfb7f | skip (Qt) | feat(tastudio): a core names the controls TAStudio shows at first (#544) |
| fefa3aa8 | skip (Qt) | fix(qt): a drive's disk name fills the status bar's room, cut in the middle only when it must (#545) |
| 7103e435 | engine half only | fix(greenzone): a budget changed applies to the open project at once (#543) |
| 02c68d56 | skip (Qt) | fix(tastudio): a trackpad scrolls the roll, and unbound arrow keys move its picked frame (#542) |
| ae70256d | portable | docs(platforms): 86Box publishes an aarch64 package too, without its recompiler |
| fec47a21 | hold (sandbox) | build(minibox): aarch64 trampolines fit any image size; aarch64 for emulator cores |
| 4f78d754 | skip (Qt) | fix(qt): a modal window is a sheet on a Mac, not over other apps (#541) |
| e2a104f0 | skip (Qt) | fix(qt): OK keeps a typed number on a Mac, the lists' text size, messages over the idle picture (#540 #539 #535) |
| dc263afb | portable | refactor: the repository's Themes folder is themes, as the bundle's is |
| 78589c0b | skip (Qt) | fix(encode): Current size is the machine's picture times the window's scale |
| 3f3f9ba0 | skip (Qt) | fix(bundle): the bundle's folders are lower case: cores/ and themes/ |
| 54c7aa71 | portable | docs(platforms): FreeJ2ME publishes an aarch64 package too (#536) |
| 075e5f33 | skip (Qt) | fix(qt): a Mac shows its own key names: Control, Option, Command (#538) |
| 501e0cff | portable | fix(bundle): the Linux bundle ships libxkbcommon beside libxkbcommon-x11 (#533) |
| a092a060 | skip (Qt) | fix(cores): Open Cores Folder makes the folder when it is not there yet (#537) |
| 06066f3c | engine half only | feat(lua): memory functions get flags and may change a value; branches keep userdata; event.onloadstate (#534) |
| cba75048 | portable | ci(release): a nightly is never of a commit older than main's workflows |
| 0b091461 | portable | ci(release): a nightly is the newest commit of main that passed CI |
| 1fa74b95 | portable | docs: an issue closes when its fix is pushed, not when a nightly has it |
| 1a708b57 | portable | docs(readme): the casual-play sentence leaves the Goals section |
| cfc7e46a | portable | docs(readme): the Core Manager line leaves the Cores section |
| d8a2c68e | portable | docs: issues also present upstream are not fixed nor reported; a shorter README (#491) |
| c33d4809 | skip (Qt) | feat(qt): Config > Display is a grid of options, each described (#527) |
| b3654bfc | skip (Qt) | fix(lua): Register To Text Editors and Duplicate to Stdout leave the Lua Console's menu (#529, #530) |
| ae9d0b1c | skip (Qt) | fix(qt): the picture's right-click menu always offers Show Menu or Hide Menu (#526) |
| 0d25ce1f | portable | ci(release): nightlies only, for Chimera and every core |
| 09d87d78 | portable | feat(release): nightlies only, and file names say x86_64 or aarch64 |
| 59ae78b3 | engine half only | feat(engine): each core package says which systems it runs on |
| b9c65cd3 | hold (sandbox) | build(minibox): main takes miniBox 3848e4e, so cores can be built for an Intel Mac |
| 8fc72d58 | skip (Qt) | fix(macos): the sandbox's log is kept in the data folder on a Mac |
| 8d01c0d2 | skip (Qt) | fix(lua): client.sleep waits its whole time on Windows too |
| c52a981a | skip (Qt) | fix(tastudio): a Copy whose clipboard is busy is set again, and said when it never takes (#485) |
| 0173b58b | portable | fix(macos): the Mac app carries LICENSES.md, with Qt's licence, as every bundle does |
| 9bfe8eb8 | portable | docs: the six builds - which to download, how each is built and tested, what runs where |
| cd2b6739 | portable | ci(windows): the window's tests on Windows have the ffmpeg the release ships |
| 68687ac4 | skip (Qt) | fix(windows): the window's tests pass on Windows: its style, fonts to measure with, a whole bundle |
| 81df982e | skip (Qt) | fix(qt): more of one tester's reports: About centred, snow boxes, RAM Watch columns and on-screen watches, Real Time at the left (#517-#524) |
| 2c962317 | skip (Qt) | feat(qt): Macro Input is a step-by-step wizard (#511) |
| 107be121 | skip (Qt) | feat(qt): save data gets its own folder; Core-Specific Tools is one list (#500, #502) |
| 62213f18 | skip (Qt) | fix(qt): fixes from one tester's reports: closing TAStudio while playing, window sizes, save names (#498-#516) |
| 0c232d97 | portable | docs(readme): the star counter links to the repository's page |
| 552d998a | portable | ci(windows): the window's tests run on Windows, against the Qt the release ships |
| b9409b23 | skip (Qt) | test(windows): the test pack can carry the window and the tests a Python script runs |
| 9247b0f6 | portable | ci(macos): the Mac icon is compiled by Apple's actool, on macOS 26 for Apple silicon |
| 3699b622 | portable | fix(macos): an actool that failed no longer passes for the next one |
| 55fad24f | portable | ci(aarch64): the Mac icon is compiled by a clean-room actool, as Apple's does not run on the runner |
| 6bf59445 | portable | ci(macos): the Apple silicon app is built on macOS 26 |
| c882b0bb | portable | ci(release): each Windows bundle is started on a Windows machine of its CPU |
| 69b90628 | portable | ci: the Windows arm64 bundle's Qt build can run Qt's tools; the Mac icon after the tests |
| 930091ad | portable | ci(release): a pull request that changes how bundles are made builds all six as a trial |
| 1a38f9c9 | portable | ci(macos): the icon is compiled with an Xcode 26 whose actool works on the runner |
| 2024cdf7 | portable | ci(macos): Xcode's first launch before the icon; actool says why it failed |
| f39a9e5d | portable | ci(macos): measure how late a runner's timers wake, and turn coalescing off for the tests |
| b3ccfb82 | skip (Qt) | test(view models): the throttles' measurements are printed; the 48 kHz check counts dropped sound |
| 61e70400 | hold (sandbox) | test: the Windows tests get the packages they open; Apple silicon's test core has no fault handler |
| 26745486 | skip (Qt) | build: miniBox a3d706f, so an Intel Mac runs cores with thread-local variables |
| 0c7c5a68 | portable | ci(windows): every test's whole output is kept, and uploaded when one fails |
| 62ffad27 | skip (Qt) | ci(windows): the tests are built as the release builds them, and pass on Windows x64 |
| a9f77da3 | engine candidate | test(state history): spill folders are removed after the history lets go of its file |
| 52dea77b | engine half only | feat(lua): a core may call back at the memory a script watches (#496) |
| 84312d64 | engine candidate | fix(windows): what the first run of the engine's tests on Windows on arm64 found |
| 2fdcbad9 | hold (sandbox) | build: miniBox 23fa1c3, Windows on arm64's halts, wild calls and x18 |
| 3ecb76aa | portable | build(ffmpeg): x265's two OLD policies go, which CMake 4 refuses |
| d7522c63 | portable | docs(design): Qt stays as it is built (#492) |
| 19cd8d31 | engine candidate | test: the media and state history tests read their files as Windows has them |
| 471b316b | hold (sandbox) | build: miniBox 2f20665, the TEB's stack bounds on Windows on arm64 |
| b2ed1d1d | portable | build(ffmpeg): x265 configures under CMake 4 |
| f6743848 | hold (sandbox) | build: miniBox b68ad43, an Intel Mac mapping a guest's RWX pages as asked |
| 1586ed46 | engine candidate | ci(windows): Chimera's tests run on Windows, x64 and arm64 |
| 0c0ba0cf | hold (sandbox) | build(windows): the Windows arm64 bundle, and the release builds all six platforms |
| f55300df | engine half only | build(windows): Chimera's engine and tools build for Windows on arm64 |
| 15397670 | skip (Qt) | fix(tastudio): a held key clicks a button once; branches and markers are picked and removed together (#494) |
| 9d9153dd | portable | docs(readme): a box under the tagline asks readers to star the project |
| dc151be8 | hold (sandbox) | build(macos): Chimera for Apple silicon, beside the Intel build |
| c5b26b48 | portable | feat(cores): ScummVM and Cemu join the official cores (#465, #459) |
| 04e956e2 | portable | build(ffmpeg): its sources come from GitHub when ffmpeg.org or Debian cannot be had (#492) |
| fa499b9c | portable | build(ffmpeg): the shipped ffmpeg writes and reads AVI too (#492) |
| bf821013 | skip (Qt) | build(ffmpeg): Chimera builds the ffmpeg it ships, with only what Encode Video uses (#492) |
| 5dd0f378 | skip (Qt) | feat(display): a size for the on-screen text when it does not grow with the window (#488) |
| f0cc3a1e | engine candidate | perf(engine): a full greenzone thins a little every frame, its merges on a helper thread (#482) |
| 60034261 | skip (Qt) | fix(tastudio): new branches and markers are picked and shown; Update, Follow Cursor and Enter work (#483 #484 #486 #487 #490) |
| e3169985 | hold (sandbox) | build(aarch64): miniBox c03a0da, upstream main with the aarch64 port merged |
| e3001e8d | hold (sandbox) | build(aarch64): miniBox 5032bac, the aarch64 branch on miniBox 0266ed5 |
| ef61382d | hold (sandbox) | build(aarch64): miniBox c3ca2e9, whose aarch64 guests read their thread pointer from memory |
| 9a80c516 | portable | docs: most users want the x64 package, wherever both CPUs are offered |
| 44e6e9dc | hold (sandbox) | build(aarch64): miniBox 2817e29, whose build info names the CPU it was built for |
| 6277b09c | engine candidate | fix(aarch64): the engine's build info names the CPU it was built for |
| a267f105 | hold (sandbox) | build(aarch64): miniBox e734c62, whose aarch64 guest finds no host values in v0-v31 |
| afb1bfa8 | portable | build(publish): a package's CPU is x64 or arm64, the names the frontend's bundles use |
| 20eee932 | portable | release: an arm64 Linux bundle beside the x64 ones |
| 8ae2624e | portable | build: fetch-ffmpeg.sh takes the ffmpeg for the CPU it builds on |
| ff10d9f3 | portable | ci: the witness jobs run on aarch64 as well as x86-64 |
| d6968dd6 | hold (sandbox) | build(aarch64): miniBox's host sources name x86-64 and aarch64, and refuse any other |
| f9bf2d01 | portable | build(publish): a core publishes one package per CPU, <id>-<version>-<arch>.chimeraCore |
| 6f9f25b4 | skip (Qt) | fix(session): recording mode is the window's alone; two tests wait instead of sleeping |
| 6b68f541 | hold (sandbox) | build: miniBox 8e2c... the gs block helpers are macOS's only, so Linux builds again |
| 78414eac | skip (Qt) | fix(macos): what the tests with the synthetic core found, and CI runs them one at a time |
| d46eae44 | portable | docs: the icon's SVGs are kept once, in Chimera.icon/Assets, and nothing links to them |
| af19c99e | portable | docs: docs/icon.svg and docs/icon-dark.svg are links to the files in Chimera.icon |
| cab5e682 | portable | feat(macos): the app's icon is the Icon Composer one, light and dark, in the app and in CI |
| ad34a3a7 | engine half only | fix(macos): the build asks for macOS 14.4, not for the version of the Mac that made it |
| a5000c14 | portable | ci(macos): the Linux job builds miniBox's guest compiler and runtime object by name |
| 75b23d9f | portable | ci(macos): build, test and package Chimera for macOS x86-64 on an Intel runner |
| f331a28d | skip (Qt) | fix(macos): the Dock keeps the app's icon, not the small picture the program set at start |
| 0f7e5424 | skip (Qt) | feat(macos): settings, cores and Config > Paths' folders are in ~/Library/Application Support/Chimera |
| ec045963 | skip (Qt) | feat(macos): Chimera.app, the window and everything it runs on in one app |
| 61e0cfb9 | portable | feat(macos): ffmpeg 4.4.1 for macOS, and fetch-ffmpeg.sh runs on a Mac |
| f4c3b2f9 | skip (Qt) | feat(macos): the menus are in the system menu bar, and the windows have no bar of their own |
| b26a788b | skip (Qt) | feat(macos): the Qt window builds for x86-64 macOS and plays a game |
| 55307a92 | engine candidate | feat(macos): the engine and miniBox build for x86-64 macOS and pass their tests |
| 87d0f81a | hold (sandbox) | build(aarch64): the native libraries build on an aarch64 host, with miniBox's aarch64 host |
| 9d7b85fe | skip (Qt) | fix(qt): Capture OSD keeps the counters (#477); a script's TAStudio cells are asked once a drawing (#476) |
| 08ee9de1 | engine half only | feat(project): a project is a zip of its JSON and its input's lines; the greenzone is written when it closes |
| d43bb214 | engine half only | feat(tools): a core's tool is given no game file, so any name but tool and settings is its own |
| af99c84b | engine candidate | fix(engine): chimera-run in a bundle gives its texts as sentences, not keys |
| 7beeb4e8 | portable | docs(cores): Gens-Rerecording joins the third-party cores |
| a8655e12 | skip (Qt) | fix(lua): a script's TAStudio column is one column, and goes with its script (#475) |
| 6862ee93 | engine half only | fix(qt): readable menus and lists in dark mode, the Hex Editor's rows and columns, 8-byte values (#471 #472 #473 #474) |
| f310b9b0 | skip (Qt) | fix(qt): Config > Messages lists Game Time only with a game timer (#470) |
| c8ee037f | skip (Qt) | fix(hotkeys): a gamepad input bound to a hotkey runs it (#466) |
| d0d04efa | skip (Qt) | fix(session): a package's renderer counts only on the machines it is for (#446) |
| 03f106b6 | skip (Qt) | fix(tastudio): an input roll gets no more than its share of the room (#442) |
| cdb708a4 | engine half only | fix(qt): the menu bar keeps its words after full screen; the project description is gone (#451 #440) |
| 5ad79a22 | skip (Qt) | feat(customize): a project opened or made starts paused |
| 685d8031 | engine half only | feat(tastudio): Header Info, Comments and Subtitles are gone (#439 #440) |
| 3040d840 | skip (Qt) | fix(qt): Controller Config builds only the page shown, and Stay on Top keeps the title bar (#438 #443 #441) |
| d133badd | skip (Qt) | fix(qt): menus as wide as their rows, below the menu bar, closed with their window (#452 #450 #463 #449) |
| 6cff4c3d | skip (Qt) | fix(session): an edit lets a running machine run on, and the last game's picture is not shown (#454 #432 #464) |
| b5d5342c | skip (Qt) | fix(qt): Escape empties a key box, no "(Finished)" while recording, the speed kept when paused, folders named as before (#431 #433 #448 #461 #435) |
| 972a7cd0 | engine half only | fix(memory): buses are memory domains again, and RAM Search searches when it opens (#456) |
| 041dca9d | skip (Qt) | fix(tastudio): Ctrl+C and Enter work again, a click gives the roll the keyboard, a double click sets a marker (#453 #437 #455 #462 #436) |
| 14037f6e | engine half only | feat(tools): a core's own tools, under Tools > Core-Specific Tools |
| 8ee74abf | engine candidate | feat(languages): the engine's table check holds proper nouns |
| 27ff230d | portable | feat(cores): Mesen's roster entry lists the Pocket Challenge V2 (#410) |
| 142aebeb | portable | feat(cores): 86Box, ZEsarUX and RSDKv5U join the official cores (#408, #407, #409) |
| 512d7ed2 | portable | feat(cores): Mesen runs every console Mesen emulates (#410) |
| b8a30557 | portable | docs(cores): official and third-party cores are listed apart |
| a398c039 | portable | feat(cores): Mesen joins the official cores (#410) |
| 46c463ed | portable | feat(cores): Supermodel joins the official cores (#354) |
| cb46d2b7 | skip (Qt) | fix(texts): proper nouns are never translated; a report's build is English |
| 3f8ea7e2 | engine half only | feat(qt): View > Display Real Time |
| 924e3708 | skip (Qt) | feat(qt): the Language menu names each language in English too (#430) |
| 0f6692a7 | skip (Qt) | feat(qt): the Emulator menu holds only what the core offers (#428) |
| 4216be00 | engine half only | fix(qt): Report an Issue names the graphics card (#427) |
| c15e9c98 | skip (Qt) | fix(tastudio): auto-restore is off until it is ticked (#426) |
| ac8d4716 | skip (Qt) | fix(qt): every file dropped on a New Project slot is added (#425) |
| 7672cf02 | skip (Qt) | fix(qt): a mouse button let go is told where the pointer was (#424) |
| 68d7fbb6 | skip (Qt) | fix(recovery): the crash record is put in place whole, as the snapshot is |
| d1e5f117 | skip (Qt) | fix(qt): New Project looks at the cores folder each time it opens (#423) |
| cf0b1474 | skip (Qt) | fix(qt): RAM Watch and RAM Search lists have their grid again |
| 492d2055 | skip (Qt) | fix(qt): Capture OSD writes the window's picture size; text breaks at new lines; Lua Console grid |
| 36c2bffe | skip (Qt) | fix(qt): New Project waits for the project it makes and says why one is refused (#422) |
| 9db55824 | portable | test(texts): Qt's logging rule is a name, not a text (#419) |
| 46f2af95 | skip (Qt) | fix(ui): mute Qt's Wayland text-input warning (#419) |
| 7b20b177 | skip (Qt) | feat(ui): on Linux, files and folders are picked with the desktop's own chooser (#420) |
| cad85ea0 | skip (Qt) | fix(qt): Controllers says what its tick boxes do (#418) |
| ee404150 | skip (Qt) | fix(qt): settings are shown in Chimera's grid (#417) |
| 535c191d | skip (Qt) | fix(display): the machine's own shape is the size it declares (#414) |
| 510239af | skip (Qt) | fix(qt): a project that does not open says why in a box (#422) |
| 668411fb | skip (Qt) | feat(qt): Config > Language has an icon |
| 76738ec7 | skip (Qt) | fix(tastudio): the window waits on nothing after a frame (#404) |
| f925b957 | skip (Qt) | fix(tastudio): the piano roll keeps up with the game (#404) |
| e3cf918c | skip (Qt) | feat(windows): chimera.exe carries version information |
| c1abf86d | portable | test(engine): the RAM search test may take two minutes on a CI runner |
| 23ef2de0 | engine half only | fix(input): every button reaches the movie, not only the first 64 (#411) |
| 25848631 | engine candidate | fix(engine): a history of another machine leaves the greenzone as it was |
| 7ccf90ad | portable | ci: the published cores' check is its own workflow, so no core can block CI |
| fa8cd6e0 | skip (Qt) | fix(tastudio): a short way back after an edit with Turbo seek is one seek again (#406) |
| 196edc5b | skip (Qt) | fix(ui): a key a hotkey takes no longer opens a menu (#405) |
| e1fe294c | skip (Qt) | feat(ui): the text over the picture grows with the window (#401) |
| 44d3fdec | skip (Qt) | feat(ui): the settings file is beside the program (#403) |
| 559866d5 | skip (Qt) | fix(tastudio): the way back after an edit no longer freezes the window (#406) |
| e38cae0c | skip (Qt) | fix(ui): Config > Paths folders are used again, and Save As proposes a name (#402, #399) |
| 6a49eb78 | skip (Qt) | fix(project): a game dropped for New Project does not take the last game's files (#398) |
| 0ac8165b | skip (Qt) | fix(ui): closing TAStudio closes the project (#400) |
| f12244bd | skip (Qt) | fix(project): a project opened from another path drops the cache made for the other |
| f621249b | engine candidate | fix(languages): a core's texts come from any table it has |
| 9a162f31 | engine half only | fix(project): a project saved under another name gets an id of its own |
| 41ddb228 | portable | test(synth): the synthetic core's texts in every language |
| 9f1ea4ad | portable | feat(languages): Chimera in 26 languages |
| d460b63b | engine candidate | feat(languages): a reason inside a core's text has its own arguments, and a report is a text a line |
| 999f79dd | engine half only | feat(languages): every text of Chimera is a key; a core's texts are keys of its own tables |
| 7013a849 | engine half only | feat(languages): the window's texts are keys in one table per language |
| 1bb84087 | engine candidate | test(history): the state history tests hold the near band's stride |
| 10573e56 | skip (Qt) | fix(window): the picture is drawn opaque on Wayland too (#393) |
| b1e2e2ca | skip (Qt) | test(lua): the gameinfo check says what it got when it fails |
| 59ce4af0 | portable | fix(release): the Linux tarball unpacks into one folder (#394) |
| 1dcb97b6 | skip (Qt) | fix(tastudio): loading a branch asks nothing; the message warns instead (#395) |
| 159c251b | portable | docs(roster): the Godot 4.7 core is named for its version |
| 66d6a944 | engine half only | refactor(project): the old "input" string is not read |
| c8ae2b47 | engine candidate | feat(project): a project's input is its columns and its entries |
| fe66cadc | skip (Qt) | feat(ui): the window in seven languages, chosen in Config > Language (#376) |
| a0692bfd | engine half only | feat(controls): the core says which player each control belongs to; the roll draws lines between players |
| 7aeaf7c4 | skip (Qt) | fix(ui): Recent Projects is as wide as its longest path, each whole on hover |
| 43a70b57 | skip (Qt) | feat(project): missing firmware is asked for when a project opens (#397) |
| b9dda9a6 | skip (Qt) | fix(tastudio): right button + wheel seeks from the current frame |
| 09423c57 | skip (Qt) | feat(tastudio): TAStudio's columns are chosen in Config > Controllers (#391) |
| 136ad139 | portable | docs(encode): the default MP4 stays 4:2:0, by the owner's choice (#375) |
| ca75e07c | skip (Qt) | fix(ui): New Project asks about the open project's changes over itself, and pauses the machine (#392) |
| d6ac5398 | engine half only | fix(audio): a core's sound is converted to 44100 Hz again (#372) |
| 36c38040 | skip (Qt) | feat(ui): the window can be translated: the machinery, no language yet (#376) |
| 974c8d6a | skip (Qt) | fix(encode): videos keep the machine's picture shape, and the lossless presets every pixel (#375) |
| 23f25986 | skip (Qt) | fix(firmware): a file found by its name must also have the declared size (#379) |
| cce09178 | skip (Qt) | fix(compile): a game's compiled code belongs to the core build that made it (#383, #361) |
| 5456a0ea | skip (Qt) | test(session): fast forward's pictures are counted once they have arrived |
| dfa69252 | skip (Qt) | fix(osd): a long message over the picture wraps at the picture's width (#378) |
| ef9ed506 | portable | fix(bundle): the Linux bundle carries the folder list Qt's file dialogs need (#389) |
| b1642986 | skip (Qt) | fix(hotkeys): the Hotkeys window opens and closes at once (#388) |
| 3b04a0c0 | skip (Qt) | fix(dialogs): Qt's file dialogs are drawn inside their window, in the theme's colours (#389) |
| e47945e5 | skip (Qt) | fix(wizard): firmware entries sharing an id are told apart by the setting that picks them (#390) |
| 64df2643 | skip (Qt) | fix(crash): the window registers the crash module again, and says what a crash was |
| ba39f9de | portable | docs(licence): NOTICE names what still comes from BizHawk, not its frontend |
| 74022e7c | engine half only | refactor: the code and documents describe only Chimera as it is (owner) |
| 23534484 | skip (Qt) | fix(build): the files the last commit removed by mistake are back |
| 10725684 | skip (Qt) | fix(release): the Qt source and Linux build scripts are executable |
| 7f202eb9 | engine half only | fix(ui): controls are the chosen machine's, and an unplayable movie ends a headless run |
| 2e696077 | skip (Qt) | fix(ui): a saved project carries the game's timer at the movie's end again |
| c1ac3562 | obsolete (C# era) | feat(ui): the Qt window is Chimera's only window; the C# one is removed |
| c0fbf48f | skip (Qt) | feat(ui): what the cores' tests need of a headless run |
| bcf26d87 | obsolete (C# era) | feat(hotkeys): Open, Close and Load Last Project replace the ROM ones |
| f6cd8a04 | engine half only | feat(chimera-run): a project is the only way to a machine |
| 8e888145 | engine candidate | feat(chimera-run): a made project takes its movie with --movie |
| 6afea84e | engine candidate | fix(chimera-run): a made project's files are checked against its settings |
| 530b5591 | engine candidate | fix(chimera-run): a made project's machine follows its game's kind of file |
| 981b2343 | obsolete (C# era) | feat(gui): a game opens only in a project, back on the way to main |
| 7511d2b9 | skip (Qt) | feat(ui): --headless, for runs nobody watches |
| 7bf9f324 | portable | fix(ui): the Windows folder reads ICO, JPEG and GIF images |
| cbe647f0 | skip (Qt) | feat(ui): the program is chimera (chimera.exe on Windows) |
| ae185f1d | skip (Qt) | fix(ui): scripts' turns are not replaced while one waits on the window |
| 96b7e375 | skip (Qt) | fix(ui): scroll bars always seen; the clock start is one of the settings |
| d9eea30e | skip (Qt) | fix(ui): no console in a release; a cores folder; core log in Help |
| 4ea5ce50 | skip (Qt) | fix(ui): a project opens without firmware its package calls optional |
| a8383f79 | skip (Qt) | chore(ui): one function shows a window and brings it forward |
| 7fb3b1fe | skip (Qt) | test(ui): a Lua line that differs says what it was |
| b66f84cd | skip (Qt) | fix(ui): Load Config and the data folder's checks work on Windows |
| 68d769ea | skip (Qt) | fix(ui): the Windows build compiles again |
| b5ed112a | skip (Qt) | chore(ui): code nothing calls taken out |
| b1374da2 | skip (Qt) | feat(ui): Chimera's command line, drops by kind, and The core stopped |
| 2ccbedd2 | skip (Qt) | fix(ui): the review's findings before the window replaces Chimera's |
| 8b561c78 | engine half only | feat(chimera-run): a project is the only way to a machine |
| 627eba8c | engine candidate | feat(chimera-run): a made project takes its movie with --movie |
| b5954008 | engine candidate | fix(chimera-run): a made project's files are checked against its settings |
| 27a10f98 | engine candidate | fix(chimera-run): a made project's machine follows its game's kind of file |
| 4fa77daa | obsolete (C# era) | Revert "revert: "feat(gui): a game opens only in a project"" |
| efd4bab7 | skip (Qt) | feat(qt): a game runs only in a project; pointer sensitivity (owner) |
| c84bdd4f | skip (Qt) | feat(qt): bindings, paths, recent files and tool settings come from Chimera too |
| d3014f0d | skip (Qt) | feat(qt): Chimera's settings are carried over once, at the first start (owner) |
| c75e6d97 | skip (Qt) | feat(qt): Lua's gettool and getconfig give the Qt window's own (owner) |
| 2e1ab4a6 | portable | docs(qt): what is left of Chimera's, as of today |
| d212c609 | portable | docs(qt): the memory tools' freezes and the debugger's limits, said as they are |
| f9b79272 | skip (Qt) | feat(qt): Windows Fullscreen Hacks, as Chimera's Display window |
| 22fe5a31 | skip (Qt) | feat(qt): the snowy null core, as Chimera's Display window |
| 2ae0da51 | skip (Qt) | feat(qt): Scale OSD text with system scale, as Chimera's Display window |
| 52500c61 | skip (Qt) | feat(qt): User Prescale and Auto Prescale, as Chimera's Display window |
| cca905aa | skip (Qt) | feat(qt): Customize's GameCube adapter switch, as Chimera's |
| 2232bad3 | skip (Qt) | feat(qt): Chimera's single instance mode, on Windows |
| 81e4cc7e | skip (Qt) | feat(qt): keys are named by their place, as Chimera's, with its two key options |
| bbfab66f | skip (Qt) | feat(qt): forms.openfile shows the file dialog and waits, as Chimera's |
| 6de26d91 | skip (Qt) | feat(qt): a game's own names for its controls, in the column titles' tooltip |
| b16d5fee | skip (Qt) | feat(qt): Chimera's jump list on Windows; a game alone on the command line |
| b075c6a8 | skip (Qt) | fix(qt): gameinfo says the game as Chimera's does |
| 62b18153 | skip (Qt) | feat(qt): gui's client surface and images drawn with cache false |
| 2219e506 | skip (Qt) | feat(qt): a turbo seek can be stopped part way |
| 229b8bb2 | skip (Qt) | feat(qt): compile sessions sized and halved as Chimera's; CHIMERA_DATA_HOME |
| 08ef7dc2 | skip (Qt) | feat(qt): the cheats question, firmware candidates, capture on top |
| 8da264fb | skip (Qt) | feat(qt): client.show_future, the picture's geometry and extra padding |
| c6779724 | skip (Qt) | feat(qt): 78 more of Chimera's Lua functions, and its full functions list |
| 2ee8a247 | skip (Qt) | feat(qt): Chimera's MCP server is in the window (#355) |
| fedcf2a9 | skip (Qt) | feat(qt): a setting may be a uint, a long, a ulong or a date and time (#358) |
| aecfab58 | skip (Qt) | fix(qt): registering with Sublime Text makes its Lua folder, as Chimera's |
| 31064342 | skip (Qt) | fix(qt): check boxes and radio buttons can be seen on a dark theme |
| 8ec823f1 | skip (Qt) | feat(qt): the piano roll's drags as Chimera's |
| f7913250 | skip (Qt) | fix(qt): TAStudio's menus have Chimera's items and no others |
| 9734fdfa | skip (Qt) | feat(qt): the piano roll is drawn as Chimera's |
| 114845bd | skip (Qt) | feat(qt): the main window's status bar is Chimera's |
| 683bd6b0 | skip (Qt) | feat(qt): TAStudio's side and status line as Chimera's |
| 0e7fba8e | skip (Qt) | feat(qt): an open project's settings can be changed (#370) |
| 39a3bb94 | skip (Qt) | refactor(qt): the folders the window makes are named in lower case |
| 9a61f1e4 | engine half only | build(qt): the Windows folder holds the program; DLLs in lib, tools in bin |
| 4d13ae70 | portable | build(qt): Qt for Windows is patched where its thread clean-up faulted |
| 7c302c3e | engine half only | fix(qt): Flash pictures keep their shape, right button + wheel seeks, Chimera's icon |
| bdb81d07 | skip (Qt) | fix(qt): a core with a hardware renderer gets the GPU bridge |
| 417527bb | skip (Qt) | feat(qt): the menus have Chimera's icons and dividers that can be seen |
| 58977342 | skip (Qt) | fix(qt): a movie line is read from its first '/', for paste and Lua (#142) |
| c82198f9 | skip (Qt) | fix(qt): Windows folders chosen in a dialog are read right (#142) |
| e5f726e4 | portable | fix(qt): the Windows folder carries SQLite's DLL (#142) |
| f83e0d21 | engine half only | feat(qt): New Project compiles a game's code first, as Chimera's (#142) |
| 559ed748 | skip (Qt) | feat(qt): paste takes lines copied with frame numbers; Chimera's draw menu words (#142) |
| 48f2a082 | skip (Qt) | feat(qt): Turbo and unthrottled draw one frame in N, 3 at first (#142) |
| 26eee5a2 | portable | docs(qt): "Yours to decide" points to where the answers are (#142) |
| 78b6f830 | portable | docs(qt): the owner's answers on Qt for Windows, going public and macOS (#142) |
| 08c546dd | skip (Qt) | feat(qt): the Firmware window finds, folds and opens a file's folder (#142) |
| 7b481b78 | skip (Qt) | feat(qt): loading a branch offers to update the current one first (#142) |
| a616f4c5 | skip (Qt) | feat(qt): the piano roll's frame numbers take the Frame# Column colours (#142) |
| e3612ea6 | engine half only | feat(qt): New Project asks when the machine's clock starts (#142) |
| 3928a4d9 | skip (Qt) | fix(qt): the Cache Manager picks the open or last used project at first (#142) |
| 5435296b | skip (Qt) | feat(qt): no frame skip; every frame run is drawn (#142) |
| 8f135014 | skip (Qt) | feat(qt): nothing pauses the machine but the person (#142) |
| 78ee6efc | skip (Qt) | feat(qt): Encode Video's Capture OSD and Capture Lua (#142) |
| 1be1d1aa | skip (Qt) | fix(qt): Unfreeze All and Unfreeze Address show in the memory tools (#142) |
| 59f19dc9 | skip (Qt) | feat(qt): Lua scripts run on the machine's thread, around every frame (#142) |
| f64d5751 | engine half only | feat(qt): the session journals its own movie against a crash (#142) |
| a4ba7da7 | engine half only | feat(qt): frozen addresses for this session only, kept by the engine (#142) |
| 3f9a3ff6 | skip (Qt) | feat(qt): TAStudio's recording mode, as Chimera's (#142) |
| 5503d28d | skip (Qt) | fix(qt): error messages in the theme's error colour (#142) |
| f6e2c224 | skip (Qt) | fix(qt): the chosen theme colours every control from the start (#142) |
| 216d7fb6 | skip (Qt) | fix(qt): firmware below the Firmware folder is in it, as Chimera now says (#142, chimera#359) |
| ef329a5b | portable | docs(qt): Cancel Seek is listed as not here, and why (#142) |
| 174b6ee0 | skip (Qt) | feat(qt): TAStudio's several input rolls, as Chimera's (#142) |
| fe83d84f | skip (Qt) | feat(qt): Window Size as Chimera's, with each system's scale and Static Size (#142) |
| d73dddee | portable | docs(qt): the Cache Manager's budgets button is no longer listed as missing (#142) |
| c7149b98 | skip (Qt) | feat(qt): the pointer as a relative axis, and Chimera's Capture Mouse (#142) |
| 23a759a8 | skip (Qt) | feat(qt): Register To Text Editors writes Chimera's Sublime Text 2 completions (#142) |
| 5322a613 | skip (Qt) | feat(qt): the Lua Functions List has Chimera's columns and descriptions (#142) |
| a08732e0 | skip (Qt) | feat(qt): Lua forms stay in the platform's colours and over the main window (#142) |
| d84b7de2 | skip (Qt) | feat(qt): Locate Project Files is laid out and worded as Chimera's (#142) |
| 97a48af9 | skip (Qt) | feat(qt): Undo History is laid out as Chimera's (#142) |
| b21e8cc6 | skip (Qt) | feat(qt): TAStudio Settings is laid out as Chimera's, with Apply and Cancel (#142) |
| d5fb2fc0 | skip (Qt) | feat(qt): New Project is Chimera's wizard, and movie import its own form (#142) |
| b77adc10 | skip (Qt) | feat(qt): Encode Video words its progress and refusals as Chimera's (#142) |
| 2b7ff935 | skip (Qt) | feat(qt): Report an Issue is laid out and worded as Chimera's (#142) |
| 27687399 | skip (Qt) | feat(qt): the Media Maker is laid out and worded as Chimera's (#142) |
| ae27912e | skip (Qt) | feat(qt): the Batch Runner is laid out and worded as Chimera's BatchRun (#142) |
| 033ea6ea | skip (Qt) | feat(qt): Pre-Compiled Modules as Chimera's (#142) |
| c24a92fa | skip (Qt) | feat(qt): Greenzone budgets as Chimera's, for the Cache Manager's project (#142) |
| af291eb5 | skip (Qt) | feat(qt): the Cache Manager as Chimera's (#142) |
| 0a60dc9b | skip (Qt) | feat(qt): the Core Manager as Chimera's (#142) |
| 961aaff7 | skip (Qt) | feat(qt): Go to Frame as Chimera's (#142) |
| c5def937 | skip (Qt) | feat(qt): Autofire Configuration in Chimera's words and layout (#142) |
| de7e5a59 | skip (Qt) | feat(qt): the Theme window as Chimera's chooser, with its preview (#142) |
| 78e4b85e | skip (Qt) | feat(qt): Data Directory laid out and asking as Chimera's (#142) |
| ba8260d3 | skip (Qt) | feat(qt): the Firmware window as Chimera's survey (#142) |
| 4420ab85 | skip (Qt) | feat(qt): Path Configuration as Chimera's, with per-system folders (#142) |
| b336377b | skip (Qt) | feat(qt): Configure On Screen Messages laid out as Chimera's (#142) |
| 4ac3323c | skip (Qt) | feat(qt): Sound Configuration laid out as Chimera's (#142) |
| 8ad9b798 | skip (Qt) | feat(qt): Display Configuration and the window's chrome as Chimera's (#142) |
| c4990082 | skip (Qt) | feat(qt): the picture's right-click menu, double click and Customize as Chimera's (#142) |
| 62591b21 | skip (Qt) | feat(qt): the Configure Hotkeys window and its view model (#142) |
| 942dd0d1 | skip (Qt) | feat(qt): Configure Hotkeys laid out and working as Chimera's (#142) |
| 391fb29e | skip (Qt) | feat(qt): Controller Config laid out and working as Chimera's (#142) |
| e59945d4 | skip (Qt) | feat(qt): the Surface Viewer laid out as Chimera's (#142) |
| ed059ac2 | skip (Qt) | feat(qt): the Debugger laid out as Chimera's (#142) |
| 764b8913 | skip (Qt) | feat(qt): the Log Window picks lines as Chimera's and has its Settings menu (#142) |
| ff8f97f2 | skip (Qt) | feat(qt): the Trace Logger laid out as Chimera's; Paths has Tools and Debug Logs (#142) |
| cd017f14 | skip (Qt) | feat(qt): Macro Input laid out as Chimera's (#142) |
| 277b2bb4 | skip (Qt) | feat(qt): the Hex Editor laid out as Chimera's, with its keys and menus (#142) |
| 5d44090e | skip (Qt) | feat(qt): RAM Search laid out as Chimera's, with its settings (#142) |
| 62a87786 | skip (Qt) | feat(qt): RAM Watch laid out as Chimera's, with its menus and dialogs (#142) |
| 6fad12f3 | skip (Qt) | feat(qt): tool windows get Chimera's window items in Settings (#142) |
| ba1664e5 | skip (Qt) | feat(qt): the Lua Console laid out as Chimera's, a list of scripts (#142) |
| 15c6992e | skip (Qt) | test(qt): the settings test hears the answer as it is said (#142) |
| 14ad2988 | skip (Qt) | feat(qt): tastudio.onqueryitemicon and clearIconCache (#142) |
| f556cc49 | skip (Qt) | feat(qt): tastudio's own columns and cell callbacks (#142) |
| 8c8e6743 | skip (Qt) | feat(qt): Shift held drags the main window without TAStudio (#142) |
| c80454da | skip (Qt) | feat(qt): Chimera's audio and vsync throttles (#142) |
| 16320a79 | skip (Qt) | feat(qt): TAStudio's Rotate input roll (#142) |
| fca782f6 | skip (Qt) | fix(qt): Chimera's separate windows and their titles (#142) |
| 0f4549c2 | skip (Qt) | feat(qt): tastudio's greenzone and branch callbacks (#142) |
| 5990fa7f | skip (Qt) | feat(qt): Chimera's --userdata pairs (#142) |
| 86622431 | skip (Qt) | feat(qt): Lua's SQL library (#142) |
| ee82f13b | skip (Qt) | feat(qt): Lua's comm library, memory-mapped files (#142) |
| 69d35398 | skip (Qt) | feat(qt): Lua's hash_region and joypad's analog calls (#142) |
| 1bccbf84 | skip (Qt) | feat(qt): Lua's forms library, a script's own windows (#142) |
| 89bbfd41 | skip (Qt) | feat(qt): Lua's tastudio library (#142) |
| 6b880ff6 | skip (Qt) | feat(qt): Lua's game library, a game core's properties by name (#142) |
| 8cf3bc5c | skip (Qt) | feat(qt): Lua's memorysavestate, userdata and input libraries (#142) |
| ece3a3ad | skip (Qt) | feat(qt): Chimera's Encode Video, in place of AVI/WAV recording (#142) |
| caf6265e | skip (Qt) | feat(qt): the run's own markers: Run start, Last input, Run end (#142) |
| 5036a25e | skip (Qt) | feat(qt): Chimera's idle eye while nothing is open (#142) |
| 4dc72b94 | skip (Qt) | fix(qt): the main window's menus are Chimera's (#142) |
| 8921edd2 | skip (Qt) | feat(qt): TAStudio's right-click menu on the frames picked (#142) |
| db3593ee | skip (Qt) | fix(qt): the piano roll is in a TAStudio window of its own (#142) |
| 78ed3978 | skip (Qt) | feat(qt): Chimera's Lua gui library (#142) |
| 62000e69 | skip (Qt) | feat(qt): Chimera's Lua libraries, first round (#142) |
| c37f4eb3 | skip (Qt) | feat(qt): Chimera's Movie hotkeys, as TAStudio answers them (#142) |
| 9af4a68e | skip (Qt) | feat(qt): Chimera's AVI/WAV recording (#142) |
| ef801bf6 | skip (Qt) | feat(qt): Chimera's Macro Tool (#142) |
| d9141cc6 | skip (Qt) | feat(qt): Chimera's Surface Viewer (#142) |
| 8cf45451 | skip (Qt) | feat(qt): Chimera's Trace Logger and the Debugger's registers (#142) |
| 0dae7789 | skip (Qt) | feat(qt): the compile cache and Pre-Compiled Modules (#142) |
| 6e657c85 | skip (Qt) | feat(qt): Chimera's Data Directory (#142) |
| bbd6e51e | skip (Qt) | feat(qt): Chimera's Save Config and Load Config (#142) |
| d1ce3122 | skip (Qt) | feat(qt): Chimera's Firmware window (#142) |
| ce225fa0 | skip (Qt) | feat(qt): Chimera's Paths window (#142) |
| 174cf47d | skip (Qt) | feat(qt): Chimera's greenzone budgets (#142) |
| 5e524cda | skip (Qt) | feat(qt): Chimera's Display window - shape, bars, whole steps, crop (#142) |
| 2a56c56d | skip (Qt) | feat(qt): Chimera's Sound window - device, buffer, volumes (#142) |
| 00878949 | skip (Qt) | feat(qt): Chimera's Messages window - places and colours over the picture (#142) |
| c3666028 | skip (Qt) | feat(qt): Chimera's Speed/Skip - fast forward speed and frame skip (#142) |
| 086a805f | skip (Qt) | feat(qt): Chimera's Key Priority (#142) |
| ff412911 | skip (Qt) | feat(qt): Chimera's Customize window (#142) |
| f4f5a462 | skip (Qt) | feat(qt): Turbo, Frame Inch, window size steps and four switches (#142) |
| ba77eb00 | skip (Qt) | feat(qt): the Cache Manager's locks, limit and auto-clean (#142) |
| a3ebecf0 | skip (Qt) | feat(qt): File > Screenshot, with Screenshot As (#142) |
| 8b557bbb | skip (Qt) | feat(qt): unsaved work is kept against a crash and offered back (#142) |
| 7352e12c | skip (Qt) | feat(ui): a project's missing files found, as Chimera finds them |
| 876b761e | skip (Qt) | feat(ui): Chimera's Cache Manager, for the project caches |
| 836ef433 | skip (Qt) | feat(ui): Chimera's Reproducible Media Maker |
| 56fb69a6 | skip (Qt) | feat(ui): Chimera's Batch Runner, each game in the headless runner |
| 49a68a36 | skip (Qt) | feat(ui): Tools > Export Core Log, as Chimera has it |
| 6b856f4f | skip (Qt) | feat(ui): Chimera's Help menu, with Report an Issue |
| d081f012 | skip (Qt) | feat(ui): Chimera's Emulator menu: game time, save data, movie import |
| 93dfc20a | skip (Qt) | feat(ui): the rest of Chimera's View menu |
| 0b45ff84 | skip (Qt) | feat(ui): a branch's picture shown while the pointer is over it |
| affc8c41 | skip (Qt) | feat(ui): lag frames hidden in the piano roll, as TAStudio hides them |
| a2636bf3 | skip (Qt) | feat(ui): the Columns menu grouped as Chimera's TAStudio groups it |
| e4ccef77 | skip (Qt) | feat(ui): TAStudio's macros, saved and placed as Chimera does |
| b17b6b87 | skip (Qt) | feat(ui): the piano roll's rows coloured as TAStudio colours them |
| e7c74c72 | skip (Qt) | feat(ui): a column's Auto-hold holds, fires or plays its pattern |
| 813a6e3a | skip (Qt) | feat(ui): TAStudio's paint patterns, kept in the project |
| 176e85d3 | skip (Qt) | feat(ui): TAStudio's scrolling and hidden-column editing settings |
| ccff846e | skip (Qt) | feat(ui): unsaved changes, autosave and TAStudio's settings window |
| ba853034 | skip (Qt) | feat(ui): the piano roll's undo history, by Chimera's step names |
| 8e13e39f | skip (Qt) | feat(ui): the branch list's own actions, with Chimera's one step of undo |
| 05d5e9f0 | skip (Qt) | feat(ui): autohold and autofire, as Chimera has them |
| 864e2deb | skip (Qt) | feat(ui): Chimera's analog keys on the picked analog cell |
| 7d3eaef6 | skip (Qt) | feat(ui): Save Project Backup, Clear Greenzone and the state history check |
| f6ff645a | skip (Qt) | feat(ui): the piano roll's columns shown and hidden, kept as Chimera keeps them |
| 07041e45 | skip (Qt) | feat(ui): a project's header, comments and subtitles, edited and shown |
| dae35e42 | skip (Qt) | feat(ui): more of Chimera's hotkeys: green arrow, clone N times, reselect |
| 2a60f325 | skip (Qt) | feat(ui): Chimera's counters over the picture |
| f14315ab | skip (Qt) | feat(ui): speed, throttle, sound and screenshots, with Chimera's hotkeys |
| 9bba9b60 | skip (Qt) | feat(ui): Catch in Controls and Hotkeys takes a gamepad input too |
| 0964cfb5 | skip (Qt) | feat(ui): Chimera's hotkeys for what the prototype has, shown in the menus |
| 54155197 | skip (Qt) | feat(ui): New Project scans a folder for missing firmware |
| 392075fb | skip (Qt) | feat(ui): a file dropped on the window starts a project from it |
| 0bf29ccf | skip (Qt) | feat(ui): New Project asks for the renderer beside the machine |
| 87fea787 | skip (Qt) | fix(ui): a machine's narrowed settings are offered as it narrows them |
| 360898b9 | skip (Qt) | feat(ui): New Project lists packages as Chimera does, emulators and games apart |
| 22b5d83d | skip (Qt) | feat(ui): a slot takes the kinds of file the chosen machine claims |
| 05c100ee | skip (Qt) | feat(ui): another game takes back what was suggested for a seeded project's game |
| a535216a | skip (Qt) | feat(ui): New Project opens on the last project's answers |
| f7d611ce | skip (Qt) | feat(ui): a new project is written when it is first saved |
| 12c4d4ef | skip (Qt) | fix(ui): a seek cut short by a delete and its undo says nothing either |
| 00c21226 | engine half only | feat(ui): New Project takes a game's own settings and firmware from the core |
| 3068f9a3 | skip (Qt) | fix(ui): going back after an edit no longer adds frames to the movie |
| 926608ae | skip (Qt) | feat(ui): New Project takes a game core's own firmware and remembers chosen files |
| a1f48ca4 | skip (Qt) | feat(ui): a package's presets offered above the settings, in Settings and New Project |
| 9342cdf9 | skip (Qt) | feat(ui): New Project applies the settings a core suggests for the game |
| ae857ecb | skip (Qt) | feat(ui): a movie made elsewhere imported into a new project |
| d62ac822 | skip (Qt) | feat(ui): RAM Watch watches a game core's properties by name |
| 09b9c9fe | skip (Qt) | feat(ui): TAStudio's Greenzone box, saved in the project as Chimera saves it |
| 40088643 | portable | build(ui): the Windows folder carries Chimera's shipped themes beside the program |
| 51e8e559 | skip (Qt) | feat(ui): Chimera's colour themes, the built-in two and the shipped ones |
| a94bbce2 | skip (Qt) | feat(ui): controls and hotkeys, changed and remembered, with Chimera's keys |
| 9fbcfeba | skip (Qt) | feat(ui): analog controls in the piano roll, shown and edited |
| 1728163d | engine half only | feat(ui): TAStudio's editing: frames picked, put in, taken out, cloned, cleared, copied and pasted |
| f01f2488 | portable | docs(ui): two decisions for the owner: where Lua scripts run, and frozen addresses |
| 5eb2ad2e | skip (Qt) | feat(ui): a hex editor, reading only the lines on screen |
| 9bdfc651 | skip (Qt) | fix(ui): --play starts whatever opens first, a project a script made too |
| d4be1a4c | skip (Qt) | feat(ui): New Project, so any game can be opened without the C# window |
| c43c1ea5 | skip (Qt) | feat(ui): RAM Watch's marked watches are shown over the picture |
| f664ebbd | skip (Qt) | feat(ui): RAM Search shows and takes values every way RAM Watch does |
| fe9b24bf | skip (Qt) | feat(ui): RAM Watch, reading Chimera's .wch files and writing values as Chimera does |
| 39ec6a1e | engine half only | feat(ui): rerecords are counted, by the engine, as Chimera's TAStudio counts them |
| 1a6937cf | engine half only | feat(ui): the greenzone is kept with the project, and the machine's identity is the engine's |
| 065ea818 | engine half only | feat(ui): a branch keeps its state in a file, so loading it is a state load |
| 6e84bed5 | skip (Qt) | feat(ui): Open Game asks only for the game, and its core is found by its kind of file |
| fc01db9a | skip (Qt) | feat(ui): a game's session made a project (#142, prototype) |
| 276cd603 | skip (Qt) | feat(ui): the window remembers its folders, options and recent projects (#142, prototype) |
| b2313b45 | skip (Qt) | feat(ui): projects open with their installed core and their firmware (#142, prototype) |
| 187c5561 | engine half only | feat(engine,ui): the engine owns the movie the piano roll edits (#142, prototype) |
| cad827e1 | portable | docs(ui): the prototype document follows markers, undo and branches (#142) |
| 1ff48db8 | skip (Qt) | feat(ui): branches of the movie, kept in the project (#142, prototype) |
| b75e55ad | skip (Qt) | feat(ui): undo and redo in the piano roll (#142, prototype) |
| cfab28f1 | skip (Qt) | feat(ui): markers in the piano roll, kept in the project (#142, prototype) |
| 59c6c65d | portable | docs(ui): the paragraph on frame numbers had the wrong heading (#142) |
| 60366108 | portable | docs(ui): what the Qt window prototype found, and the rest of the move (#142) |
| 3eddb680 | skip (Qt) | feat(ui): a Lua console window, and a test that the whole window loads (#142, prototype) |
| 7e14e822 | skip (Qt) | feat(ui): a setting that names machines shows for those alone (#142, prototype) |
| 3cb33144 | skip (Qt) | feat(ui): a machine's settings in the Qt window, from its package (#142, prototype) |
| 2cb7c856 | skip (Qt) | feat(ui): RAM Search in the Qt window (#142, prototype) |
| 44983dbc | skip (Qt) | feat(ui): the piano roll follows the light or dark theme (#142, prototype) |
| aa1e6698 | engine half only | feat(ui): projects in the Qt window - open, play, edit, save (#142, prototype) |
| b2e1b39d | skip (Qt) | feat(ui): seeking, the greenzone and edits in the Qt window (#142, prototype) |
| 99dafca0 | skip (Qt) | build(ui): the Qt window for Windows, and what running it there found (#142) |
| c1427e70 | skip (Qt) | docs(ui): what the Qt window prototype is and how to build it (#142) |
| feb987f6 | skip (Qt) | feat(ui): Lua scripts over the view models' registry (#142, prototype) |
| 92896bff | skip (Qt) | feat(ui): sound in the Qt window through OpenAL Soft (#142, prototype) |
| a026f5f6 | skip (Qt) | feat(ui): gamepads in the Qt window, named as Chimera names them (#142, prototype) |
| 4f2c7882 | skip (Qt) | feat(ui): a Qt window over view models with no Qt in them (#142, prototype) |


