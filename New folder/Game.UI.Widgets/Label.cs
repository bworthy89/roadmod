using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class Label : NamedWidgetWithTooltip
{
	public enum Level
	{
		Title,
		SubTitle,
		GroupTitle
	}

	public Level level;

	public bool beta;

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("level");
		writer.Write((int)level);
		writer.PropertyName("beta");
		writer.Write(beta);
	}
}
