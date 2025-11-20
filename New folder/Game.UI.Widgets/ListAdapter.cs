#define UNITY_ASSERTIONS
using System;
using System.Collections;
using System.Collections.Generic;
using Colossal.Annotations;
using Game.Reflection;
using Unity.Assertions;

namespace Game.UI.Widgets;

public class ListAdapter : ListAdapterBase<IList>
{
	public Type listType { get; set; }

	public override void InsertElement(int index)
	{
		Assert.IsTrue(index >= 0);
		Assert.IsTrue(index <= base.length);
		IList list = base.accessor.GetTypedValue();
		if (list == null)
		{
			list = (IList)ListAdapterBase<IList>.CreateInstance(listType);
			base.accessor.SetTypedValue(list);
		}
		object value = ListAdapterBase<IList>.CreateInstance(base.elementType);
		list.Insert(index, value);
	}

	public override void DeleteElement(int index)
	{
		base.accessor.GetTypedValue().RemoveAt(index);
	}

	public override void Clear()
	{
		base.accessor.GetTypedValue()?.Clear();
	}

	public static ListAdapter FromList<T>([NotNull] List<T> list, IEditorGenerator generator)
	{
		return new ListAdapter
		{
			accessor = new DelegateAccessor<IList>(() => list),
			generator = generator,
			elementType = typeof(T),
			listType = typeof(List<T>),
			level = 0
		};
	}
}
