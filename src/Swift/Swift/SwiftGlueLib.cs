using System;

using Swift.Interop;

namespace Swift
{

	// FIXME: Remove this when mono supports Swift's calling convention
	public class SwiftGlueLib : NativeLib
	{
		public const string Path = "libSwiftUIGlue.dylib";

		public static SwiftGlueLib Pointers { get; } = new SwiftGlueLib ();

		internal SwiftGlueLib () : base (Path)
		{
		}

		IntPtr _bodyProtocolWitness;
		public IntPtr BodyProtocolWitness
			=> _bodyProtocolWitness == IntPtr.Zero ? (_bodyProtocolWitness = RequireSymbol ("$s11SwiftUIGlue9ThunkViewV4bodyq_vg")) : _bodyProtocolWitness;
	}

	public class SwiftGlueiOSLib : SwiftGlueLib
	{
		public new const string Path = "libSwiftUIGlue_IOS.dylib";
	}
}
