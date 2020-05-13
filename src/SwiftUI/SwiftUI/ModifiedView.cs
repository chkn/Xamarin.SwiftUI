using Swift.Interop;

namespace SwiftUI
{
	[SwiftImport (SwiftUILib.Path)]
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