using System;
using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.Reflection;

namespace Game.UI.Widgets;

public class DropdownField<T> : Field<T>, IWarning
{
	private int m_ItemsVersion = -1;

	private bool m_Warning;

	public DropdownItem<T>[] items { get; set; } = Array.Empty<DropdownItem<T>>();

	[CanBeNull]
	public Func<int> itemsVersion { get; set; }

	[CanBeNull]
	public Func<bool> warningAction { get; set; }

	public ITypedValueAccessor<DropdownItem<T>[]> itemsAccessor { get; set; }

	public new IWriter<T> valueWriter
	{
		protected get
		{
			return base.valueWriter;
		}
		set
		{
			base.valueWriter = value;
		}
	}

	public new IReader<T> valueReader
	{
		protected get
		{
			return base.valueReader;
		}
		set
		{
			base.valueReader = value;
		}
	}

	public bool warning
	{
		get
		{
			return m_Warning;
		}
		set
		{
			warningAction = null;
			m_Warning = value;
		}
	}

	public override string propertiesTypeName => "Game.UI.Widgets.DropdownField";

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (itemsAccessor != null)
		{
			int num = itemsVersion?.Invoke() ?? 0;
			if (num != m_ItemsVersion)
			{
				widgetChanges |= WidgetChanges.Properties;
				m_ItemsVersion = num;
				items = itemsAccessor.GetTypedValue() ?? Array.Empty<DropdownItem<T>>();
			}
		}
		if (warningAction != null)
		{
			bool flag = warningAction();
			if (flag != m_Warning)
			{
				m_Warning = flag;
				widgetChanges |= WidgetChanges.Properties;
			}
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("items");
		writer.ArrayBegin(items.Length);
		DropdownItem<T>[] array = items;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Write(valueWriter, writer);
		}
		writer.ArrayEnd();
		writer.PropertyName("warning");
		writer.Write(warning);
	}
}
