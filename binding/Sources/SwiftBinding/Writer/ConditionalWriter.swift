// Matches Bindings by ID between platforms and conditionalizes any differences in output

import Foundation

public struct ConditionalWriter {
	let allSdks: Set<SDK>

	enum WriterAction: Hashable {
		case writeText(String)
		case writeChild(Binding)
		indirect case conditional(Set<SDK>, WriterAction)

		func hash(into hasher: inout Hasher)
		{
			switch self {
			case .writeText(let str): hasher.combine(str)
			case .writeChild(let binding): hasher.combine(binding.id)
			case .conditional(_, let action):
				// purposefully ignore the SDKs
				hasher.combine(action)
			}
		}

		static func == (lhs: ConditionalWriter.WriterAction, rhs: ConditionalWriter.WriterAction) -> Bool
		{
			if case .writeText(let text1) = lhs, case .writeText(let text2) = rhs {
				return text1 == text2
			}
			if case .writeChild(let child1) = lhs, case .writeChild(let child2) = rhs {
				return child1.id == child2.id
			}
			if case .conditional(_, let action1) = lhs, case .conditional(_, let action2) = rhs {
				// purposefully ignore the SDKs
				return action1 == action2
			}
			return false
		}
	}

	class SdkWriter: Writer {
		var actions: [WriterAction] = []

		func write(_ text: String)
		{
			actions.append(.writeText(text))
		}

		public func write(child: Binding)
		{
			actions.append(.writeChild(child))
		}
	}

	public init(_ allSdks: Set<SDK>) {
		self.allSdks = allSdks
	}

	static func actions(for binding: Binding) -> [WriterAction]
	{
		let writer = SdkWriter()
		binding.write(writer)
		return writer.actions
	}

	public func write(bindings: [(SDK,Binding)], to writer: Writer)
	{
		//convert this to lists of conditional actions by SDK
		let actions: [[WriterAction]] = bindings.map {[ .conditional([$0.0], .writeChild($0.1)) ]}
		write(actions: actions, to: writer)
	}

	func write(actions: [[WriterAction]], to writer: Writer)
	{
		// compare actions for all SDKs and merge into one list of WriterActions
		if let mergedActions = actions.reduce(nil, merge) {
			let coalescedActions = ConditionalWriter.coalesce(mergedActions)
			for action in coalescedActions {
				write(action: action, to: writer)
			}
		}
	}

	func merge(_ initial: [WriterAction]?, with unexpandedActions2: [WriterAction]) -> [WriterAction]?
	{
		guard let unexpandedActions1 = initial else { return unexpandedActions2 }

		// first, expand all the conditional writeChild values
		let actions1 = ConditionalWriter.expand(unexpandedActions1)
		let actions2 = ConditionalWriter.expand(unexpandedActions2)

		let diff = actions2.difference(from: actions1).inferringMoves()

		// first, align the collections so we can merge them pairwise
		var combined = actions1
		for change in diff {
			switch change {

			case .insert(offset: let index, element: let action, associatedWith: _):
				guard case .conditional(_, _) = action else {
					assertionFailure("SDK differences must be conditional")
					return nil
				}
				combined.insert(action, at: index)
			case .remove(offset: let index, element: let action, associatedWith: let moved):
				guard case .conditional(_, _) = action else {
					assertionFailure("SDK differences must be conditional")
					return nil
				}
				if let _ = moved {
					combined.remove(at: index)
				}
			}
		}

		for action2 in actions2 {
			let i = combined.firstIndex(where: { $0 == action2 })!
			let action1 = combined[i]
			if case .conditional(let sdks1, let action) = action1, case .conditional(let sdks2, _) = action2 {
				let sdks = sdks1.union(sdks2)
				if sdks == allSdks {
					combined[i] = action
				} else {
					combined[i] = .conditional(sdks, action)
				}
			}
		}

		return combined
	}

	static func expand(_ acts: [WriterAction]) -> [WriterAction]
	{
		var result: [WriterAction] = []
		for action in acts {
			switch action {
			case .conditional(let sdks, .writeChild(let binding)):
				let expandedActions: [WriterAction] = actions(for: binding).map { .conditional(sdks, $0) }
				result.append(contentsOf: expandedActions)
			default:
				result.append(action)
			}
		}
		return result
	}

	static func coalesce(_ acts: [WriterAction]) -> [WriterAction]
	{
		var result: [WriterAction] = []
		for action in acts {
			switch action {
			case .conditional(let sdks1, .writeText(let str1)):
				if case .conditional(let sdks2, .writeText(let str2)) = result.last, sdks1 == sdks2 {
					result.removeLast(1)
					result.append(.conditional(sdks1, .writeText(str2 + str1)))
				} else {
					result.append(action)
				}
			default:
				result.append(action)
			}
		}
		return result
	}

	func write(action: WriterAction, to writer: Writer)
	{
		switch action {
		case .writeText(let text): writer.write(text)
		case .writeChild(let child): writer.write(child: child)
		case .conditional(let sdks, let conditionalAction):
			let defines = sdks.map({ $0.conditionalDefine }).sorted().joined(separator: " || ")
			writer.write("#if \(defines)\n")
			write(action: conditionalAction, to: writer)
			writer.write("#endif\n")
		}
	}
}
