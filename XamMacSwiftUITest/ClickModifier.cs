using System;
using SwiftUI;

namespace XamMacSwiftUITest
{
    internal class ClickModifier : ViewModifier
    {
        public ClickModifier () : base()
        {
            Console.WriteLine( "Creating ClickModifier");
        }
    }
}