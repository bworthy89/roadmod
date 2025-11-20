using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetLanePrefab) })]
public class SecondaryLane : ComponentBase
{
	public SecondaryLaneInfo[] m_LeftLanes;

	public SecondaryLaneInfo[] m_RightLanes;

	public SecondaryLaneInfo2[] m_CrossingLanes;

	public bool m_CanFlipSides;

	public bool m_DuplicateSides;

	public bool m_RequireParallel;

	public bool m_RequireOpposite;

	public bool m_SkipSafePedestrianOverlap;

	public bool m_SkipSafeCarOverlap;

	public bool m_SkipUnsafeCarOverlap;

	public bool m_SkipSideCarOverlap;

	public bool m_SkipTrackOverlap;

	public bool m_SkipMergeOverlap;

	public bool m_FitToParkingSpaces;

	public bool m_EvenSpacing;

	public bool m_InvertOverlapCuts;

	public float3 m_PositionOffset;

	public float2 m_LengthOffset;

	public float m_CutMargin;

	public float m_CutOffset;

	public float m_CutOverlap;

	public float m_Spacing;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_LeftLanes != null)
		{
			for (int i = 0; i < m_LeftLanes.Length; i++)
			{
				prefabs.Add(m_LeftLanes[i].m_Lane);
			}
		}
		if (m_RightLanes != null)
		{
			for (int j = 0; j < m_RightLanes.Length; j++)
			{
				prefabs.Add(m_RightLanes[j].m_Lane);
			}
		}
		if (m_CrossingLanes != null)
		{
			for (int k = 0; k < m_CrossingLanes.Length; k++)
			{
				prefabs.Add(m_CrossingLanes[k].m_Lane);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SecondaryLaneData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Net.SecondaryLane>());
	}
}
