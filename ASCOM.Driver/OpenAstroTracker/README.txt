+--------------------------------------------------------------------------+
|                                                                          |
|   OpenAstroTracker ASCOM Driver V6.6.7.3 Pre-Release                         |
|   Published: 23. June 2025                                              |
|                                                                          |
+--------------------------------------------------------------------------+

This is the latest ASCOM driver available for the OAT and OAM. It allows 
various client programs to communicate with the OAT using both standard 
LX200 Meade protocol commands, as well as proprietary OAT extensions to 
that protocol.

The driver is compatible with ASCOM 6.5 and 7.0.

The driver also contains controls for the OAT and OAM so that it can be 
initialized, configured, slewed, unparked, homed, focused and parked 
from the driver Properties dialog.

 * Tested on MKS Gen L V2.1, Arduino Mega and ESP32. No other variants of 
   Arduino have been tested, or are officially supported.
 * Tested with V1.10.4 firmware.
 * Tested with V1.11.0 firmware.
 * Tested with V1.13.12 firmware (RECOMMENDED).
 * It will probably work with earlier version (down to V1.6.32 and later).


Support
-------
 * For any issues or questions arising from the use of this driver, or any
   other OAT-related questions or to interact with the OAT community, 
   please visit our Discord server.


Testing
-------
 * 6.6.7.3 Not tested.

 * 6.6.7.2 Conformance Test (2025-Apr-20) 
    - Telescope: All tests passed, no errors, no warnings, 18 issues (all expected). 

 * 6.6.7.1 Conformance Test (2025-Feb-28) 
    - Telescope: All tests passed, no errors, no warnings, 23 issues (all expected). 

 * 6.6.7.0 No Conformance Test (2025-Feb-05) 

 * 6.6.6.9 Conformance Test (2025-Feb-02) 
    - Telescope: All tests passed, no errors, no warnings, 23 issues (all expected). 
    - Focuser:  All tests passed, no errors, no warnings, no issues. 

 * 6.6.6.8 Conformance Test (2024-Jan-20) 
    - Telescope: All tests passed, no errors, no warnings, 8 issues. 
    - Focuser:  All tests passed, no errors, no warnings, no issues. 

 * 6.6.6.7 Conformance Test (2024-Jan-19) 
    - Telescope: All tests passed, no errors, no warnings, 8 issues. 
    - Focuser:  All tests passed, no errors, no warnings, no issues. 

 * 6.6.6.5 Conformance Test (2022-Mar-16) 
    - Telescope: All tests passed, no errors, no warnings, 4 issues. 
    - Focuser:  All tests passed, no errors, no warnings, no issues. 

 * 6.6.6.4 Conformance Test (2022-Feb-01)
    - Telescope: All tests passed, no errors, no warnings, 4 issues. 
    - Focuser:  All tests passed, no errors, no warnings, no issues. 


CAUTION
-------
Neither the mount nor the driver currently support setting any slew limits. 
Thus it is quite possible the mount may be told to slew to a position that 
causes the DEC axis to crash into the RA axis or results in an untenable 
position for your camera, or any number of other "Bad Things (tm)".  
So... keep your towel handy and supervise operations.


Release History
---------------
 - 6.6.7.3 : Pre-Released 2025-06-23
   Added support for ASCOM driver to get and set time and date from and to mount.
 
 - 6.6.7.2 : Pre-Released 2025-04-20
   FindHome and AtHome implemented.
   Fixed a bug that caused errors when syncing location.
   Propagated location changes to the mount if connected.
   Fix user-input number parsing issues for non-US locales. 

 - 6.6.7.1 : Pre-Released 2025-02-28
   Fixed a bug that caused errors when syncing location.
   Propagated location changes to the mount if connected.

 - 6.6.7.0 : Pre-Released 2025-02-05
   Fixed a bug that would parse user-entered numbers in the en-US culture.

 - 6.6.6.9 : Released 2025-02-02
   Fixed a bug that caused an Exception when a floating point Elevation was entered.
   Fixed a bug where pressing the Stop button caused a communication timeout error.

 - 6.6.6.8 : Released 2024-01-20
   Added support for actually, really setting Longitude and Latitude.

 - 6.6.6.7 : Released 2024-01-19
   Added support for setting Longitude and Latitude.
   Increased allowed range of DEC and RA steps for OAM support.

 - 6.6.6.6 : Released 2022-03-27
   Adapted to use Firmware V1.11.5 and later. This firmware fixes the Longitude 
     and UTC offset bugs.

 - 6.6.6.5 : Released 2022-03-17
   Properties dialog: Added ability to invoke Hall sensor-based Auto Home RA
   Properties dialog: Added ability to set LST
   Properties dialog: Added Stop button
   Fixed a bug that made Park DEC go in the wrong direction
   Added finer-grained logging ability
   Added message number to logging calls
   Changed logging to flush at regular intervals
   Allowed read before write of Target coords (this causes the conformance 
     issues, but allows NINA/KSTARS to work)
   Now supports COM10 and higher
   Removed call to oat to get sync solutions
   Support switching initial direction for RA Auto Home (Shift key)
   Improve tooltips throughout Setup dialog
   Improved error handling and messaging on connection issues
      
 - 6.6.6.4 : Pre-Released 2022-02-01
   Properties dialog: Added ability to invoke Hall sensor-based Auto Home RA
   Properties dialog: Added ability to set LST
   Properties dialog: Added Stop button
   Fixed a bug that made Park DEC go in the wrong direction
   Added finer-grained logging ability
   Added message number to logging calls
   Changed logging to flush at regular intervals
   Allowed read before write of Target coords (this causes the conformance 
     issues, but allows NINA/KSTARS to work)

 - 6.6.6.2 : Pre-Released 2021-11-05
   Added ability to record sync solutions in debug log

 - 6.6.6.0 : Released 2021-10-16
   Added ability to control OAT stepper motors, unpark, park, home and set 
   home as well as view some basic info. 

 - 6.6.5.0 : Released 2021-07-06
   Added ability to connect to OAT and set steps and speed.
 
 - 6.6.3.0 : Released 2021-06-13
   Changed passthrough command to support double-# terminated reply.
 
 - 6.6.2.0 : Released 2021-06-12
   Changed passthrough command to support OATControl.
 
 - 6.6.1.0 : Released 2021-05-03
   Added support for OAT focuser.
 
 - 6.5.4.0 : Released 2021-04-15
   Changed available and default baud rate(s).
 
 - 6.5.3.0 : Released 2021-04-10
   Fixed Sync command. Added choosable baudrate. Lots of bug fixes.
 
 - 0.3.1.0 : Released 2021-03-20
   Added Rates support and fixed a bug causing an error in MoveAxis.
 
 - 0.3.0.0 : Released 2020-11-30
   Added move with slewrate support, stable release
 
 - 0.2.0.0b : Released 2020-04-20
   Local Server version of driver, first release
 
 - 0.1.4.2b : Released 2020-04-20
   Driver uses :CM LX200 Protocol command to sync
 
 - 0.1.4.1b : Released 2020-04-18
   Driver not correctly handling return value from Halt
 
 - 0.1.4b : Released 2020-04-18
   Implement pulse guiding.
   Uninstaller was not correctly removing previous driver DLL

 - 0.1.3.1 : Released 2020-04-16
   Allow , as decimal separator where Windows regional settings use it.

 - 0.1.3.0 : Released 2020-04-15
   Initial release
