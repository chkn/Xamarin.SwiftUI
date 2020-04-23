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

	[AttributeUsage (AttributeTargets.Class, Inherited = true)]
	sealed class CustomViewAttribute : SwiftTypeAttribute
	{
		protected internal override SwiftType GetSwiftType (Type attributedType, SwiftType []? genericArgs)
			=> new CustomViewType (attributedType);
	}

	/// <summary>
	/// A SwiftUI view.
	/// </summary>
	/// <remarks>
	/// This class is used as a base class, both for bindings of built-in
	///  SwiftUI views, and to define new custom views.
	/// Bindings of built-in SwiftUI views must have a <see cref="SwiftImportAttribute"/>,
	///  otherwise the type is assumed to be a custom view.
	/// New custom views must supply a read-only property called <c>Body</c> that
	///  declares its return type as a concrete subclass of <see cref="View"/>
	///   (it may not declare its return type as the <see cref="View"/> base class itself).
	/// </remarks>
	[CustomView]
	[SwiftProtocol (SwiftUILib.Path, "SwiftUI", "View")]
	public unsafe abstract class View : SwiftStruct
	{
		// by convention:
		//public abstract TBody Body { get; }

		// non-null if this is a custom (managed) View implementation
		internal CustomViewType? CustomViewType => SwiftType as CustomViewType;

		GCHandle gch;
		long refCount = 0; // number of refs passed to native code

		public View ()
		{
		}

		/// <summary>
		/// Only to be used for opaque <c>some View</c> return types.
		/// </summary>
		protected View (TaggedPointer data)
		{
			this.data = data;
		}

		internal void AddRef ()
		{
			// When the ref count goes to 1, we know that native code is making
			//  a copy of our data, so convert to normal GCHandle to retain our
			//  managed instance..
			if (Interlocked.Increment (ref refCount) == 1)
				SetGCHandle (data.Pointer, GCHandleType.Normal);
		}

		internal void UnRef ()
		{
			// When ref count goes back to 0, native code is no longer holding
			//  any copies of our data, so we can convert the GCHandle back to
			//  weak to allow us to be collected...
			if (Interlocked.Decrement (ref refCount) == 0)
				SetGCHandle (data.Pointer, GCHandleType.WeakTrackResurrection);
		}

		protected override void InitNativeData (void* handle)
		{
			var cvt = CustomViewType;
			Debug.Assert (cvt != null, "View bindings must override InitNativeData and not call base");

			// First alloc a weak GCHandle, since we don't know if native code will
			//  make a copy or not...
			SetGCHandle (handle, GCHandleType.WeakTrackResurrection);

			// FIXME: '!' shouldn't be needed as we have Debug.Assert
			cvt!.InitNativeFields (this, handle);
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			if (gch.IsAllocated)
				gch.Free ();
		}

		void SetGCHandle (void* handle, GCHandleType type)
		{
			if (gch.IsAllocated)
				gch.Free ();
			gch = GCHandle.Alloc (this, type);
			var ptr = GCHandle.ToIntPtr (gch);
			((CustomViewData*)handle)->GcHandleToView = ptr;
		}
	}
}
