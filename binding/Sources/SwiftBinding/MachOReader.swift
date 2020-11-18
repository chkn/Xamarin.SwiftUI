
import MachO
import Foundation

struct MachOReader {

	enum Header {
        case mh(header: mach_header)
        case mh64(header: mach_header_64)
        case fat(header: fat_header)
    }

	struct Slice {
		let offset: UInt64 // offset of start of header
		let reader: MachOReader

		private var symtab: symtab_command = symtab_command()

		init?(_ reader: MachOReader, at offset: UInt64) {
			self.reader = reader
			self.offset = offset
			guard initSymtab() else { return nil }
		}

		private mutating func initSymtab() -> Bool
		{
			// FIXME: Support 64-bit fat/32-bit? Don't care at the moment
			switch reader.readHeader(at: offset) {
				case .mh64(_):
					self.symtab = reader.findSymtab()
					return true
				default:
					return false
			}
		}

		func findSymbol(_ fn: (String) -> Bool) -> UInt64
		{
			reader.seek(to: offset + UInt64(symtab.symoff))

			for _ in 0..<symtab.nsyms {
				let sym: nlist_64 = reader.read()

				let stridx = sym.n_un.n_strx
				if stridx == 0 {
					continue
				}

				let prevOffset = reader.currentOffset
				reader.seek(to: offset + UInt64(symtab.stroff) + UInt64(stridx))

				if let name = reader.readCString(), fn(name) {
					print("\(name): Type: \(sym.n_type), value: \(sym.n_value), sect: \(sym.n_sect), desc: \(sym.n_desc)")
					return offset + UInt64(sym.n_value)
				}

				reader.seek(to: prevOffset)
			}
			return 0
		}
	}

	private let handle: FileHandle
	var currentOffset: UInt64 { handle.offsetInFile }

    private init?(_ path: URL)
    {
		guard let hndl = try? FileHandle(forReadingFrom: path) else { return nil }
        self.handle = hndl
    }

	static func slices(in file: URL) -> [Slice]?
	{
		guard let r = MachOReader(file) else { return nil }

		// Read the header to see if it's fat or not
		switch r.readHeader(at: 0) {
		case .fat(header: let fh):
			guard fh.magic == FAT_CIGAM else { return nil }
			var result: [Slice] = []
			let count = CFSwapInt32(fh.nfat_arch)

			for _ in 0..<count {
				let arch: fat_arch = r.read()
				let offs = UInt64(CFSwapInt32(arch.offset))

				if let slice = Slice(r, at: offs) {
					result.append(slice)
				}
			}
			return result
		default:
			// Just one slice
			return [Slice(r, at: 0)].compactMap({ $0 })
		}
	}

    func seek(to offset: UInt64)
    {
        handle.seek(toFileOffset: offset)
    }

    func read(length: Int) -> Data
    {
        handle.readData(ofLength: length)
    }

    func read<T>() -> T
    {
        read(length: MemoryLayout<T>.size).withUnsafeBytes({ $0.load(as: T.self) })
    }

	// FIXME: Is there a better way?
	private func readCString() -> String?
	{
		read(length: 1024).withUnsafeBytes({ String(utf8String: $0) })
	}

	private func readHeader(at offset: UInt64 = 0) -> Header?
	{
		// read magic
		seek(to: offset)
        let magic: UInt32 = read()

		// reset and read whole header
        seek(to: offset)
        switch magic {
        case MH_MAGIC:
            return .mh(header: read())
        case MH_MAGIC_64:
            return .mh64(header: read())
        default:
			let fatHeader: fat_header = read()
            return CFSwapInt32(fatHeader.magic) == FAT_MAGIC ? .fat(header: fatHeader) : nil
        }
	}

	// assumes file is seeked to load commands
	private func findSymtab() -> symtab_command
	{
		var offset = currentOffset
		var symtab: symtab_command = read()
		while symtab.cmd != LC_SYMTAB {
			offset += UInt64(symtab.cmdsize)
			seek(to: offset)
			symtab = read()
		}
		return symtab
	}
}
