using Colossal.UI.Binding;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class SearchField : Widget, ISettable, IWidget, IJsonWritable
{
	public interface IAdapter
	{
		string searchQuery { get; set; }
	}

	private string m_Value;

	public IAdapter adapter { get; set; }

	public bool shouldTriggerValueChangedEvent => true;

	public void SetValue(IJsonReader reader)
	{
		reader.Read(out string value);
		SetValue(value);
	}

	public void SetValue(string value)
	{
		if (value != m_Value)
		{
			adapter.searchQuery = value;
		}
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (adapter.searchQuery != m_Value)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Value = adapter.searchQuery;
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("value");
		writer.Write(m_Value ?? string.Empty);
	}
}
