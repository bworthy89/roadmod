namespace Game.UI.Widgets;

public class ValueField : ReadonlyField<string>
{
	public override string GetValue()
	{
		return base.GetValue() ?? string.Empty;
	}
}
