using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Prefabs;

[ComponentMenu("Rendering/", new Type[] { typeof(RenderPrefab) })]
public class EmissiveProperties : ComponentBase
{
	public enum Purpose
	{
		None,
		DaytimeRunningLight,
		Headlight_HighBeam,
		Headlight_LowBeam,
		TurnSignalLeft,
		TurnSignalRight,
		RearLight,
		BrakeLight,
		ReverseLight,
		Clearance,
		DaytimeRunningLightLeft,
		DaytimeRunningLightRight,
		SignalGroup1,
		SignalGroup2,
		SignalGroup3,
		SignalGroup4,
		SignalGroup5,
		SignalGroup6,
		SignalGroup7,
		SignalGroup8,
		SignalGroup9,
		SignalGroup10,
		SignalGroup11,
		Interior1,
		DaytimeRunningLightAlt,
		TrafficLight_Red,
		TrafficLight_Yellow,
		TrafficLight_Green,
		PedestrianLight_Stop,
		PedestrianLight_Walk,
		RailCrossing_Stop,
		Dashboard,
		Clearance2,
		NeonSign,
		DecorativeLight,
		Emergency1,
		Emergency2,
		Emergency3,
		Emergency4,
		Emergency5,
		Emergency6,
		MarkerLights,
		CollectionLights,
		RearAlarmLights,
		FrontAlarmLightsLeft,
		FrontAlarmLightsRight,
		TaxiSign,
		Warning1,
		Warning2,
		WorkLights,
		Emergency7,
		Emergency8,
		Emergency9,
		Emergency10,
		BrakeAndTurnSignalLeft,
		BrakeAndTurnSignalRight,
		TaxiLights,
		LandingLights,
		WingInspectionLights,
		LogoLights,
		PositionLightLeft,
		PositionLightRight,
		PositionLights,
		AntiCollisionLightsRed,
		AntiCollisionLightsWhite,
		SearchLightsFront,
		SearchLights360,
		NumberLight,
		Interior2,
		BoardingLightLeft,
		BoardingLightRight,
		EffectSource,
		BuildingActive
	}

	[Serializable]
	public class MultiLightMapping : LightProperties
	{
		public int layerId = -1;
	}

	[Serializable]
	public class SingleLightMapping : LightProperties
	{
		public int materialId;
	}

	[Serializable]
	public class LightProperties
	{
		public Purpose purpose;

		public Color color = Color.white;

		public Color colorOff = Color.black;

		public float intensity;

		public float luminance;

		public float responseTime;

		public int animationIndex = -1;
	}

	[Serializable]
	public class AnimationProperties
	{
		public float m_Duration;

		public AnimationCurve m_Curve;
	}

	[Serializable]
	public class SignalGroupAnimation
	{
		public float m_Duration;

		public SignalGroupMask[] m_SignalGroupMasks;
	}

	public const float kIntensityMultiplier = 100f;

	public List<SingleLightMapping> m_SingleLights;

	[FormerlySerializedAs("m_LayersMapping")]
	public List<MultiLightMapping> m_MultiLights;

	public List<AnimationProperties> m_AnimationCurves;

	public List<SignalGroupAnimation> m_SignalGroupAnimations;

	public bool hasSingleLights
	{
		get
		{
			if (m_SingleLights != null)
			{
				return m_SingleLights.Count > 0;
			}
			return false;
		}
	}

	public bool hasMultiLights
	{
		get
		{
			if (m_MultiLights != null)
			{
				return m_MultiLights.Count > 0;
			}
			return false;
		}
	}

	public bool hasAnyLights
	{
		get
		{
			if (!hasSingleLights)
			{
				return hasMultiLights;
			}
			return true;
		}
	}

	public int lightsCount
	{
		get
		{
			int num = 0;
			if (hasSingleLights)
			{
				num += m_SingleLights.Count;
			}
			if (hasMultiLights)
			{
				num += m_MultiLights.Count;
			}
			return num;
		}
	}

	public int GetSingleLightOffset(int materialId)
	{
		int num = 1;
		if (hasMultiLights)
		{
			num += m_MultiLights.Count;
		}
		if (hasSingleLights)
		{
			for (int i = 0; i < m_SingleLights.Count; i++)
			{
				if (m_SingleLights[i].materialId == materialId)
				{
					return num + i;
				}
			}
		}
		return 0;
	}

	public bool IsSingleLightMaterialId(int materialId)
	{
		if (hasSingleLights)
		{
			foreach (SingleLightMapping singleLight in m_SingleLights)
			{
				if (singleLight.materialId == materialId)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ProceduralLight>());
		if ((m_AnimationCurves != null && m_AnimationCurves.Count != 0) || (m_SignalGroupAnimations != null && m_SignalGroupAnimations.Count != 0))
		{
			components.Add(ComponentType.ReadWrite<LightAnimation>());
		}
	}
}
