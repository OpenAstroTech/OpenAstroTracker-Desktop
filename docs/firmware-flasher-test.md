# Configure & flash firmware from OATControl: test version (2026-11-14, extended 2026-11-15)

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

1. `tools/firmware-config/export-steps.mjs`: exports the website's step definitions to `OATControl/Resources/FirmwareConfig/steps.json`. Conditions are pre-parsed with the website's own `parser.js`, so OATControl doesn't reimplement the parser. Usage: `node export-steps.mjs <config-web-checkout> <out.json>`.
2. `OATControl/Firmware/FirmwareConfigWizard.cs`: C# port of the `WizardStep.jsx` logic (step skipping, conditions, defaults, file generation). JavaScript quirks that affect the output are kept on purpose (for example a default of `0` becomes empty, like `0 || ''`).
3. `OATControl/Firmware/FirmwareBuilder.cs`: lists the firmware versions from GitHub, downloads and extracts to `%LocalAppData%\OATFW\fw` (a short path to stay under the Windows path limit), maps the board answer to the PlatformIO env via `-D BOARD=...` in `platformio.ini`, applies OATFWGUI's hot patches, and runs `pio` to build or upload.
4. `OATControl/DlgFirmwareFlasher.xaml(.cs)`: the wizard plus a build & flash page. It's opened from App Settings → General → "Configure & Flash Firmware...".

## Added on 2026-11-15

1. **Firmware list:** by default it shows the latest release, the `develop` branch, the release before the latest, and the newest V1.11 release. The V1.11 entry stays because the OAT wizard's "Official Versions (V1.11.x)" answer auto-selects it. The "List all releases and branches" checkbox adds every older release and every branch of the firmware repo. Only stable releases are ever selected automatically. The `oae-fw` entry was removed: v1.14.0 already contains the OAE board.
2. **Custom firmware folder:** the "Folder..." button builds a local folder (one containing `platformio.ini`) in place, for firmware with your own changes.
   - If the folder already has a different `Configuration_local.hpp`, the build asks whether to replace it with the wizard's config or to build with the folder's file. Nothing else in the folder is modified; the hot patches are skipped.
   - The log notes any `Configuration_local_<board>.hpp`, because the firmware prefers those over `Configuration_local.hpp`.
   - Folders inside `%LocalAppData%\OATFW` are rejected, because a release download deletes that directory.
3. **Persistent answers:** the wizard answers and the chosen custom folder are saved to `%AppData%\OpenAstroTracker\FirmwareConfig.json` and restored when the dialog opens. The answers are replayed against the current step definitions, and the replay stops at the first answer that no longer fits, so the user continues from there. "Redo Config" in the left panel discards the answers after a confirmation; it replaces "Start Over".
4. **Dropdowns:** they have a themed style local to the dialog. The default template showed the theme's light text on a white system popup.

## Update notifications (2026-11-15)

1. **Firmware popup:** when a mount connects with firmware older than the latest release, the update dialog used for OATControl (`DlgUpdateAvailable`) pops up after the connection wizard and homing are done. "Upgrade" opens this dialog with that firmware selected; "Skip This Version" silences it for that version. The "!" badge on Mount Settings is unchanged.
2. **Unreleased changes:** App Settings → "Notify about unreleased firmware changes (dev branch)", off by default. For a mount on the latest release it lists the commits on `develop` that are not released yet (GitHub compare API). `develop` reports the same version string as the latest release, so OATControl can't tell whether the mount already runs it. The notice therefore repeats at each app start until that develop commit is skipped or flashed from here. Every successful flash of `develop` from this dialog records the exact commit (read from the downloaded zip, where GitHub stores it as the zip comment), so that commit is not announced again; flashing anything else afterwards forgets it. Flashes made outside OATControl can't be detected.
3. **Flashing while connected:** Build & Flash offers to disconnect the mount, since the dialog is modal and the main window can't be reached. It then tests that the serial port can be opened and stops before the build if another program holds it (over ASCOM the driver keeps the port while any other client is connected).
4. **No internet:** "Check For Updates" says so instead of reporting the latest version.
5. **Verified:** the check was run against live GitHub for an old mount version, a skipped release, the toggle off and on, a skipped develop commit and a dead connection. The popup and the upgrade path were not exercised in the running app. Adversarial review: 5 of 8 findings fixed.
6. **Unfixed review findings:** the popup can open over another open dialog if the check finishes after connecting; clicking Upgrade forgets a saved custom folder; with more than 250 unreleased commits the skip would stick to the wrong commit.

## How it was verified

1. **Wizard matches the website:** 400 random answer paths were replayed through the real website React component (Jest) and through the C# engine. Steps visited, choices offered and generated files were identical in all 400. Deliberately breaking the C# logic produced mismatches, so the comparison does catch errors.
2. **Real builds:** firmware v1.14.0 with generated configs built for `ramps` and `esp32`. A RAMPS build with an ESP32 config failed on the firmware's own board-mismatch `#error`, which confirms the generated config is really compiled in.
3. **The dialog:** rendered in the Red Astronomy and Daylight themes; it downloads the firmware, auto-selects the matching board env and lists the serial ports.
4. **Review:** an adversarial review found 7 issues and all are fixed: a crash in port listing, a disposed-process race, a download that couldn't be cancelled, closing the window mid-upload, the firmware version not following changed answers, release-selection races, and pre-releases being auto-selected.
5. **2026-11-15 additions:** saving and restoring answers was tested over 300 random wizard runs (complete and partial): position, answers and generated file were identical after a restore, and a saved answer that no longer exists stopped the restore at that step. The firmware list was checked against live GitHub. The custom folder, the persistence and the dropdown style were reviewed adversarially and the findings fixed, apart from the two under open points; the firmware list changes were not reviewed. None of these additions has been clicked through in the dialog, and no build from a custom folder has been run.

## Open points / not done

1. Upload to a real board has **not** been tested (no hardware available). Only builds were run.
2. PlatformIO must already be installed (`pio.exe` on the PATH or in `%USERPROFILE%\.platformio`). Bundling Python and PlatformIO, like OATFWGUI does, is not done.
3. The website images (9.7 MB) are not included; the choices are text-only buttons.
4. `steps.json` is a copy. Until the website itself loads the exported JSON, the export has to be re-run whenever the website changes. The recommended next step is to have the website consume the same JSON, so there is a single source of truth.
5. Going back in the wizard deliberately differs from the website: it forgets that answer and the ones after it. The website keeps later answers, which makes it skip steps whose answers no longer apply (arguably a website bug).
6. An existing bug unrelated to this work: the post-build event in `OATCommunications.ASCOM.csproj` doesn't quote its paths, so builds fail in folders with spaces. Workaround: build with `-p:PostBuildEvent=` and copy `OATCommunications.ASCOM.dll` next to the exe by hand.
7. A saved custom folder is forgotten if its `platformio.ini` can't be read when the dialog opens (for example an unplugged drive), and the dialog can hang for a few seconds on open if the saved folder is on an unreachable network path.
8. An alternative to local builds that is worth discussing is a cloud build service. OATControl would then only need avrdude/esptool, giving a much smaller install, in exchange for running and paying for a server.

## Questions for the devs

1. Is folding config and flashing into OATControl the right direction, with OATFWGUI kept for Linux?
2. Local PlatformIO (bundled on first use) or a cloud build?
3. Is it OK to move the website's step definitions to a shared JSON file?
