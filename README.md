OpenAstroTracker-Desktop
========================

This repository holds the high-level software for the OAT. Currently the main components are:

- **OATControl**   - a C#/WPF application for Windows that allows control of the OAT via serial or WiFi connection.
- **ASCOM.Driver** - a .NET ASCOM driver for the OAT. Installation file for use with InnoSetup Compiler included.

There are also some low-level communications libraries that the above mentioned projects use.

To install any of these, click on the latest release under Releases to the right and above this text. Then download the latest version of the package you're after. 
- **OATControl** is an installer adn can be run after download. It will uninstall older versions, if needed.
- **OATCommTester** also can simply be unzipped in a folder and the executable started.
- **ASCOM Driver** is an installer and can be run after download. It will uninstall older versions, if needed.

Change Log:
===========
**OATControl V1.0.2.4 - Updates**
- Added support for Hall sensor based RA auto homing.

**ASCOM.Driver V6.6.5.0 - Updates**
- Added ability to connect to OAT from the driver properties and do rudimentary operations (slew, home, set home, adjust RA steps, DEC  steps and speed factor).

**OATControl V1.0.2.3 - Updates**
- Added support for focuser reset on Settings page.

**OATControl V1.0.2.2 - Updates**
- DEC Go Home command was requiring one firmware version too high. Fixed.
- Focuser position was being queried incorrectly. Fixed.

**ASCOM.Driver V6.6.3.0 - Updates**
- Changed pass through command to correctly handle all Meade commands that OAT implements.

**OATControl V1.0.2.1 - Updates**
- Enabled OATControl to use ASCOM driver.
- Added support for communication handlers to open a settings dialog.
- Added display of active connection to main window.
- Added ability to set a DEC offset from power on state to home and to slew there (requires V1.9.16 firmware) on button press (see Settings Dialog)
- Mini Control can be set to be Always On Top (right click on Mini-Control button in main UI).
- Swapped out the application settings logic so settings don't get lost between builds.
- State of Show DEC limits toggle is now persisted.
- Made some cosmetic changes in Connection dialog to accomodate setup.
- Made some cosmetic changes in Main window to line up display of stepper and mount values.
- Changed all black or dark text to light red for better readability.

**OATControl V1.0.1.2 - Updates**
- Re-enabled logging by default and provided a -nolog switch instead.
- Added display of Focus stepping position if focuser addon is present.

**OATControl V1.0.1.1 - Updates**
- Fixed communication bug that prevented ESP32 from staying connected.

**OATControl V1.0.1.0 - Updates**
- Support new split ALT/AZ configuration
- Allow ALT control in MiniController window (keys 'W' and 'S' to move up and down, 1 - 4 to select distance).
- Allow AZ control in MiniController (keys 'A' and 'D' to move left and right, 1 - 4 to select distance).
- Allow Focus control in MiniController (keys 'X' and 'C' move in and out)
- Added shortcut key display to MiniController arrows
- Added control area labels and changed some label colors in MiniController
- Added LST display to main window.
- Fixed the Stop button to actually work.
- Fixed some culture specific code that was causing issues with OAT on non-US machines.
- Changed the way that connection retries work.
- Hardened app to better protect against disconnects and to prevent zombie OATControls and random hangs when communication issues arise.
- Turned logging of for release app, but support -log commandline parameter to turn it on.

**OATControl V1.0.0.2 - Updates**
- Improved Target Chooser display. Fewer columns, better readability, DEC limits filtering.
- Added axis displays for RA and DEC that show current and target position, as well as limits on DEC (if set). Also added a button to set the lower limit from the current position.
- Added Baudrate selection step in connection dialog (Firmware V1.9.04 and later use 19200, earlier versions use 57600)
- Increased the time needed to start changing Target coodinates with mouse before they auto-incremented or decremented.
- Slew progress now uses actual stepper positions instead of stellar coordinates.
- Users now get asked to confirm if the want to slew to a location that is outside of the set DEC limits.
- Updated Polaris coordinates for Polar Alignment function 
- Hid the network settings the Settings dialog if no Wifi is enabled.
- Fixed a crash caused by trying to send a command to a disconnected device.
- Fixed a crash caused by reading temperature without the addon hardware present.
- Fixed a sporadic crash in connection dialog.
- Fixed a sporadic inability to shutdown properly after more than one connection session.

