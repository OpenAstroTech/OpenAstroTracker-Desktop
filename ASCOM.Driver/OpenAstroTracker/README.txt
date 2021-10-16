+========================================+
| OpenAstroTracker ASCOM Driver V6.6.6.0 |
| Published: 16. October 2021            |
+========================================+

This is the latest ASCOM driver available for the OAT. It allows various 
client programs to communicate with the OAT using both standard LX200 Meade
protocol commands, as well as proprietary OAT extensions to that protocol.

The driver also contains some very rudimentary controls for the OAT so that
it can be unparked, homed, and parked from the driver Properties dialog.

 * Tested on MKS Gen L V2.1, Arduino Mega and ESP32. No other variants of 
   Arduino have been tested, or are officially supported.
 * Tested with V1.9.33 firmware, which is the RECOMMENDED version (although
   not released at this time, but coming soon).
 * It will probably work with earlier version (down to V1.6.32 and later).


Support
-------
 * For any issues or questions arising from teh use of this driver, or any
   other OAT-related questions or to interact with the OAT community, please
   visit our Discord server.


Testing
-------
Last Conformance Test - 2021-04-09 - All tests passed, no errors, no issues.


CAUTION
-------
Neither the mount nor the driver currently support setting any slew limits. 
Thus it is quite possible the mount may be told to slew to a position that 
causes the DEC axis to crash into the RA axis or results in an untennable 
position for your camera, or any number of other "Bad Things (tm)".  
So... keep track of your own towel and supervise operations.


Release History
---------------

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
