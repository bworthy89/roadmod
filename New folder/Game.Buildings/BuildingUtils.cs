#define UNITY_ASSERTIONS
using System;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Agents;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Vehicles;
using Game.Zones;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Buildings;

public static class BuildingUtils
{
	public struct LotInfo
	{
		public float3 m_Position;

		public float2 m_Extents;

		public float m_Radius;

		public float m_Circular;

		public quaternion m_Rotation;

		public float3 m_FrontHeights;

		public float3 m_RightHeights;

		public float3 m_BackHeights;

		public float3 m_LeftHeights;

		public float3 m_FlatX0;

		public float3 m_FlatZ0;

		public float3 m_FlatX1;

		public float3 m_FlatZ1;

		public float4 m_MinLimit;

		public float4 m_MaxLimit;
	}

	public const float MAX_ROAD_CONNECTION_DISTANCE = 8.4f;

	public const float GEOMETRY_SIZE_OFFSET = 0.4f;

	public const float MIN_BUILDING_HEIGHT = 5f;

	public const float MIN_CONSTRUCTION_HEIGHT = 15f;

	public const float RANDOM_CONSTRUCTION_HEIGHT = 5f;

	public const float COLLAPSE_ACCELERATION = 5f;

	public static Quad3 CalculateCorners(Game.Objects.Transform transform, int2 lotSize)
	{
		return CalculateCorners(transform.m_Position, transform.m_Rotation, (float2)lotSize * 4f);
	}

	public static Quad3 CalculateCorners(float3 position, quaternion rotation, float2 halfLotSize)
	{
		float3 @float = math.mul(rotation, new float3(0f, 0f, -1f));
		float3 float2 = math.mul(rotation, new float3(-1f, 0f, 0f));
		float3 float3 = @float * halfLotSize.y;
		float3 float4 = float2 * halfLotSize.x;
		float3 float5 = position + float3;
		float3 float6 = position - float3;
		return new Quad3(float5 - float4, float5 + float4, float6 + float4, float6 - float4);
	}

	public static float3 CalculateFrontPosition(Game.Objects.Transform transform, int lotDepth)
	{
		float3 position = new float3(0f, 0f, (float)lotDepth * 4f);
		return ObjectUtils.LocalToWorld(transform, position);
	}

	public static float GetEfficiency(BufferAccessor<Efficiency> bufferAccessor, int i)
	{
		if (bufferAccessor.Length == 0)
		{
			return 1f;
		}
		return GetEfficiency(bufferAccessor[i]);
	}

	public static float GetImmediateEfficiency(BufferAccessor<Efficiency> bufferAccessor, int i)
	{
		if (bufferAccessor.Length == 0)
		{
			return 1f;
		}
		return GetImmediateEfficiency(bufferAccessor[i]);
	}

	public static float GetEfficiency(Entity entity, ref BufferLookup<Efficiency> bufferLookup)
	{
		if (!bufferLookup.TryGetBuffer(entity, out var bufferData))
		{
			return 1f;
		}
		return GetEfficiency(bufferData);
	}

	public static float GetEfficiency(DynamicBuffer<Efficiency> buffer)
	{
		float num = 1f;
		foreach (Efficiency item in buffer)
		{
			num *= math.max(0f, item.m_Efficiency);
		}
		if (!(num > 0f))
		{
			return 0f;
		}
		return math.max(0.01f, math.round(100f * num) / 100f);
	}

	public static float GetEfficiencyExcludingFactor(DynamicBuffer<Efficiency> buffer, EfficiencyFactor factor)
	{
		float num = 1f;
		foreach (Efficiency item in buffer)
		{
			if (item.m_Factor != factor)
			{
				num *= math.max(0f, item.m_Efficiency);
			}
		}
		if (!(num > 0f))
		{
			return 0f;
		}
		return math.max(0.01f, math.round(100f * num) / 100f);
	}

	public static float GetImmediateEfficiency(DynamicBuffer<Efficiency> buffer)
	{
		float num = 1f;
		foreach (Efficiency item in buffer)
		{
			EfficiencyFactor factor = item.m_Factor;
			if (factor <= EfficiencyFactor.Disabled || factor == EfficiencyFactor.ServiceBudget)
			{
				num *= math.max(0f, item.m_Efficiency);
			}
		}
		if (!(num > 0f))
		{
			return 0f;
		}
		return math.max(0.01f, math.round(100f * num) / 100f);
	}

	public static float GetEfficiency(Span<float> factors)
	{
		float num = 1f;
		Span<float> span = factors;
		for (int i = 0; i < span.Length; i++)
		{
			float y = span[i];
			num *= math.max(0f, y);
		}
		if (!(num > 0f))
		{
			return 0f;
		}
		return math.max(0.01f, math.round(100f * num) / 100f);
	}

	public static void GetEfficiencyFactors(DynamicBuffer<Efficiency> buffer, Span<float> factors)
	{
		factors.Fill(1f);
		foreach (Efficiency item in buffer)
		{
			factors[(int)item.m_Factor] = item.m_Efficiency;
		}
	}

