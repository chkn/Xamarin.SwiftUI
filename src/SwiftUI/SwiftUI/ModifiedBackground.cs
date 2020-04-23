using System;
using Swift;
using Swift.Interop;

namespace SwiftUI
{
    public class ModifiedBackground<TView, TBackground> : View
		where TView : View
        where TBackground : View
    {
        public unsafe static SwiftType SwiftType {
            get {
                var arrayArgumentsPointer = stackalloc void*[2];
                arrayArgumentsPointer[0] = SwiftType.Of (typeof (TView))!.Metadata;
                arrayArgumentsPointer[1] = SwiftType.Of (typeof(TBackground))!.Metadata;
                var opaqueTypeMetadata = SwiftCoreLib.GetOpaqueTypeMetadata (0, arrayArgumentsPointer, SwiftUILib.Types.ViewBackgroundTypeDescriptor, 0);
                return new SwiftType (new IntPtr (opaqueTypeMetadata));
            }
        }

        internal ModifiedBackground (TaggedPointer taggedPointer, SwiftType viewType) : base (taggedPointer, viewType)
        {
        }
    }
}