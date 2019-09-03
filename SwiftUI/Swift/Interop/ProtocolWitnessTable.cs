using System;

namespace Swift.Interop
{
	public unsafe struct ProtocolWitnessTable
	{
		public ProtocolConformanceDescriptor* ProtocolDescriptor;
		// .. vtable follows ..
	}
}
