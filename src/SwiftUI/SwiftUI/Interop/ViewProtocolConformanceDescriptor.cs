using System;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI.Interop
{
	/// <summary>
	/// A non-retroactive conformance descriptor for the <c>SwiftUI.View</c> protocol.
	/// </summary>
	[StructLayout (LayoutKind.Sequential)]
	unsafe ref struct ViewProtocolConformanceDescriptor
	{
		// https://github.com/apple/swift/blob/ebcbaca9681816b9ebaa7ba31ef97729e707db93/include/swift/ABI/MetadataValues.h#L1089
		const int WitnessTableFirstRequirementOffset = 1;

		// 3 requirements that we will implement (Swift runtime will fill in the rest):
		// https://github.com/apple/swift/blob/a5d3ddaf6b57a6466909dde5e6b2f2185920ac81/stdlib/public/runtime/Metadata.cpp
		// From ^ .. "The requirement descriptor may be NULL, in which case this is a
		// requirement introduced in a later version of the protocol."
		static readonly IntPtr [] reqs = new [] {
			// associated conformance descriptor for SwiftUI.View.Body: SwiftUI.View
			SwiftUILib.Lib.TryGetSymbol ("$s7SwiftUI4ViewP4BodyAC_AaBTn"),

			// associated type descriptor for Body
			SwiftUILib.Lib.TryGetSymbol ("$s4Body7SwiftUI4ViewPTl"),

			// method descriptor for SwiftUI.View.body.getter : A.Body
			SwiftUILib.Lib.TryGetSymbol ("$s7SwiftUI4ViewP4body4BodyQzvgTq")
		};

		// Swift metadata makes copious use of RelativePointers: int32 offsets,
		//  which are efficient when all the metadata is in a single object file.
		// However, our metadata is dynamically generated and we can't guarantee
		//  our allocated memory will be close enough to the other metadata we need
		//  to reference. So, we allocate extra space for full-sized indirected pointers.
		// FIXME: can we improve this situation?
		RelativePointer conformanceDescriptorPtr; // < must be first
		void* protocolDescriptor;
		void* typeDescriptor;
		void* req0, req1, req2;

		public ProtocolConformanceDescriptor ConformanceDescriptor;

		ResilientWitnessesHeader WitnessesHeader;
		ResilientWitness
			AssocConformanceDesc, // reqs [0]
			AssocTypeDesc,        // reqs [1]
			BodyGetterDesc;       // reqs [2]

		GenericWitnessTable GenericWitnessTable;

		// Swift also wants us to allocate a cache of
		// GenericWitnessTable.NumGenericMetadataPrivateDataWords pointers (currently 16):
		IntPtr cache0, cache1, cache2, cache3, cache4, cache5, cache6, cache7;
		IntPtr cache8, cache9, cache10, cache11, cache12, cache13, cache14, cache15;

		void* ProtocolDescriptor {
			get => protocolDescriptor;
			set {
				protocolDescriptor = value;

				// FIXME: Shouldn't need fixed here; this is a ref struct
				//  (remove when https://github.com/dotnet/csharplang/issues/1792 is fixed)
				fixed (void** ptr = &protocolDescriptor)
					ConformanceDescriptor.ProtocolPtr.SetTarget (ptr, indirect: true);
			}
		}

		void* TypeDescriptor {
			get => typeDescriptor;
			set {
				typeDescriptor = value;

				// FIXME: Shouldn't need fixed here; this is a ref struct
				//  (remove when https://github.com/dotnet/csharplang/issues/1792 is fixed)
				fixed (void* ptr = &typeDescriptor)
					ConformanceDescriptor.TypeDescriptorPtr.Target = ptr;
			}
		}

		public void Populate (NominalTypeDescriptor* conformingType)
		{
			var flags = new ConformanceFlags (TypeReferenceKind.IndirectTypeDescriptor,
				hasResilientWitnesses: true, hasGenericWitnessTable: true);

			ProtocolDescriptor = SwiftUILib.ViewProtocol;
			TypeDescriptor = conformingType;
			ConformanceDescriptor.WitnessTablePatternPtr.Target = null;
			ConformanceDescriptor.Flags = flags;

			WitnessesHeader.NumWitnesses = 3;
			req0 = (void*)reqs [0];
			req1 = (void*)reqs [1];
			req2 = (void*)reqs [2];

			// FIXME: Shouldn't need fixed here; this is a ref struct
			//  (remove when https://github.com/dotnet/csharplang/issues/1792 is fixed)
			fixed (ViewProtocolConformanceDescriptor* dest = &this) {
				conformanceDescriptorPtr.Target = &dest->ConformanceDescriptor;

				// HACK: We can't guarantee that our witnesses will fall within
				//   the range of RelativePointer, so we provide dummy witnesses and then
				//   fix them up once Swift has instantiated the full WitnessTable
				//  (which uses full-size pointers, not RelativePointer)
				AssocConformanceDesc.Requirement.SetTarget (&dest->req0, indirect: true);
				AssocConformanceDesc.Witness.Target = &dest->req0;

				AssocTypeDesc.Requirement.SetTarget (&dest->req1, indirect: true);
				AssocTypeDesc.Witness.Target = &dest->req1;

				BodyGetterDesc.Requirement.SetTarget (&dest->req2, indirect: true);
				BodyGetterDesc.Witness.Target = &dest->req2;

				GenericWitnessTable.PrivateData.Target = &dest->cache0;
			}

			GenericWitnessTable.WitnessTableSizeInWords = 0;
			GenericWitnessTable.WitnessTablePrivateSizeInWordsAndRequiresInstantiation = 1;
			GenericWitnessTable.Instantiator.Target = null;
		}

		public void FixupAndRegister (
			ProtocolWitnessTable* witnessTable,
			ProtocolWitnessTable* assocConformance, // reqs [0]
			TypeMetadata* assocType, // reqs [1]
			IntPtr bodyGetter   // reqs [2], function pointer to PtrPtrFunc
		)
		{
			// See HACK note above. This method sets our witnesses to their proper values..

			var wt = (IntPtr)witnessTable;
			var loops = SwiftUILib.ViewProtocol->NumRequirements + WitnessTableFirstRequirementOffset;
			for (var i = WitnessTableFirstRequirementOffset; i < loops; i++) {
				var offs = i * IntPtr.Size;
				var req = (void*)Marshal.ReadIntPtr (wt, offs);

				// FIXME: Shouldn't need fixed here; this is a ref struct
				//  (remove when https://github.com/dotnet/csharplang/issues/1792 is fixed)
				fixed (ViewProtocolConformanceDescriptor* dest = &this) {
					if (req == &dest->req0)
						Marshal.WriteIntPtr (wt, offs, (IntPtr)assocConformance);
					else if (req == &dest->req1)
						Marshal.WriteIntPtr (wt, offs, (IntPtr)assocType);
					else if (req == &dest->req2)
						Marshal.WriteIntPtr (wt, offs, bodyGetter);
				}
			}

			// FIXME: Shouldn't need fixed here; this is a ref struct
			fixed (ViewProtocolConformanceDescriptor* dest = &this) {
				var ptr = &dest->conformanceDescriptorPtr;
				swift_registerProtocolConformances (ptr, ptr + 1);
			}
		}

		[DllImport (SwiftCoreLib.Path)]
		static extern void swift_registerProtocolConformances (RelativePointer* begin, RelativePointer* end);
	}
}
