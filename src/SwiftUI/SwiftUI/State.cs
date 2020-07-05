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
	public sealed class State<TValue> : SwiftStruct, ISwiftValue
	{
		TValue initialValue;

		SwiftType? swiftType, valueType;
		Nullability valueNullability;

		protected override SwiftType SwiftType => swiftType ??= base.SwiftType;
		SwiftType ValueType
			=> valueType ??= SwiftType.Of (typeof (TValue), valueNullability) ?? throw new UnknownSwiftTypeException (typeof (TValue));

		public unsafe TValue Value {
			get {
				// Don't force premature initialization- in the common case we're in a custom view,
				//  it will init us when it's ready..
				if (!NativeDataInitialized)
					return initialValue;

				// Allocate memory for the value
				var ptr = Marshal.AllocHGlobal (ValueType.NativeDataSize);
				try {
					// FIXME: Results in 2 copies- can we do better?
					using (var handle = GetSwiftHandle ())
						GetWrappedValue ((void*)ptr, handle.Pointer, ValueType.Metadata);
					return (TValue)SwiftValue.FromNative (ptr, typeof (TValue), valueNullability)!;
				} finally {
					Marshal.FreeHGlobal (ptr);
				}
			}
			set {
				// Don't force premature initialization- in the common case we're in a custom view,
				//  it will init us when it's ready..
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

		void ISwiftValue.SetSwiftType (SwiftType swiftType, Nullability nullability)
		{
			Debug.Assert (valueType == null);
			this.swiftType = swiftType;
			this.valueNullability = nullability [0];
		}

		protected override unsafe void InitNativeData (void* handle)
		{
			using (var value = initialValue.GetSwiftHandle (valueNullability))
				Init (handle, value.Pointer, value.SwiftType.Metadata);
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
