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
	unsafe interface ICustomView : ISwiftFieldExposable, IView
	{
		new CustomViewType SwiftType { get; }
		ViewType IView.SwiftType => SwiftType;

		void OverwriteNativeData (void* newData);

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
		internal IntPtr GcHandleToView;
		public ICustomView View => (ICustomView)GCHandle.FromIntPtr (GcHandleToView).Target;
	}

	/// <summary>
	/// A custom view.
	/// </summary>
	/// <remarks>
	/// An implementing class must supply a read-only property called "Body" that
	///  returns a concrete implementation of IView (a type with a <c>SwiftType</c>
	///  static property).
	/// </remarks>
	/// <typeparam name="T">The concrete type that implements this abstract class.</typeparam>
	public unsafe abstract class CustomView<T> : SwiftStruct<T>, ICustomView
		where T : CustomView<T>
	{
		// Since this class is generic, we will end up with a cached CustomViewType
		//  for each implementing type T.
		static CustomViewType CreateViewType () => new CustomViewType (typeof (T));
		readonly static Lazy<CustomViewType> swiftType
			= new Lazy<CustomViewType> (CreateViewType, LazyThreadSafetyMode.ExecutionAndPublication);

		public static ViewType SwiftType => swiftType.Value;
		CustomViewType ICustomView.SwiftType => swiftType.Value;
		protected override SwiftType SwiftStructType => swiftType.Value;

		// controls the lifetime of the GCHandle
		long refCount = 1;
		void ICustomView.AddRef () => Interlocked.Increment (ref refCount);
		void ICustomView.OverwriteNativeData (void* newData) => OverwriteNativeData (newData);

		// by convention:
		//public abstract TBody Body { get; }

		protected override void InitNativeData (byte [] data, int offset)
		{
			fixed (void* handle = &data [offset]) {
				var cvd = (CustomViewData*)handle;
				cvd->GcHandleToView = GCHandle.ToIntPtr (GCHandle.Alloc (this));
			}
			swiftType.Value.InitNativeFields (this, data, offset);
		}

		protected override void DestroyNativeData (void* handle)
		{
			// Do not call base here, as the VWT implementation simply calls back to Dispose
			CustomViewData* cvd = (CustomViewData*)handle;
			swiftType.Value.DestroyNativeFields (this, handle);
			if (Interlocked.Decrement (ref refCount) <= 0)
				GCHandle.FromIntPtr (cvd->GcHandleToView).Free ();
		}

		public override T Copy ()
		{
			// Do not call base here- we don't need to copy our data buffer
			Interlocked.Increment (ref refCount);
			return (T)this;
		}
	}
}
