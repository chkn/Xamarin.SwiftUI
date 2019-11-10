using System;
using System.Buffers;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	public unsafe abstract class View : IView
	{
		Data* handle;
		long refCount;

		public MemoryHandle Handle
			=> new MemoryHandle (handle == null ? (handle = CreateNativeData ()) : handle);

		internal abstract CustomViewType ViewType { get; }
		SwiftType ISwiftValue.SwiftType => ViewType;
		ViewType IView.SwiftType => ViewType;

		internal View ()
		{
		}

		/// <summary>
		/// The native data, a pointer to which is passed to Swift.
		/// </summary>
		internal struct Data {
			internal IntPtr GcHandleToView;
			// .. the rest of the fields here ..

			public View View => (View)GCHandle.FromIntPtr (GcHandleToView).Target;
		}

		internal void AddRef ()
			=> Interlocked.Increment (ref refCount);

		Data* CreateNativeData ()
		{
			Debug.Assert (refCount == 0);

			// Allocate memory
			var result = (Data*)Marshal.AllocHGlobal (ViewType.NativeDataSize);

			// Assign fields
			result->GcHandleToView = GCHandle.ToIntPtr (GCHandle.Alloc (this));

			// The GCHandle above takes a ref
			AddRef ();

			return result;
		}

		public void Dispose ()
		{
			if (Interlocked.Decrement (ref refCount) != 0)
				return;

			if (handle != null) {
				GCHandle.FromIntPtr (handle->GcHandleToView).Free ();
				Marshal.FreeHGlobal ((IntPtr)handle);
				handle = null;
			}
		}
	}

	/// <summary>
	/// A custom view.
	/// </summary>
	/// <typeparam name="TBody">The type of body view this custom view has</typeparam>
	public abstract class View<TBody, TState> : View where TBody : IView
	{
		static CustomViewType CreateViewType () => CustomViewType.For (typeof (TBody), typeof (TState));

		private protected static Lazy<CustomViewType> swiftType
			= new Lazy<CustomViewType> (CreateViewType, LazyThreadSafetyMode.ExecutionAndPublication);

		internal override CustomViewType ViewType => swiftType.Value;
		public static ViewType SwiftType => swiftType.Value;

		public abstract TBody Body { get; }
	}
}
