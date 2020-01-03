using System;
using System.Buffers;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	using static Button;

	public sealed class Button<TLabel> : View where TLabel : View
	{
		static ViewType LabelType => ViewType.Of (typeof (TLabel)) ??
			throw new ArgumentException ("Expected ViewType", nameof (TLabel));

		public static ViewType SwiftType { get; } = SwiftUILib.Types.Button (LabelType);
		protected internal override ViewType ViewType => SwiftType;

		Action action;
		TLabel label;

		public Button (Action action, TLabel label)
		{
			this.action = action ?? throw new ArgumentNullException (nameof (action));
			this.label = label ?? throw new ArgumentNullException (nameof (label));
		}

		protected override unsafe void InitNativeData (void* handle)
		{
			var ctx = GCHandle.ToIntPtr (GCHandle.Alloc (action));
			using (var labelData = label.GetHandle ())
				Init (handle, OnActionDel, OnDisposeDel, ctx, labelData.Pointer, LabelType.Metadata, LabelType.ViewConformance);
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
