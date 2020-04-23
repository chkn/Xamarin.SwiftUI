using System;
namespace Swift.Interop
{
	public class UnknownSwiftTypeException : Exception
	{
		public UnknownSwiftTypeException (Type type)
			: base ($"Unknown Swift type for '{type}'. Try adding a SwiftImportAttribute.")
		{
		}
	}
}
