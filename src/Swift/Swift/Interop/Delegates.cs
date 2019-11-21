using System;

namespace Swift.Interop
{
	// Can't marshal generic Func<_,_> or Action<_> so we need to define the specific delegate
	//  types we need...

	unsafe delegate void PtrPtrFunc (void* param1, void* param2);

	unsafe delegate void* PtrToPtrFunc (void* param);

	unsafe delegate void DestroyFunc (void* obj, TypeMetadata* typeMetadata);

	unsafe delegate void* TransferFunc (void* dest, void* src, TypeMetadata* typeMetadata);
}
