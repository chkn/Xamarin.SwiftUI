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
	/// <summary>
	/// The native data, a pointer to which is passed to Swift.
	/// </summary>
	[StructLayout (LayoutKind.Sequential)]
	struct CustomViewData
	{
		internal IntPtr GcHandleToView;
		public View View => (View)GCHandle.FromIntPtr (GcHandleToView).Target;
	}

	/// <summary>
	/// A SwiftUI view.
	/// </summary>
	/// <remarks>
	/// This class is used as a base class, both for bindings of built-in
	///  SwiftUI views, and to define new custom views. 
	/// Bindings of built-in SwiftUI views must override the <see cref="ViewType"/>
	///  property.
	/// New custom views must supply a read-only property called <c>Body</c> that
	///  declares its return type as a concrete subclass of <see cref="View"/>
	///   (it may not declare its return type as the <see cref="View"/> base class itself).
	/// </remarks>
	public unsafe abstract class View : SwiftStruct
	{
		ViewType? viewType;
		protected internal virtual ViewType ViewType => viewType ?? (viewType = CustomViewType.Of (GetType ())!);
		protected sealed override SwiftType SwiftStructType => ViewType;

		// non-null if this is a custom (managed) View implementation
		CustomViewType? CustomViewType => ViewType as CustomViewType;

		GCHandle gch;
		long refCount = 0; // controls the lifetime of the GCHandle
		internal void AddRef () => Interlocked.Increment (ref refCount);

		// by convention:
		//public abstract TBody Body { get; }

		protected override void InitNativeData (void* handle)
		{
			var cvt = CustomViewType;
			Debug.Assert (cvt != null, "View bindings must override InitNativeData and not call base");

			gch = GCHandle.Alloc (this);
			((CustomViewData*)handle)->GcHandleToView = GCHandle.ToIntPtr (gch);
			cvt.InitNativeFields (this, handle);
		}

		protected internal override void DestroyNativeData (void* handle)
		{
			var cvt = CustomViewType;
			if (cvt == null) {
				base.DestroyNativeData (handle);
			} else {
				// Do not call base here, as the VWT implementation simply calls back to Dispose
				cvt.DestroyNativeFields (this, handle);
				if (gch.IsAllocated && Interlocked.Decrement (ref refCount) <= 0)
					gch.Free ();
			}
		}
	}
}
