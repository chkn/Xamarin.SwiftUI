using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	public unsafe struct TaggedPointer : IEquatable<TaggedPointer>, IDisposable
	{
		void* ptr;

		public void* Pointer => Untag (ptr);
		public bool IsOwned => IsTagged (ptr);

		public TaggedPointer (void* ptr, bool owned)
		{
			this.ptr = owned? Tag (ptr) : ptr;
			Debug.Assert (owned || !IsTagged (ptr));
		}

		public TaggedPointer (IntPtr ptr, bool owned): this ((void*)ptr, owned)
		{
		}

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public static TaggedPointer AllocHGlobal (int size)
			=> new TaggedPointer (Marshal.AllocHGlobal (size), owned: true);

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public static TaggedPointer AllocHGlobal (IntPtr size)
			=> new TaggedPointer (Marshal.AllocHGlobal (size), owned: true);

		public void Dispose ()
		{
			if (IsTagged (ptr))
				Marshal.FreeHGlobal ((IntPtr)Untag (ptr));
			ptr = null;
		}

		public override int GetHashCode () => (int)ptr;
		public override bool Equals (object? obj) => Equals ((TaggedPointer)obj!);

		public bool Equals (TaggedPointer other)
			=> Pointer == other!.Pointer;

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public static bool operator == (TaggedPointer tp, void* ptr)
			=> tp.Pointer == ptr;

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public static bool operator != (TaggedPointer tp, void* ptr)
			=> tp.Pointer != ptr;

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public static void* Tag (void* ptr) => (void*)((long)ptr | 1);

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public static void* Untag (void* ptr) => (void*)((long)ptr & ~1);

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public static bool IsTagged (void* ptr) => ((long)ptr & 1) == 1;
	}
}
