using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public struct FlexLayout : IJsonWritable
{
	public static FlexLayout Default => new FlexLayout(0f, 1f, -1);

	public static FlexLayout Fill => new FlexLayout(1f, 0f, 0);

	public float grow { get; set; }

	public float shrink { get; set; }

	public int basis { get; set; }

	public FlexLayout(float grow, float shrink, int basis)
	{
		this.grow = grow;
		this.shrink = shrink;
		this.basis = basis;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("grow");
		writer.Write(grow);
		writer.PropertyName("shrink");
		writer.Write(shrink);
		writer.PropertyName("basis");
		writer.Write(basis);
		writer.TypeEnd();
	}
}
