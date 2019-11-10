# Hopper script to set types/xrefs for some Swift metadata structures

doc = Document.getCurrentDocument()

TEXT = doc.getSegmentByName("__TEXT")

addrs = TEXT.getNamedAddresses()

def setInt32(addr, comment):
	TEXT.setTypeAtAddress(addr, 4, Segment.TYPE_INT32)
	TEXT.setInlineCommentAtAddress(addr, comment)

def setRelPtr(addr, comment):
	offs = TEXT.readUInt32LE(addr)
	if offs > 2147483647:
		offs -= 2**32
	TEXT.setTypeAtAddress(addr, 4, Segment.TYPE_INT32)
	TEXT.setInlineCommentAtAddress(addr, comment + " (offset " + str(offs) + ")")
	if offs != 0:
		TEXT.addReference(addr, addr + offs)
	return offs

def setRelStrPtr(addr, comment):
	offs = setRelPtr(addr, comment)
	str_addr = addr + offs
	i = str_addr
	b = TEXT.readByte(i)
	while b != 0:
		i += 1
		b = TEXT.readByte(i)
	TEXT.setTypeAtAddress(str_addr, i - str_addr, Segment.TYPE_ASCII)

def setContextDescriptor(addr):
	setInt32(addr, "Flags")
	setRelPtr(addr + 4, "ParentPtr")
	return addr + 8

for i in range(len(addrs)):
	addr = addrs[i]
	name = TEXT.getDemangledNameAtAddress(addr)

	if name.startswith("module descriptor "):
		addr = setContextDescriptor(addr)
		setRelStrPtr(addr, "NamePtr")

	elif name.startswith("nominal type descriptor for "):
		addr = setContextDescriptor(addr)
		setRelStrPtr(addr, "NamePtr")
		setRelPtr(addr + 4, "AccessFunctionPtr")
		setRelPtr(addr + 8, "FieldsPtr")
