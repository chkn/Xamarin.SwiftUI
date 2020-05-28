using System;

using Swift;

namespace SwiftUI
{
	public class ViewBuilder : FunctionBuilderAttribute
	{
		public static EmptyView BuildBlock () => new EmptyView ();
		/*
		public static EmptyDummy BuildBlock () => new EmptyDummy ();
		public static T BuildBlock<T> (T content) => content;
		public static TupleDummy<T, U> BuildBlock<T, U> (T content1, U content2) => new TupleDummy<T, U> ();
		public static T BuildOptional<T> (T arg) => arg;
		public static EitherDummy<T, U> BuildEitherTrue<T, U> (T trueValue) => new EitherDummy<T, U> ();
		public static EitherDummy<T, U> BuildEitherFalse<T, U> (U falseValue) => new EitherDummy<T, U> ();
		*/
	}
}
