OpenAstroTracker-Desktop
========================

This repository holds the high-level software for the OAT. Currently the main components are:

- **OATControl**   - a C#/WPF application for Windows that allows control of the OAT via serial or WiFi connection.
- **OATCommTestConsole** - a C#/Console application for Windows that tests basic connectivity of the OAT via Serial connection.
- **ASCOM.Driver** - a .NET ASCOM driver for the OAT. Installation file for use with InnoSetup Compiler included.

There are also some low-level communications libraries that the above mentioned projects use.

To install any of these, click on the latest release under Releases to the right and above this text. Then download the latest version of the package you're after. 
- **OATControl** can simply be unzipped in a folder and the executable started.
- **OATCommTester** also can simply be unzipped in a folder and the executable started.
- **ASCOM Driver** is an installer and can be run after download. It will uninstall older versions, if needed.

Change Log:
===========
**V0.9.9.30 - Updates**
- Revamped target choice into it's own modeless dialog. Targets are sorted by distance from current target. Double-click to transfer coordinates to target.
- Improved connectivity failure detection. Attempt a few retries.
- Added display of driver and microstepping settings (V1.8.76 or later required)
- Fixed display of remaining time to be correct regardless of microstep settings (V1.8.76 or later required)
- Fixed the bug that caused steps to be reverted to 1 when changing target coordinates (*facepalm*)

**V0.9.9.29 - Updates**
- Improved communications layer to handle Meade idiosyncracy in SC command
- Added some more points of interest.

**V0.9.9.28 - Updates**
- Sometimes reconnects would fail and crash OATControl. Now they don't crash it.
- Added a connection time display.
- Serial communciation would report successful connection even though it wasn't.
- Made double-click on device proceed to next step in connection dialog.

**V0.9.9.27 - Updates**
- TCP connections did not correctly shutdown the communications thread on disconnect. This could leave zombie OATcontrol processes on the machine.
- With no GPS, the lat long was not retrieved from OAT as the default for manual entry. It is now.
- Detected the incorrect RA/DEC steps in the Settings and pop up a message box suggesting factory reset.
- Fixed label width at bottom of main UI.

**V0.9.9.26 - Updates**
- Added label to slew rate boxes.
- Added log folder link to main UI.
- Added log file flushing in case of crash.

**V0.9.9.25 - Updates**
- Added Slewrate controls to Mini controller, bound to 1, 2, 3, and 4 keys.

**V0.9.9.24 - Updates**
- Added a mini controller that shows RA/DEC and allows manual slewing, homing and park, with keyboard support.
- Site altitude is now persisted across sessions.
- Connections are attempted multiple times if they fail.
- Prevented crashes when the firmware said GPS or Digital Level was attached, but weren't actually connected.
- Window positions are persisted across sessions.
- Moved Settings button down to above Connect button.

**V0.9.9.23 - Updates**
- Removed dummy call to consume second reply from :SC# command, since receive buffer is now cleared before each command

**V0.9.9.22 - Updates**
- Hardened network display in Settings dialog against bad data.
- Added synchronization to wait for jobs processor to quit before closing port.

**V0.9.9.21 - Updates**
- Fixed a parsing bug for float values.
- Seperated the network info in the Settings Dialog.

**V0.9.9.20 - Updates**
- Moved some calibration info to a seperate Settings dialog that also has a lot of additional info.
- Arrows on Target RA and DEC coordinates now auto-repeat and accelerate.
- Added a Factory Reset command (it's in the Settings dialog).
- If no GPS fix is attained, the LatLong is retrieved from OAT and used as the default.
- LatLong is displayed with two decimal places.
- Fixed some LatLong handling in the communications layer.
- Prevented from identical ports showing up in the list of available devices (Windows listed my COM5 port twice).
- Cleared the Receive Buffer before sending a new command to OAT.
- Detect that firmware is compiled with DEBUG_LEVEL and let user know.

**V0.9.9.18 - Updates**
- Fixed order of startup commands to correctly set target RA after date upload.

**V0.9.9.17 - Updates**
- Complete refactor of OAT communication handling to use multi-threaded, job queue system.
- Increased serial timeout to 1000ms.
- Corrected longitude handling and now sends time, date and location to OAT.
- Warning: may not work with GPS add on (yet).

**V0.9.9.16 - Updates**
- Various float CultureInfo conversion fixes
- Removed ASCOM library dependency to calculate LST

**V0.9.9.15 - Updates**
- Fix for switching RA/DEC steps to floating point.

**V0.9.9.14 - Updates**
- Added Button to open logs folder.
- Widened Connection dialog.

**V0.9.9.13 - Updates**
- Improved detection of connection error conditions.

**V0.9.9.12 - Updates**
- Added Pitch and Roll display to connection dialog (if Digital Level present)

**V0.9.9.11 - Updates**
- Adapted manual slewing to use real units. Requires V1.8.46 or later OAT firmware.

**V0.9.9.10 - Updates**
- Changed the connect flow to allow leveling (if present) and use the GPS (if present).
- Polar Alignment now uses levelling and has a better flow.

**V0.9.9.05 - Updates**
- Fixed a communication bug that occasionally caused issues after a slew command.
- Added a progress bar to RA and DEC that shows how far is left to slew.
- Added an application icon (feel free to improve it!)
- Added two more nebula to the points of interest
