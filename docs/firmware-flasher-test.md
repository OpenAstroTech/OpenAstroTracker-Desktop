# Configure & flash firmware from OATControl: test version (2026-11-14)

Branch: `feature/firmware-flasher-test` (not committed yet). Status: prototype, waiting for feedback from the other devs before continuing.

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
3. `OATControl/Firmware/FirmwareBuilder.cs`: lists GitHub releases (plus the `develop` and `oae-fw` branches), downloads and extracts to `%LocalAppData%\OATFW\fw` (a short path to stay under the Windows path limit), maps the board answer to the PlatformIO env via `-D BOARD=...` in `platformio.ini`, applies OATFWGUI's hot patches, and runs `pio` to build or upload.
4. `OATControl/DlgFirmwareFlasher.xaml(.cs)`: the wizard plus a build & flash page. It's opened from App Settings → General → "Configure & Flash Firmware...".

## How it was verified

1. **Wizard matches the website:** 400 random answer paths were replayed through the real website React component (Jest) and through the C# engine. Steps visited, choices offered and generated files were identical in all 400. Deliberately breaking the C# logic produced mismatches, so the comparison does catch errors.
2. **Real builds:** firmware v1.14.0 with generated configs built for `ramps` and `esp32`. A RAMPS build with an ESP32 config failed on the firmware's own board-mismatch `#error`, which confirms the generated config is really compiled in.
3. **The dialog:** rendered in the Red Astronomy and Daylight themes; it downloads the firmware, auto-selects the matching board env and lists the serial ports.
4. **Review:** an adversarial review found 7 issues and all are fixed: a crash in port listing, a disposed-process race, a download that couldn't be cancelled, closing the window mid-upload, the firmware version not following changed answers, release-selection races, and pre-releases being auto-selected.

## Open points / not done

1. Upload to a real board has **not** been tested (no hardware available). Only builds were run.
2. PlatformIO must already be installed (`pio.exe` on the PATH or in `%USERPROFILE%\.platformio`). Bundling Python and PlatformIO, like OATFWGUI does, is not done.
3. The website images (9.7 MB) are not included; the choices are text-only buttons.
4. `steps.json` is a copy. Until the website itself loads the exported JSON, the export has to be re-run whenever the website changes. The recommended next step is to have the website consume the same JSON, so there is a single source of truth.
5. Going back in the wizard deliberately differs from the website: it forgets that answer and the ones after it. The website keeps later answers, which makes it skip steps whose answers no longer apply (arguably a website bug).
6. An existing bug unrelated to this work: the post-build event in `OATCommunications.ASCOM.csproj` doesn't quote its paths, so builds fail in folders with spaces. Workaround: build with `-p:PostBuildEvent=` and copy `OATCommunications.ASCOM.dll` next to the exe by hand.
7. An alternative to local builds that is worth discussing is a cloud build service. OATControl would then only need avrdude/esptool, giving a much smaller install, in exchange for running and paying for a server.

## Questions for the devs

1. Is folding config and flashing into OATControl the right direction, with OATFWGUI kept for Linux?
2. Local PlatformIO (bundled on first use) or a cloud build?
3. Is it OK to move the website's step definitions to a shared JSON file?