**OATControl V0.9.9.30 - Updates**
- Revamped target choice into it's own modeless dialog. Targets are sorted by distance from current target. Double-click to transfer coordinates to target.
- Improved connectivity failure detection. Attempt a few retries.
- Added display of driver and microstepping settings (V1.8.76 or later required)
- Fixed display of remaining time to be correct regardless of microstep settings (V1.8.76 or later required)
- Fixed the bug that caused steps to be reverted to 1 when changing target coordinates (*facepalm*)

**OATControl V0.9.9.29 - Updates**
- Improved communications layer to handle Meade idiosyncracy in SC command
- Added some more points of interest.

**OATControl V0.9.9.28 - Updates**
- Sometimes reconnects would fail and crash OATControl. Now they don't crash it.
- Added a connection time display.
- Serial communciation would report successful connection even though it wasn't.
- Made double-click on device proceed to next step in connection dialog.

**OATControl V0.9.9.27 - Updates**
- TCP connections did not correctly shutdown the communications thread on disconnect. This could leave zombie OATcontrol processes on the machine.
- With no GPS, the lat long was not retrieved from OAT as the default for manual entry. It is now.
- Detected the incorrect RA/DEC steps in the Settings and pop up a message box suggesting factory reset.
- Fixed label width at bottom of main UI.

**OATControl V0.9.9.26 - Updates**
- Added label to slew rate boxes.
- Added log folder link to main UI.
- Added log file flushing in case of crash.

**OATControl V0.9.9.25 - Updates**
- Added Slewrate controls to Mini controller, bound to 1, 2, 3, and 4 keys.

**OATControl V0.9.9.24 - Updates**
- Added a mini controller that shows RA/DEC and allows manual slewing, homing and park, with keyboard support.
- Site altitude is now persisted across sessions.
- Connections are attempted multiple times if they fail.
- Prevented crashes when the firmware said GPS or Digital Level was attached, but weren't actually connected.
- Window positions are persisted across sessions.
- Moved Settings button down to above Connect button.

**OATControl V0.9.9.23 - Updates**
- Removed dummy call to consume second reply from :SC# command, since receive buffer is now cleared before each command

**OATControl V0.9.9.22 - Updates**
- Hardened network display in Settings dialog against bad data.
- Added synchronization to wait for jobs processor to quit before closing port.

**OATControl V0.9.9.21 - Updates**
- Fixed a parsing bug for float values.
- Seperated the network info in the Settings Dialog.

**OATControl V0.9.9.20 - Updates**
- Moved some calibration info to a seperate Settings dialog that also has a lot of additional info.
- Arrows on Target RA and DEC coordinates now auto-repeat and accelerate.
- Added a Factory Reset command (it's in the Settings dialog).
- If no GPS fix is attained, the LatLong is retrieved from OAT and used as the default.
- LatLong is displayed with two decimal places.
- Fixed some LatLong handling in the communications layer.
- Prevented from identical ports showing up in the list of available devices (Windows listed my COM5 port twice).
- Cleared the Receive Buffer before sending a new command to OAT.
- Detect that firmware is compiled with DEBUG_LEVEL and let user know.

**OATControl V0.9.9.18 - Updates**
- Fixed order of startup commands to correctly set target RA after date upload.

**OATControl V0.9.9.17 - Updates**
- Complete refactor of OAT communication handling to use multi-threaded, job queue system.
- Increased serial timeout to 1000ms.
- Corrected longitude handling and now sends time, date and location to OAT.
- Warning: may not work with GPS add on (yet).

**OATControl V0.9.9.16 - Updates**
- Various float CultureInfo conversion fixes
- Removed ASCOM library dependency to calculate LST

**OATControl V0.9.9.15 - Updates**
- Fix for switching RA/DEC steps to floating point.

**OATControl V0.9.9.14 - Updates**
- Added Button to open logs folder.
- Widened Connection dialog.

**OATControl V0.9.9.13 - Updates**
- Improved detection of connection error conditions.

**OATControl V0.9.9.12 - Updates**
- Added Pitch and Roll display to connection dialog (if Digital Level present)

**OATControl V0.9.9.11 - Updates**
- Adapted manual slewing to use real units. Requires V1.8.46 or later OAT firmware.

**OATControl V0.9.9.10 - Updates**
- Changed the connect flow to allow leveling (if present) and use the GPS (if present).
- Polar Alignment now uses levelling and has a better flow.

**OATControl V0.9.9.05 - Updates**
- Fixed a communication bug that occasionally caused issues after a slew command.
- Added a progress bar to RA and DEC that shows how far is left to slew.
- Added an application icon (feel free to improve it!)
- Added two more nebula to the points of interest
