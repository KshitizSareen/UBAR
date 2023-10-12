using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.InteropServices;
using WinUIEx;
using Microsoft.UI.Windowing;

namespace UBAR
{
    // Class containing native methods to work with the cursor position
    public static class NativeMethods
    {
        // Struct representing a point in 2D space
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        // External method to get cursor position from user32.dll
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        // Convenient method to get current cursor position
        public static POINT GetCursorPosition()
        {
            GetCursorPos(out POINT lpPoint);
            return lpPoint;
        }
    }

    // Main Window of the application
    public sealed partial class MainWindow : Window
    {
        // Constants
        private const int timerIntervalMillis = 16;
        private const int timerCountLimit = 3;
        private const int handleBarWidth = 15;
        private const int handleBarHeight = 300;

        // Timers for different functionalities
        private readonly DispatcherTimer resizeTimer;
        private readonly DispatcherTimer tickTimer;
        private readonly DispatcherTimer restoreTimer;

        // Other private fields
        private int tickCount;
        private int targetStepX = 100;
        private int pressedCount;
        private bool maximizedImage;
        private bool isResizing;
        private bool handleBarOpened;

        private int screenWidth;
        private int screenHeight;
        private int initialX;
        private int initialY;
        private int initialWindowWidth = 150;
        private int initialWindowHeight = 300;

        // Constructor
        public MainWindow()
        {
            InitializeComponent();

            InitializeWindow();

            // Setting up timers
            resizeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(timerIntervalMillis)
            };
            resizeTimer.Tick += OnResizeTimerTick;

            tickTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            tickTimer.Tick += Timer_Tick;

            restoreTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(timerIntervalMillis)
            };
            restoreTimer.Tick += OnRestoreTimerTick;

