using System;

using Swift.Interop;

namespace Swift
{

	// FIXME: Remove this when mono supports Swift's calling convention
	public class SwiftGlueLib : NativeLib
	{
		public const string Path = /*TODO Remove ../ for iOS */ "../Frameworks/SwiftUIGlue.framework/SwiftUIGlue";

		public static SwiftGlueLib Pointers { get; } = new SwiftGlueLib ();

		internal SwiftGlueLib () : base (Path)
		{
		}

		IntPtr _bodyProtocolWitness;
		public IntPtr BodyProtocolWitness
			=> _bodyProtocolWitness == IntPtr.Zero ? (_bodyProtocolWitness = RequireSymbol ("$s11SwiftUIGlue9ThunkViewV4bodyq_vg")) : _bodyProtocolWitness;
	}
}
