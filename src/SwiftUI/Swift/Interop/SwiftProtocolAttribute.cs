using System;

namespace Swift.Interop
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Interface)]
	public class SwiftProtocolAttribute : Attribute
	{
		readonly NativeLib lib;
		readonly string mangledName;

		public unsafe ProtocolDescriptor* Descriptor => (ProtocolDescriptor*)lib.RequireSymbol (mangledName);

		public SwiftProtocolAttribute (string libraryPath, string module, string name)
			: this (libraryPath, SwiftType.Mangle (module, name))
		{
		}

		public SwiftProtocolAttribute (string libraryPath, string mangledName)
		{
			if (!mangledName.StartsWith ("$s", StringComparison.Ordinal))
				mangledName = "$s" + mangledName;
			if (!mangledName.EndsWith ("Mp", StringComparison.Ordinal))
				mangledName += "Mp";

			this.lib = NativeLib.Get (libraryPath);
			this.mangledName = mangledName;
		}
	}
}