            // Event handler for the key down event on the window content
            Content.KeyDown += Window_KeyDown;
        }

        // Initialize the window settings
        private void InitializeWindow()
        {
            ExtendsContentIntoTitleBar = true;
            var windowManager = WindowManager.Get(this);
            windowManager.IsTitleBarVisible = false;
            windowManager.IsResizable = false;

            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
            screenWidth = displayArea.WorkArea.Width;
            screenHeight = displayArea.WorkArea.Height;

            initialX = screenWidth - handleBarWidth;
            initialY = (screenHeight - handleBarHeight) / 2;

            handle.Visibility = Visibility.Collapsed;
            this.MoveAndResize(initialX, initialY, initialWindowWidth, initialWindowHeight);

            image.Height = screenHeight;
        }

        // Handle the key down event for the window
        private void Window_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // When the space key is pressed
            if (e.Key == Windows.System.VirtualKey.Space)
            {
                pressedCount = 0;
                ShowHandleBar();
                ResetTimer();
            }
        }

        // Reset the timer
        private void ResetTimer()
        {
            tickCount = 0;
            tickTimer.Start();
        }

        // Show the handle bar
        private void ShowHandleBar()
        {
            if (handleBarOpened) return;

            handle.Visibility = Visibility.Visible;
            handleBarOpened = true;
        }

        // Hide the handle bar
        private void HideHandleBar()
        {
            handleBarOpened = false;
            handle.Visibility = Visibility.Collapsed;
        }

        // Tick event for the tick timer
        private void Timer_Tick(object sender, object e)
        {
            tickCount++;
            if (tickCount >= timerCountLimit)
            {
                HideHandleBar();
                tickTimer.Stop();
            }
        }

        // Pointer pressed event for the resize handle
        private void ResizeHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isResizing = true;
            tickTimer.Stop();
            Content.CapturePointer(e.Pointer);
        }

        // Pointer released event for the resize handle
        private void ResizeHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (isResizing)
            {
                isResizing = false;
                ResetTimer();
                Content.ReleasePointerCapture(e.Pointer);
            }
        }

        // Event when the resize timer ticks
        private void OnResizeTimerTick(object sender, object e)
        {
            if (AppWindow.Position.X > 0)
            {
                AdjustWindowPositionAndImageWidth();
                return;
            }

            MaximizeWindowView();
        }

        // Adjust window position and the width of the image during resizing
        private void AdjustWindowPositionAndImageWidth()
        {
            // Check if the new X position is still within the screen boundaries
            if (AppWindow.Position.X - targetStepX > 0)
            {
                this.Move(AppWindow.Position.X - targetStepX, AppWindow.Position.Y);
                image.Width += targetStepX;
            }
            else
            {
                this.Move(0, AppWindow.Position.Y);
                image.Width = Bounds.Width;
            }
        }

        // Maximize the window view
        private void MaximizeWindowView()
        {
            Canvas.SetLeft(image, 0);
            resizeTimer.Stop();
            this.SetWindowPresenter(AppWindowPresenterKind.FullScreen);
            handle.Visibility = Visibility.Collapsed;
            handleBarOpened = false;
            SetCanvasPosition(handle, Bounds.Width - handleBarWidth, (Bounds.Height - handleBarHeight) / 2);
            SetCanvasPosition(ButtonGrid, Bounds.Width, (Bounds.Height - handleBarHeight) / 2);
            image.Width = Bounds.Width;
            image.Height = Bounds.Height;
        }

        // Event when the toggle button is checked
        private void toggle1_Checked(object sender, RoutedEventArgs e)
        {
            if (pressedCount == 1)
            {
                ResetForToggleChecked();
                return;
            }
            Toggle1.IsChecked = true;
            ExpandWindow();
        }

        // Reset certain settings when the toggle button is checked
        private void ResetForToggleChecked()
        {
            pressedCount = 0;
            ShowHandleBar();
            ResetTimer();
            Toggle1.IsTabStop = false;
            Content.IsTabStop = true;
        }

        // Expand the window
        private void ExpandWindow()
        {
            this.MoveAndResize(AppWindow.Position.X, 0, screenWidth, screenHeight);
            SetCanvasPosition(handle, 0, (Bounds.Height - handleBarHeight) / 2);
            SetCanvasPosition(ButtonGrid, Bounds.Width, (Bounds.Height - handleBarHeight) / 2);
            resizeTimer.Start();
            maximizedImage = true;
            pressedCount++;
            Toggle1.IsTabStop = false;
        }

        // Event when the toggle button is unchecked
        private void toggle1_Unchecked(object sender, RoutedEventArgs e)
        {
            if (pressedCount == 1)
            {
                HandleUncheckedWithPressedCount();
                return;
            }
            RestoreWindowToDefault();
        }

        // Handle unchecked state when pressed count is 1
        private void HandleUncheckedWithPressedCount()
        {
            ShowHandleBar();
            ResetTimer();
            Toggle1.IsChecked = true;
            Toggle1.IsTabStop = false;
            Content.IsTabStop = true;
        }

        // Event when the restore timer ticks
        private void OnRestoreTimerTick(object sender, object e)
        {
            if (AppWindow.Position.X < initialX)
            {
                AdjustRestoreWindowPosition();
                return;
            }

            FinalizeRestoreView();
        }

        // Adjust window position during restore
        private void AdjustRestoreWindowPosition()
        {
            if (AppWindow.Position.X + targetStepX < screenWidth)
            {
                this.Move(AppWindow.Position.X + targetStepX, AppWindow.Position.Y);
            }
            else
            {
                this.Move(screenWidth, AppWindow.Position.Y);
                image.Width = 0;
            }
        }

        // Finalize restoring the window view
        private void FinalizeRestoreView()
        {
            restoreTimer.Stop();
            this.MoveAndResize(initialX, initialY, initialWindowWidth, initialWindowHeight);
            SetCanvasPosition(handle, 0, 0);
            SetCanvasPosition(image, handleBarWidth, 0);
            image.Width = 0;
            SetCanvasPosition(ButtonGrid, handleBarWidth, 0);
            maximizedImage = false;
            handleBarOpened = false;
            handle.Visibility = Visibility.Collapsed;
        }

        // Restore the window to its default settings
        private void RestoreWindowToDefault()
        {
            Toggle1.IsChecked = false;
            this.SetWindowPresenter(AppWindowPresenterKind.Default);
            restoreTimer.Start();  // Start the restore timer when the window should be restored
        }

        // Handle manipulation delta for resizing
        private void Manipulator_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (isResizing)
            {
                AdjustByCursorPosition(e);
                tickTimer.Stop();
            }
        }

        // Adjust window size and position based on cursor position during resizing
        private void AdjustByCursorPosition(ManipulationDeltaRoutedEventArgs e)
        {
            var cursorPosition = NativeMethods.GetCursorPosition();
            if (!maximizedImage)
            {
                var newPosition = Math.Clamp(cursorPosition.X, screenWidth - AppWindow.Size.Width, screenWidth - handleBarWidth);
                this.Move(newPosition, AppWindow.Position.Y);
            }
            else
            {
                var handleX = e.Position.X;
                var newPosition = Math.Clamp(handleX, Bounds.Width - initialWindowWidth, Bounds.Width - handleBarWidth);
                SetCanvasPosition(handle, newPosition, (Bounds.Height - handleBarHeight) / 2);
                SetCanvasPosition(ButtonGrid, newPosition + handleBarWidth, (Bounds.Height - handleBarHeight) / 2);
            }
        }

        // Set position of an UI element within a canvas
        private void SetCanvasPosition(UIElement element, double left, double top)
        {
            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
        }
    }
}
