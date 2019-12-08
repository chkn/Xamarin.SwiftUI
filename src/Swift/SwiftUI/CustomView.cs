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
	/// <remarks>
	/// An implementing class must supply a read-only property called "Body" that
	///  returns a concrete implementation of IView (a type with a <c>SwiftType</c>
	///  static property).
	/// </remarks>
	/// <typeparam name="T">The concrete type that implements this abstract class.</typeparam>
	public unsafe abstract class CustomView<T> : RefView<T>, ICustomView
		where T : CustomView<T>
	{
		// Since this class is generic, we will end up with a cached CustomViewType
		//  for each implementing type T.
		static CustomViewType CreateViewType () => new CustomViewType (typeof (T));
		readonly static Lazy<CustomViewType> swiftType
			= new Lazy<CustomViewType> (CreateViewType, LazyThreadSafetyMode.ExecutionAndPublication);

		public static ViewType SwiftType => swiftType.Value;
		protected override ViewType ViewType => swiftType.Value;

		protected override long NativeDataSize => swiftType.Value.NativeDataSize;

		long refCount = 1;

		// by convention:
		//public abstract TBody Body { get; }

		IView ICustomView.Body => (IView)swiftType.Value.BodyProperty.GetValue (this);
		void ICustomView.AddRef () => Interlocked.Increment (ref refCount);

		protected override void InitNativeData (byte [] data)
		{
			if (refCount < 1)
				throw new ObjectDisposedException (GetType ().FullName);

			fixed (void* ptr = &data[0]) {
				CustomViewData* cvd = (CustomViewData*)ptr;
				cvd->GcHandleToView = GCHandle.ToIntPtr (GCHandle.Alloc (this));
			}
		}

		protected override void DestroyNativeData (byte [] data)
		{
			// Do not call base here, as the VWT implementation simply calls back to Dispose
			fixed (void* ptr = &data [0]) {
				CustomViewData* cvd = (CustomViewData*)ptr;
				GCHandle.FromIntPtr (cvd->GcHandleToView).Free ();
			}
		}

		public override T Copy ()
		{
			// Do not call base here- we don't need to copy our data buffer
			Interlocked.Increment (ref refCount);
			return (T)this;
		}

		public override void Dispose ()
		{
			if (Interlocked.Decrement (ref refCount) <= 0)
				base.Dispose ();
		}
	}
}
