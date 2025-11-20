using System;
using Colossal.UI.Binding;

namespace Game.UI.InGame;

public readonly struct CitizenHappiness : IJsonWritable
{
	private static readonly string[] kHappinessPaths = new string[5] { "Media/Game/Icons/Depressed.svg", "Media/Game/Icons/Sad.svg", "Media/Game/Icons/Neutral.svg", "Media/Game/Icons/Content.svg", "Media/Game/Icons/Happy.svg" };

	private CitizenHappinessKey key { get; }

	public CitizenHappiness(CitizenHappinessKey key)
	{
		this.key = key;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(typeof(CitizenHappiness).FullName);
		writer.PropertyName("key");
		writer.Write(Enum.GetName(typeof(CitizenHappinessKey), key));
		writer.PropertyName("iconPath");
		writer.Write(kHappinessPaths[(int)key]);
		writer.TypeEnd();
	}
}
