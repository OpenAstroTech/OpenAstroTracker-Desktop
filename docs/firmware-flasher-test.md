# Configure & flash firmware from OATControl: test version (2026-11-14, extended 2026-11-15 and 2026-11-20)

Branch: `feature/firmware-flasher-test` (not committed yet). Status: prototype; the questions for the other devs at the end are still open.

## Goal

Users currently need three tools to get firmware onto a mount:
1. config.openastrotech.com ([OpenAstroTracker-Firmware-Config-Web](https://github.com/OpenAstroTech/OpenAstroTracker-Firmware-Config-Web)) to generate `Configuration_local.hpp`.
2. [OATFWGUI](https://github.com/OpenAstroTech/OATFWGUI) to download, build and upload the firmware.
3. OATControl to control the mount.

The test folds steps 1 and 2 into OATControl. OATFWGUI stays for Linux users, since OATControl is Windows-only.

## Key constraints found

1. The firmware configuration is compile-time `#define`s, so every user needs their own build. A prebuilt .hex can't be flashed directly; a compiler toolchain (PlatformIO) or a cloud build service is required.
2. OATFWGUI is a thin front end on PlatformIO, with about 500 lines of logic: it downloads the firmware zip from GitHub, lists the environments in `platformio.ini`, copies in the config, and runs `pio run -e <env> [-t upload --upload-port COMx]`. It also applies hot patches to `platformio.ini` for old releases, and adds an ESP32 pre-script that works around the Windows command-line length limit.
3. The website's wizard steps are JavaScript (`oatSteps.js`, `oamSteps.js`, `oaeSteps.js`, `commonSteps.js`), not JSON, but the data is JSON-shaped. Conditions such as `($radrv == TU)` are strings read by a small custom parser (`parser.js`). The rules that turn answers into `#define` lines live in `WizardStep.jsx`.

## What was built

1. The export of the website's step definitions to JSON, with conditions pre-parsed by the website's own `parser.js` so OATControl doesn't reimplement the parser. It started as `tools/firmware-config/export-steps.mjs` here; since 2026-11-20 it lives in the website repo as `scripts/export-steps.mjs` (see "Shared wizard steps" below), and the copy here was removed.
2. `OATControl/Firmware/FirmwareConfigWizard.cs`: C# port of the `WizardStep.jsx` logic (step skipping, conditions, defaults, file generation). JavaScript quirks that affect the output are kept on purpose (for example a default of `0` becomes empty, like `0 || ''`).
3. `OATControl/Firmware/FirmwareBuilder.cs`: lists the firmware versions from GitHub, downloads and extracts to `%LocalAppData%\OATFW\fw` (a short path to stay under the Windows path limit), maps the board answer to the PlatformIO env via `-D BOARD=...` in `platformio.ini`, applies OATFWGUI's hot patches, and runs `pio` to build or upload.
4. `OATControl/DlgFirmwareFlasher.xaml(.cs)`: the wizard plus a build & flash page. It's opened from App Settings → General → "Configure & Flash Firmware...".

## Added on 2026-11-15

1. **Firmware list:** by default it shows the latest release, the `develop` branch, the release before the latest, and the newest V1.11 release. The V1.11 entry stays because the OAT wizard's "Official Versions (V1.11.x)" answer auto-selects it. The "List all releases and branches" checkbox adds every older release and every branch of the firmware repo. Only stable releases are ever selected automatically. The `oae-fw` entry was removed: v1.14.0 already contains the OAE board.
2. **Custom firmware folder:** the "Folder..." button builds a local folder (one containing `platformio.ini`) in place, for firmware with your own changes.
   - If the folder already has a `Configuration_local.hpp`, that file is used instead of the wizard's config. The flash page shows it and it can be edited there: "Save" writes it back (a build saves edits too), "Use Wizard Config" fills the editor with the wizard's output. Unsaved edits are asked about when closing or switching firmware, and a build asks which version to keep if the file was also changed by another program. A folder without the file gets the wizard's config. Nothing else in the folder is modified; the hot patches are skipped.
   - The log notes any `Configuration_local_<board>.hpp`, because the firmware prefers those over `Configuration_local.hpp`.
   - Folders inside `%LocalAppData%\OATFW` are rejected, because a release download deletes that directory.
3. **Persistent answers:** the wizard answers and the chosen custom folder are saved to `%AppData%\OpenAstroTracker\FirmwareConfig.json` and restored when the dialog opens. The answers are replayed against the current step definitions, and the replay stops at the first answer that no longer fits, so the user continues from there. "Redo Config" in the left panel discards the answers after a confirmation; it replaces "Start Over".
4. **Dropdowns:** they have a themed style local to the dialog. The default template showed the theme's light text on a white system popup.

## Update notifications (2026-11-15)

1. **Firmware popup:** when a mount connects with firmware older than the latest release, the update dialog used for OATControl (`DlgUpdateAvailable`) pops up after the connection wizard and homing are done. "Upgrade" opens this dialog with that firmware selected; "Skip This Version" silences it for that version. The "!" badge on Mount Settings is unchanged.
2. **Unreleased changes:** App Settings → "Notify about unreleased firmware changes (dev branch)", off by default. For a mount on the latest release it lists the commits on `develop` that are not released yet (GitHub compare API). `develop` reports the same version string as the latest release, so OATControl can't tell whether the mount already runs it. The notice therefore repeats at each app start until that develop commit is skipped or flashed from here. Every successful flash of `develop` from this dialog records the exact commit (read from the downloaded zip, where GitHub stores it as the zip comment), so that commit is not announced again; flashing anything else afterwards forgets it. Flashes made outside OATControl can't be detected.
3. **Flashing while connected:** Build & Flash offers to disconnect the mount, since the dialog is modal and the main window can't be reached. It then tests that the serial port can be opened and stops before the build if another program holds it (over ASCOM the driver keeps the port while any other client is connected).
4. **No internet:** "Check For Updates" says so instead of reporting the latest version.
5. **Verified:** the check was run against live GitHub for an old mount version, a skipped release, the toggle off and on, a skipped develop commit and a dead connection. The update notification has since been confirmed working in the running app (reported by Fabian, 2026-11-18). Adversarial review: 5 of 8 findings fixed.
6. **Unfixed review findings:** the popup can open over another open dialog if the check finishes after connecting; clicking Upgrade forgets a saved custom folder; with more than 250 unreleased commits the skip would stick to the wrong commit.

## How it was verified

1. **Wizard matches the website:** 400 random answer paths were replayed through the real website React component (Jest) and through the C# engine. Steps visited, choices offered and generated files were identical in all 400. Deliberately breaking the C# logic produced mismatches, so the comparison does catch errors.
2. **Real builds:** firmware v1.14.0 with generated configs built for `ramps` and `esp32`. A RAMPS build with an ESP32 config failed on the firmware's own board-mismatch `#error`, which confirms the generated config is really compiled in.
3. **The dialog:** rendered in the Red Astronomy and Daylight themes; it downloads the firmware, auto-selects the matching board env and lists the serial ports.
4. **Review:** an adversarial review found 7 issues and all are fixed: a crash in port listing, a disposed-process race, a download that couldn't be cancelled, closing the window mid-upload, the firmware version not following changed answers, release-selection races, and pre-releases being auto-selected.
5. **2026-11-15 additions:** saving and restoring answers was tested over 300 random wizard runs (complete and partial): position, answers and generated file were identical after a restore, and a saved answer that no longer exists stopped the restore at that step. The firmware list was checked against live GitHub. The custom folder, the persistence and the dropdown style were reviewed adversarially and the findings fixed, apart from the two under open points; the firmware list changes were not reviewed. None of these additions has been clicked through in the dialog, and no build from a custom folder has been run.

## Open points / not done

1. Upload has been tested on a real ESP32 board, several times (reported by Fabian, 2026-11-18). Upload to an AVR board (RAMPS, MKS Gen L) has not been reported yet.
2. Missing PlatformIO is solved by the "Download Flash Tools" button (2026-11-20, see below).
3. The website images (9.7 MB) are not included; the choices are text-only buttons.
4. Solved 2026-11-20: OATControl downloads the steps from the website (see "Shared wizard steps" below). The embedded `steps.json` is only the offline fallback.
5. Going back in the wizard deliberately differs from the website: it forgets that answer and the ones after it. The website keeps later answers, which makes it skip steps whose answers no longer apply (arguably a website bug).
6. An existing bug unrelated to this work: the post-build event in `OATCommunications.ASCOM.csproj` doesn't quote its paths, so builds fail in folders with spaces. Workaround: build with `-p:PostBuildEvent=` and copy `OATCommunications.ASCOM.dll` next to the exe by hand.
7. A saved custom folder is forgotten if its `platformio.ini` can't be read when the dialog opens (for example an unplugged drive), and the dialog can hang for a few seconds on open if the saved folder is on an unreachable network path.
8. Decided 2026-11-20 (Fabian): local PlatformIO, no cloud build.

## "Download Flash Tools" button (built 2026-11-20)

1. **Finding PlatformIO:** the flash page looks for `pio.exe` on the PATH, in `%USERPROFILE%\.platformio\penv`, then the private copy, and uses the first of version 6.1.16 or newer (the version OATFWGUI pins). Older installs are logged and left alone.
2. **The button:** shown when no usable PlatformIO was found, or when Git is missing. It is enabled once a firmware and board are selected, and reads "Download Flash Tools, about X MB", where X counts only the missing parts. Build and Flash stay disabled until the tools are there. Releases, the board and the port can be chosen without PlatformIO.
3. **What it installs** (in `%LocalAppData%\OATFW\tools`, nothing on PATH, nothing else changed):
   1. Python 3.11.9 from the official python.org NuGet package (17 MB download, 45 MB on disk), checked against a fixed SHA-256.
   2. PlatformIO 6.1.16 via `pip --isolated` into that Python (2 MB download, 34 MB on disk).
   3. MinGit 2.56.1 from Git for Windows (38 MB download, 93 MB on disk), checked against a fixed SHA-256, only if `git.exe` is not on the PATH. **Git turned out to be required:** every board's `lib_deps` are `github.com/...#tag` URLs and the RP2040 platform is a git URL, so PlatformIO fails without Git. This was not in the original plan.
   4. The compiler and libraries of the selected board (`pio pkg install -e <env>`), after the hot patches, since those pin the AVR platform version. Downloads measured for firmware v1.14: AVR about 55 MB (334 MB on disk), ESP32 about 370 MB (1.6 GB on disk). RP2040 is not measured, so the button shows "+ compiler" for it.
4. **Compilers:** go to `%USERPROFILE%\.platformio` if it exists (reusing what an existing install downloaded), else `%LocalAppData%\OATFW\core`. A `PLATFORMIO_CORE_DIR` set by the user wins.
5. **Every pio run** drops `PYTHONHOME`/`PYTHONPATH` and sets `PYTHONNOUSERSITE=1` and `PIP_USER=0`, so a Python configured elsewhere can't leak in. The private Git is put first on PATH only for pio.
6. **Decided:** Python already on the PC is not reused; the board's compiler is part of the button's download.
7. **Verified (2026-11-20)** with a harness that calls the real `FlashTools` API from the built OATControl.exe:
   1. With Git removed from PATH, it installed Python, MinGit and PlatformIO, the git-URL libraries installed through the private Git, and a RAMPS build with the private pio succeeded. The reported size was 57 MB before (17 + 2 + 38, the AVR compiler was already in `.platformio`) and 0 after.
   2. A wrong Python hash was rejected and left no Python folder behind; restoring it reinstalled cleanly with Git on PATH.
   3. Not done: the dialog itself has not been clicked through, and the install into a fresh `OATFW\core` (no `.platformio`) was only run with pio directly, not through OATControl. On Fabian's PC the button doesn't appear (PlatformIO 6.2.0 and Git are installed); to see it, rename `%USERPROFILE%\.platformio` and remove Git from PATH, or test on another PC.
8. **Not pinned:** PlatformIO's own pip dependencies are resolved from PyPI at install time (only PlatformIO's version is fixed).

## Config website findings (2026-11-20)

1. **Converting the steps to JSON is easy:** the step files (`commonSteps.js`, `oatSteps.js`, `oamSteps.js`, `oaeSteps.js`, `sharedDefaults.js`, about 1,500 lines) are already plain data. The factories take no arguments, nothing is a function, and defaults are strings like `'{Defaults.PowerRating.rastpr}'` resolved at runtime. The only JavaScript parts are imports, the factory wrappers and `...createWiFiSteps()`-style reuse. Before PR #38, the export from website master (`3c384e2`, v3.11.0) was byte-identical to the embedded `steps.json`.
2. **Recommended way to share them:** keep the JavaScript files as the source. Move `export-steps.mjs` into the website repo and run it at build time to write `public/steps.json`, which the site then serves at config.openastrotech.com. OATControl downloads it at runtime and keeps the embedded copy as an offline fallback. The alternative is to rewrite the step files as JSON, which touches every file the website devs edit for little gain.
3. **Deployment:** a push to master runs `.github/workflows/deploy.yml`. It SSHes to the server, which runs `git pull` and Docker (`npm start`, Node 13.12), and serves `public/`. Node 13.12 can't run the export script (top-level await needs Node 14.8 or newer), so the Dockerfile's Node version must be raised for option 2.
4. **Drift that JSON doesn't solve:** the rules that turn answers into `#define` lines are in `WizardStep.jsx` (653 lines) and in the C# port. The 400-path parity test (React component vs C# engine) should run in the website's CI.
5. **Why PRs sit open:** the ruleset "Don't push to master" sets 0 required approvals, but its "Restrict updates" rule blocks every change to master, PR merges included. Only org admins and repo admins can bypass it, with the "Merge without waiting for requirements" checkbox. A review doesn't unblock a merge. To fix it, untick "Restrict updates" and keep "Require a pull request", "Block force pushes" and "Restrict deletions". Set "Required approvals" to 1 if reviews should really be required. Not changed yet; agree it with ClutchplateDude and andre-stefanov first. Copilot code review runs automatically on PRs.
6. **PR #38, "fix OAT DEC Circumference":**
   1. **The bug:** since at least V3.00 (2025-09-01), the shared DEC stepper step wrote `#define DEC_WHEEL_CIRCUMFERENCE 816.814f` (the OAM value) into OAT configs too. The firmware's `Configuration_adv.hpp` only sets the OAT value (565.5f) when the local config doesn't, so affected OAT mounts calculate about 44% too many DEC steps (816.814 / 565.5 ≈ 1.44). The OAE was not affected.
   2. **Affected users:** OAT users who generated a config on the website since then should remove that line from their `Configuration_local.hpp` (or regenerate it) and reflash. This is worth a release note or announcement. OATControl's flasher has the same bug until `steps.json` is re-exported.
   3. **Fixing the PR:** as first pushed, it also moved `DEC_INVERT_DIR` into a step every OAM goes through. On master it's only written with a TMC2209-UART DEC driver. It also wrote the circumference line as a nested array, which only rendered by accident and would break the C# wizard. Commit `644aeed` fixed both: OAM now uses its own copy of the DEC stepper step that keeps the line. A field-by-field comparison of the step data exported from master and from the PR showed that the only difference is the OAT's circumference line; OAM and OAE are identical.
   4. **Status:** ready to merge, with the bypass checkbox and squash. A merge deploys to the live site immediately.
7. **Done:** #38 merged (`b135f57`) and exported into OATControl's embedded `steps.json`. PR #35 ("Add configuration for OpenAstroExplorer and bump to v3.11", open since 2025-10-25) is still open, although master already contains the OAE steps.

## Shared wizard steps (built 2026-11-20)

1. **Website** (branch `feature/download-wizard-steps` in the config-web repo, not pushed yet):
   1. `scripts/export-steps.mjs` writes `public/steps.json`, adding `"schemaVersion": 1`. It loads the step files from a temp copy marked as ES modules, because the server's Node 13.12 can't load them directly.
   2. `prestart` and `prebuild` npm scripts run the export, and the Dockerfile runs it once more while building the image, as root. A failed export never stops the site: `prestart` continues and the Docker step only prints a message.
   3. `.gitignore` excludes the generated file.
2. **Website tests:**
   1. Node 22: the export works, then react-scripts 4 can't start, as it can't on any Node 17 or newer.
   2. Node 16.20.2: `npm start` serves `/steps.json` as JSON. A deliberately broken parser made `prestart` print the failure and continue (exit 0), while the script alone exited 1.
   3. Node 13.12 with npm 6 as root in WSL Ubuntu, replaying the Dockerfile: `npm install`, the build step and `npm start` all worked, and the served file was byte-identical to the Node 22 export. npm 6 didn't drop to an unprivileged user there.
   4. Not run: a real Docker/Alpine build.
3. **OATControl:**
   1. `Firmware/WizardSteps.cs` fetches `https://config.openastrotech.com/steps.json` when the flasher opens, with a 5 s timeout.
   2. A file is accepted only if it parses, has `schemaVersion` 1, and the C# wizard runs once through every tracker to a generated configuration. The site answers unknown paths with its HTML page, which this rejects.
   3. A good download is saved to `%LocalAppData%\OATFW\steps.json`. Fallback order: download, then the saved copy (only if it is newer than the OATControl build), then the embedded copy. The footer says which one is in use. A failure to save the copy is only logged.
   4. Validation runs one path per tracker the first step offers. If a downloaded file fails later in another branch while the dialog opens, the dialog falls back to the built-in copy. The download runs off the UI thread.
   5. The embedded `steps.json` was re-exported with `schemaVersion` and the #38 fix.
4. **OATControl tests** (harness against the built exe), all passed:
   1. The embedded file and a site-served file validate.
   2. Rejected: HTML, `schemaVersion` 2, a missing `schemaVersion`, and #38's original nested array in a literal.
   3. Fallback: no download gives the built-in copy, a valid saved copy wins over the built-in one, and a broken saved copy falls back to the built-in one.
   4. Against a local HTTP server: the download is used and saved, a second download replaces the saved copy without leaving temp files, an HTML answer falls back to the saved copy without overwriting it, and a saved copy older than the build loses to the built-in one.
   5. An adversarial review found 5 issues, all fixed.
   6. Not tested: clicking through the dialog, including its fallback when a downloaded file fails, and the live site (it serves the file only after the website branch is deployed; today OATControl gets a 404 there).
5. **Rule for the website from now on:** step files and `parser.js` must stay within Node 13.12 syntax (no `?.`, `??`) until the site's Node is upgraded. The website can only move past Node 16 by replacing react-scripts 4 (for example with Vite).

## Questions for the devs

1. Is folding config and flashing into OATControl the right direction, with OATFWGUI kept for Linux?
2. ~~Local PlatformIO or a cloud build?~~ Decided: local PlatformIO.
3. ~~Is it OK to move the website's step definitions to a shared JSON file?~~ Done instead by generating `steps.json` from the website's own files (see "Shared wizard steps").
