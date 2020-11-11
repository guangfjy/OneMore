﻿//************************************************************************************************
// Copyright © 2020 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn
{
	using River.OneMoreAddIn.Models;
	using River.OneMoreAddIn.Settings;
	using System.Linq;
	using System.Xml;
	using System.Xml.Linq;


	internal class HighlightCommand : Command
	{
		public HighlightCommand()
		{
		}


		public override void Execute(params object[] args)
		{

			System.Diagnostics.Debugger.Launch();


			using (var one = new OneNote(out var page, out var ns))
			{
				var updated = false;
				var index = 0;

				var meta = page.Root.Elements(ns + "Meta")
					.FirstOrDefault(e => e.Attribute("name").Value == "omHighlightIndex");

				if (meta != null)
				{
					if (int.TryParse(meta.Attribute("content").Value, out index))
					{
						index = index < 4 ? index + 1 : 0;
					}
				}

				var dark = page.GetPageColor(out _, out _).GetBrightness() < 0.5;
				var color = GetColor(index, dark);

				//updated = page.EditSelected((s) =>
				//{
				//	var o = s.StartsWith("<") && s.EndsWith(">")
				//		? (XNode)XElement.Parse(s)
				//		: new XText(s);

				//	return new XElement("span",
				//		new XAttribute("style", $"background:{color}"),
				//		o);
				//});

				var cursor = page.GetEmptyCursor();

				if (cursor != null)
				{
					// T elements can only be a child of an OE but can also have other T siblings...

					// is there a preceding T?
					if ((cursor.PreviousNode is XElement prev) && !prev.GetCData().EndsWithWhitespace())
					{
						prev.EditLastWord((s) =>
						{
							return new XElement("span",
								new XAttribute("style", $"background:{color}"),
								s);
						});

						updated = true;
					}

					// is there a following T?
					if ((cursor.NextNode is XElement next) && !next.GetCData().StartsWithWhitespace())
					{
						next.EditFirstWord((s) =>
						{
							return new XElement("span",
								new XAttribute("style", $"background:{color}"),
								s);
						});

						updated = true;
					}
				}
				else
				{
					// detect all selected text runs
					var runs = page.Root.Descendants(ns + "T")
						.Where(e => e.Attributes("selected").Any(a => a.Value == "all")
							&& e.FirstNode?.NodeType == XmlNodeType.CDATA)
						.Select(e => e.FirstNode as XCData);

					if (runs?.Any() == true)
					{
						foreach (var run in runs)
						{
							// blindly wrap; OneNote will normalize nested styles
							run.Value = $"<span style='background:{color}'>{run.Value}</span>";
						}

						updated = true;
					}
				}

				if (updated)
				{
					SetMeta(page, meta, index);
					one.Update(page);
				}
			}
		}


		private string GetColor(int index, bool dark)
		{
			if (dark)
			{
				switch (index)
				{
					case 1: return "#008000";   // Dark Green
					case 2: return "#00B0F0";   // Turquoise
					case 3: return "#800080";   // Dark Purple
					case 4: return "#0000FF";   // Blue
					default: return "#808000";  // Dark Yellow
				}
			}

			var theme = new SettingsProvider()
				.GetCollection("HighlightsSheet")?.Get<string>("theme");

			if (theme == null)
			{
				return "yellow";
			}

			if (theme == "Faded")
			{
				switch (index)
				{
					case 1: return "#CCFFCC";   // Light Green
					case 2: return "#CCFFFF";   // Sky Blue
					case 3: return "#FF99CC";   // Pink
					case 4: return "#99CCFF";   // Light Blue
					default: return "#FFFF99";  // Light Yellow
				}
			}

			if (theme == "Deep")
			{
				switch (index)
				{
					case 1: return "#92D050";   // Lime
					case 2: return "#33CCCC";   // Teal
					case 3: return "#CC99FF";   // Lavender
					case 4: return "#00B0F0";   // Turquoise
					default: return "#FFC000";  // Gold
				}
			}

			// theme "Normal"
			switch (index)
			{
				case 1: return "#00FF00";   // Light Green
				case 2: return "#00FFFF";   // Sky Blue
				case 3: return "#FF00CC";   // Pink
				case 4: return "#0000FF";   // Light Blue
				default: return "#FFFF00";  // Light Yellow
			}
		}


		private void SetMeta(Page page, XElement meta, int index)
		{
			if (meta == null)
			{
				var ns = page.Namespace;

				meta = new XElement(ns + "Meta",
					new XAttribute("name", "omHighlightIndex"),
					new XAttribute("content", index.ToString())
					);

				// add into schema sequence...
				var after = page.Root.Elements(ns + "QuickStyleDef").LastOrDefault();

				if (after == null)
				{
					after = page.Root.Elements(ns + "TagDef").LastOrDefault();
				}

				if (after == null)
				{
					page.Root.AddFirst(meta);
				}
				else
				{
					after.AddAfterSelf(meta);
				}
			}
			else
			{
				meta.Attribute("content").Value = index.ToString();
			}
		}
	}
}