	public static void SetEfficiencyFactors(DynamicBuffer<Efficiency> buffer, Span<float> factors)
	{
		buffer.Clear();
		for (int i = 0; i < factors.Length; i++)
		{
			if ((double)math.abs(factors[i] - 1f) > 0.001)
			{
				buffer.Add(new Efficiency((EfficiencyFactor)i, factors[i]));
			}
		}
	}

	public static void SetEfficiencyFactor(DynamicBuffer<Efficiency> buffer, EfficiencyFactor factor, float efficiency)
	{
		for (int i = 0; i < buffer.Length; i++)
		{
			if (buffer[i].m_Factor == factor)
			{
				if (math.abs(efficiency - 1f) > 0.001f)
				{
					buffer[i] = new Efficiency(factor, efficiency);
				}
				else
				{
					buffer.RemoveAt(i);
				}
				return;
			}
		}
		if (math.abs(efficiency - 1f) > 0.001f)
		{
			buffer.Add(new Efficiency(factor, efficiency));
		}
	}

	public static float GetEfficiencyFactor(DynamicBuffer<Efficiency> buffer, EfficiencyFactor factor)
	{
		for (int i = 0; i < buffer.Length; i++)
		{
			if (buffer[i].m_Factor == factor)
			{
				return buffer[i].m_Efficiency;
			}
		}
		return 1f;
	}

	public static float2 ApproximateEfficiencyFactors(float targetEfficiency, float2 weights)
	{
		Assert.IsTrue(targetEfficiency >= 0f && targetEfficiency <= 1f);
		Assert.IsTrue(math.cmin(weights) >= 0f);
		bool2 @bool = weights > 0.001f;
		if (targetEfficiency == 1f || !math.all(@bool))
		{
			return math.select(1f, targetEfficiency, @bool);
		}
		if (targetEfficiency == 0f)
		{
			return math.select(1f, 0f, @bool);
		}
		float num = (weights.x + weights.y) / (2f * weights.x * weights.y);
		float num2 = (1f - targetEfficiency) / (weights.x * weights.y);
		float num3 = num - math.sqrt(num * num - num2);
		return 1f - num3 * weights;
	}

	public static float4 ApproximateEfficiencyFactors(float targetEfficiency, float4 weights)
	{
		Assert.IsTrue(targetEfficiency >= 0f && targetEfficiency <= 1f);
		Assert.IsTrue(math.cmin(weights) >= 0f);
		float num = math.cmax(weights);
		if (targetEfficiency == 1f || num == 0f)
		{
			return 1f;
		}
		if (targetEfficiency == 0f)
		{
			return math.select(1f, 0f, weights > 1.1920929E-07f);
		}
		float num2 = -1f / num;
		float num3 = 0f;
		float4 result = default(float4);
		for (int i = 0; i < 16; i++)
		{
			float num4 = (num2 + num3) / 2f;
			result = num4 * weights + 1f;
			float num5 = result.x * result.y * result.z * result.w;
			num2 = math.select(num2, num4, num5 < targetEfficiency);
			num3 = math.select(num3, num4, num5 > targetEfficiency);
		}
		return result;
	}

	public static float GetEfficiency(byte rawValue)
	{
		return (float)(int)rawValue / 100f;
	}

	public static int GetLevelingCost(AreaType areaType, BuildingPropertyData propertyData, int currentlevel, DynamicBuffer<CityModifier> cityEffects)
	{
		if (currentlevel >= 5)
		{
			return 1073741823;
		}
		int num = propertyData.CountProperties();
		float value;
		switch (areaType)
		{
		case AreaType.Residential:
			value = num * Mathf.RoundToInt(math.pow(2f, 2 * currentlevel) * 40f);
			break;
		case AreaType.Commercial:
		case AreaType.Industrial:
			value = num * Mathf.RoundToInt(math.pow(2f, 2 * currentlevel) * 160f);
			if (propertyData.m_AllowedStored != Resource.NoResource)
			{
				value *= 4f;
			}
			break;
		default:
			return 1073741823;
		}
		CityUtils.ApplyModifier(ref value, cityEffects, CityModifierType.BuildingLevelingCost);
		return Mathf.RoundToInt(value);
	}

	public static int GetBuildingConditionChange(AreaType areaType, BuildingConfigurationData buildingConfigurationData)
	{
		return areaType switch
		{
			AreaType.Residential => buildingConfigurationData.m_BuildingConditionIncrement.x, 
			AreaType.Commercial => buildingConfigurationData.m_BuildingConditionIncrement.y, 
			AreaType.Industrial => buildingConfigurationData.m_BuildingConditionIncrement.z, 
			_ => 0, 
		};
	}

