using System;
using Swift;
using Swift.Interop;

namespace SwiftUI
{
    public class ModifiedOpacity<T> : View where T : View
    {
        public unsafe static SwiftType SwiftType {
            get {
                var arrayArgumentsPointer = stackalloc void*[1];
                arrayArgumentsPointer[0] = SwiftType.Of (typeof (T))!.Metadata;
                var opaqueTypeMetadata = SwiftCoreLib.GetOpaqueTypeMetadata (0, arrayArgumentsPointer, SwiftUILib.Types.ViewOpacityTypeDescriptor, 0);
                return new SwiftType (new IntPtr (opaqueTypeMetadata));
            }
        }

        internal ModifiedOpacity (TaggedPointer taggedPointer, SwiftType viewType) : base (taggedPointer, viewType)
        {
        }
    }
}