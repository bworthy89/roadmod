using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Objects;

public static class ObjectUtils
{
	public struct ActivityStartPositionCache
	{
		public ActivityType m_ActivityType;

		public float3 m_PositionOffset;

		public quaternion m_RotationOffset;
	}

	public const float MAX_SPAWN_LOCATION_CONNECTION_DISTANCE = 32f;

	public const float MIN_TREE_WOOD_RESOURCE = 1f;

	public const float MAX_TREE_AGE = 40f;

	public const float TREE_AGE_PHASE_CHILD = 0.1f;

	public const float TREE_AGE_PHASE_TEEN = 0.15f;

	public const float TREE_AGE_PHASE_ADULT = 0.35f;

	public const float TREE_AGE_PHASE_ELDERLY = 0.35f;

	public const float TREE_AGE_PHASE_DEAD = 0.05f;

	public const float TREE_WOOD_GROWTH_CHILD = 0.2f;

	public const float TREE_WOOD_GROWTH_TEEN = 0.5f;

	public const float TREE_WOOD_GROWTH_ADULT = 0.3f;

	public static float3 GetSize(Bounds3 bounds)
	{
		return new float3
		{
			xz = math.max(-bounds.min, bounds.max).xz * 2f,
			y = bounds.max.y
		};
	}

