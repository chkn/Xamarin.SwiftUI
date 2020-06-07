using System;

using Foundation;
using UIKit;

using SwiftUI;
using XamMacSwiftUITest;

namespace XamiOSSwiftUITest
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }

        public override void WillTerminate (UIApplication application)
        {
            GC.Collect ();
        }

        public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
        {
            Window = new UIWindow (UIScreen.MainScreen.Bounds);

            Window.RootViewController = new UIHostingViewController (new ClickButton ());

            Window.MakeKeyAndVisible ();
            return true;
        }
    }
}