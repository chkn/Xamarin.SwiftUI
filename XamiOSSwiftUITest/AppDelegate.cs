using System;

using Foundation;
using UIKit;

using SwiftUI;
using XamSwiftUITestShared;

namespace XamiOSSwiftUITest
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : UIResponder, IUIApplicationDelegate
    {
        [Export("window")]
        public UIWindow Window { get; set; }

        [Export("applicationWillTerminate:")]
        public void WillTerminate (UIApplication application)
        {
            GC.Collect ();
        }

        [Export("application:didFinishLaunchingWithOptions:")]
        public bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
        {
            Window = new UIWindow (UIScreen.MainScreen.Bounds);

            Window.RootViewController = UIHostingViewController.Create (new ClickButton ());

            Window.MakeKeyAndVisible ();
            return true;
        }
    }
}

