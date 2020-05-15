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
            // In case we get an EntryPointNotFoundException 
            var exception = Record.Exception (() => {
                Type t = typeof (Color);
                var properties = t.GetProperties (BindingFlags.Public | BindingFlags.Static);
                foreach (var item in properties) {
                    var colour = item.GetValue (null);
                    Assert.NotNull (colour);
                };

            });

            Assert.Null (exception);
        }

        [Fact]
        public void ColorHSBOCallWorks ()
        {
            // In case we get an EntryPointNotFoundException 
            var exception = Record.Exception (() => {
                var colour = new Color (0.0f, 0.6f, 0.0f, 0.5f);

                Assert.NotNull (colour);
            });

            Assert.Null (exception);
        }

        [Fact]
        public void ColorColorSpaceWOCallWorks ()
        {
            // In case we get an EntryPointNotFoundException
            var exception = Record.Exception (() => {
                var colour = new Color (RGBColorSpace.DisplayP3, 0.6f, 0.5f);

                Assert.NotNull (colour);
            });

            Assert.Null (exception);
        }

        [Fact]
        public void ColorColorSpaceRGBOCallWorks ()
        {
            // In case we get an EntryPointNotFoundException
            var exception = Record.Exception (() => {
                var colour = new Color (RGBColorSpace.sRGB, 0.0f, 0.6f, 0.0f, 0.5f);

                Assert.NotNull (colour);
            });

            Assert.Null (exception);
        }
    }
}