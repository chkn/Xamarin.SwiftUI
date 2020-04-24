using System;
using Swift;
using Swift.Interop;

namespace SwiftUI
{
    [SwiftImport (SwiftUILib.Path, "$s7SwiftUI4ViewPAAE7opacityyQrSdFQOMQ")]
    public class ModifiedOpacity<T> : View where T : View
    {
        internal ModifiedOpacity (TaggedPointer taggedPointer)
            : base (taggedPointer)
        {
        }
    }
}