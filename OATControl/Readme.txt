Revision History
----------------

OATControl V1.0.1.1                                              18 May 2021
- Fixed communication bug that prevented ESP32 from staying connected.

OATControl V1.0.1.0                                              15 May 2021
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

OATControl V1.0.0.4                                              23 April 2021
 - Fixed a bug that would cause issues in regions/countries that did not use 
   the point ('.') character as a decimal separator.

OATControl V1.0.0.3                                              22 April 2021
 - Added support to run ALT motor from MiniController
 