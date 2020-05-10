using System;

namespace Swift
{
	/// <summary>
	/// An attribute that can be applied to an <see cref="Action"/> parameter to transform its block body
	///  into an expression.
	/// </summary>
	/// <remarks>
	/// Subclasses should define one or more of the following static methods:
	///  - public static TResult BuildBlock (...)
	///  - public static TResult BuildOptional (TArg? arg)
	///  - public static TResult&lt;TTrue,TFalse&gt; BuildEitherTrue (TTrue trueValue)
	///  - public static TResult&lt;TTrue,TFalse&gt; BuildEitherFalse (TFalse falseValue)
	/// <para/>
	/// Typically, an additional overload of the attributed method is provided to accept the tranformed
	///  expression. For instance, given a subclass <c>MyFunctionBuilder</c> defining the above methods:
	///  public TypeErasedResult Mthd ([MyFunctionBuilder] Action build) { ... }
	///  public TResult Mthd&lt;TResult&gt; (TResult value) { ... }
	/// <para/>
	///  The function builder transformation requires an external source generator. One known place where
	///   this is currently provided is in the <c>Body</c> property of a custom <see cref="SwiftUI.View"/>.
	/// </remarks>
	[AttributeUsage (AttributeTargets.Parameter, Inherited = true)]
	public abstract class FunctionBuilderAttribute : Attribute
	{
	}
}
