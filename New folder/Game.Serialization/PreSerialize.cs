using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Serialization;

public class PreSerialize<T> : GameSystemBase where T : ComponentSystemBase, IPreSerialize
{
	private SaveGameSystem m_SaveGameSystem;

	private T m_System;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SaveGameSystem = base.World.GetOrCreateSystemManaged<SaveGameSystem>();
		m_System = base.World.GetOrCreateSystemManaged<T>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_System.PreSerialize(m_SaveGameSystem.context);
	}

	[Preserve]
	public PreSerialize()
	{
	}
}
