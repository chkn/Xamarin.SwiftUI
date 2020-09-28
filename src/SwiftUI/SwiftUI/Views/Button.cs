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
		public Action Action { get; }
		public TLabel Label { get; [Obsolete(Msg.SetterInternal, false)] set; }

		// designated for use with view builder syntax only
		#pragma warning disable 8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
		public Button (Action action)
		#pragma warning restore 8618
		{
			Action = action ?? throw new ArgumentNullException (nameof (action));
		}

		public Button (Action action, TLabel label)
		{
			Action = action ?? throw new ArgumentNullException (nameof (action));
			#pragma warning disable 618 // obsolete
			Label = label ?? throw new ArgumentNullException (nameof (label));
			#pragma warning restore 618
		}

		protected override unsafe void InitNativeData (void* handle, Nullability nullability)
		{
			var ctx = GCHandle.ToIntPtr (GCHandle.Alloc (Action));
			using (var lbl = Label.GetSwiftHandle (nullability [0])) {
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
