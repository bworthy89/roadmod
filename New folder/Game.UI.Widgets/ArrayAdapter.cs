#define UNITY_ASSERTIONS
using System;
using Unity.Assertions;

namespace Game.UI.Widgets;

public class ArrayAdapter : ListAdapterBase<Array>
{
	public override void InsertElement(int index)
	{
		Assert.IsTrue(index >= 0);
		Assert.IsTrue(index <= base.length);
		Array typedValue = base.accessor.GetTypedValue();
		object value = ListAdapterBase<Array>.CreateInstance(base.elementType);
		Array array = Array.CreateInstance(base.elementType, base.length + 1);
		array.SetValue(value, index);
		if (typedValue != null)
		{
			Array.Copy(typedValue, array, index);
			Array.Copy(typedValue, index, array, index + 1, typedValue.Length - index);
		}
		base.accessor.SetTypedValue(array);
	}

	public override void DeleteElement(int index)
	{
		Array typedValue = base.accessor.GetTypedValue();
		Array array = Array.CreateInstance(base.elementType, typedValue.Length - 1);
		Array.Copy(typedValue, 0, array, 0, index);
		Array.Copy(typedValue, index + 1, array, index, typedValue.Length - index - 1);
		base.accessor.SetTypedValue(array);
	}

	public override void Clear()
	{
		Array typedValue = Array.CreateInstance(base.elementType, 0);
		base.accessor.SetTypedValue(typedValue);
	}
}
