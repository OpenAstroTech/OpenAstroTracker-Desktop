---
name: release
description: Use when the user wants to create a release, make a release, ship, cut a release, or publish a new version of OATControl. Triggers on phrases like "make a release", "let's ship", "let's release", "create a release", "ship it", "cut a release", "new release", "/release".
---

# OATControl Release

Create a new OATControl release. Follow these steps in order. Stop and ask the user before proceeding if any step fails.

## Pre-flight

1. Verify `gh` CLI is available and authenticated (`gh auth status`)
2. Verify `curl` is available (`which curl`)
3. Verify git working tree is clean (`git status`)
4. If either fails, stop and tell the user what to fix

## Step 1: Version

1. Read current version from `OATControl/Properties/AssemblyInfo.cs` line 54: `[assembly: AssemblyVersion("X.Y.Z.W")]`
2. Show the current version to the user
3. Propose the next version by bumping the minor segment (e.g. `1.2.0.0` → `1.3.0.0`)
4. Ask the user to confirm or provide a different version
5. Validate the new version is strictly higher than the current version
6. Store as `$VERSION` (e.g. `1.3.0.0`) and `$TAG` (e.g. `V1.3.0.0`)

## Step 2: Version Bump

Update these two files:

**`OATControl/Properties/AssemblyInfo.cs`** — Update both lines:
```
[assembly: AssemblyVersion("$VERSION")]
[assembly: AssemblyFileVersion("$VERSION")]
```

**`OATControl/OATControl Setup.iss`** — Update line 5:
```
#define MyAppVersion "$VERSION"
```

## Step 2b: Firmware Wizard Steps

The firmware flasher downloads its wizard steps from the config website; the copy built into OATControl is the offline fallback. Refresh it from the site so each release ships the latest steps:

All commands run from the repo root in the Bash tool.

1. Download to a temp file next to the current copy:
   ```
   curl -fsSL --max-time 30 -o OATControl/Resources/FirmwareConfig/steps.download.json https://config.openastrotech.com/steps.json
   ```
2. Quick check that it's the steps file and not the site's HTML page:
   ```
   powershell -NoProfile -Command 'try { if ((Get-Content -Raw OATControl/Resources/FirmwareConfig/steps.download.json | ConvertFrom-Json).schemaVersion -ne 1) { exit 1 } } catch { exit 1 }'
   ```
3. If the download or the check fails, delete `steps.download.json`, keep the current `steps.json`, tell the user, and continue with Step 3.
4. Otherwise move it over the current copy (`mv OATControl/Resources/FirmwareConfig/steps.download.json OATControl/Resources/FirmwareConfig/steps.json`) and show `git diff --stat -- OATControl/Resources/FirmwareConfig/steps.json`, so the user sees whether the steps changed.
5. If it changed, run the full check OATControl runs on a download (one run through each tracker to a generated configuration): build Debug, then call the check against the new file:
   ```
   msbuild OATControl/OATControl.csproj -p:Configuration=Debug -v:m
   powershell -NoProfile -Command '$ErrorActionPreference="Stop"; try { [void][Reflection.Assembly]::LoadFrom((Resolve-Path OATControl/bin/Debug/OATControl.exe)); [void][OATControl.Firmware.WizardSteps]::Validate([IO.File]::ReadAllText((Resolve-Path OATControl/Resources/FirmwareConfig/steps.json))); "steps.json OK" } catch { "steps.json INVALID: " + $_.Exception.InnerException.Message; exit 1 }'
   ```
   If it prints INVALID, restore the file (`git restore OATControl/Resources/FirmwareConfig/steps.json`), tell the user the site's steps fail in OATControl, and continue with the current copy.

## Step 3: Changelog + Readme

1. Get the last release tag: `git tag --sort=-v:refname | grep -E '^V1\.' | head -1`
2. Get commits since that tag: `git log $LAST_TAG..HEAD --oneline`
3. Generate bullet points from the commit messages (combine related commits, make them concise)
4. Present the bullet list to the user for editing
5. Once approved, prepend to both files:

**`OATControl/CHANGELOG.md`** — Markdown format:
```markdown
## OATControl V$VERSION ($DATE)

- Change one.
- Change two.
```

**`OATControl/Readme.txt`** — Plain text format matching existing entries. Use the exact column alignment pattern:
```
OATControl V$VERSION                                         $DATE
- One-line change.
- Another change.
```
The date format is `%d %b %Y` (e.g. `24 May 2026`). The header line is 65 characters wide — pad with spaces so the date aligns to the right edge.

## Step 4: Commit

1. Stage only the changed files: `AssemblyInfo.cs`, `OATControl Setup.iss`, `CHANGELOG.md`, `Readme.txt`, and `OATControl/Resources/FirmwareConfig/steps.json` if Step 2b changed it
2. Commit: `git commit -m "Release $TAG"`
3. Push the commit to the remote branch

## Step 5: Create or Update PR

1. Check if an open PR exists: `gh pr list --head <branch> --state open`
2. If no PR exists, create one with the release changes included in the description
3. If PR already exists, it's now updated with the release files
4. Tell the user to **merge the PR** and wait for confirmation

## Step 6: Switch to Master

After the PR is merged:

1. Switch to master and pull the merge:
   ```
   git checkout master && git pull
   ```
2. Tag the merge commit:
   ```
   git tag -a $TAG -m "Release $TAG"
   ```
3. Push the tag: `git push --tags`

## Step 7: Build Pause

Tell the user:

> **Build required.** Please do the following in Visual Studio:
> 1. Build OATControl in **Release** mode
> 2. Run **InnoSetup** on `OATControl/OATControl Setup.iss`
> 3. Confirm when done

Wait for the user to confirm, then verify the installer exists at `OATControl/bin/SetupOutput/OATControlSetup.exe`. If not found, ask the user to check.

## Step 8: GitHub Draft Release

1. Create a temporary file with the changelog bullet points (just the bullets, no header) for release notes
2. Create draft release:
   ```
   gh release create $TAG --draft --title "OATControl $TAG Release" --notes-file <temp-file> "OATControl/bin/SetupOutput/OATControlSetup.exe"
   ```
3. Report the draft release URL to the user
4. Clean up the temporary notes file

## Step 9: Discord Announcement

1. Read the webhook URL from `.claude/settings.local.json` key `discordReleaseWebhook`
2. If the key is missing, ask the user for the webhook URL and save it to `.claude/settings.local.json`
3. Build the announcement message:
   ```
   @everyone A new OATControl version ($TAG) has been released. These are the main changes:

   - Bullet one.
   - Bullet two.

   Let us know if there are any issues.

   OpenAstroTech Team
   $RELEASE_URL
   ```
   Use the same user-facing bullet points from Step 3. The `$RELEASE_URL` is the GitHub release URL from Step 8.
4. POST to the webhook:
   ```bash
   curl -X POST "$WEBHOOK_URL" \
     -H "Content-Type: application/json" \
     -d '{"content": "<message with \\n for line breaks>"}'
   ```
5. Report success to the user

## Error Handling

- If any step fails, report the error clearly and stop
- Do not attempt partial recovery or skip steps
- The user can fix the issue and re-run `/release` from the beginning, or continue manually
