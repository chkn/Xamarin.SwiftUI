
using System;
using System.ComponentModel;

using UIKit;
using SwiftUI;
using Foundation;

namespace CSharpSamples.Classic.iOS
{
	[DesignTimeVisible (true)]
	public partial class SwiftUIViewController : UIHostingViewController
	{
		public SwiftUIViewController () : base (new HelloView ())
		{
		}

		public SwiftUIViewController (IntPtr handle) : base (handle)
		{
		}

		// This allows us to call our default constructor when created from the storyboard.
		[Export ("awakeAfterUsingCoder:")]
		public SwiftUIViewController AwakeAfterUsingCoder (NSCoder coder)
			=> new SwiftUIViewController ();
	}
}