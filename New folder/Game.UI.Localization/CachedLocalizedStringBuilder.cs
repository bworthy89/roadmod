using System;
using System.Collections.Generic;

namespace Game.UI.Localization;

public class CachedLocalizedStringBuilder<T>
{
	private readonly Func<T, LocalizedString> m_Builder;

	private readonly Dictionary<T, LocalizedString> m_Cache;

	public LocalizedString this[T key]
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

	public CachedLocalizedStringBuilder(Func<T, LocalizedString> builder)
	{
		m_Builder = builder;
		m_Cache = new Dictionary<T, LocalizedString>();
	}

	public static CachedLocalizedStringBuilder<T> Value(Func<T, string> builder)
	{
		return new CachedLocalizedStringBuilder<T>((T key) => LocalizedString.Value(builder(key)));
	}

	public static CachedLocalizedStringBuilder<T> Id(Func<T, string> builder)
	{
		return new CachedLocalizedStringBuilder<T>((T key) => LocalizedString.Id(builder(key)));
	}
}
