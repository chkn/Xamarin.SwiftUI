using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using Xunit;

namespace SwiftUI.Analyzers.Tests
{
	public class SourceGeneratorTests
	{
		readonly FunctionBuilder viewBuilder = new FunctionBuilder (ParseTypeName ("B"));

		static BlockSyntax GetBlock (string typeName)
		{
			var compilation = Subjects.TestViewBuilders;
			var mthd = compilation.GetTypeByMetadataName (typeName)
			                      .GetMembers ().Single (m => !m.IsImplicitlyDeclared)
			                      .DeclaringSyntaxReferences.Single ();
			var syntax = (MethodDeclarationSyntax)mthd.GetSyntax ();
			return syntax.Body;
		}

		void AssertCode (string typeName, string expected)
			=> Assert.Equal (expected, viewBuilder.Visit (GetBlock (typeName)).ToString ());

		[Fact]
		public void TestEmptyBlock ()
			=> AssertCode (nameof (EmptyBlock), "B.BuildBlock()");

		[Fact]
		public void TestZeroBlock ()
			=> AssertCode (nameof (ZeroBlock), "B.BuildBlock(M.Zero ())");

		[Fact]
		public void TestZeroZeroBlock ()
			=> AssertCode (nameof (ZeroZeroBlock), "B.BuildBlock(M.Zero (),M.Zero ())");

		[Fact]
		public void TestOneZeroBlock ()
			=> AssertCode (nameof (OneZeroBlock), "B.BuildBlock(M.One (M.Zero ()))");

		[Fact]
		public void TestOptionalZeroBlock ()
			=> AssertCode (nameof (OptionalZeroBlock), "B.BuildBlock(B.BuildOptional(true?M.Zero ():null))");

		[Fact]
		public void TestOptionalZeroZeroBlock ()
			=> AssertCode (nameof (OptionalZeroZeroBlock), "B.BuildBlock(B.BuildOptional(M.Cond?B.BuildBlock(M.Zero (),M.Zero ()):null))");

		[Fact]
		public void TestEitherZeroZeroBlock ()
			=> AssertCode (nameof (EitherZeroZeroBlock), "B.BuildBlock(M.Cond?B.BuildEitherTrue(M.Zero ()):B.BuildEitherFalse(M.Zero ()))");
	}
}
