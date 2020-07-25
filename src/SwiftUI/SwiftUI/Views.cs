using System;

namespace SwiftUI
{
	/// <summary>
	/// Syntactic sugar for creating views in C#.
	/// </summary>
	/// <remarks>
	/// This class can be used with <c>using static</c>.
	/// </remarks>
	public static partial class Views
	{
		const string SourceGeneratorMessage = "Source generator required";

		#region Button
		public static Button<Text> Button (string label, Action action)
			=> new Button<Text> (action, new Text (label));

		public static Button<TLabel> Button<TLabel> (Action action, TLabel label) where TLabel : View
			=> new Button<TLabel> (action, label);

		public static View Button (Action action, [ViewBuilder] Action label)
			=> throw new NotImplementedException (SourceGeneratorMessage);
		#endregion

		public static Text Text (string verbatim) => new Text (verbatim);
	}
}
