__________________________________________________________________________________________

Package "Legs Animator"
Version 1.0.4.4.2

Made by FImpossible Creations - Filip Moeglich
http://fimpossiblecreations.pl
FImpossibleCreations@Gmail.com or FImpossibleGames@Gmail.com

__________________________________________________________________________________________

Asset Store Page: https://assetstore.unity.com/publishers/37262
Youtube: https://www.youtube.com/channel/UCDvDWSr6MAu1Qy9vX4w8jkw
Facebook: https://www.facebook.com/FImpossibleCreations
Twitter (@FimpossibleC): https://twitter.com/FImpossibleC
Discord Server: https://www.discord.gg/Y3WrzQp

___________________________________________________


Package Contests:

User Manual: Check the .pdf file

DEMO - Legs Animator.unitypackage
Package contains scenes with features examples of the plugin.

Legs Animator - Assembly Definitions.unitypackage (Supported since Unity 2017)
Assembly Definition files to speed up compilation time of your project (Fimpossible Directories will have no influence on compilation time of whole project)


Link to the Legs Animator implementations with other IK plugins (Final IK):
https://drive.google.com/drive/folders/1M5FZvrLCqUlsVa8iqNvdtDMmfr4uzDNV?usp=sharing


__________________________________________________________________________________________
Description:

Solve all of your leg animating problems with Legs Animator!

Legs Animator is component which provides a lot of features for characters with legs... so for almost all kinds of creatures.

List of features:

Aligning legs on uneven terrain
Handling leg attachement points (gluing)
Executing complex attachement transition animations (idle gluing)
Automatic turning-rotating in place leg animation (idle gluing)
Fixing sliding feet for no-root motion animations (movement gluing)
Animating hips stability giving realistic feel to the animations
Providing API for custom extensions of Legs Animator
Automatic strafe and 360 movement animating module (using single clip)
Push Impulses API (for landing bend impacts and others)
Extra helper features for automatic animations enchancing
Step Events handling for step sounds and particles
Fast setup and setup speedup tools
Works on any type of rig
Highly Optimized
Check Manual for more


__________________________________________________________________________________________

Version 1.0.4.4.2
- Fix: When using 'Keep Attached' No Raycast Behaviour mode, foot will use feet rotation angle limits

Version 1.0.4.4.1
- Fix: legs IK floor level refresh when doing ik setup on the rotated model

Version 1.0.4.4
- Added possibility to remove leg during playmode. myLegsAnimator.RemoveLeg(index)

Version 1.0.4.3
- Additional null protection for gluing attachements unexpected transform destruction
- Legs Blend Only On Idle example module

Version 1.0.4.2
- Fix for delayed initialization error on the build

Version 1.0.4.1
- Added 'Do Step Animation On Distance Factor' under gluing motion settings, to allow applying hips motion on smaller steps

Version 1.0.4.0
- Added 'Advanced' algorithm mode in the Leg Helper Module
- Added possibility to cull object when multiple renderers are invisible in the camera view

Version 1.0.3.9
- Added possibility to store "setup pose" in the editor, to make legs animator use it as referece pose on init (Setup -> IK)

Version 1.0.3.8
- 'Rigidbody Step Further' module file renamed to 'Step Further with Velocity'
- 'Step Further with Velocity' module will use position delta velocity if no rigidbody is found and not using Legs Animator.desired movement direction

Version 1.0.3.7
- 'Rigidbody Step Further' module now will work properly with 'Along Bones' raycast style
- 'Rigidbody Step Further' now contains ProvideVelocity() method

Version 1.0.3.6
- Fade Legs On Animator extra feature now will read provided state names/tags with spaces in names properly

Version 1.0.3.5
- Legs Animator asset store link in "?" button on the right up corner of inspector window
- Performance debugger switch by right-clicking on the component header
- Updated editor scripts

Version 1.0.3.4
- Fixed hips elevate properties display in the inspector GUI

Version 1.0.3.3
- Added RefreshHasOppositeLeg() method in the leg instance class

Version 1.0.3.2
- Fixed bug with not activating again after switching IsRagdolled to true

Version 1.0.3.1
- Added "SendOnStopping" property to allow sending step events more often
- Added few GUI improvements for the step event receiver property field

Version 1.0.3
- No Raycast Behaviour toggle under Setup -> Detection Bookmark
- More Spherecats Settings under Setup -> Detection Bookmark
- Fixed few rare issues with legs reinitialization 
- Change: ZeroStepsOnNoRaycast is deprecated variable now. NoRaycastGroundBehaviour enum is the replacement.

Version 1.0.2
- Added possibility to raycast continously when using custom raycast for the leg
- Added 'Edge Detector' custom module to put leg on the collider edge if detected

Version 1.0.1.9
- 'Only Local Animation' gluing variable, for characters on fast moving platforms
- NaN protection for target foot IK position

Version 1.0.1.8
- Fixed Calibrate option (fixing trigger colliders attached to legs)

Version 1.0.1.7
- added ILegRaiseReceiver interface to trigger custom action on leg raise

Version 1.0.1.6
- public Controll_DefineHashes method for refresing mecanim properties read values

Version 1.0.1.5
- Added possibility to override IK Processors with custom IK (inherited from FimpIK_Limb)
It's placed under 'Modules - Community' directory.

Version 1.0.1.4
- Added "Unity Humanoid IK" hint mode
- Added 'Override Direction' switch (coing) for 360 directional movement module

Version 1.0.1.3
- Added "Start Bone" raycast origin mode
- Added "Redirect Raycasting" module

Version 1.0.1.2
- Added buttons on the right to animator variables fields for quick select animator properies.
- Added Animal preset button (slightly different than insect preset)

Version 1.0.1.1
- Added Inverse Hint toggle under Setup->IK->IK Leg Settings
It will reverse bend direction for leg.
- Added support for script recompilling on script reload (unity 2022.3+)

Version 1.0.1 (containing hotfixes since v1.0.0)
- Added unscaled delta time switch
- Body Matrix Module - Selective Axis option
- (1.0.0.2.6) Now the Base module for auto-change parameters on animator states/tags has a feature to automatically detect top most weight layer to read animator states from
- (1.0.0.2.5) When gluing fade is almost value zero and becomes deactivated, its sheduling attachement refresh for next activation
- (1.0.0.2.4) Added Base module for auto-change parameters on animator states/tags being played
- New 2 modules: Utilities/Parameters -> Fade Gluing On Animator and Fade Legs System On Animator
- (1.0.0.2.3) Replaced float.MaxValue inside Mathf.SmoothDamp methods. float.MaxValue was unsafe and in rare cases was generating NaN errors.
- Few extra protections for NaN exceptions
- (1.0.0.2.2) Added User Teleport method
- Calibration for the optional spine and chest bone
- Updated 'All Fimpossible Creations Assembly Definitions' package file
- Fixed few GUI errors
- (1.0.0.2.1) Motion -> Gluing -> Unglue On - Now detects Yaw feet angle difference more precisely
- (1.0.0.2.0) Added hips NaN protections.
- Added Gluing Fade on animation tag and animator property value modules (can be added using project file field)
- (1.0.0.1.0) 360 movement extra refresh on re-activate
- User_OverwriteIKCoords() method for custom feet IK position control

Version 1.0.0:
> Initial release

