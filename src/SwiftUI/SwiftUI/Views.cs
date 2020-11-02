using System;

namespace SwiftUI
{
	/// <summary>
	/// Syntactic sugar for creating views.
	/// </summary>
	/// <remarks>
	/// This class can be used with <c>using static</c> in C# or <c>open type</c> in F#.
	/// </remarks>
	public static class Views
	{
		#region Button
		public static Button<Text> Button (string label, Action action)
			=> new Button<Text> (action, new Text (label));

		public static Button<TLabel> Button<TLabel> (Action action, TLabel label) where TLabel : View
			=> new Button<TLabel> (action, label);

		public static View Button (Action action, [ViewBuilder] Action label)
			=> throw new NotImplementedException (Msg.SourceGeneratorReqd);
		#endregion

		public static Text Text (string verbatim) => new Text (verbatim);
	}
}
