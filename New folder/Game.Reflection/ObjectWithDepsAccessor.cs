using System;
using System.Reflection;
using Colossal.Annotations;
using Unity.Jobs;

namespace Game.Reflection;

public class ObjectWithDepsAccessor<T> : ObjectAccessor<T>
{
	[CanBeNull]
	private readonly FieldInfo[] m_Deps;

	public ObjectWithDepsAccessor([NotNull] T obj, [NotNull] FieldInfo[] deps)
		: base(obj, readOnly: true)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		m_Deps = deps ?? throw new ArgumentNullException("deps");
	}

	public override object GetValue()
	{
		if (m_Deps != null)
		{
			FieldInfo[] deps = m_Deps;
			for (int i = 0; i < deps.Length; i++)
			{
				((JobHandle)deps[i].GetValue(m_Object)).Complete();
			}
		}
		return base.GetValue();
	}
}
