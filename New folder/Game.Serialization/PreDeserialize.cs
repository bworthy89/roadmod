using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Serialization;

public class PreDeserialize<T> : GameSystemBase where T : ComponentSystemBase, IPreDeserialize
{
	private LoadGameSystem m_LoadGameSystem;

	private T m_System;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_System = base.World.GetOrCreateSystemManaged<T>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_System.PreDeserialize(m_LoadGameSystem.context);
	}

	[Preserve]
	public PreDeserialize()
	{
	}
}
