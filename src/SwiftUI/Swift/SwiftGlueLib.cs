using System;

using Swift.Interop;

namespace Swift
{
	public static class SwiftGlueLib
	{
		public const string Path =
		#if __MACOS__ || NETSTANDARD
			"libSwiftUIGlue.dylib";
		#else
			"Frameworks/SwiftUIGlue.framework/SwiftUIGlue";
		#endif

		// convenience
		static NativeLib Lib => NativeLib.Get (Path);

		static IntPtr _bodyProtocolWitness;
		internal static IntPtr BodyProtocolWitness
			=> _bodyProtocolWitness == IntPtr.Zero ? (_bodyProtocolWitness = Lib.RequireSymbol ("$s11SwiftUIGlue9ThunkViewV4bodyq_vg")) : _bodyProtocolWitness;
	}
}
