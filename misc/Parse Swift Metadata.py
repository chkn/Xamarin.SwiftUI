# Hopper script to set types/xrefs for some Swift metadata structures

doc = Document.getCurrentDocument()

def processSeg(seg):
	addrs = seg.getNamedAddresses()

	def setInt16(addr, comment):
		seg.setTypeAtAddress(addr, 2, Segment.TYPE_INT16)
		seg.setInlineCommentAtAddress(addr, comment)

	def setInt32(addr, comment):
		seg.setTypeAtAddress(addr, 4, Segment.TYPE_INT32)
		seg.setInlineCommentAtAddress(addr, comment)

	def setInt64(addr, comment):
		seg.setTypeAtAddress(addr, 8, Segment.TYPE_INT64)
		seg.setInlineCommentAtAddress(addr, comment)

	def setPtr(addr, comment = None):
		seg.setTypeAtAddress(addr, 8, Segment.TYPE_INT64)
		if comment:
			seg.setInlineCommentAtAddress(addr, comment)
		ptr = seg.readUInt64LE(addr)
		if ptr != 0:
			seg.addReference(addr, ptr)
			doc.setOperandFormat(addr, 0, Document.FORMAT_ADDRESS)

	def setRelPtr(addr, comment):
		offs = seg.readUInt32LE(addr)
		if offs > 2147483647:
			offs -= 2**32
		seg.setTypeAtAddress(addr, 4, Segment.TYPE_INT32)
		seg.setInlineCommentAtAddress(addr, comment + " (offset " + str(offs) + ")")
		if offs != 0:
			seg.addReference(addr, addr + offs)
			doc.setOperandFormat(addr, 0, Document.FORMAT_SIGNED)
		return offs

	def setRelIndPtr(addr, comment):
		offsPlusInd = seg.readUInt32LE(addr)
		offs = offsPlusInd & ~1
		if offs > 2147483647:
			offs -= 2**32
		seg.setTypeAtAddress(addr, 4, Segment.TYPE_INT32)
		seg.setInlineCommentAtAddress(addr, comment + " (offset " + str(offs) + ")")
		if offs != 0:
			seg.addReference(addr, addr + offs)
			doc.setOperandFormat(addr, 0, Document.FORMAT_SIGNED)
			if offsPlusInd & 1 == 1: # indirect
				setPtr(addr + offs)
		return offs

	def setRelStrPtr(addr, comment):
		offs = setRelPtr(addr, comment)
		str_addr = addr + offs
		i = str_addr
		b = seg.readByte(i)
		while b != 0:
			i += 1
			b = seg.readByte(i)
		seg.setTypeAtAddress(str_addr, i - str_addr, Segment.TYPE_ASCII)

	def setContextDescriptor(addr):
		setInt32(addr, "Flags")
		setRelPtr(addr + 4, "ParentPtr")
		return addr + 8

	for i in range(len(addrs)):
		addr = addrs[i]
		name = seg.getDemangledNameAtAddress(addr)

		if not name:
			continue

		if name.startswith("module descriptor "):
			addr = setContextDescriptor(addr)
			setRelStrPtr(addr, "NamePtr")

		elif name.startswith("nominal type descriptor for "):
			flags = seg.readUInt32LE(addr)
			addr = setContextDescriptor(addr)
			setRelStrPtr(addr, "NamePtr")
			setRelPtr(addr + 4, "AccessFunctionPtr")
			setRelPtr(addr + 8, "FieldsPtr")
			if flags & 17 == 17: # struct
				setInt32(addr + 12, "NumberOfFields")
				setInt32(addr + 16, "FieldOffsetVectorOffset")
				

		elif name.startswith("value witness table for "):
			setPtr(addr, "InitBufferWithCopy")
			setPtr(addr + 8, "Destroy")
			setPtr(addr + 16, "InitWithCopy")
			setPtr(addr + 24, "AssignWithCopy")
			setPtr(addr + 32, "InitWithTake")
			setPtr(addr + 40, "AssignWithTake")
			setPtr(addr + 48, "GetEnumTagSinglePayload")
			setPtr(addr + 56, "StoreEnumTagSinglePayload")
			setInt64(addr + 64, "Size")
			setInt64(addr + 72, "Stride")
			setInt32(addr + 80, "Flags")

		elif name.startswith("full type metadata for "):
			setPtr(addr)

		elif name.startswith("type metadata for "):
			setInt64(addr, "MetadataKind")
			setPtr(addr + 8)

		elif name.startswith("reflection metadata field descriptor "):
			setRelPtr(addr, "MangledTypeName")
			setRelPtr(addr + 4, "Superclass")
			setInt16(addr + 8, "Kind")
			setInt16(addr + 10, "FieldRecordSize")
			addr += 12
			setInt32(addr, "NumFields")
			numFields = seg.readUInt32LE(addr)
			addr += 4
			for i in range(numFields):
				seg.setCommentAtAddress(addr, "Field " + str(i))
				setInt32(addr, "Flags")
				setRelPtr(addr + 4, "MangledTypeName")
				setRelPtr(addr + 8, "FieldName")
				addr += 12

		elif name.startswith("protocol conformance descriptor for "):
			setRelIndPtr(addr, "ProtocolPtr")
			setRelPtr(addr + 4, "TypeDescriptorPtr")
			setRelPtr(addr + 8, "WitnessTablePatternPtr")
			setInt32(addr + 12, "Flags")
			flags = seg.readUInt32LE(addr + 12)
			addr += 16
			if flags & 64 == 64: # retroactive
				setRelIndPtr(addr, "RetroactiveContextPointer")
				addr += 4
			#FIXME: NumConditionalRequirements
			if flags & 65536 == 65536: #has resilient witnesses
				setInt32(addr, "NumWitnesses")
				witnesses = seg.readUInt32LE(addr)
				addr += 4
				for i in range(witnesses):
					setRelIndPtr(addr, "Requirement")
					setRelPtr(addr + 4, "Witness")
					addr += 8
			if flags & 131072 == 131072: #has generic witness
				setInt16(addr, "WitnessTableSizeInWords")
				setInt16(addr + 2, "WitnessTablePrivateSizeInWordsAndRequiresInstantiation")
				setRelPtr(addr + 4, "Instantiator")
				setRelPtr(addr + 8, "PrivateData")

		elif name.startswith("protocol descriptor for "):
			addr = setContextDescriptor(addr)
			setRelStrPtr(addr, "NamePtr")
			setInt32(addr + 4, "NumRequirementsInSignature")
			setInt32(addr + 8, "NumRequirements")
			setRelStrPtr(addr + 12, "AssociatedTypeNamesPtr")

processSeg(doc.getSegmentByName("__TEXT"))
processSeg(doc.getSegmentByName("__DATA_CONST"))
