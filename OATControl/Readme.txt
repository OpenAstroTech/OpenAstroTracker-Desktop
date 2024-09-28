Revision History
----------------

OATControl V1.1.7.0                                             28 Sept 2024
- Removed OAT Simulation code for standalone tool.
- Checklist title can be changed and the list can be edited directly from 
  the list.

OATControl V1.1.6.0                                             27 Sept 2024
- Checklist can now be moved and shown either at startup, on connect, or 
  on demand.

OATControl V1.1.5.0                                             24 Sept 2024
- Added support for a checklist to get setup for imaging.

OATControl V1.1.4.0                                             17 July 2024
- Added RA/DEC autohoming buttons and status to main window.
- Added support for AZ/ALT homing support. Mount keeps track of AZ and ALT
  movements across sessions and can slew back to home.

OATControl V1.1.3.0                                              2 June 2024
- Actually send (and save) the direction and distance set in settings 
  for Autohoming to OAT.

OATControl V1.1.2.0                                              11 May 2024
- Inverted DEC Parking Offset (park/unpark) for OAT

OATControl V1.1.1.0                                            13 April 2024
- Added support for showing Info Displays from mount firmware.
- Hardened some parsing from OAT replies to avoid crashes.

OATControl V1.1.0.0                                          14 January 2024
- Added an application Settings dialog that allows configuring the Autohome
  settings, the default baudrate, and the Targets list.
- Connecting via Serial no longer asks for Baudrate (taken from Settings)
- Targets list can be configured.
- Added 5th speed for slewing AZ and ALT. They now go 30, 7.5, 2.0, 0.25 and 
  0.05 arcminute distances, respectively.
- Fixed a bug that stored DEC Homing offset incorrectly.

OATControl V1.0.6.1                                             18 July 2023
- Fixed a bug that prevented DEC autohoming from running during connection.
- Fixed some bugs that were incorrectly persisting custom commands.

OATControl V1.0.6.0                                             16 July 2023
- Improved OAM support
- Fixed bug that made MiniControl Coordinates not update when slewing
- Added motor indicator to MiniController

OATControl V1.0.5.0                                              13 May 2023
- Fixed bug that never completed RA Homing when Track On Boot was disabled
  in the firmware
- Added support for OAM DEC display

OATControl V1.0.4.2                                              10 Jan 2023
- Get remaining tracking time from OAT (V1.12.6 firmware and above).
- Show active hemisphere on Settings page (V1.12.5 firmware and above).
- Fixed a bug that did not store all macro commands on Settings page.
- Made Sidereal Time and Remaining Time display sync with OAT once a minute 
  and use clock in between.
- Fixed bug that did not detect Idle state of OAT when it was Parked.
- Increased Send and Receive timeout on Serial connections to 2s
- Added steps calibration function to Settings page.

OATControl V1.0.3.0                                              27 Mar 2022
- Added support for firmware V1.11.5 that fixes the Meade longitude and UTC
  offset bugs.

OATControl V1.0.2.8                                              17 Mar 2022
- Added error handling to the parameters that update in real-time.
- Calculation of target marker is correct now (takes tracking into account)
- Fixed code to support COM10 and above.
- DEC axis is now labeled as degrees from home
- Button to make target equal to current has been labelled Update
- Improved logging output format.
- Logfile is now flushed at regular intervals instead of when logs are 
  submitted.
- Serial job sequence numbers and result status are attached to jobs now.
- Number of successful and failed jobs are tracked and dumped to log on exit.

OATControl V1.0.2.7                                              21 Jan 2022
- Added ability to define up to 4 custom commands to the Settings dialog.
- Update Target Chooser coordinates only when it's visible (performance issue).
- Added option to run RA Homing and DEC unparking to connection dialog.
- Improved readability of message boxes.
- Fixed ability to type numbers into target coordinates

OATControl V1.0.2.6                                              18 Oct 2021
- Refactored the way that coordinate system is handled to better cope with 
  negative coordinates.
- Fixed DEC sign for simulation
- Tracking indicator is not turned off when guiding anymore.
- Updated Target Chooser point positions after slewing is finished
- Rearranged and tidied up Settings dialog

OATControl V1.0.2.4                                              18 Jul 2021
- Fixed persistence of some settigns (ShowLimits, KeepMiniWindowOnTop,..)
- Added support for Hall sensor-based autohoming for the RA ring (requires 
  firmware V1.9.21)
- Added ability to park DEC before power off.
- Fixed Guiding indicator. Tracking remains on.
- Fixed indicator for RA position to take tracked distance into account.
- Moved some Settings buttons around.
- Added support for focuser reset on Settings page.

OATControl V1.0.2.2                                              21 Jun 2021
- DEC Go Home command was requiring one firmware version too high. Fixed.
- Focuser position was being queried incorrectly. Fixed.

OATControl V1.0.2.1                                              14 Jun 2021
- Enabled OATControl to use ASCOM driver.
- Added support for communication handlers to open a settings dialog.
- Added display of active connection to main window.
- Added ability to set a DEC offset from power on state to home and to slew 
  there (requires V1.9.16 firmware) on button press (see Settings Dialog)
- Mini Control can be set to be Always On Top (right click on Mini-Control 
  button in main UI).
- Swapped out the application settings logic so settings don't get lost 
  between builds.
- State of Show DEC limits toggle is now persisted.
- Made some cosmetic changes in Connection dialog to accomodate setup.
- Made some cosmetic changes in Main window to line up display of stepper 
  and mount values.
- Changed all black or dark text to light red for better readability.

OATControl V1.0.1.2                                              20 May 2021
- Re-enabled logging by default and provided a -nolog switch instead.
- Added display of Focus stepping position if focuser addon is present.

OATControl V1.0.1.1                                              18 May 2021
- Fixed communication bug that prevented ESP32 from staying connected.

OATControl V1.0.1.0                                              15 May 2021
- Support new split ALT/AZ configuration
- Allow ALT control in MiniController window (keys 'W' and 'S' to move up 
  and down, 1 - 4 to select distance).
- Allow AZ control in MiniController (keys 'A' and 'D' to move left and 
  right, 1 - 4 to select distance).
- Allow Focus control in MiniController (keys 'X' and 'C' move in and out)
- Added shortcut key display to MiniController arrows
- Added control area labels and changed some label colors in MiniController
- Added LST display to main window.
- Fixed the Stop button to actually work.
- Fixed some culture specific code that was causing issues with OAT on 
  non-US machines.
- Changed the way that connection retries work.
- Hardened app to better protect against disconnects and to prevent zombie 
  OATControls and random hangs when communication issues arise.
- Turned logging of for release app, but support -log commandline parameter 
  to turn it on.

OATControl V1.0.0.4                                              23 April 2021
 - Fixed a bug that would cause issues in regions/countries that did not use 
   the point ('.') character as a decimal separator.

OATControl V1.0.0.3                                              22 April 2021
 - Added support to run ALT motor from MiniController
 