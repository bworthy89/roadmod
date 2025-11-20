using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public abstract class Field<T> : ReadonlyField<T>, ISettable, IWidget, IJsonWritable
{
	private IReader<T> m_ValueReader;

	protected IReader<T> valueReader
	{
		get
		{
			return m_ValueReader ?? (m_ValueReader = ValueReaders.Create<T>());
		}
		set
		{
			m_ValueReader = value;
		}
	}

	public bool shouldTriggerValueChangedEvent => true;

	public void SetValue(IJsonReader reader)
	{
		valueReader.Read(reader, out var value);
		SetValue(value);
	}

	public virtual void SetValue(T value)
	{
		base.accessor.SetTypedValue(value);
	}
}
