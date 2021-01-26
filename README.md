OpenAstroTracker-Desktop
========================

This repository holds the high-level software for the OAT. Currently the main components are:

- **OATControl**   - a C#/WPF application for Windows that allows control of the OAT via serial or WiFi connection.
- **OATCommTestConsole** - a C#/Console application for Windows that tests basic connectivity of the OAT via Serial connection.
- **ASCOM.Driver** - a .NET ASCOM driver for the OAT. Installation file for use with InnoSetup Compiler included.

There are also some low-level communications libraries that the above mentioned projects use.

Change Log:
===========
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
