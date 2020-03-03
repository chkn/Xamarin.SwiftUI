using System;

using Foundation;
using UIKit;

using SwiftUI;

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

            Window.RootViewController = UIHostingViewController.Create (new ClickButton ());

            Window.MakeKeyAndVisible ();
            return true;
        }
    }

    public class ClickButton : View
    {
        State<int> counter = new State<int>(0);
        public Button<Text> Body =>
            new Button<Text>(() => counter.Value += 1, new Text(string.Format("Clicked {0} times", counter.Value)));
    }
}

