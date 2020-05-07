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

            Window.RootViewController = new UIHostingViewController (new ClickButton ());

            Window.MakeKeyAndVisible ();
            return true;
        }
    }

    public class ClickButton : View
    {
        State<int?> counter = new State<int?> (null);

        // Using a Custom Asset Colour
        const string ColourAssetName = "MyYellow";
        Color myYellow = new Color (ColourAssetName);

        public ModifiedBackground<Button<Text>, ModifiedBackground<Text, Color>> Body {
            get {
                Button<Text> button = null;
                button = new Button<Text> (
                    () => {
                        var value = counter.Value ?? 0;
                        counter.Value = value + 1;
                    }, new Text (string.Format (counter.Value.HasValue ? "Clicked {0} times" : "Never been clicked", counter.Value))
                );

                var colour = counter.Value.HasValue ? counter.Value % 2 == 0 ? Color.Red : Color.Blue : myYellow;
                var colourText = counter.Value.HasValue ? counter.Value % 2 == 0 ? nameof (Color.Red) : nameof (Color.Blue) : ColourAssetName;

                return button.Background (new Text (colourText).Background (colour));
            }
        }
    }
}