	public static int GetAbandonCost(AreaType areaType, BuildingPropertyData buildingPropertyData, int currentLevel, int levelingCost, DynamicBuffer<CityModifier> cityEffects)
	{
		int num = ((currentLevel == 5) ? GetLevelingCost(areaType, buildingPropertyData, 4, cityEffects) : levelingCost);
		if (areaType == AreaType.Residential && buildingPropertyData.m_ResidentialProperties > 1)
		{
			num = Mathf.RoundToInt((float)(num * (6 - currentLevel)) / math.sqrt(buildingPropertyData.m_ResidentialProperties));
		}
		return num;
	}

	public static AreaType GetAreaType(Entity buildPrefab, ref ComponentLookup<SpawnableBuildingData> spawnableBuildingDatas, ref ComponentLookup<ZoneData> zoneDatas)
	{
		if (spawnableBuildingDatas.HasComponent(buildPrefab) && zoneDatas.HasComponent(spawnableBuildingDatas[buildPrefab].m_ZonePrefab))
		{
			return zoneDatas[spawnableBuildingDatas[buildPrefab].m_ZonePrefab].m_AreaType;
		}
		return AreaType.None;
	}

	public static bool CheckOption(Building building, BuildingOption option)
	{
		return (building.m_OptionMask & (uint)(1 << (int)option)) != 0;
	}

	public static bool CheckOption(InstalledUpgrade installedUpgrade, BuildingOption option)
	{
		return (installedUpgrade.m_OptionMask & (uint)(1 << (int)option)) != 0;
	}

	public static void ApplyModifier(ref float value, DynamicBuffer<BuildingModifier> modifiers, BuildingModifierType type)
	{
		if (modifiers.Length > (int)type)
		{
			float2 delta = modifiers[(int)type].m_Delta;
			value += delta.x;
			value += value * delta.y;
		}
	}

	public static bool HasOption(BuildingOptionData optionData, BuildingOption option)
	{
		return (optionData.m_OptionMask & (uint)(1 << (int)option)) != 0;
	}

	public static int GetVehicleCapacity(float efficiency, int capacity)
	{
		return math.select(0, (int)math.clamp((long)(efficiency * (float)capacity), 1L, capacity), efficiency > 0.001f && capacity > 0);
	}

	public static bool GetAddress(EntityManager entityManager, Entity entity, out Entity road, out int number)
	{
		if (entityManager.TryGetComponent<Building>(entity, out var component))
		{
			return GetAddress(entityManager, entity, component.m_RoadEdge, component.m_CurvePosition, out road, out number);
		}
		if (entityManager.TryGetComponent<Attached>(entity, out var component2))
		{
			return GetAddress(entityManager, entity, component2.m_Parent, component2.m_CurvePosition, out road, out number);
		}
		road = Entity.Null;
		number = 0;
		return false;
	}

	public static bool GetRandomOutsideConnectionByParameters(ref NativeList<Entity> outsideConnections, ref ComponentLookup<OutsideConnectionData> outsideConnectionDatas, ref ComponentLookup<PrefabRef> prefabRefs, Unity.Mathematics.Random random, float4 outsideConnectionSpawnParameters, out Entity result)
	{
		OutsideConnectionTransferType ocTransferType = OutsideConnectionTransferType.None;
		float num = random.NextFloat(1f);
		if (num < outsideConnectionSpawnParameters.x)
		{
			ocTransferType = OutsideConnectionTransferType.Road;
		}
		else if (num < outsideConnectionSpawnParameters.x + outsideConnectionSpawnParameters.y)
		{
			ocTransferType = OutsideConnectionTransferType.Train;
		}
		else if (num < outsideConnectionSpawnParameters.x + outsideConnectionSpawnParameters.y + outsideConnectionSpawnParameters.z)
		{
			ocTransferType = OutsideConnectionTransferType.Air;
		}
		else if (num < outsideConnectionSpawnParameters.x + outsideConnectionSpawnParameters.y + outsideConnectionSpawnParameters.z + outsideConnectionSpawnParameters.w)
		{
			ocTransferType = OutsideConnectionTransferType.Ship;
		}
		return GetRandomOutsideConnectionByTransferType(ref outsideConnections, ref outsideConnectionDatas, ref prefabRefs, random, ocTransferType, out result);
	}

