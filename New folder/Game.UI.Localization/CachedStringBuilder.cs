using System;
using System.Collections.Generic;

namespace Game.UI.Localization;

public class CachedStringBuilder<T>
{
	private readonly Func<T, string> m_Builder;

	private readonly Dictionary<T, string> m_Cache;

	public string this[T key]
	{
		get
		{
			if (m_Cache.TryGetValue(key, out var value))
			{
				return value;
			}
			return m_Cache[key] = m_Builder(key);
		}
	}

	public CachedStringBuilder(Func<T, string> builder)
	{
		m_Builder = builder;
		m_Cache = new Dictionary<T, string>();
	}
}
