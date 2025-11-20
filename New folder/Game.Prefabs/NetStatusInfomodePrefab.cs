using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Tools/Infomode/", new Type[] { })]
public class NetStatusInfomodePrefab : GradientInfomodeBasePrefab
{
	public NetStatusType m_Type;

	public Bounds1 m_Range;

	public float m_FlowSpeed;

	public float m_FlowTiling;

	public float m_MinFlow;

	public override string infomodeTypeLocaleKey => "NetworkColor";

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<InfoviewNetStatusData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		InfoviewNetStatusData componentData = new InfoviewNetStatusData
		{
			m_Type = m_Type,
			m_Range = m_Range
		};
		if (m_FlowTiling != 0f)
		{
			componentData.m_Tiling = 1f / m_FlowTiling;
		}
		entityManager.SetComponentData(entity, componentData);
	}

	public override bool CanActivateBoth(InfomodePrefab other)
	{
		if (other is NetStatusInfomodePrefab netStatusInfomodePrefab && VisibleOnRoadSurface() && netStatusInfomodePrefab.VisibleOnRoadSurface())
		{
			return false;
		}
		return base.CanActivateBoth(other);
	}

	public override void GetColors(out Color color0, out Color color1, out Color color2, out float steps, out float speed, out float tiling, out float fill)
	{
		base.GetColors(out color0, out color1, out color2, out steps, out speed, out tiling, out fill);
		speed = m_FlowSpeed;
		if (m_FlowTiling != 0f)
		{
			tiling = 1f / m_FlowTiling;
		}
		if (m_MinFlow != 0f)
		{
			fill = 1f / m_MinFlow;
		}
	}

	private bool VisibleOnRoadSurface()
	{
		NetStatusType type = m_Type;
		if ((uint)type <= 4u || (uint)(type - 11) <= 1u)
		{
			return true;
		}
		return false;
	}
}
