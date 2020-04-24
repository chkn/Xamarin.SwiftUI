using System;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	// Can't marshal generic Func<_,_> or Action<_> so we need to define the specific delegate
	//  types we need...

	unsafe delegate void PtrFunc (void* param1);

	unsafe delegate void PtrPtrFunc (void* param1, void* param2);

	unsafe delegate void* PtrToPtrFunc (void* param);

	unsafe delegate void DestroyFunc (void* obj, TypeMetadata* typeMetadata);

	unsafe delegate void* TransferFunc (void* dest, void* src, TypeMetadata* typeMetadata);

	unsafe delegate uint GetEnumTagSinglePayloadFunc (void* dest, uint emptyCases, TypeMetadata* typeMetadata);
	unsafe delegate void StoreEnumTagSinglePayloadFunc (void* dest, uint whichCase, uint emptyCases, TypeMetadata* typeMetadata);

	unsafe delegate IntPtr MetadataReq1 (long metadataReq, IntPtr p1);
	unsafe delegate IntPtr MetadataReq2 (long metadataReq, IntPtr p1, IntPtr p2);
	unsafe delegate IntPtr MetadataReq3 (long metadataReq, IntPtr p1, IntPtr p2, IntPtr p3);
	unsafe delegate IntPtr MetadataReq4 (long metadataReq, IntPtr p1, IntPtr p2, IntPtr p3, IntPtr p4);
	unsafe delegate IntPtr MetadataReq5 (long metadataReq, IntPtr p1, IntPtr p2, IntPtr p3, IntPtr p4, IntPtr p5);
	unsafe delegate IntPtr MetadataReq6 (long metadataReq, IntPtr p1, IntPtr p2, IntPtr p3, IntPtr p4, IntPtr p5, IntPtr p6);

	static class MetadataReq
	{
		public static Delegate MakeDelegate (int arity, IntPtr ftnPtr) => arity switch {
			1 => Marshal.GetDelegateForFunctionPointer<MetadataReq1> (ftnPtr),
			2 => Marshal.GetDelegateForFunctionPointer<MetadataReq2> (ftnPtr),
			3 => Marshal.GetDelegateForFunctionPointer<MetadataReq3> (ftnPtr),
			4 => Marshal.GetDelegateForFunctionPointer<MetadataReq4> (ftnPtr),
			5 => Marshal.GetDelegateForFunctionPointer<MetadataReq5> (ftnPtr),
			6 => Marshal.GetDelegateForFunctionPointer<MetadataReq6> (ftnPtr),
			_ => throw new NotImplementedException (arity.ToString ())
		};
	}
}
