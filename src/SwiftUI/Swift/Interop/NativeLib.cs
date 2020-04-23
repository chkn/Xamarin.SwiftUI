using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	public unsafe sealed class NativeLib : IDisposable, IEquatable<NativeLib>
	{
		const string LibDL = "/usr/lib/libdl.dylib";

		// lock!
		readonly static Dictionary<string,WeakReference<NativeLib>> cache = new Dictionary<string,WeakReference<NativeLib>> ();

		IntPtr handle;

		NativeLib (string path)
		{
			handle = Dlopen (path, 0);
			if (handle == IntPtr.Zero) {
				var err = Dlerror ();
				var errStr = (err != IntPtr.Zero)? Marshal.PtrToStringAnsi (err) : path;
				throw new DllNotFoundException (errStr);
			}
		}

		public static NativeLib Get (string path)
		{
			lock (cache) {
				if (!cache.TryGetValue (path, out var wr) || !wr.TryGetTarget (out var lib)) {
					lib = new NativeLib (path);
					cache [path] = new WeakReference<NativeLib> (lib);
				}
				return lib;
			}
		}

		public IntPtr TryGetSymbol (string symbol) => Dlsym (handle, symbol);

		public IntPtr RequireSymbol (string symbol)
		{
			var sym = TryGetSymbol (symbol);
			if (sym == IntPtr.Zero)
				throw new EntryPointNotFoundException (symbol);
			return sym;
		}

		// FIXME: Does this belong somewhere else?
		public ProtocolDescriptor* GetProtocol (string module, string name)
			=> (ProtocolDescriptor*)RequireSymbol ("$s" + SwiftType.Mangle (module, name) + "Mp");

		public bool Equals (NativeLib other) => handle == other.handle;
		public override bool Equals (object obj) => Equals ((NativeLib)obj);
		public override int GetHashCode () => unchecked ((int)handle);

		public void Dispose ()
		{
			if (handle != IntPtr.Zero) {
				Dlclose (handle);
				handle = IntPtr.Zero;
				GC.SuppressFinalize (this);
			}
		}
		~NativeLib () => Dispose ();

		[DllImport (LibDL, EntryPoint = "dlopen")]
		static extern IntPtr Dlopen (string path, int mode);

		[DllImport (LibDL, EntryPoint = "dlsym")]
		static extern IntPtr Dlsym (IntPtr lib, string symbol);

		[DllImport (LibDL, EntryPoint = "dlclose")]
		static extern int Dlclose (IntPtr lib);

		[DllImport (LibDL, EntryPoint = "dlerror")]
		static extern IntPtr Dlerror ();
	}
}
