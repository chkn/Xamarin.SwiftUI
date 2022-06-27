import Foundation

public class PrimaryCtorBinding: MethodBinding {
	public override func write(_ writer: Writer)
	{
		writeParameterList(writer)
	}
}

