using System;
using System.Buffers;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	using static State;

	public sealed class State<TValue> : SwiftStruct<State<TValue>>
	{
		static SwiftType ValueType => Swift.Interop.SwiftType.Of (typeof (TValue)) ??
			throw new ArgumentException ("Expected SwiftType", nameof (TValue));

		public static SwiftType SwiftType { get; } = SwiftUILib.Types.State (ValueType);
		protected override SwiftType SwiftStructType => SwiftType;

		TValue initialValue;

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
					using (var handle = GetHandle ())
						GetWrappedValue ((void*)ptr, handle.Pointer, ValueType.Metadata);
					return SwiftValue.FromNative<TValue> (ptr);
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

				using (var handle = GetHandle ())
				using (var valueHandle = value.ToSwiftValue ()?.GetHandle ())
					SetWrappedValue (handle.Pointer, valueHandle.HasValue? valueHandle.Value.Pointer : null, ValueType.Metadata);
			}
		}

		public State (TValue initialValue)
		{
			this.initialValue = initialValue;
		}

		protected override unsafe void InitNativeData (void* handle)
		{
			using (var value = initialValue.ToSwiftValue ()?.GetHandle ())
				Init (handle, value.HasValue? value.Value.Pointer : null, ValueType.Metadata);
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