	public static bool GetRandomOutsideConnectionByTransferType(ref NativeList<Entity> outsideConnections, ref ComponentLookup<OutsideConnectionData> outsideConnectionDatas, ref ComponentLookup<PrefabRef> prefabRefs, Unity.Mathematics.Random random, OutsideConnectionTransferType ocTransferType, out Entity result)
	{
		NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.Temp);
		if (ocTransferType != OutsideConnectionTransferType.None)
		{
			for (int i = 0; i < outsideConnections.Length; i++)
			{
				Entity prefab = prefabRefs[outsideConnections[i]].m_Prefab;
				if (outsideConnectionDatas.HasComponent(prefab) && (ocTransferType & outsideConnectionDatas[prefab].m_Type) != OutsideConnectionTransferType.None)
				{
					nativeList.Add(outsideConnections[i]);
				}
			}
		}
		result = Entity.Null;
		if (nativeList.Length > 0)
		{
			result = nativeList[random.NextInt(nativeList.Length)];
			return true;
		}
		return false;
	}

	public static OutsideConnectionTransferType GetOutsideConnectionType(Entity building, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<OutsideConnectionData> outsideConnectionDatas)
	{
		if (outsideConnectionDatas.HasComponent(prefabRefs[building].m_Prefab))
		{
			return outsideConnectionDatas[prefabRefs[building].m_Prefab].m_Type;
		}
		return OutsideConnectionTransferType.None;
	}

	public static bool GetAddress(EntityManager entityManager, Entity entity, Entity edge, float curvePos, out Entity road, out int number)
	{
		if (entityManager.TryGetComponent<Aggregated>(edge, out var component) && entityManager.TryGetBuffer(component.m_Aggregate, isReadOnly: true, out DynamicBuffer<AggregateElement> buffer))
		{
			float num = 0f;
			for (int i = 0; i < buffer.Length; i++)
			{
				AggregateElement aggregateElement = buffer[i];
				float num2 = num;
				if (entityManager.TryGetComponent<Curve>(aggregateElement.m_Edge, out var component2) && entityManager.TryGetComponent<Composition>(aggregateElement.m_Edge, out var component3) && entityManager.TryGetComponent<NetCompositionData>(component3.m_Edge, out var component4))
				{
					float2 x = math.normalizesafe(MathUtils.StartTangent(component2.m_Bezier).xz);
					float2 y = math.normalizesafe(MathUtils.EndTangent(component2.m_Bezier).xz);
					float num3 = ZoneUtils.GetCellWidth(component4.m_Width);
					float num4 = math.acos(math.clamp(math.dot(x, y), -1f, 1f));
					num2 += component2.m_Length + num3 * num4 * 0.5f;
				}
				bool flag = i == 0;
				bool flag2 = i == buffer.Length - 1;
				bool flag3 = aggregateElement.m_Edge == edge;
				bool flag4 = false;
				if (flag3 || flag || flag2)
				{
					Edge component7;
					Edge component8;
					if (!flag)
					{
						if (entityManager.TryGetComponent<Edge>(aggregateElement.m_Edge, out var component5) && entityManager.TryGetComponent<Edge>(buffer[i - 1].m_Edge, out var component6) && (component5.m_End == component6.m_Start || component5.m_End == component6.m_End))
						{
							flag4 = true;
						}
					}
					else if (!flag2 && entityManager.TryGetComponent<Edge>(aggregateElement.m_Edge, out component7) && entityManager.TryGetComponent<Edge>(buffer[i + 1].m_Edge, out component8) && (component7.m_Start == component8.m_Start || component7.m_Start == component8.m_End))
					{
						flag4 = true;
					}
					if (flag && entityManager.TryGetComponent<Edge>(aggregateElement.m_Edge, out var component9) && entityManager.TryGetComponent<Roundabout>(flag4 ? component9.m_End : component9.m_Start, out var component10))
					{
						num += component10.m_Radius;
					}
					if (flag3)
					{
						Bounds1 t = new Bounds1(flag4 ? curvePos : 0f, flag4 ? 1f : curvePos);
						float num5 = math.saturate(MathUtils.Length(component2.m_Bezier, t) / math.max(1f, component2.m_Length));
						float num6 = math.lerp(num, num2, num5);
						bool flag5 = false;
						if (entityManager.TryGetComponent<Game.Objects.Transform>(entity, out var component11))
						{
							if (num5 < 0.01f && entityManager.TryGetComponent<Edge>(aggregateElement.m_Edge, out var component12) && entityManager.TryGetComponent<Roundabout>(component12.m_Start, out var component13) && entityManager.TryGetComponent<PrefabRef>(entity, out var component14) && entityManager.TryGetComponent<BuildingData>(component14.m_Prefab, out var component15))
							{
								float3 @float = CalculateFrontPosition(component11, component15.m_LotSize.y);
								float2 value = MathUtils.StartTangent(component2.m_Bezier).xz;
								if (MathUtils.TryNormalize(ref value))
								{
									float valueToClamp = math.dot(value, component2.m_Bezier.a.xz - @float.xz);
									valueToClamp = math.clamp(valueToClamp, 0f, component13.m_Radius);
									num6 += math.select(0f - valueToClamp, valueToClamp, flag4);
								}
							}
							if (num5 > 0.99f && entityManager.TryGetComponent<Edge>(aggregateElement.m_Edge, out var component16) && entityManager.TryGetComponent<Roundabout>(component16.m_End, out var component17) && entityManager.TryGetComponent<PrefabRef>(entity, out var component18) && entityManager.TryGetComponent<BuildingData>(component18.m_Prefab, out var component19))
							{
								float3 float2 = CalculateFrontPosition(component11, component19.m_LotSize.y);
								float2 value2 = MathUtils.EndTangent(component2.m_Bezier).xz;
								if (MathUtils.TryNormalize(ref value2))
								{
									float valueToClamp2 = math.dot(value2, float2.xz - component2.m_Bezier.d.xz);
									valueToClamp2 = math.clamp(valueToClamp2, 0f, component17.m_Radius);
									num6 += math.select(valueToClamp2, 0f - valueToClamp2, flag4);
								}
							}
							float2 x2 = component11.m_Position.xz - MathUtils.Position(component2.m_Bezier, curvePos).xz;
							float2 y2 = MathUtils.Right(MathUtils.Tangent(component2.m_Bezier, curvePos).xz);
							flag5 = math.dot(x2, y2) > 0f != flag4;
						}
						road = component.m_Aggregate;
						number = Mathf.RoundToInt(num6 / 8f) * 2 + ((!flag5) ? 1 : 2);
						return true;
					}
				}
				num = num2;
			}
		}
		road = Entity.Null;
		number = 0;
		return false;
	}

	public static LotInfo CalculateLotInfo(float2 extents, Game.Objects.Transform transform, Game.Objects.Elevation elevation, Lot lot, PrefabRef prefabRef, DynamicBuffer<InstalledUpgrade> upgrades, ComponentLookup<Game.Objects.Transform> transforms, ComponentLookup<PrefabRef> prefabRefs, ComponentLookup<ObjectGeometryData> objectGeometryDatas, ComponentLookup<BuildingTerraformData> buildingTerraformDatas, ComponentLookup<BuildingExtensionData> buildingExtensionDatas, bool defaultNoSmooth, out bool hasExtensionLots)
	{
		LotInfo result = new LotInfo
		{
			m_Position = transform.m_Position,
			m_Extents = extents,
			m_Rotation = transform.m_Rotation,
			m_Radius = math.length(extents),
			m_Circular = 0f,
			m_FrontHeights = lot.m_FrontHeights,
			m_RightHeights = lot.m_RightHeights,
			m_BackHeights = lot.m_BackHeights,
			m_LeftHeights = lot.m_LeftHeights,
			m_FlatX0 = 0f - extents.x,
			m_FlatZ0 = 0f - extents.y,
			m_FlatX1 = extents.x,
			m_FlatZ1 = extents.y,
			m_MinLimit = new float4(-extents.xy, extents.xy),
			m_MaxLimit = new float4(-extents.xy, extents.xy)
		};
		if (objectGeometryDatas.TryGetComponent(prefabRef.m_Prefab, out var componentData))
		{
			bool flag = (componentData.m_Flags & Game.Objects.GeometryFlags.Standing) != 0;
			bool test = ((uint)componentData.m_Flags & (uint)((!flag) ? 1 : 256)) != 0;
			result.m_Circular = math.select(0f, 1f, test);
		}
		if (buildingTerraformDatas.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
		{
			result.m_Position.y += componentData2.m_HeightOffset;
			result.m_FlatX0 = componentData2.m_FlatX0;
			result.m_FlatZ0 = componentData2.m_FlatZ0;
			result.m_FlatX1 = componentData2.m_FlatX1;
			result.m_FlatZ1 = componentData2.m_FlatZ1;
			result.m_MinLimit = componentData2.m_Smooth;
			result.m_MaxLimit = componentData2.m_Smooth;
		}
		else
		{
			componentData2.m_DontLower = defaultNoSmooth;
			componentData2.m_DontRaise = defaultNoSmooth;
		}
		hasExtensionLots = false;
		if (upgrades.IsCreated)
		{
			for (int i = 0; i < upgrades.Length; i++)
			{
				Entity upgrade = upgrades[i].m_Upgrade;
				PrefabRef prefabRef2 = prefabRefs[upgrade];
				if (buildingExtensionDatas.TryGetComponent(prefabRef2.m_Prefab, out var componentData3) && !componentData3.m_External && buildingTerraformDatas.TryGetComponent(prefabRef2.m_Prefab, out var componentData4))
				{
					float3 @float = transforms[upgrade].m_Position - transform.m_Position;
					float num = 0f;
					if (objectGeometryDatas.TryGetComponent(prefabRef2.m_Prefab, out var componentData5))
					{
						bool flag2 = (componentData5.m_Flags & Game.Objects.GeometryFlags.Standing) != 0;
						bool test2 = ((uint)componentData5.m_Flags & (uint)((!flag2) ? 1 : 256)) != 0;
						num = math.select(0f, 1f, test2);
					}
					result.m_FlatX0 = math.min(result.m_FlatX0, componentData4.m_FlatX0 + @float.x);
					result.m_FlatZ0 = math.min(result.m_FlatZ0, componentData4.m_FlatZ0 + @float.z);
					result.m_FlatX1 = math.max(result.m_FlatX1, componentData4.m_FlatX1 + @float.x);
					result.m_FlatZ1 = math.max(result.m_FlatZ1, componentData4.m_FlatZ1 + @float.z);
					if (!math.all(componentData4.m_Smooth + @float.xzxz == result.m_MaxLimit) || num != result.m_Circular)
					{
						hasExtensionLots = true;
					}
				}
			}
		}
		result.m_MinLimit.xy = math.min(new float2(result.m_FlatX0.y, result.m_FlatZ0.y), result.m_MinLimit.xy);
		result.m_MinLimit.zw = math.max(new float2(result.m_FlatX1.y, result.m_FlatZ1.y), result.m_MinLimit.zw);
		extents = math.max(extents, 8f);
		if (componentData2.m_DontLower)
		{
			result.m_MinLimit = new float4(extents.xy, -extents.xy);
		}
		if (elevation.m_Elevation > 0f || componentData2.m_DontRaise)
		{
			result.m_MaxLimit = new float4(extents.xy, -extents.xy);
		}
		return result;
	}

	public static float SampleHeight(ref LotInfo lotInfo, float3 position)
	{
		position = math.mul(math.inverse(lotInfo.m_Rotation), position - lotInfo.m_Position);
		Bezier4x2 curve = new Bezier4x2(new float2(lotInfo.m_RightHeights.x, lotInfo.m_FrontHeights.x), new float2(lotInfo.m_RightHeights.y, lotInfo.m_LeftHeights.z), new float2(lotInfo.m_RightHeights.z, lotInfo.m_LeftHeights.y), new float2(lotInfo.m_BackHeights.x, lotInfo.m_LeftHeights.x));
		Bezier4x2 curve2 = new Bezier4x2(new float2(lotInfo.m_RightHeights.x, lotInfo.m_BackHeights.x), new float2(lotInfo.m_FrontHeights.z, lotInfo.m_BackHeights.y), new float2(lotInfo.m_FrontHeights.y, lotInfo.m_BackHeights.z), new float2(lotInfo.m_FrontHeights.x, lotInfo.m_LeftHeights.x));
		float2 @float = math.clamp(position.xz, -lotInfo.m_Extents, lotInfo.m_Extents);
		float2 float2 = 0.5f / math.max(0.01f, lotInfo.m_Extents);
		float2 float3 = position.xz * float2 + 0.5f;
		float2 float4 = math.saturate(float3);
		float2 x = position.xz - @float;
		float2 float5 = float3 - math.sign(x) * float2 * 2f;
		float2 float6 = (float5 - 0.5f) * (lotInfo.m_Extents * 2f);
		x = 8f - 8f / (1f + math.abs(x) * 0.125f);
		float4 falseValue = new float4(lotInfo.m_FlatX0.xy, lotInfo.m_FlatZ0.yx);
		float4 falseValue2 = new float4(lotInfo.m_FlatZ0.xy, lotInfo.m_FlatX0.yx);
		float4 falseValue3 = new float4(lotInfo.m_FlatX1.xy, lotInfo.m_FlatZ0.yz);
		float4 falseValue4 = new float4(lotInfo.m_FlatZ1.xy, lotInfo.m_FlatX0.yz);
		falseValue = math.select(falseValue, new float4(lotInfo.m_FlatX0.yy, lotInfo.m_FlatZ0.x, lotInfo.m_FlatZ1.x), @float.y > falseValue.w);
		falseValue2 = math.select(falseValue2, new float4(lotInfo.m_FlatZ0.yy, lotInfo.m_FlatX0.x, lotInfo.m_FlatX1.x), @float.x > falseValue2.w);
		falseValue3 = math.select(falseValue3, new float4(lotInfo.m_FlatX1.yy, lotInfo.m_FlatZ0.z, lotInfo.m_FlatZ1.z), @float.y > falseValue3.w);
		falseValue4 = math.select(falseValue4, new float4(lotInfo.m_FlatZ1.yy, lotInfo.m_FlatX0.z, lotInfo.m_FlatX1.z), @float.x > falseValue4.w);
		falseValue = math.select(falseValue, new float4(lotInfo.m_FlatX0.yz, lotInfo.m_FlatZ1.xy), @float.y > falseValue.w);
		falseValue2 = math.select(falseValue2, new float4(lotInfo.m_FlatZ0.yz, lotInfo.m_FlatX1.xy), @float.x > falseValue2.w);
		falseValue3 = math.select(falseValue3, new float4(lotInfo.m_FlatX1.yz, lotInfo.m_FlatZ1.zy), @float.y > falseValue3.w);
		falseValue4 = math.select(falseValue4, new float4(lotInfo.m_FlatZ1.yz, lotInfo.m_FlatX1.zy), @float.x > falseValue4.w);
		float4 start = new float4(falseValue.x, falseValue2.x, falseValue3.x, falseValue4.x);
		float4 end = new float4(falseValue.y, falseValue2.y, falseValue3.y, falseValue4.y);
		float4 float7 = new float4(falseValue.z, falseValue2.z, falseValue3.z, falseValue4.z);
		float4 float8 = new float4(falseValue.w, falseValue2.w, falseValue3.w, falseValue4.w);
		float4 t = (@float.yxyx - float7) / math.max(float8 - float7, 0.1f);
		t = math.lerp(start, end, t);
		t = math.saturate(new float4(t.xy - @float, @float - t.zw) / math.max(new float4(t.xy + lotInfo.m_Extents, lotInfo.m_Extents - t.zw), 0.1f));
		float4 t2 = (float6.yxyx - float7) / math.max(float8 - float7, 0.1f);
		t2 = math.lerp(start, end, t2);
		t2 = math.saturate(new float4(t2.xy - float6, float6 - t2.zw) / math.max(new float4(t2.xy + lotInfo.m_Extents, lotInfo.m_Extents - t2.zw), 0.1f));
		float4 float9 = new float4
		{
			xz = MathUtils.Position(curve, float4.y),
			yw = MathUtils.Position(curve2, float4.x)
		};
		float9 *= t;
		float9.xy += float9.zw;
		float4 float10 = new float4
		{
			xz = MathUtils.Position(curve, float5.y),
			yw = MathUtils.Position(curve2, float5.x)
		};
		float10 *= t2;
		float10.xy += float10.zw;
		float9.xy += (float9.xy - float10.xy) * x.xy * 0.5f;
		t.xy = math.max(t.xy, t.zw);
		t.xy /= math.max(1f, t.x + t.y);
		t.x = math.select(t.y, 1f - t.x, t.x > t.y);
		float9.x = math.lerp(float9.x, float9.y, t.x);
		position.y = float9.x;
		return lotInfo.m_Position.y + position.y;
	}

	public static float GetCollapseTime(float height)
	{
		return math.sqrt(math.max(0f, height) * 0.4f);
	}

	public static float GetCollapseHeight(float time)
	{
		return 2.5f * math.lengthsq(time);
	}

	public static MaintenanceType GetMaintenanceType(Entity entity, ref ComponentLookup<Park> parks, ref ComponentLookup<NetCondition> netConditions, ref ComponentLookup<Edge> edges, ref ComponentLookup<Surface> surfaces, ref ComponentLookup<Vehicle> vehicles)
	{
		if (parks.HasComponent(entity))
		{
			return MaintenanceType.Park;
		}
		if (netConditions.HasComponent(entity))
		{
			if (!surfaces.TryGetComponent(entity, out var componentData) && edges.TryGetComponent(entity, out var componentData2) && surfaces.TryGetComponent(componentData2.m_Start, out var componentData3) && surfaces.TryGetComponent(componentData2.m_End, out var componentData4))
			{
				componentData.m_AccumulatedSnow = (byte)(componentData3.m_AccumulatedSnow + componentData4.m_AccumulatedSnow + 1 >> 1);
			}
			if (componentData.m_AccumulatedSnow >= 15)
			{
				return MaintenanceType.Snow;
			}
			return MaintenanceType.Road;
		}
		if (vehicles.HasComponent(entity))
		{
			return MaintenanceType.Vehicle;
		}
		return MaintenanceType.None;
	}

	public static void CalculateUpgradeRangeValues(quaternion rotation, BuildingData ownerBuildingData, BuildingData buildingData, ServiceUpgradeData serviceUpgradeData, out float3 forward, out float width, out float length, out float roundness, out bool circular)
	{
		forward = math.forward(rotation);
		if (ownerBuildingData.m_LotSize.y < ownerBuildingData.m_LotSize.x)
		{
			ownerBuildingData.m_LotSize = ownerBuildingData.m_LotSize.yx;
			forward.xz = MathUtils.Right(forward.xz);
		}
		float num = serviceUpgradeData.m_MaxPlacementDistance + (float)buildingData.m_LotSize.y * 8f;
		width = (float)ownerBuildingData.m_LotSize.x * 8f + num * 2f;
		length = (float)ownerBuildingData.m_LotSize.y * 8f + num * 2f;
		roundness = math.max(0f, num - 40f) * 1.2f + 8f;
		width = math.min(length, math.max(width, roundness * 2f));
		roundness = math.min(roundness, width * 0.5f);
		circular = length * 0.5f - roundness < 1f;
	}

	public static bool IsHomelessShelterBuilding(Entity propertyEntity, ref ComponentLookup<Park> parks, ref ComponentLookup<Abandoned> abandoneds)
	{
		if (!parks.HasComponent(propertyEntity))
		{
			return abandoneds.HasComponent(propertyEntity);
		}
		return true;
	}

	public static bool IsHomelessShelterBuilding(EntityManager entityManager, Entity propertyEntity)
	{
		if (!entityManager.HasComponent<Park>(propertyEntity))
		{
			return entityManager.HasComponent<Abandoned>(propertyEntity);
		}
		return true;
	}

	public static bool IsHomelessHousehold(Household household, Entity propertyEntity, ref ComponentLookup<Park> parks, ref ComponentLookup<Abandoned> abandoneds)
	{
		if ((household.m_Flags & HouseholdFlags.MovedIn) != HouseholdFlags.None)
		{
			if (!(propertyEntity == Entity.Null))
			{
				return IsHomelessShelterBuilding(propertyEntity, ref parks, ref abandoneds);
			}
			return true;
		}
		return false;
	}

	public static bool IsHomelessHousehold(EntityManager entityManager, Entity householdEntity)
	{
		if (entityManager.TryGetComponent<Household>(householdEntity, out var component) && (component.m_Flags & HouseholdFlags.MovedIn) != HouseholdFlags.None && !entityManager.HasComponent<MovingAway>(householdEntity))
		{
			if (entityManager.TryGetComponent<PropertyRenter>(householdEntity, out var component2) && !(component2.m_Property == Entity.Null) && !entityManager.HasComponent<Park>(component2.m_Property))
			{
				return entityManager.HasComponent<Abandoned>(component2.m_Property);
			}
			return true;
		}
		return false;
	}

	public static Entity GetPropertyFromRenter(Entity renter, ref ComponentLookup<HomelessHousehold> homelessHouseholds, ref ComponentLookup<PropertyRenter> propertyRenters)
	{
		if (homelessHouseholds.HasComponent(renter))
		{
			return homelessHouseholds[renter].m_TempHome;
		}
		if (propertyRenters.HasComponent(renter))
		{
			return propertyRenters[renter].m_Property;
		}
		return Entity.Null;
	}

	public static Entity GetHouseholdHomeBuilding(Entity householdEntity, ref ComponentLookup<PropertyRenter> propertyRenters, ref ComponentLookup<HomelessHousehold> homelessHouseholds)
	{
		if (propertyRenters.TryGetComponent(householdEntity, out var componentData))
		{
			return componentData.m_Property;
		}
		if (homelessHouseholds.TryGetComponent(householdEntity, out var componentData2))
		{
			return componentData2.m_TempHome;
		}
		return Entity.Null;
	}

	public static Entity GetHouseholdHomeBuilding(Entity householdEntity, ref ComponentLookup<PropertyRenter> propertyRenters, ref ComponentLookup<HomelessHousehold> homelessHouseholds, ref ComponentLookup<TouristHousehold> touristHouseholds)
	{
		if (propertyRenters.TryGetComponent(householdEntity, out var componentData))
		{
			return componentData.m_Property;
		}
		if (touristHouseholds.TryGetComponent(householdEntity, out var componentData2) && propertyRenters.TryGetComponent(componentData2.m_Hotel, out componentData))
		{
			return componentData.m_Property;
		}
		if (homelessHouseholds.TryGetComponent(householdEntity, out var componentData3))
		{
			return componentData3.m_TempHome;
		}
		return Entity.Null;
	}

	public static int GetShelterHomelessCapacity(Entity buildingPrefabEntity, ref ComponentLookup<BuildingData> buildingDatas, ref ComponentLookup<BuildingPropertyData> buildingPropertyDatas)
	{
		if (!buildingDatas.HasComponent(buildingPrefabEntity))
		{
			return 0;
		}
		BuildingData buildingData = buildingDatas[buildingPrefabEntity];
		int num = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
		if (!buildingPropertyDatas.HasComponent(buildingPrefabEntity))
		{
			return num / 4;
		}
		BuildingPropertyData buildingPropertyData = buildingPropertyDatas[buildingPrefabEntity];
		float num2 = buildingPropertyData.m_ResidentialProperties;
		if (buildingPropertyData.m_AllowedSold != Resource.NoResource || buildingPropertyData.m_AllowedManufactured != Resource.NoResource || buildingPropertyData.m_AllowedStored != Resource.NoResource)
		{
			num2 += buildingPropertyData.m_SpaceMultiplier * (float)num;
		}
		return Mathf.CeilToInt(num2 / 2f);
	}

	public static void GetNumberOfConnectedLines(Entity entity, ref NativeList<Entity> linesResult, ref BufferLookup<ConnectedRoute> connectedRouteBuffers, ref BufferLookup<Game.Objects.SubObject> subObjectBuffers, ref ComponentLookup<Owner> owners)
	{
		if (connectedRouteBuffers.TryGetBuffer(entity, out var bufferData))
		{
			for (int i = 0; i < bufferData.Length; i++)
			{
				if (owners.TryGetComponent(bufferData[i].m_Waypoint, out var componentData) && !linesResult.Contains(componentData.m_Owner))
				{
					linesResult.Add(in componentData.m_Owner);
				}
			}
		}
		if (subObjectBuffers.TryGetBuffer(entity, out var bufferData2))
		{
			for (int j = 0; j < bufferData2.Length; j++)
			{
				GetNumberOfConnectedLines(bufferData2[j].m_SubObject, ref linesResult, ref connectedRouteBuffers, ref subObjectBuffers, ref owners);
			}
		}
	}

	public static Entity GetTopOwner(Entity entity, ref ComponentLookup<Owner> owners)
	{
		Owner componentData;
		while (owners.TryGetComponent(entity, out componentData))
		{
			entity = componentData.m_Owner;
		}
		return entity;
	}
}
