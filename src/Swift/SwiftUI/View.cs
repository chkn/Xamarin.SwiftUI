using System;
using System.Buffers;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	interface ICustomView : IView
	{
		IView Body { get; }

		/// <summary>
		/// Called by Swift to add a reference to our native data.
		/// </summary>
		void AddRef ();
	}

	/// <summary>
	/// The native data, a pointer to which is passed to Swift.
	/// </summary>
	[StructLayout (LayoutKind.Sequential)]
	struct CustomViewData
	{
		internal IntPtr GcHandleToView; // < MUST be first (see ThunkView in SwiftUIGlue)
		public ICustomView View => (ICustomView)GCHandle.FromIntPtr (GcHandleToView).Target;
	}

	/// <summary>
	/// A custom view.
	/// </summary>
	/// <typeparam name="TBody">The type of body view this custom view has</typeparam>
	public unsafe abstract class View<TBody, TState> : ICustomView
		where TBody : IView
	{
		static CustomViewType CreateViewType ()
			=> CustomViewType.For (typeof (TBody), typeof (TState));

		readonly static Lazy<CustomViewType> swiftType
			= new Lazy<CustomViewType> (CreateViewType, LazyThreadSafetyMode.ExecutionAndPublication);

		public static ViewType SwiftType => swiftType.Value;
		SwiftType ISwiftValue.SwiftType => SwiftType;
		ViewType IView.SwiftType => SwiftType;

		CustomViewData* handle;
		long refCount;

		public MemoryHandle Handle
			=> new MemoryHandle (handle == null ? (handle = CreateNativeData ()) : handle);

		public abstract TBody Body { get; }
		IView ICustomView.Body => Body;

		CustomViewData* CreateNativeData ()
		{
			Debug.Assert (refCount == 0);

			// Allocate memory
			var result = (CustomViewData*)Marshal.AllocHGlobal (swiftType.Value.NativeDataSize);

			// Assign fields
			result->GcHandleToView = GCHandle.ToIntPtr (GCHandle.Alloc (this));

			// The GCHandle above takes a ref
			AddRef ();

			return result;
		}

		void AddRef () => Interlocked.Increment (ref refCount);
		void ICustomView.AddRef () => AddRef ();

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
}
