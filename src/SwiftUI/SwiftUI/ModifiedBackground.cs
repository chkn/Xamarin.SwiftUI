using System;
using Swift;
using Swift.Interop;

namespace SwiftUI
{
    [SwiftImport (SwiftUILib.Path, "$s7SwiftUI4ViewPAAE10background_9alignmentQrqd___AA9AlignmentVtAaBRd__lFQOMQ")]
    public class ModifiedBackground<TView, TBackground> : View
        where TView : View
        where TBackground : View
    {
        internal ModifiedBackground (TaggedPointer taggedPointer)
            : base (taggedPointer)
        {
        }
    }
}