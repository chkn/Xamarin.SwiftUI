using System;
using System.Reflection;
using System.Collections.Generic;

using Xunit;
using Swift.Interop;

namespace SwiftUI.Tests
{
    public class ValueTests : TestFixture
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

        [Theory]
        [MemberData (nameof (Tuples))]
        public unsafe void TupleValuesRoundtrip (object tuple)
        {
            var handle = tuple.GetSwiftHandle ();
            var tuple2 = SwiftValue.FromNative ((IntPtr)handle.Pointer, tuple.GetType ());
            Assert.Equal (tuple, tuple2);
        }

        public static IEnumerable<object []> Tuples
            => new[] {
                new object[] { Tuple.Create (1) },
                new object[] { Tuple.Create (1, 2) },
                new object[] { Tuple.Create (1, 2, 3, 4, 5, 6, 7) },
                new object[] { Tuple.Create (1, 2, 3, 4, 5, 6, 7, 8) },
                new object[] { Tuple.Create (1, 2, 3, 4, 5, 6, 7, Tuple.Create (8)) },
                new object[] { new Tuple<int,int,int,int,int,int,int,Tuple<int,int>> (1, 2, 3, 4, 5, 6, 7, Tuple.Create (8, 9)) },
                new object[] { ValueTuple.Create (1) },
                new object[] { (1, 2) },
                new object[] { (1, 2, 3, 4, 5, 6, 7) },
                new object[] { (1, 2, 3, 4, 5, 6, 7, 8) },
                new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9) }
            };
    }
}