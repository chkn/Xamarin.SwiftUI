using System;
using System.Buffers;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	using static Button;

	public class Button<TLabel> : RefView<Button<TLabel>>
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
			this.label = label ?? throw new ArgumentNullException (nameof (label));
			this.action = action ?? throw new ArgumentNullException (nameof (action));
		}

		protected override unsafe void InitNativeData (byte [] data)
		{
			// We only need the action instance
			var ctx = GCHandle.ToIntPtr (GCHandle.Alloc (action));

			fixed (void* handle = &data[0])
			using (var labelData = label!.GetHandle ())
				Init (handle, OnActionDel, OnDisposeDel, ctx, labelData.Pointer, LabelType.Metadata, LabelType.ViewConformance);

			label = null;
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
