=============================
OpenAstroTracker ASCOM Driver 
=============================

Version 6.5.4.0, published 11 April 2021
----------------------------------------

* History
	* 6.5.4.0	2021-04-11		:		CHANGE : Changed available and default baud rate(s).
	* 6.5.3.0	2021-04-10		:		CHANGE : Fixed Sync command. Added choosable baudrate. Lots of bug fixes.
	* 0.3.1.0	2021-03-20		:		CHANGE : Added Rates support and fixed a bug causing an error in MoveAxis.
	* 0.3.0.0	2020-11-30		:		CHANGE : Added move with slewrate support, stable release
	* 0.2.0.0b	2020-04-20		:		CHANGE : Local Server version of driver, first release
	* 0.1.4.2b	2020-04-20		:		CHANGE : Driver uses :CM LX200 Protocol command to sync
	* 0.1.4.1b	2020-04-18		:		BUGFIX : Driver not correctly handling return value from Halt
	* 0.1.4b	2020-04-18		:		CHANGE : Implement pulse guiding.
										BUGFIX : Uninstaller was not correctly removing previous driver DLL
	* 0.1.3.1	2020-04-16		:		BUGFIX : Allow , as decimal separator where Windows regional settings use it.
	* 0.1.3.0	2020-04-15		:		Initial release


* Arduino information
	* Tested on Arduino Mega and ESP32.  No other variants of Arduino have been tested, or are officially supported (MKS falls under Mega).
	* Tested with V1.9.03 firmware, which is the RECOMMENDED version.
	* Will probably work with earlier version (down to V1.6.32 and later).

* Last Conformance Test - 2021-04-09 - All tests passed, no errors, no issues.

* Cautions and warnings
	* CAUTION : Neither the mount nor the driver currently support setting any slew limits.  Thus it is quite possible 
				the mount may be told to slew to a position that causes a crash with some gadget you've printed and 
				added, or results in an untennable position for your camera, or any number of other "Bad Things (tm)".  
				So...keep track of your own towel.

* Known Issues
	* All known current issues are tracked on the Discord server.

* Work in Progress / Coming soon
	* Ability to set home position of mount via ASCOM
