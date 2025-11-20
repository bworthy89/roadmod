using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.UI.Localization;

namespace Game.Prefabs.Modes;

public class GameModeInfo : IJsonWritable
{
	public string id { get; set; }

	public string image { get; set; }

	public string decorateImage { get; set; }

	public LocalizedString[] descriptions { get; set; }

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(typeof(GameModeInfo).FullName);
		writer.PropertyName("id");
		writer.Write(id);
		writer.PropertyName("image");
		writer.Write(image);
		writer.PropertyName("decorateImage");
		writer.Write(decorateImage);
		writer.PropertyName("descriptions");
		writer.Write((IList<LocalizedString>)descriptions);
		writer.TypeEnd();
	}
}
