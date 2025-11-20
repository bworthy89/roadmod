using System;
using Colossal.UI.Binding;

namespace Game.UI.InGame;

public readonly struct CitizenCondition : IJsonWritable, IComparable<CitizenCondition>
{
	private static readonly string[] kConditionPaths = new string[7] { "Media/Game/Icons/ConditionSick.svg", "Media/Game/Icons/ConditionInjured.svg", "Media/Game/Icons/ConditionHomeless.svg", "Media/Game/Icons/ConditionMalcontent.svg", "Media/Game/Icons/ConditionWeak.svg", "Media/Game/Icons/ConditionInDistress.svg", "Media/Game/Icons/ConditionEvacuated.svg" };

	private CitizenConditionKey key { get; }

	public CitizenCondition(CitizenConditionKey key)
	{
		this.key = key;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(typeof(CitizenCondition).FullName);
		writer.PropertyName("key");
		writer.Write(Enum.GetName(typeof(CitizenConditionKey), key));
		writer.PropertyName("iconPath");
		writer.Write(kConditionPaths[(int)key]);
		writer.TypeEnd();
	}

	public int CompareTo(CitizenCondition other)
	{
		return key.CompareTo(other.key);
	}
}