	public static Bounds3 CalculateBounds(float3 position, quaternion rotation, ObjectGeometryData geometryData)
	{
		if ((geometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
		{
			float num = geometryData.m_Size.x * 0.5f;
			return new Bounds3(position + new float3(0f - num, geometryData.m_Bounds.min.y, 0f - num), position + new float3(num, geometryData.m_Bounds.max.y, num));
		}
		return CalculateBounds(position, rotation, geometryData.m_Bounds);
	}

	public static Bounds3 GetBounds(ObjectGeometryData geometryData)
	{
		Bounds3 bounds = geometryData.m_Bounds;
		if ((geometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
		{
			bounds.min.xz = geometryData.m_Size.xz * -0.5f;
			bounds.max.xz = geometryData.m_Size.xz * 0.5f;
		}
		return bounds;
	}

	public static Bounds3 GetBounds(Stack stack, ObjectGeometryData geometryData, StackData stackData)
	{
		Bounds3 bounds = GetBounds(geometryData);
		switch (stackData.m_Direction)
		{
		case StackDirection.Right:
			bounds.x = stack.m_Range;
			break;
		case StackDirection.Up:
			bounds.y = stack.m_Range;
			break;
		case StackDirection.Forward:
			bounds.z = stack.m_Range;
			break;
		}
		return bounds;
	}

	public static Bounds3 CalculateBounds(float3 position, quaternion rotation, Stack stack, ObjectGeometryData geometryData, StackData stackData)
	{
		Line3.Segment segment = default(Line3.Segment);
		switch (stackData.m_Direction)
		{
		case StackDirection.Right:
			segment.a = LocalToWorld(position, rotation, new float3(stack.m_Range.min, 0f, 0f));
			segment.b = LocalToWorld(position, rotation, new float3(stack.m_Range.max, 0f, 0f));
			break;
		case StackDirection.Up:
			segment.a = LocalToWorld(position, rotation, new float3(0f, stack.m_Range.min, 0f));
			segment.b = LocalToWorld(position, rotation, new float3(0f, stack.m_Range.max, 0f));
			break;
		case StackDirection.Forward:
			segment.a = LocalToWorld(position, rotation, new float3(0f, 0f, stack.m_Range.min));
			segment.b = LocalToWorld(position, rotation, new float3(0f, 0f, stack.m_Range.max));
			break;
		default:
			return CalculateBounds(position, rotation, geometryData);
		}
		if ((geometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
		{
			float num = geometryData.m_Size.x * 0.5f;
			return new Bounds3(MathUtils.Min(segment) + new float3(0f - num, geometryData.m_Bounds.min.y, 0f - num), MathUtils.Max(segment) + new float3(num, geometryData.m_Bounds.max.y, num));
		}
		return CalculateBounds(segment, rotation, geometryData.m_Bounds);
	}

	public static Bounds3 CalculateBounds(float3 position, quaternion rotation, Bounds3 bounds)
	{
		float3 @float = math.mul(rotation, new float3(1f, 0f, 0f));
		float3 float2 = math.mul(rotation, new float3(0f, 1f, 0f));
		float3 float3 = math.mul(rotation, new float3(0f, 0f, 1f));
		float3 x = @float * bounds.min.x;
		float3 y = @float * bounds.max.x;
		float3 x2 = float2 * bounds.min.y;
		float3 y2 = float2 * bounds.max.y;
		float3 x3 = float3 * bounds.min.z;
		float3 y3 = float3 * bounds.max.z;
		return new Bounds3
		{
			min = position + math.min(x, y) + math.min(x2, y2) + math.min(x3, y3),
			max = position + math.max(x, y) + math.max(x2, y2) + math.max(x3, y3)
		};
	}

	public static Bounds3 CalculateBounds(Line3.Segment positionRange, quaternion rotation, Bounds3 bounds)
	{
		float3 @float = math.mul(rotation, new float3(1f, 0f, 0f));
		float3 float2 = math.mul(rotation, new float3(0f, 1f, 0f));
		float3 float3 = math.mul(rotation, new float3(0f, 0f, 1f));
		float3 x = @float * bounds.min.x;
		float3 y = @float * bounds.max.x;
		float3 x2 = float2 * bounds.min.y;
		float3 y2 = float2 * bounds.max.y;
		float3 x3 = float3 * bounds.min.z;
		float3 y3 = float3 * bounds.max.z;
		return new Bounds3
		{
			min = MathUtils.Min(positionRange) + math.min(x, y) + math.min(x2, y2) + math.min(x3, y3),
			max = MathUtils.Max(positionRange) + math.max(x, y) + math.max(x2, y2) + math.max(x3, y3)
		};
	}

	public static Quad3 CalculateBaseCorners(float3 position, quaternion rotation, Bounds3 bounds)
	{
		float3 @float = math.mul(rotation, new float3(0f, 0f, 1f));
		float3 float2 = math.mul(rotation, new float3(1f, 0f, 0f));
		float3 float3 = position + @float * bounds.max.z;
		float3 float4 = position + @float * bounds.min.z;
		float3 float5 = float2 * bounds.max.x;
		float3 float6 = float2 * bounds.min.x;
		return new Quad3(float3 + float6, float3 + float5, float4 + float5, float4 + float6);
	}

	public static Quad3 CalculateBaseCorners(float3 position, quaternion rotation, float2 size)
	{
		size *= 0.5f;
		float3 @float = math.mul(rotation, new float3(0f, 0f, 1f)) * size.y;
		float3 float2 = math.mul(rotation, new float3(1f, 0f, 0f)) * size.x;
		float3 float3 = position + @float;
		float3 float4 = position - @float;
		return new Quad3(float3 - float2, float3 + float2, float4 + float2, float4 - float2);
	}

	public static float3 CalculatePointVelocity(float3 offset, Moving moving)
	{
		return moving.m_Velocity + math.cross(moving.m_AngularVelocity, offset);
	}

	public static float3 CalculateMomentOfInertia(quaternion rotation, float3 size)
	{
		size *= 0.5f;
		size *= size;
		float3 @float = math.abs(math.rotate(rotation, new float3(size.x, 0f, 0f)));
		float3 float2 = math.abs(math.rotate(rotation, new float3(0f, size.y, 0f)));
		float3 float3 = math.abs(math.rotate(rotation, new float3(0f, 0f, size.z)));
		float3 float4 = @float + float2 + float3;
		return float4.yzx + float4.zxy;
	}

	public static Transform InverseTransform(Transform transform)
	{
		Transform result = default(Transform);
		result.m_Position = -transform.m_Position;
		result.m_Rotation = math.inverse(transform.m_Rotation);
		return result;
	}

	public static float3 LocalToWorld(Transform transform, float3 position)
	{
		return transform.m_Position + math.mul(transform.m_Rotation, position);
	}

	public static float3 LocalToWorld(float3 transformPosition, quaternion transformRotation, float3 position)
	{
		return transformPosition + math.mul(transformRotation, position);
	}

	public static Bezier4x3 LocalToWorld(float3 transformPosition, quaternion transformRotation, Bezier4x3 curve)
	{
		Bezier4x3 result = default(Bezier4x3);
		result.a = LocalToWorld(transformPosition, transformRotation, curve.a);
		result.b = LocalToWorld(transformPosition, transformRotation, curve.b);
		result.c = LocalToWorld(transformPosition, transformRotation, curve.c);
		result.d = LocalToWorld(transformPosition, transformRotation, curve.d);
		return result;
	}

	public static Transform LocalToWorld(Transform transform, float3 position, quaternion rotation)
	{
		Transform result = default(Transform);
		result.m_Position = transform.m_Position + math.mul(transform.m_Rotation, position);
		result.m_Rotation = math.mul(transform.m_Rotation, rotation);
		return result;
	}

	public static InterpolatedTransform LocalToWorld(InterpolatedTransform transform, float3 position, quaternion rotation)
	{
		InterpolatedTransform result = transform;
		result.m_Position = transform.m_Position + math.mul(transform.m_Rotation, position);
		result.m_Rotation = math.mul(transform.m_Rotation, rotation);
		return result;
	}

	public static Transform LocalToWorld(Transform parentTransform, Transform transform)
	{
		Transform result = default(Transform);
		result.m_Position = parentTransform.m_Position + math.mul(parentTransform.m_Rotation, transform.m_Position);
		result.m_Rotation = math.mul(parentTransform.m_Rotation, transform.m_Rotation);
		return result;
	}

	public static Transform WorldToLocal(Transform inverseParentTransform, Transform transform)
	{
		Transform result = default(Transform);
		result.m_Position = math.mul(inverseParentTransform.m_Rotation, transform.m_Position + inverseParentTransform.m_Position);
		result.m_Rotation = math.mul(inverseParentTransform.m_Rotation, transform.m_Rotation);
		return result;
	}

	public static float3 WorldToLocal(Transform inverseParentTransform, float3 position)
	{
		return math.mul(inverseParentTransform.m_Rotation, position + inverseParentTransform.m_Position);
	}

	public static CollisionMask GetCollisionMask(ObjectGeometryData geometryData, Elevation elevation, bool ignoreMarkers)
	{
		if ((geometryData.m_Flags & GeometryFlags.Marker) != 0 && ignoreMarkers)
		{
			return (CollisionMask)0;
		}
		CollisionMask collisionMask = (CollisionMask)0;
		if ((geometryData.m_Flags & GeometryFlags.ExclusiveGround) != GeometryFlags.None)
		{
			collisionMask |= CollisionMask.OnGround | CollisionMask.ExclusiveGround;
		}
		if (elevation.m_Elevation < 0f)
		{
			collisionMask |= CollisionMask.Underground;
			if ((elevation.m_Flags & ElevationFlags.Lowered) != 0)
			{
				collisionMask |= CollisionMask.Overground;
			}
		}
		else
		{
			collisionMask |= CollisionMask.Overground;
		}
		return collisionMask;
	}

	public static CollisionMask GetCollisionMask(ObjectGeometryData geometryData, bool ignoreMarkers)
	{
		if ((geometryData.m_Flags & GeometryFlags.Marker) != 0 && ignoreMarkers)
		{
			return (CollisionMask)0;
		}
		CollisionMask collisionMask = (CollisionMask)0;
		if ((geometryData.m_Flags & (GeometryFlags.ExclusiveGround | GeometryFlags.BaseCollision)) != GeometryFlags.None)
		{
			collisionMask |= CollisionMask.ExclusiveGround;
		}
		return collisionMask | (CollisionMask.OnGround | CollisionMask.Overground);
	}

	public static int GetContructionCost(int constructionCost, Tree tree, in EconomyParameterData economyParameterData)
	{
		return (tree.m_State & (TreeState.Teen | TreeState.Adult | TreeState.Elderly | TreeState.Dead | TreeState.Stump)) switch
		{
			TreeState.Teen => constructionCost * economyParameterData.m_TreeCostMultipliers.x, 
			TreeState.Adult => constructionCost * economyParameterData.m_TreeCostMultipliers.y, 
			TreeState.Elderly => constructionCost * economyParameterData.m_TreeCostMultipliers.z, 
			_ => constructionCost, 
		};
	}

	public static int GetRelocationCost(int constructionCost, EconomyParameterData economyParameterData)
	{
		int num = (constructionCost + 1000) / 2000 * 500;
		num = (int)((float)math.select(num, 500, num == 0 && constructionCost > 0) * economyParameterData.m_RelocationCostMultiplier);
		return math.min(num, constructionCost);
	}

	public static int GetRelocationCost(int constructionCost, Recent recent, uint simulationFrame, EconomyParameterData economyParameterData)
	{
		int refundAmount = GetRefundAmount(recent, simulationFrame, economyParameterData);
		constructionCost = math.max(constructionCost / 4, constructionCost - refundAmount);
		return GetRelocationCost(constructionCost, economyParameterData);
	}

	public static int GetRebuildCost(int constructionCost)
	{
		int num = (constructionCost + 500) / 1000 * 500;
		num = math.select(num, 500, num == 0 && constructionCost > 0);
		return math.min(num, constructionCost);
	}

	public static int GetRebuildCost(int constructionCost, Recent recent, uint simulationFrame, EconomyParameterData economyParameterData)
	{
		int refundAmount = GetRefundAmount(recent, simulationFrame, economyParameterData);
		constructionCost = math.max(constructionCost / 4, constructionCost - refundAmount);
		return GetRebuildCost(constructionCost);
	}

	public static int GetUpgradeCost(int constructionCost, int originalCost)
	{
		return math.max(0, constructionCost - originalCost);
	}

	public static int GetUpgradeCost(int constructionCost, int originalCost, Recent recent, uint simulationFrame, EconomyParameterData economyParameterData)
	{
		if (constructionCost >= originalCost)
		{
			return GetUpgradeCost(constructionCost, originalCost);
		}
		recent.m_ModificationCost = math.min(recent.m_ModificationCost, originalCost - constructionCost);
		return -GetRefundAmount(recent, simulationFrame, economyParameterData);
	}

	public static int GetRefundAmount(Recent recent, uint simulationFrame, EconomyParameterData economyParameterData)
	{
		if ((float)simulationFrame < (float)recent.m_ModificationFrame + 262144f * economyParameterData.m_BuildRefundTimeRange.x)
		{
			return (int)((float)recent.m_ModificationCost * economyParameterData.m_BuildRefundPercentage.x);
		}
		if ((float)simulationFrame < (float)recent.m_ModificationFrame + 262144f * economyParameterData.m_BuildRefundTimeRange.y)
		{
			return (int)((float)recent.m_ModificationCost * economyParameterData.m_BuildRefundPercentage.y);
		}
		if ((float)simulationFrame < (float)recent.m_ModificationFrame + 262144f * economyParameterData.m_BuildRefundTimeRange.z)
		{
			return (int)((float)recent.m_ModificationCost * economyParameterData.m_BuildRefundPercentage.z);
		}
		return 0;
	}

	public static float CalculateWoodAmount(Tree tree, Plant plant, Damaged damaged, TreeData treeData)
	{
		float num = 0f;
		switch (tree.m_State & (TreeState.Teen | TreeState.Adult | TreeState.Elderly | TreeState.Dead | TreeState.Stump))
		{
		case TreeState.Teen:
			num = math.lerp(0.2f, 0.7f, (float)(int)tree.m_Growth * 0.00390625f) * treeData.m_WoodAmount;
			break;
		case TreeState.Adult:
			num = math.lerp(0.7f, 1f, (float)(int)tree.m_Growth * 0.00390625f) * treeData.m_WoodAmount;
			break;
		case TreeState.Elderly:
			num = treeData.m_WoodAmount;
			break;
		case TreeState.Dead:
		case TreeState.Stump:
			return 0f;
		default:
			num = math.lerp(0f, 0.2f, (float)(int)tree.m_Growth * 0.00390625f) * treeData.m_WoodAmount;
			break;
		}
		return num * (1f - plant.m_Pollution) * (1f - GetTotalDamage(damaged));
	}

	public static float CalculateGrowthRate(Tree tree, Plant plant, TreeData treeData)
	{
		float num = 0f;
		switch (tree.m_State & (TreeState.Teen | TreeState.Adult | TreeState.Elderly | TreeState.Dead | TreeState.Stump))
		{
		case TreeState.Teen:
			num = 0.025f * treeData.m_WoodAmount;
			break;
		case TreeState.Adult:
			num = 3f / 140f * treeData.m_WoodAmount;
			break;
		case TreeState.Elderly:
		case TreeState.Dead:
		case TreeState.Stump:
			return 0f;
		default:
			num = 0.05f * treeData.m_WoodAmount;
			break;
		}
		return num * (1f - plant.m_Pollution);
	}

	public static Tree InitializeTreeState(float age)
	{
		Tree result = default(Tree);
		if (age < 0.1f)
		{
			result.m_Growth = (byte)math.clamp(Mathf.FloorToInt(age * 2560f), 0, 255);
		}
		else if (age < 0.25f)
		{
			result.m_State = TreeState.Teen;
			result.m_Growth = (byte)math.clamp(Mathf.FloorToInt((age - 0.1f) * 1706.6666f), 0, 255);
		}
		else if (age < 0.6f)
		{
			result.m_State = TreeState.Adult;
			result.m_Growth = (byte)math.clamp(Mathf.FloorToInt((age - 0.25f) * 731.4286f), 0, 255);
		}
		else if (age < 0.95000005f)
		{
			result.m_State = TreeState.Elderly;
			result.m_Growth = (byte)math.clamp(Mathf.FloorToInt((age - 0.6f) * 731.4286f), 0, 255);
		}
		else
		{
			result.m_State = TreeState.Dead;
			result.m_Growth = (byte)math.clamp(Mathf.FloorToInt((age - 0.95f) * 5120f), 0, 255);
		}
		return result;
	}

	public static void UpdateAnimation(Entity prefab, float timeStep, PseudoRandomSeed pseudoRandomSeed, DynamicBuffer<MeshGroup> meshGroups, ref BufferLookup<SubMeshGroup> subMeshGroupBuffers, ref BufferLookup<CharacterElement> characterElementBuffers, ref BufferLookup<SubMesh> subMeshBuffers, ref BufferLookup<AnimationClip> animationClipBuffers, ref BufferLookup<AnimationMotion> animationMotionBuffers, AnimatedPropID oldPropID, AnimatedPropID newPropID, ActivityCondition conditions, ref float maxSpeed, ref byte activity, ref float3 targetPosition, ref float3 targetDirection, ref Transform transform, ref TransformFrame oldFrameData, ref TransformFrame newFrameData)
	{
		bool flag = newFrameData.m_Activity == 0;
		bool flag2 = oldFrameData.m_Activity == 0;
		if (oldFrameData.m_Activity != newFrameData.m_Activity)
		{
			byte parentActivity = GetParentActivity(oldFrameData.m_Activity);
			byte parentActivity2 = GetParentActivity(newFrameData.m_Activity);
			if (parentActivity != 0)
			{
				if (parentActivity != newFrameData.m_Activity)
				{
					newFrameData.m_Activity = parentActivity;
				}
				else
				{
					flag = true;
				}
			}
			else if (parentActivity2 != 0)
			{
				if (parentActivity2 != oldFrameData.m_Activity)
				{
					newFrameData.m_Activity = parentActivity2;
				}
				else
				{
					flag2 = true;
				}
			}
		}
		CharacterElement characterElement;
		AnimationClip animationClip;
		bool crossFade;
		float stateDuration;
		switch (oldFrameData.m_State)
		{
		case TransformState.Default:
			flag2 = true;
			break;
		case TransformState.Idle:
			if (newFrameData.m_State == TransformState.Idle && newFrameData.m_Activity == oldFrameData.m_Activity)
			{
				newFrameData.m_StateTimer = (ushort)(oldFrameData.m_StateTimer + 1);
				return;
			}
			stateDuration = GetStateDuration(prefab, TransformState.Idle, oldFrameData.m_Activity, pseudoRandomSeed, oldPropID, conditions, meshGroups, ref subMeshGroupBuffers, ref characterElementBuffers, ref subMeshBuffers, ref animationClipBuffers, out characterElement, out animationClip, out crossFade);
			if (animationClip.m_Playback != AnimationPlayback.RandomLoop && stateDuration > 0f)
			{
				float2 @float = default(float2);
				@float.x = (float)(int)oldFrameData.m_StateTimer * timeStep;
				@float.y = @float.x + timeStep;
				@float = math.floor(@float / math.select(stateDuration, stateDuration * 0.5f, animationClip.m_Playback == AnimationPlayback.HalfLoop));
				if (@float.x > @float.y - 0.5f)
				{
					newFrameData.m_State = TransformState.Idle;
					newFrameData.m_Activity = oldFrameData.m_Activity;
					newFrameData.m_StateTimer = (ushort)(oldFrameData.m_StateTimer + 1);
					maxSpeed = 0f;
					return;
				}
			}
			break;
		case TransformState.Move:
			if (newFrameData.m_State == TransformState.Move)
			{
				newFrameData.m_StateTimer = (ushort)(oldFrameData.m_StateTimer + 1);
				return;
			}
			break;
		case TransformState.Start:
		{
			stateDuration = GetStateDuration(prefab, TransformState.Start, oldFrameData.m_Activity, pseudoRandomSeed, oldPropID, conditions, meshGroups, ref subMeshGroupBuffers, ref characterElementBuffers, ref subMeshBuffers, ref animationClipBuffers, out characterElement, out animationClip, out crossFade);
			float2 x2 = default(float2);
			x2.x = (float)(int)oldFrameData.m_StateTimer * timeStep;
			x2.y = x2.x + timeStep;
			x2 = math.min(x2, stateDuration);
			if (animationClip.m_MotionRange.y != animationClip.m_MotionRange.x && x2.y > x2.x && stateDuration > 0f)
			{
				DynamicBuffer<AnimationMotion> motions2 = animationMotionBuffers[characterElement.m_Style];
				ApplyRootMotion(ref transform, ref newFrameData, motions2, characterElement.m_ShapeWeights, animationClip.m_MotionRange, new float3(x2, 1f) / stateDuration);
				targetPosition = transform.m_Position;
				targetDirection = math.forward(transform.m_Rotation);
			}
			if (x2.y < stateDuration)
			{
				newFrameData.m_State = TransformState.Start;
				newFrameData.m_Activity = oldFrameData.m_Activity;
				newFrameData.m_StateTimer = (ushort)(oldFrameData.m_StateTimer + 1);
				maxSpeed = 0f;
				return;
			}
			if (newFrameData.m_Activity == oldFrameData.m_Activity)
			{
				return;
			}
			break;
		}
		case TransformState.End:
		{
			stateDuration = GetStateDuration(prefab, TransformState.End, oldFrameData.m_Activity, pseudoRandomSeed, oldPropID, conditions, meshGroups, ref subMeshGroupBuffers, ref characterElementBuffers, ref subMeshBuffers, ref animationClipBuffers, out characterElement, out animationClip, out crossFade);
			float2 x3 = default(float2);
			x3.x = (float)(int)oldFrameData.m_StateTimer * timeStep;
			x3.y = x3.x + timeStep;
			x3 = math.min(x3, stateDuration);
			if (animationClip.m_MotionRange.y != animationClip.m_MotionRange.x && x3.y > x3.x && stateDuration > 0f)
			{
				DynamicBuffer<AnimationMotion> motions3 = animationMotionBuffers[characterElement.m_Style];
				ApplyRootMotion(ref transform, ref newFrameData, motions3, characterElement.m_ShapeWeights, animationClip.m_MotionRange, new float3(x3, 1f) / stateDuration);
				targetPosition = transform.m_Position;
				targetDirection = math.forward(transform.m_Rotation);
			}
			if (x3.y < stateDuration)
			{
				newFrameData.m_State = TransformState.End;
				newFrameData.m_Activity = oldFrameData.m_Activity;
				newFrameData.m_StateTimer = (ushort)(oldFrameData.m_StateTimer + 1);
				maxSpeed = 0f;
				return;
			}
			flag2 = true;
			break;
		}
		case TransformState.Action:
		case TransformState.Done:
		{
			stateDuration = GetStateDuration(prefab, TransformState.Action, oldFrameData.m_Activity, pseudoRandomSeed, oldPropID, conditions, meshGroups, ref subMeshGroupBuffers, ref characterElementBuffers, ref subMeshBuffers, ref animationClipBuffers, out characterElement, out animationClip, out crossFade);
			float2 x = default(float2);
			x.x = (float)(int)oldFrameData.m_StateTimer * timeStep;
			x.y = x.x + timeStep;
			x = math.min(x, stateDuration);
			if (animationClip.m_MotionRange.y != animationClip.m_MotionRange.x && x.y > x.x && stateDuration > 0f)
			{
				DynamicBuffer<AnimationMotion> motions = animationMotionBuffers[characterElement.m_Style];
				ApplyRootMotion(ref transform, ref newFrameData, motions, characterElement.m_ShapeWeights, animationClip.m_MotionRange, new float3(x, 1f) / stateDuration);
				if (animationClip.m_Playback != AnimationPlayback.OptionalOnce)
				{
					targetPosition = transform.m_Position;
					targetDirection = math.forward(transform.m_Rotation);
				}
			}
			if (animationClip.m_Playback != AnimationPlayback.OptionalOnce || maxSpeed < 0.1f)
			{
				if (x.y < stateDuration)
				{
					newFrameData.m_State = TransformState.Action;
					newFrameData.m_Activity = oldFrameData.m_Activity;
					newFrameData.m_StateTimer = (ushort)(oldFrameData.m_StateTimer + 1);
					maxSpeed = 0f;
					return;
				}
				if (newFrameData.m_Activity == oldFrameData.m_Activity && newFrameData.m_Activity == 10)
				{
					newFrameData.m_State = TransformState.Done;
					newFrameData.m_Activity = oldFrameData.m_Activity;
					newFrameData.m_StateTimer = (ushort)math.min(65535, oldFrameData.m_StateTimer + 1);
					maxSpeed = 0f;
					return;
				}
			}
			if (newFrameData.m_Activity == oldFrameData.m_Activity)
			{
				targetDirection = default(float3);
				activity = 0;
				newFrameData.m_Activity = activity;
			}
			flag2 = true;
			break;
		}
		}
		if (!flag2 && (stateDuration = GetStateDuration(prefab, TransformState.End, oldFrameData.m_Activity, pseudoRandomSeed, oldPropID, conditions, meshGroups, ref subMeshGroupBuffers, ref characterElementBuffers, ref subMeshBuffers, ref animationClipBuffers, out characterElement, out animationClip, out var crossFade2)) > 0f)
		{
			newFrameData.m_State = TransformState.End;
			newFrameData.m_Activity = oldFrameData.m_Activity;
		}
		else if (!flag && (stateDuration = GetStateDuration(prefab, TransformState.Start, newFrameData.m_Activity, pseudoRandomSeed, newPropID, conditions, meshGroups, ref subMeshGroupBuffers, ref characterElementBuffers, ref subMeshBuffers, ref animationClipBuffers, out characterElement, out animationClip, out crossFade2)) > 0f)
		{
			newFrameData.m_State = TransformState.Start;
		}
		else
		{
			if (!((stateDuration = GetStateDuration(prefab, TransformState.Action, newFrameData.m_Activity, pseudoRandomSeed, newPropID, conditions, meshGroups, ref subMeshGroupBuffers, ref characterElementBuffers, ref subMeshBuffers, ref animationClipBuffers, out characterElement, out animationClip, out crossFade2)) > 0f))
			{
				return;
			}
			newFrameData.m_State = TransformState.Action;
		}
		newFrameData.m_StateTimer = (ushort)math.select(0, 1, crossFade2);
		maxSpeed = 0f;
		if (crossFade2 && animationClip.m_MotionRange.y != animationClip.m_MotionRange.x && stateDuration > 0f)
		{
			DynamicBuffer<AnimationMotion> motions4 = animationMotionBuffers[characterElement.m_Style];
			ApplyRootMotion(ref transform, ref newFrameData, motions4, characterElement.m_ShapeWeights, animationClip.m_MotionRange, new float3(0f, timeStep, 1f) / stateDuration);
			if (newFrameData.m_State != TransformState.Action || animationClip.m_Playback != AnimationPlayback.OptionalOnce)
			{
				targetPosition = transform.m_Position;
				targetDirection = math.forward(transform.m_Rotation);
			}
		}
	}

	public static Transform GetActivityStartPosition(Entity prefab, DynamicBuffer<MeshGroup> meshGroups, Transform activityTransform, TransformState state, ActivityType activityType, PseudoRandomSeed pseudoRandomSeed, AnimatedPropID propID, ActivityCondition conditions, ref BufferLookup<SubMeshGroup> subMeshGroupBuffers, ref BufferLookup<CharacterElement> characterElementBuffers, ref BufferLookup<SubMesh> subMeshBuffers, ref BufferLookup<AnimationClip> animationClipBuffers, ref BufferLookup<AnimationMotion> animationMotionBuffers, ref ActivityStartPositionCache cache)
	{
		if (activityType != cache.m_ActivityType)
		{
			cache.m_ActivityType = activityType;
			CharacterElement characterElement;
			AnimationClip animationClip;
			bool crossFade;
			float stateDuration = GetStateDuration(prefab, state, (byte)activityType, pseudoRandomSeed, propID, conditions, meshGroups, ref subMeshGroupBuffers, ref characterElementBuffers, ref subMeshBuffers, ref animationClipBuffers, out characterElement, out animationClip, out crossFade);
			if (animationClip.m_MotionRange.y != animationClip.m_MotionRange.x && stateDuration > 0f)
			{
				DynamicBuffer<AnimationMotion> motions = animationMotionBuffers[characterElement.m_Style];
				GetRootMotion(motions, animationClip.m_MotionRange, characterElement.m_ShapeWeights, 0f, out var rootOffset, out var rootVelocity, out var rootRotation);
				GetRootMotion(motions, animationClip.m_MotionRange, characterElement.m_ShapeWeights, 1f, out var rootOffset2, out rootVelocity, out var rootRotation2);
				cache.m_RotationOffset = math.inverse(rootRotation2);
				cache.m_PositionOffset = math.mul(cache.m_RotationOffset, rootOffset - rootOffset2);
				cache.m_RotationOffset = math.mul(cache.m_RotationOffset, rootRotation);
			}
			else
			{
				cache.m_PositionOffset = default(float3);
				cache.m_RotationOffset = quaternion.identity;
			}
		}
		if (cache.m_ActivityType != ActivityType.None)
		{
			return LocalToWorld(activityTransform, cache.m_PositionOffset, cache.m_RotationOffset);
		}
		return activityTransform;
	}

	public static byte GetParentActivity(byte activity)
	{
		return (ActivityType)activity switch
		{
			ActivityType.GroundLaying => 5, 
			ActivityType.Reading => 4, 
			_ => 0, 
		};
	}

	private static void ApplyRootMotion(ref Transform transform, ref TransformFrame newFrameData, DynamicBuffer<AnimationMotion> motions, Game.Rendering.BlendWeights weights, int2 motionRange, float3 deltaRange)
	{
		GetRootMotion(motions, motionRange, weights, deltaRange.x, out var rootOffset, out var _, out var rootRotation);
		GetRootMotion(motions, motionRange, weights, deltaRange.y, out var rootOffset2, out var rootVelocity2, out var rootRotation2);
		transform.m_Rotation = math.mul(transform.m_Rotation, math.inverse(rootRotation));
		transform.m_Position += math.mul(transform.m_Rotation, rootOffset2 - rootOffset);
		newFrameData.m_Velocity += math.mul(transform.m_Rotation, rootVelocity2 * deltaRange.z);
		transform.m_Rotation = math.normalize(math.mul(transform.m_Rotation, rootRotation2));
	}

	public static float GetStateDuration(Entity prefab, TransformState state, byte activity, PseudoRandomSeed pseudoRandomSeed, AnimatedPropID propID, ActivityCondition conditions, DynamicBuffer<MeshGroup> meshGroups, ref BufferLookup<SubMeshGroup> subMeshGroupBuffers, ref BufferLookup<CharacterElement> characterElementBuffers, ref BufferLookup<SubMesh> subMeshBuffers, ref BufferLookup<AnimationClip> animationClipBuffers, out CharacterElement characterElement, out AnimationClip animationClip, out bool crossFade)
	{
		characterElement = default(CharacterElement);
		animationClip = default(AnimationClip);
		animationClip.m_PropID = AnimatedPropID.None;
		crossFade = false;
		AnimationType animationType;
		switch (state)
		{
		case TransformState.Idle:
			animationType = AnimationType.Idle;
			break;
		case TransformState.Start:
			animationType = AnimationType.Start;
			break;
		case TransformState.End:
			animationType = AnimationType.End;
			break;
		case TransformState.Action:
			animationType = AnimationType.Action;
			break;
		case TransformState.Done:
			animationType = AnimationType.Action;
			break;
		default:
			return 0f;
		}
		float num = 0f;
		int num2 = 0;
		DynamicBuffer<CharacterElement> bufferData = default(DynamicBuffer<CharacterElement>);
		DynamicBuffer<SubMesh> dynamicBuffer = default(DynamicBuffer<SubMesh>);
		if (subMeshGroupBuffers.TryGetBuffer(prefab, out var bufferData2))
		{
			if (meshGroups.IsCreated)
			{
				num2 = meshGroups.Length;
			}
			crossFade = characterElementBuffers.TryGetBuffer(prefab, out bufferData);
		}
		else
		{
			dynamicBuffer = subMeshBuffers[prefab];
			num2 = dynamicBuffer.Length;
		}
		for (int i = 0; i < num2; i++)
		{
			CharacterElement characterElement2 = default(CharacterElement);
			if (bufferData.IsCreated)
			{
				CollectionUtils.TryGet(meshGroups, i, out var value);
				characterElement2 = bufferData[value.m_SubMeshGroup];
			}
			else
			{
				int index = i;
				if (bufferData2.IsCreated)
				{
					CollectionUtils.TryGet(meshGroups, i, out var value2);
					index = bufferData2[value2.m_SubMeshGroup].m_SubMeshRange.x;
				}
				characterElement2.m_Style = dynamicBuffer[index].m_SubMesh;
			}
			if (!animationClipBuffers.TryGetBuffer(characterElement2.m_Style, out var bufferData3))
			{
				continue;
			}
			int num3 = int.MaxValue;
			float y = 0f;
			for (int j = 0; j < bufferData3.Length; j++)
			{
				AnimationClip animationClip2 = bufferData3[j];
				if (animationClip2.m_Type == animationType && animationClip2.m_Activity == (ActivityType)activity && animationClip2.m_Layer == AnimationLayer.Body && (animationClip2.m_PropID == propID || propID == AnimatedPropID.Any) && (!(propID == AnimatedPropID.Any) || animationClip2.m_VariationCount <= 1 || pseudoRandomSeed.GetRandom((uint)(PseudoRandomSeed.kAnimationVariation ^ activity)).NextInt(animationClip2.m_VariationCount) == animationClip2.m_VariationIndex))
				{
					ActivityCondition activityCondition = animationClip2.m_Conditions ^ conditions;
					if (activityCondition == (ActivityCondition)0u)
					{
						y = animationClip2.m_AnimationLength;
						characterElement = characterElement2;
						animationClip = animationClip2;
						break;
					}
					int num4 = math.countbits((uint)activityCondition);
					if (num4 < num3)
					{
						num3 = num4;
						y = animationClip2.m_AnimationLength;
						characterElement = characterElement2;
						animationClip = animationClip2;
					}
				}
			}
			num = math.max(num, y);
		}
		return num;
	}

	public static void GetRootMotion(DynamicBuffer<AnimationMotion> motions, int2 range, Game.Rendering.BlendWeights weights, float t, out float3 rootOffset, out float3 rootVelocity, out quaternion rootRotation)
	{
		if (range.y == range.x + 1)
		{
			GetRootMotion(motions[range.x], t, out rootOffset, out rootVelocity, out rootRotation);
			return;
		}
		GetRootMotion(motions[range.x], t, out var rootOffset2, out var rootVelocity2, out var rootRotation2);
		GetRootMotion(motions[range.x + weights.m_Weight0.m_Index + 1], t, out var rootOffset3, out var rootVelocity3, out var rootRotation3);
		GetRootMotion(motions[range.x + weights.m_Weight1.m_Index + 1], t, out var rootOffset4, out var rootVelocity4, out var rootRotation4);
		GetRootMotion(motions[range.x + weights.m_Weight2.m_Index + 1], t, out var rootOffset5, out var rootVelocity5, out var rootRotation5);
		GetRootMotion(motions[range.x + weights.m_Weight3.m_Index + 1], t, out var rootOffset6, out var rootVelocity6, out var rootRotation6);
		GetRootMotion(motions[range.x + weights.m_Weight4.m_Index + 1], t, out var rootOffset7, out var rootVelocity7, out var rootRotation7);
		GetRootMotion(motions[range.x + weights.m_Weight5.m_Index + 1], t, out var rootOffset8, out var rootVelocity8, out var rootRotation8);
		GetRootMotion(motions[range.x + weights.m_Weight6.m_Index + 1], t, out var rootOffset9, out var rootVelocity9, out var rootRotation9);
		GetRootMotion(motions[range.x + weights.m_Weight7.m_Index + 1], t, out var rootOffset10, out var rootVelocity10, out var rootRotation10);
		rootOffset3 *= weights.m_Weight0.m_Weight;
		rootOffset4 *= weights.m_Weight1.m_Weight;
		rootOffset5 *= weights.m_Weight2.m_Weight;
		rootOffset6 *= weights.m_Weight3.m_Weight;
		rootOffset7 *= weights.m_Weight4.m_Weight;
		rootOffset8 *= weights.m_Weight5.m_Weight;
		rootOffset9 *= weights.m_Weight6.m_Weight;
		rootOffset10 *= weights.m_Weight7.m_Weight;
		rootVelocity3 *= weights.m_Weight0.m_Weight;
		rootVelocity4 *= weights.m_Weight1.m_Weight;
		rootVelocity5 *= weights.m_Weight2.m_Weight;
		rootVelocity6 *= weights.m_Weight3.m_Weight;
		rootVelocity7 *= weights.m_Weight4.m_Weight;
		rootVelocity8 *= weights.m_Weight5.m_Weight;
		rootVelocity9 *= weights.m_Weight6.m_Weight;
		rootVelocity10 *= weights.m_Weight7.m_Weight;
		rootOffset = rootOffset2 + rootOffset3 + rootOffset4 + rootOffset5 + rootOffset6 + rootOffset7 + rootOffset8 + rootOffset9 + rootOffset10;
		rootVelocity = rootVelocity2 + rootVelocity3 + rootVelocity4 + rootVelocity5 + rootVelocity6 + rootVelocity7 + rootVelocity8 + rootVelocity9 + rootVelocity10;
		rootRotation3 = math.slerp(quaternion.identity, rootRotation3, weights.m_Weight0.m_Weight);
		rootRotation4 = math.slerp(quaternion.identity, rootRotation4, weights.m_Weight1.m_Weight);
		rootRotation5 = math.slerp(quaternion.identity, rootRotation5, weights.m_Weight2.m_Weight);
		rootRotation6 = math.slerp(quaternion.identity, rootRotation6, weights.m_Weight3.m_Weight);
		rootRotation7 = math.slerp(quaternion.identity, rootRotation7, weights.m_Weight4.m_Weight);
		rootRotation8 = math.slerp(quaternion.identity, rootRotation8, weights.m_Weight5.m_Weight);
		rootRotation9 = math.slerp(quaternion.identity, rootRotation9, weights.m_Weight6.m_Weight);
		rootRotation10 = math.slerp(quaternion.identity, rootRotation10, weights.m_Weight7.m_Weight);
		rootRotation = math.mul(rootRotation10, math.mul(rootRotation9, math.mul(rootRotation8, math.mul(rootRotation7, math.mul(rootRotation6, math.mul(rootRotation5, math.mul(rootRotation4, math.mul(rootRotation3, rootRotation2))))))));
	}

	private static void GetRootMotion(AnimationMotion motion, float t, out float3 rootOffset, out float3 rootVelocity, out quaternion rootRotation)
	{
		Bezier4x3 curve = new Bezier4x3(motion.m_StartOffset, motion.m_StartOffset, motion.m_EndOffset, motion.m_EndOffset);
		rootOffset = MathUtils.Position(curve, t);
		rootVelocity = MathUtils.Tangent(curve, t);
		rootRotation = math.slerp(motion.m_StartRotation, motion.m_EndRotation, t);
	}

	public static float GetTotalDamage(Damaged damaged)
	{
		float3 damage = damaged.m_Damage;
		damage.z = math.max(0f, damage.z - math.min(0.5f, math.csum(damage.xy)));
		return math.min(1f, math.csum(damage));
	}

	public static void UpdateResourcesDamage(Entity entity, float totalDamage, ref BufferLookup<Renter> renterData, ref BufferLookup<Game.Economy.Resources> resourcesData)
	{
		if (!renterData.TryGetBuffer(entity, out var bufferData))
		{
			return;
		}
		for (int i = 0; i < bufferData.Length; i++)
		{
			if (!resourcesData.TryGetBuffer(bufferData[i].m_Renter, out var bufferData2))
			{
				continue;
			}
			for (int j = 0; j < bufferData2.Length; j++)
			{
				Game.Economy.Resources value = bufferData2[j];
				if (value.m_Resource != Resource.Money)
				{
					value.m_Amount = (int)((float)value.m_Amount * (1f - totalDamage));
				}
				bufferData2[j] = value;
			}
		}
	}

	public static Transform AdjustPosition(Transform transform, ref Elevation elevation, Entity prefab, out bool angledSample, ref TerrainHeightData terrainHeightData, ref WaterSurfaceData<SurfaceWater> waterSurfaceData, ref ComponentLookup<PlaceableObjectData> placeableObjectDatas, ref ComponentLookup<ObjectGeometryData> objectGeometryDatas)
	{
		Transform result = transform;
		float num = 0f;
		float num2 = 0f;
		angledSample = true;
		if (placeableObjectDatas.TryGetComponent(prefab, out var componentData))
		{
			if ((componentData.m_Flags & PlacementFlags.Hovering) != PlacementFlags.None)
			{
				result.m_Position.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, transform.m_Position);
				result.m_Position.y += componentData.m_PlacementOffset.y;
				angledSample = false;
			}
			else if ((componentData.m_Flags & (PlacementFlags.Shoreline | PlacementFlags.Floating)) != PlacementFlags.None)
			{
				WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, transform.m_Position, out result.m_Position.y, out var waterHeight, out var waterDepth);
				elevation.m_Elevation = math.select(elevation.m_Elevation, 0f, (componentData.m_Flags & PlacementFlags.Floating) != 0);
				if (waterDepth >= 0.2f)
				{
					float y = result.m_Position.y;
					result.m_Position.y = math.max(result.m_Position.y, waterHeight + componentData.m_PlacementOffset.y);
					if ((componentData.m_Flags & PlacementFlags.Floating) != PlacementFlags.None)
					{
						num2 = math.max(0f, result.m_Position.y - y);
					}
				}
				angledSample = false;
			}
			else
			{
				num = componentData.m_PlacementOffset.y;
			}
		}
		if (angledSample)
		{
			if (objectGeometryDatas.TryGetComponent(prefab, out var componentData2) && (componentData2.m_Flags & (GeometryFlags.Standing | GeometryFlags.HasBase)) != GeometryFlags.Standing)
			{
				float3 x = math.forward(transform.m_Rotation);
				x.y = 0f;
				x = math.normalizesafe(x, math.forward());
				float3 @float = new float3
				{
					xz = MathUtils.Right(x.xz)
				};
				float4 x2 = default(float4);
				x2.x = TerrainUtils.SampleHeight(ref terrainHeightData, transform.m_Position + @float * componentData2.m_Bounds.min.x + x * componentData2.m_Bounds.min.z);
				x2.y = TerrainUtils.SampleHeight(ref terrainHeightData, transform.m_Position + @float * componentData2.m_Bounds.min.x + x * componentData2.m_Bounds.max.z);
				x2.z = TerrainUtils.SampleHeight(ref terrainHeightData, transform.m_Position + @float * componentData2.m_Bounds.max.x + x * componentData2.m_Bounds.max.z);
				x2.w = TerrainUtils.SampleHeight(ref terrainHeightData, transform.m_Position + @float * componentData2.m_Bounds.max.x + x * componentData2.m_Bounds.min.z);
				if ((componentData2.m_Flags & GeometryFlags.HasBase) != GeometryFlags.None)
				{
					result.m_Position.y = math.cmax(x2);
				}
				else
				{
					float4 float2 = x2.wzyz - x2.xyxw;
					float2.xy = (float2.xz + float2.yw) / (2f * math.max(0.01f, MathUtils.Size(componentData2.m_Bounds.xz)));
					@float.y = float2.x;
					x.y = float2.y;
					x = math.normalizesafe(x, math.forward());
					float3 up = math.normalizesafe(math.cross(x, @float), math.up());
					result.m_Rotation = quaternion.LookRotationSafe(x, up);
					result.m_Position.y = math.csum(x2) * 0.25f;
				}
			}
			else
			{
				result.m_Position.y = TerrainUtils.SampleHeight(ref terrainHeightData, transform.m_Position);
				angledSample = false;
			}
			result.m_Position.y += num;
		}
		result.m_Position.y += elevation.m_Elevation;
		elevation.m_Elevation += num2;
		return result;
	}

	public static int GetSubParentMesh(ElevationFlags elevationFlags)
	{
		return (elevationFlags & (ElevationFlags.Stacked | ElevationFlags.OnGround)) switch
		{
			ElevationFlags.OnGround => -2, 
			ElevationFlags.Stacked => 1000, 
			ElevationFlags.Stacked | ElevationFlags.OnGround => -1001, 
			_ => 0, 
		};
	}

	public static float GetAttachedParentHeight(EdgeGeometry edgeGeometry, Transform transform)
	{
		float height = transform.m_Position.y;
		float bestDistance = float.MaxValue;
		GetAttachedParentHeight(edgeGeometry.m_Start.m_Left, transform, ref height, ref bestDistance);
		GetAttachedParentHeight(edgeGeometry.m_Start.m_Right, transform, ref height, ref bestDistance);
		GetAttachedParentHeight(edgeGeometry.m_End.m_Left, transform, ref height, ref bestDistance);
		GetAttachedParentHeight(edgeGeometry.m_End.m_Right, transform, ref height, ref bestDistance);
		return height;
	}

	private static void GetAttachedParentHeight(Bezier4x3 curve, Transform transform, ref float height, ref float bestDistance)
	{
		float t;
		float num = MathUtils.Distance(curve.xz, transform.m_Position.xz, out t);
		if (num < bestDistance)
		{
			height = MathUtils.Position(curve.y, t);
			bestDistance = num;
		}
	}

	public static float GetTerrainSmoothingWidth(ObjectGeometryData objectGeometryData)
	{
		return GetTerrainSmoothingWidth(MathUtils.Size(objectGeometryData.m_Bounds.xz));
	}

	public static float GetTerrainSmoothingWidth(float2 size)
	{
		return math.max(8f, math.length(size) * (1f / 12f));
	}

	public static uint GetRemainingConstructionFrames(UnderConstruction underConstruction)
	{
		return (uint)(math.clamp(100 - underConstruction.m_Progress, 0, 100) * (int)(8192u / (uint)math.max(1, underConstruction.m_Speed)) + 64);
	}

	public static uint GetTripDelayFrames(UnderConstruction underConstruction, PathInformation pathInformation)
	{
		uint remainingConstructionFrames = GetRemainingConstructionFrames(underConstruction);
		uint num = (uint)(pathInformation.m_Duration * 60f + 0.5f);
		return math.select(remainingConstructionFrames - num, 0u, num > remainingConstructionFrames);
	}

	public static bool GetStandingLegCount(ObjectGeometryData objectGeometryData, out int legCount)
	{
		bool3 test = new bool3
		{
			x = ((objectGeometryData.m_Flags & GeometryFlags.Standing) != 0),
			yz = (objectGeometryData.m_LegOffset != 0f)
		};
		int3 @int = math.select((int3)0, (int3)1, test);
		legCount = @int.x << math.csum(@int.yz);
		return test.x;
	}

	public static float3 GetStandingLegPosition(ObjectGeometryData objectGeometryData, Transform transform, int legIndex)
	{
		return transform.m_Position + math.mul(v: GetStandingLegOffset(objectGeometryData, legIndex), q: transform.m_Rotation);
	}

	public static float3 GetStandingLegOffset(ObjectGeometryData objectGeometryData, int legIndex)
	{
		float3 result = default(float3);
		bool2 test = (new int2(legIndex, math.select(legIndex, legIndex >> 1, objectGeometryData.m_LegOffset.x != 0f)) & 1) != 0;
		result.xz = math.select(-objectGeometryData.m_LegOffset, objectGeometryData.m_LegOffset, test);
		return result;
	}
}
