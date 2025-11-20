using Colossal.UI.Binding;

namespace Game.UI.InGame;

public readonly struct AgeData : IJsonWritable
{
	public int children { get; }

	public int teens { get; }

	public int adults { get; }

	public int elders { get; }

	public AgeData(int children, int teens, int adults, int elders)
	{
		this.children = children;
		this.teens = teens;
		this.adults = adults;
		this.elders = elders;
	}

	public static AgeData operator +(AgeData left, AgeData right)
	{
		return new AgeData(left.children + right.children, left.teens + right.teens, left.adults + right.adults, left.elders + right.elders);
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("values");
		writer.ArrayBegin(4u);
		writer.Write(children);
		writer.Write(teens);
		writer.Write(adults);
		writer.Write(elders);
		writer.ArrayEnd();
		writer.PropertyName("total");
		writer.Write(children + teens + adults + elders);
		writer.TypeEnd();
	}
}
