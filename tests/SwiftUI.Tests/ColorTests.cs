using System;
using System.Reflection;
using Xunit;

namespace SwiftUI.Tests
{
    public class ColorTests : TestFixture
    {
        [Fact]
        public void AllStaticColorCallsWork ()
        {
            Type t = typeof(Color);
            var properties = t.GetProperties (BindingFlags.Public | BindingFlags.Static);
			foreach (var item in properties) {
                var value = item.GetValue (null);
                Assert.NotNull (value);
			}
        }

        [Fact]
        public void ColorHSBOCallWorks ()
        {
            var colour = new Color (0.0f, 0.6f, 0.0f, 0.5f);

            Assert.NotNull (colour);
        }
    }
}