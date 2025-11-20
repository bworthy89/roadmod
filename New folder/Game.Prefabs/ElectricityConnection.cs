using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetPrefab) })]
public class ElectricityConnection : ComponentBase
{
	public enum Voltage : byte
	{
		Low = 0,
		High = 1,
		Invalid = byte.MaxValue
	}

	public Voltage m_Voltage;

	public FlowDirection m_Direction = FlowDirection.Both;

	public int m_Capacity;

	public NetPieceRequirements[] m_RequireAll;

	public NetPieceRequirements[] m_RequireAny;

	public NetPieceRequirements[] m_RequireNone;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ElectricityConnectionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
