using System;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	public delegate T ViewBody<T> () where T : IView;

	public unsafe abstract class View : IView
	{
		Data* handle;
		long refCount;

		public MemoryHandle Handle
			=> new MemoryHandle (handle == null ? (handle = CreateNativeData ()) : handle);

		internal abstract ViewType ViewType { get; }
		SwiftType ISwiftValue.SwiftType => ViewType;
		ViewType IView.SwiftType => ViewType;

		internal View ()
		{
		}

		protected T SetBody<T> (ViewBody<T> body) where T : IView
		{

		}

		internal static int GetNativeDataSize (Type closureType)
		{
			return sizeof (Data); //FIXME
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
			var result = (Data*)Marshal.AllocHGlobal (GetNativeDataSize (GetType ()));

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
				Marshal.FreeHGlobal ((IntPtr)handle);
				handle = null;
			}
		}
	}

	public class View<TBody> : View where TBody : IView
	{
#pragma warning disable RECS0108 // Warns about static fields in generic types
		static ViewType _swiftType;
		public static ViewType SwiftType {
			get => _swiftType ?? (_swiftType = CustomViewType.Create (typeof (TBody)));
			protected set => _swiftType = value;
		}
		internal override ViewType ViewType => SwiftType;
#pragma warning restore RECS0108 // Warns about static fields in generic types

		ViewBody<TBody> body;
		public TBody Body => body ();

		private protected View (ViewBody<TBody> body)
		{
			this.body = body ?? throw new ArgumentNullException (nameof (body));
		}
	}

	sealed class View<TBody,TClosure> : View<TBody> where TBody : IView
	{
		static View ()
			=> SwiftType = CustomViewType.Create (typeof (TBody), typeof (TClosure));

		public View (ViewBody<TBody> body) : base (body)
		{
		}
	}
}
