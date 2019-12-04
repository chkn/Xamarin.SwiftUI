using System;
using System.Buffers;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	interface IButton
	{
		void InvokeAction ();
	}

	public class Button<TLabel> : OpaqueView, IButton
		where TLabel : IView
	{
		static ViewType LabelType => Swift.Interop.SwiftType.Of (typeof (TLabel)) as ViewType ??
			throw new ArgumentException ("Expected ViewType", nameof (TLabel));

		public static ViewType SwiftType { get; } = SwiftUILib.Types.Button (LabelType);
		protected override ViewType ViewType => SwiftType;

		// intentionally box this so we can null it out after use and save a little memory
		IView? label;
		Action action;

		public Button (Action action, TLabel label)
		{
			this.label = label;
			this.action = action ?? throw new ArgumentNullException (nameof (action));
		}

		void IButton.InvokeAction () => action ();

		protected override unsafe void InitNativeData (IntPtr handle)
		{
			if (label is null)
				throw new InvalidOperationException ();

			using (var labelData = label.GetHandle ())
				Button.Init (handle, Button.InvokeAction, GCHandle.ToIntPtr (GCHandle), labelData.Pointer, LabelType.Metadata, LabelType.ViewConformance);

			label = null;
		}
	}

	unsafe static class Button
	{
		// FIXME: MonoPInvokeCallback
		internal static unsafe void InvokeAction (void* gcHandlePtr)
		{
			var gcHandle = GCHandle.FromIntPtr ((IntPtr)gcHandlePtr);
			((IButton)gcHandle.Target).InvokeAction ();
		}

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_Button_action_label")]
		internal static extern void Init (IntPtr result, PtrFunc action, IntPtr actionCtx, void* labelData, TypeMetadata* labelType, ProtocolWitnessTable* labelViewConformance);
	}
}
