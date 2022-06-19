using System;
using System.Linq;
using System.Threading.Tasks;


using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Xunit;

using SwiftUI.Analyzers;

namespace SwiftUI.Analyzers.Tests
{
	public class AnalyzerTests
	{
		[Theory]
		[InlineData ("TestViews.cs")]
		public void HasNoDiagnostics (string fileName)
		{
			Assert.Empty (Subjects.GetSubjectCompilation (fileName).GetDiagnostics ());
		}

		[Fact]
		public async Task HasDiagnosticsWithAnalyzer ()
		{
			Assert.NotEmpty (await Subjects.TestViews.WithViewAnalyzer ().GetAllDiagnosticsAsync ());
		}

		[Theory]
		[InlineData (nameof (CustomViewWithoutBody))]
		[InlineData (nameof (CustomViewWithViewBody))]
		public void IsCustomView (string typeName)
		{
			Assert.True (Subjects.TestViews.GetTypeByMetadataName (typeName).IsCustomView (), typeName);
		}

		[Theory]
		[InlineData (nameof (NotCustomViewBaseClass))]
		[InlineData (nameof (NotCustomViewAttribute))]
		[InlineData (nameof (NotCustomViewBaseClassDerived))]
		public void IsNotCustomView (string typeName)
		{
			Assert.False (Subjects.TestViews.GetTypeByMetadataName (typeName).IsCustomView (), typeName);
		}

		[Theory]
		[InlineData (nameof (CustomViewWithViewBody))]
		public void HasBody (string typeName)
		{
			Assert.NotNull (Subjects.TestViews.GetTypeByMetadataName (typeName).GetBodyProperty ());
		}

		[Theory]
		[InlineData (nameof (CustomViewWithoutBody))]
		public void DoesNotHaveBody (string typeName)
		{
			Assert.Null (Subjects.TestViews.GetTypeByMetadataName (typeName).GetBodyProperty ());
		}
	}
}
