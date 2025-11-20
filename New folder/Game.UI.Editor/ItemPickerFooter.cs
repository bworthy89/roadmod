using Colossal.UI.Binding;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class ItemPickerFooter : Widget, ISettable, IWidget, IJsonWritable
{
	public interface IAdapter
	{
		int length { get; }

		int columnCount { get; set; }
	}

	private int m_Length;

	private int m_ColumnCount;

	public IAdapter adapter { get; set; }

	public bool shouldTriggerValueChangedEvent => true;

	public void SetValue(IJsonReader reader)
	{
		reader.Read(out int value);
		adapter.columnCount = value;
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (adapter.length != m_Length)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Length = adapter.length;
		}
		if (adapter.columnCount != m_ColumnCount)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_ColumnCount = adapter.columnCount;
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("length");
		writer.Write(m_Length);
		writer.PropertyName("columnCount");
		writer.Write(m_ColumnCount);
	}
}
