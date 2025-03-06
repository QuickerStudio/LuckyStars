# LukyStars

3 WPF&.NET8.C# LukyStars wallpaper

                    #  Biuld by Github Copilot Pro

LuckyStars Desktop Theme App Interface & Functionality Analysis
Designed interface
In this project, I designed 3 main interfaces:

MainWindow - the main display window for wallpapers
Catcher - File drag-and-drop capture window
System tray icon - not a window in the traditional sense, but exists as a user interface
Functions of each interface
1. MainWindow
Full-screen exclusive mode displays wallpapers
Threads that occupy system wallpapers are displayed at the bottom of the desktop
Support wallpaper playback in both image and HTML formats
Use the WebView2 control to stay pinned and support interaction with HTML
The window icon does not appear in the taskbar
2. Catcher (Capture Window)
A transparent window designed to appear directly above the NotifyIcon
Detects file dragging behavior
Receive the drag-and-drop file and pass it to the main window for processing
Provide visual feedback that drag-and-drop files can be received
3. System tray icon
Click to toggle the wallpaper play/pause state
Right-click to display a menu with exit options
Support drag-and-drop file operation to activate the Catcher window
Comparison with program requirements
Core requirements in the program:
Main window function

✅ Full-screen exclusive mode
✅ Occupy the system wallpaper thread
✅ It supports image and HTML playback
✅ The WebView2 control remains pinned and supports mouse interaction with HTML
✅ The window icon disappears in the taskbar
NotifyIcon interaction

✅ Click NotifyIcon to toggle play/pause
✅ Right-click to display the exit menu
✅ Drag the file to the NotifyIcon to display the Catcher window
✅ Copy the file to the specified folder and display it
Catcher function

✅ A transparent window appears directly above the NotifyIcon
✅ Detect file dragging
✅ Get the path of the drag-and-drop file and pass it to MainWindow for playback
File storage

✅ Flat storage + smart renaming
✅ It is stored in the LuckyStarsWallpaper folder in the document directory
✅ Separate storage by file type (picture/html)
Multi-monitor support

✅ Automatically adapts to different DPI and screen layouts
✅ Support for setting different wallpapers on different displays (enhancements)
Wallpaper management

✅ Pause wallpaper playback while the system is sleeping
✅ Adjust the brightness and chromaticity of the wallpaper according to the screen information
✅ Enhancements to performance optimizations (e.g., detection of full-screen apps, etc.)
Format conversion

✅ Enhanced format conversion to support more file types
✅ FFmpeg integration is added to support video conversion
✅ Automatic download of required dependencies (silent operation)
summary
The interface and functionality of the design are fully in line with the requirements of the plan, and even enhanced in the following aspects:

Added premium support for multiple monitors, allowing you to set different wallpapers on different monitors
Smarter performance optimizations have been added to automatically adjust wallpaper behavior based on system status
The format conversion function has been expanded to support the processing of more file types
Added the silent download and integration mechanism of FFmpeg to improve the video processing power
Enhanced system integration to better respond to system events such as full-screen apps, power status, and more
The project follows. .NET 8 and WPF framework development, implemented in C# language,
