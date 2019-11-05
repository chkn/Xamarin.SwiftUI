using System;

namespace Swift.Interop
{
	// We bind Swift values in a couple ways:
	//  - ISwiftValue<T> represents a managed struct of type T that is itself the native data.
	//  - ISwiftValue represents a wrapper type that has a handle (pointer) to the data.

	/// <summary>
	/// A Swift struct or class type.
	/// </summary>
	public interface ISwiftValue : IDisposable
	{
		// by convention:
		// public static SwiftType SwiftType { get; }
		SwiftType SwiftType { get; }
		MemoryHandle Handle { get; }
	}

	/// <summary>
	/// A Swift struct that is fully represented as a managed struct.
	/// </summary>
	/// <typeparam name="T">The managed struct type that implements this value.</typeparam>
	public interface ISwiftValue<T> : ISwiftValue
		where T : unmanaged, ISwiftValue<T>
	{
		// FIXME: Add default implementation for ISwiftValue.Handle once we get DIM
	}

	public static class SwiftValue
	{
		public static bool IsBlittable<T> (this ISwiftValue<T> val) where T : unmanaged, ISwiftValue<T>
			=> val.GetType () == typeof (T);
	}
}
