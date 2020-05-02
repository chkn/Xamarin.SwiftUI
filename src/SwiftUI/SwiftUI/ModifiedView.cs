using Swift.Interop;

namespace SwiftUI
{
	[SwiftImport (SwiftUILib.Path, "$s7SwiftUI4ViewPAAE10background_9alignmentQrqd___AA9AlignmentVtAaBRd__lFQOMQ")]
    public class ModifiedView<TView, TViewModifier> : View
        where TView : View
        where TViewModifier : ViewModifier<View>
    {
        internal ModifiedView (TaggedPointer taggedPointer)
            : base (taggedPointer)
        {
        }
    }
}