using System;
using System.IO;
using System.Linq;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SwiftUI.Analyzers.Tests
{
	public static class Subjects
	{
		public static readonly Compilation TestViews = GetSubjectCompilation ("TestViews.cs");
		public static readonly Compilation TestViewBuilders = GetSubjectCompilation ("TestViewBuilders.cs");

		public static Compilation GetSubjectCompilation (string fileName)
			=> CSharpCompilation.Create ("Program", options: new CSharpCompilationOptions (OutputKind.DynamicallyLinkedLibrary))
				.AddReferences (
					MetadataReference.CreateFromFile (typeof (object).Assembly.Location),
					MetadataReference.CreateFromFile (typeof (SwiftUI.View).Assembly.Location),
					MetadataReference.CreateFromFile (GetLoadedAssemblyPath ("netstandard")),
					MetadataReference.CreateFromFile (GetLoadedAssemblyPath ("System.Runtime")))
				.AddSyntaxTrees (GetSubjectSyntax (fileName));

		public static CompilationWithAnalyzers WithViewAnalyzer (this Compilation compilation)
			=> compilation.WithAnalyzers (ImmutableArray.Create<DiagnosticAnalyzer> (new ViewAnalyzer ()));

		static SyntaxTree GetSubjectSyntax (string fileName, [CallerFilePath] string path = null)
			=> CSharpSyntaxTree.ParseText (File.ReadAllText (Path.Combine (Path.GetDirectoryName (path), "Subjects", fileName)));

		static string GetLoadedAssemblyPath (string name)
			=> AppDomain.CurrentDomain.GetAssemblies ().Single (a => a.GetName ().Name == name).Location;
	}
}
