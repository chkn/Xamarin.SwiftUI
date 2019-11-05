using System;
using System.Text;
using System.Runtime.InteropServices;

using Swift.Interop;

namespace Swift
{
	[StructLayout (LayoutKind.Sequential)]
	public readonly unsafe struct String : ISwiftValue<String>
	{
		public static SwiftType SwiftType => SwiftCoreLib.Types.String;
		SwiftType ISwiftValue.SwiftType => SwiftCoreLib.Types.String;

		public static String Empty => default;

		// FIXME: DIM
		MemoryHandle ISwiftValue.Handle => new MemoryHandle (this);

		[StructLayout (LayoutKind.Sequential)]
		readonly struct Data {
			readonly IntPtr p1, p2;
		}
		readonly Data data;

		public int Length => checked ((int)GetLength (data));

		public String (string str)
		{
			data = Create (str, (IntPtr)str.Length, (IntPtr)1);
		}

		public override string ToString ()
		{
			var len = Length;
			if (len <= 0)
				return string.Empty;

			// FIXME: GCHandle for str instead of closure?
			string? str = null;

			var arr = GetUtf8ContiguousArray (data);
			WithUnsafeBytes (bytes => {
				unsafe {
					str = Encoding.UTF8.GetString ((byte*)bytes, len);
					return (void*)0;
				}
			}, (void*)0, arr,
				elementType: SwiftCoreLib.Types.Int8.Metadata,
				resultType: SwiftCoreLib.Types.UnsafeRawPointer.Metadata);

			return str!;
		}

		public String Copy () => SwiftType.Copy (in this);

		public void Dispose () => SwiftType.Destroy (in this);

		// NOTE: Calling the Swift.String entry point that takes a UTF-16 string, as this shouldn't
		//  require managed marshaling.
		// See https://github.com/dotnet/docs/blob/master/docs/standard/native-interop/best-practices.md#string-parameters
		//
		// FIXME: Switch to Utf8String when available, as that is the native Swift string format.
		//  See https://swift.org/blog/utf8-string/
		[DllImport (SwiftFoundationLib.Path,
			CharSet = CharSet.Unicode,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$sSS10FoundationE14utf16CodeUnits5countSSSPys6UInt16VG_SitcfC")]
		static extern Data Create (
			[MarshalAs (UnmanagedType.LPWStr)] string str,
			IntPtr len,
			IntPtr unk); //FIXME: What is this last arg?? Swift always passes 0x1 here

		[DllImport (SwiftCoreLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$sSS5countSivg")]
		static extern IntPtr GetLength (Data str);

		[DllImport (SwiftCoreLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$sSS11utf8CStrings15ContiguousArrayVys4Int8VGvg")]
		static extern void* GetUtf8ContiguousArray (Data str);

		[DllImport (SwiftCoreLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$ss15ContiguousArrayV15withUnsafeBytesyqd__qd__SWKXEKlF")]
		static extern IntPtr WithUnsafeBytes (PtrToPtrFunc block, void* blockCtx, void* contiguousArray, TypeMetadata* elementType, TypeMetadata* resultType);
	}
}
