using System;
using System.Runtime.InteropServices;

namespace SwiftUI.Interop
{
	public class NativeLib : IDisposable
	{
		const string LibDL = "/usr/lib/libdl.dylib";

		protected IntPtr Handle { get; private set; }

		public NativeLib (string path)
		{
			Handle = Dlopen (path, 0);
			if (Handle == IntPtr.Zero) {
				var err = Dlerror ();
				var errStr = (err != IntPtr.Zero)? Marshal.PtrToStringAnsi (err) : path;
				throw new DllNotFoundException (errStr);
			}
		}

		public IntPtr TryGetSymbol (string symbol) => Dlsym (Handle, symbol);

		public IntPtr RequireSymbol (string symbol)
		{
			var sym = TryGetSymbol (symbol);
			if (sym == IntPtr.Zero)
				throw new EntryPointNotFoundException (symbol);
			return sym;
		}

		public void Dispose ()
		{
			if (Handle != IntPtr.Zero) {
				Dlclose (Handle);
				Handle = IntPtr.Zero;
			}
		}

		[DllImport (LibDL, EntryPoint = "dlopen")]
		public static extern IntPtr Dlopen (string path, int mode);

		[DllImport (LibDL, EntryPoint = "dlsym")]
		public static extern IntPtr Dlsym (IntPtr lib, string symbol);

		[DllImport (LibDL, EntryPoint = "dlclose")]
		public static extern int Dlclose (IntPtr lib);

		[DllImport (LibDL, EntryPoint = "dlerror")]
		public static extern IntPtr Dlerror ();
	}
}
