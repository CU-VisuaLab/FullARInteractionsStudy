# AR Interactions

## Getting Started With Wiimote
1) (Hardware) Connect the wiimote via bluetooth to your computer.  
    - Make the wiimote visible by pressing 1+2 repeatedly throughout the process.  
    - The wiimote should show up in bluetooth settings as "Nintendo-RVL-CNT-01"
    - It will ask for a passcode (Windows 10).  Instead, just don't enter anything and click "Next"
    - End connection by pulling out the batteries.
    - Any subsequent connecting of the wiimote will require that you forget the device from your computer and re-pair.
    
2) (Software) Start the wiimote app:  ```WiimoteLib_1.7\WiimoteTest``` double-click

3) (Hardware) Turn on a IR sensor bar.  When the splash screen for the app loads, look at the sensor bar.
    - When you go to the ```WiimoteTest``` app running, you should see that the wiimote state changes when you point the wiimote around.
    
4) (Software) Start the Wiimote Server.  This reads input from the wiimote to the HoloLens.
    - Open VS solution file ```WiimoteServer\WiimoteServer``` and click start.
    - Now the server is waiting for two different connections from the HoloLens--one for position and one for button input.
    
    
5) (Hardware/Software) Deploy the app to the HoloLens
    - Nothing fancy here--the .unity file is in ```ThermostatInteractiveFar```.  Build to ```ThermostatInteractiveFar\AppWiimote```, run from ```ThermostatInteractiveFar\AppWiimote\ThermostatInteractiveFar``` (C# Project File)

### Including Wiimote Input in your scene
Imprort the Wiimote custom package

Go to ```ThermostatInteractiveFar\Assets\Wiimote\Prefabs```.  Drag ```WiimoteCursor``` into root of the Hierarchy and drag ```WiimoteInputManager``` under a GameObject called "Managers".  If necessary add an EventSystem to the "Managers" GameObject--to do so, select "Managers" in the Hierarchy view and click "Add Component" in the Inspector view.  Type "EventSystem".  

Also if necessary, delete/disable any ```ObjectCursor```, ```Basic Cursor``` or any other cursor when using ```WiimoteCursor```.  Similarly, delete/disable any existing ```InputManager``` when using ```WiimoteInputManager```.

Ctrl+B to build
-Go to "Player Settings > Capabilities" and enable the "InternetClient" capability (VERY IMPORTANT)
-Build scene to a folder called ```AppWimote```

### Current Status
Wiimote input seems to scale to other projects using custom package--debugging where necessary

### Known Bug Solutions
1) SocketException raised from ```WiimoteGazeManager.cs``` or ```WiimoteGesturesInput.cs```:  For some reason the Gaze Server and Button Server return inconsistent results on lines 84 and 78 of ```WiimoteServer\WiimoteButton.cs``` and ```WiimoteServer\WiimoteGaze.cs``` respectively.  Currently the code reads ```IPAddress ipAddr = ipHost.AddressList[#];```.  Put a breakpoint on that line (F9) in ```WiimoteServer\WiimoteButton.cs``` if the exception is raised in ```WiimoteGesturesInput.cs``` or put a breakpoint in that line in ```WiimoteServer\WiimoteGaze.cs``` if the exception is raised in ```WiimoteGazeManager.cs```. Run the WiimoteServer and check out the data for ipHost.AddressList when the program reaches the breakpoint. It SEEMS like you will want the index in the code to match whichever element is an IPv4 address, but I don't know for sure.  So if that doesn't work, play around with different indices.  Just depends on what mood the wiimote server is in.

2) Humor me.  Make sure you've enabled "InternetClient" in "Player Settings > Capabilities" when you go to build in Unity.

## Getting Started with Voice Control
Should be self-explanatory:  See ```CameraNearInteraction``` and open the ```CameraNearVoice``` scene; Build (make sure "Microphone" is enabled in Player Settings) to folder "AppVoice".  Should work if you say "Up", "Down", "Left", "Right", or "Center".