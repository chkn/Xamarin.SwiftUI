using System;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	using static Button;

	[SwiftImport (SwiftUILib.Path)]
	public sealed class Button<TLabel> : View where TLabel : View
	{
		readonly Action action;
		readonly TLabel label;

		public Button (Action action, TLabel label)
		{
			this.action = action ?? throw new ArgumentNullException (nameof (action));
			this.label = label ?? throw new ArgumentNullException (nameof (label));
		}

		protected override unsafe void InitNativeData (void* handle, Nullability nullability)
		{
			var ctx = GCHandle.ToIntPtr (GCHandle.Alloc (action));
			using (var lbl = label.GetSwiftHandle (nullability [0])) {
				var lty = lbl.SwiftType;
				Init (handle, OnActionDel, OnDisposeDel, ctx, lbl.Pointer, lty.Metadata, lty.GetProtocolConformance (SwiftUILib.ViewProtocol));
			}
		}
	}

	unsafe static class Button
	{
		// FIXME: MonoPInvokeCallback
		static void OnAction (void* gcHandlePtr)
		{
			var gcHandle = GCHandle.FromIntPtr ((IntPtr)gcHandlePtr);
			((Action)gcHandle.Target).Invoke ();
		}
		internal static readonly PtrFunc OnActionDel = OnAction;

		static void OnDispose (void* gcHandlePtr)
		{
			var gcHandle = GCHandle.FromIntPtr ((IntPtr)gcHandlePtr);
			gcHandle.Free ();
		}
		internal static readonly PtrFunc OnDisposeDel = OnDispose;

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_Button_action_label")]
		internal static extern void Init (void* result, PtrFunc action, PtrFunc dispose, IntPtr ctx, void* labelData, TypeMetadata* labelType, ProtocolWitnessTable* labelViewConformance);
	}
}
