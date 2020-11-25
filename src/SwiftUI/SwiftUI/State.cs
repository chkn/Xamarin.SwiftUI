using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	using static State;

	[SwiftImport (SwiftUILib.Path)]
	public sealed record State<TValue> : SwiftStruct
	{
		TValue initialValue;

		SwiftType? valueType;
		Nullability valueNullability;

		public unsafe TValue Value {
			get {
				if (!NativeDataInitialized)
					return initialValue;

				// Allocate memory for the value
				var ptr = Marshal.AllocHGlobal (valueType!.NativeDataSize);
				try {
					// FIXME: Results in 2 copies- can we do better?
					using (var handle = GetSwiftHandle ())
						GetWrappedValue ((void*)ptr, handle.Pointer, valueType.Metadata);
					return (TValue)SwiftValue.FromNative (ptr, typeof (TValue), valueNullability)!;
				} finally {
					Marshal.FreeHGlobal (ptr);
				}
			}
			set {
				if (!NativeDataInitialized) {
					initialValue = value;
					return;
				}

				using (var handle = GetSwiftHandle ())
				using (var valueHandle = value.GetSwiftHandle (valueNullability))
					SetWrappedValue (handle.Pointer, valueHandle.Pointer, valueHandle.SwiftType.Metadata);
			}
		}

		public State (TValue initialValue)
		{
			this.initialValue = initialValue;
		}

		protected override unsafe void InitNativeData (void* handle, Nullability nullability)
		{
			valueNullability = nullability [0];
			using (var value = initialValue.GetSwiftHandle (valueNullability)) {
				valueType = value.SwiftType;
				Init (handle, value.Pointer, valueType.Metadata);
			}
		}
	}

	unsafe static class State
	{
		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_State_initialValue")]
		internal static extern void Init (void* result, void* initialValue, TypeMetadata* initialValueType);

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_State_wrappedValue_getter")]
		internal static extern void GetWrappedValue (void* result, void* state, TypeMetadata* valueType);

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_State_wrappedValue_setter")]
		internal static extern void SetWrappedValue (void* state, void* value, TypeMetadata* valueType);
	}
}
