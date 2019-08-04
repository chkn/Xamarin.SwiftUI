using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using SwiftUI.Interop;

namespace SwiftUI
{
	public unsafe abstract class View
	{
		public struct Data : IView<Data>
		{
			internal IntPtr GcHandleToView;
			// .. the rest of the fields here ..

			public ViewType<Data> SwiftType => throw new NotImplementedException ();
			SwiftType<Data> ISwiftValue<Data>.SwiftType => throw new NotImplementedException ();

			public View View => (View)GCHandle.FromIntPtr (GcHandleToView).Target;

			public Data Copy () => this;
			public void Dispose () => SwiftType.Destroy (in this);
		}

		[EditorBrowsable (EditorBrowsableState.Advanced)]
		public Data* NativeData
			=> (_nativeData == (Data*)0)? (_nativeData = CreateNativeData ()) : _nativeData;
		Data* _nativeData;

		Data* CreateNativeData ()
		{
			// Calculate size of native data...
			var size = Marshal.SizeOf<Data> ();

			// Allocate memory
			var result = (Data*)Marshal.AllocHGlobal (size);

			// Assign fields
			result->GcHandleToView = GCHandle.ToIntPtr (GCHandle.Alloc (this));

			return result;
		}
	}
}
