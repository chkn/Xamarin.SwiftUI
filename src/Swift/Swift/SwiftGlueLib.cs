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

		IntPtr _viewBodyProtocolWitness;
		public IntPtr ViewBodyProtocolWitness
			=> _viewBodyProtocolWitness == IntPtr.Zero ? (_viewBodyProtocolWitness = RequireSymbol ("$s11SwiftUIGlue9ThunkViewV4bodyq_vg")) : _viewBodyProtocolWitness;

		IntPtr _viewModifierBodyProtocolWitness;
		public IntPtr ViewModifierBodyProtocolWitness
			=> _viewModifierBodyProtocolWitness == IntPtr.Zero ? (_viewModifierBodyProtocolWitness = RequireSymbol("$s11SwiftUIGlue9ThunkViewModifierV4bodyq_vg")) : _viewModifierBodyProtocolWitness;
	}
}
