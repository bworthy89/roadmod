using System;

namespace Game.Reflection;

public interface IValueAccessor
{
	Type valueType { get; }

	IValueAccessor parent { get; }

	object GetValue();

	void SetValue(object value);
}
