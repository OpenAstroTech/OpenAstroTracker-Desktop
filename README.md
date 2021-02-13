OpenAstroTracker-Desktop
========================

This repository holds the high-level software for the OAT. Currently the main components are:

- **OATControl**   - a C#/WPF application for Windows that allows control of the OAT via serial or WiFi connection.
- **OATCommTestConsole** - a C#/Console application for Windows that tests basic connectivity of the OAT via Serial connection.
- **ASCOM.Driver** - a .NET ASCOM driver for the OAT. Installation file for use with InnoSetup Compiler included.

There are also some low-level communications libraries that the above mentioned projects use.

Change Log:
===========
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
