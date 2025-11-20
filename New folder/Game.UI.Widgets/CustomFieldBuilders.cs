using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Widgets;

public class CustomFieldBuilders : IFieldBuilderFactory
{
	public static readonly Dictionary<Type, IFieldBuilderFactory> kFactoryCache = new Dictionary<Type, IFieldBuilderFactory>();

	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		Type customFieldFactory = WidgetAttributeUtils.GetCustomFieldFactory(attributes, IsContainerType(memberType));
		if (customFieldFactory != null)
		{
			if (!kFactoryCache.TryGetValue(customFieldFactory, out var value))
			{
				if (typeof(IFieldBuilderFactory).IsAssignableFrom(customFieldFactory))
				{
					try
					{
						value = (IFieldBuilderFactory)Activator.CreateInstance(customFieldFactory);
					}
					catch (Exception exception)
					{
						UnityEngine.Debug.LogException(exception);
						value = null;
					}
					kFactoryCache[customFieldFactory] = value;
				}
				else
				{
					UnityEngine.Debug.LogError($"{customFieldFactory} is not assignable to IFieldBuilderFactory");
				}
			}
			if (value != null)
			{
				return value.TryCreate(memberType, attributes);
			}
		}
		return null;
	}

	private static bool IsContainerType(Type type)
	{
		if (typeof(IEnumerable).IsAssignableFrom(type))
		{
			return type != typeof(string);
		}
		return false;
	}
}
