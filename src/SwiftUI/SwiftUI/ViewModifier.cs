using Swift.Interop;

namespace SwiftUI
{
	[SwiftImport (SwiftUILib.Path)]
	public abstract unsafe class ViewModifier<T> : View
	{
		public abstract T Body (Content content);

		protected override void InitNativeData (void* handle)
		{
			// Using TransferFuncType.InitWithTake here in case we leak.
			//using (var dataHandle = Data.GetSwiftHandle ())
			// SwiftType.Transfer (handle, dataHandle.Pointer, TransferFuncType.InitWithTake);
		}
	}

	[SwiftImport (SwiftUILib.Path)]
	public unsafe class Content : View
	{
		protected override void InitNativeData (void* handle)
		{
			// Using TransferFuncType.InitWithTake here in case we leak.
			//using (var dataHandle = Data.GetSwiftHandle ())
			// SwiftType.Transfer (handle, dataHandle.Pointer, TransferFuncType.InitWithTake);
		}
	}
}