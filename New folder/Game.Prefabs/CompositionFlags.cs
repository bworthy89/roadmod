using System;
using Colossal.Serialization.Entities;

namespace Game.Prefabs;

public struct CompositionFlags : ISerializable, IEquatable<CompositionFlags>
{
	[Flags]
	public enum General : uint
	{
		Node = 1u,
		Edge = 2u,
		Invert = 4u,
		Flip = 8u,
		DeadEnd = 0x10u,
		Intersection = 0x20u,
		Roundabout = 0x40u,
		LevelCrossing = 0x80u,
		Crosswalk = 0x100u,
		MedianBreak = 0x200u,
		TrafficLights = 0x400u,
		Spillway = 0x800u,
		Opening = 0x1000u,
		Front = 0x2000u,
		Back = 0x4000u,
		RemoveTrafficLights = 0x8000u,
		AllWayStop = 0x1000000u,
		Pavement = 0x2000000u,
		Gravel = 0x4000000u,
		Tiles = 0x8000000u,
		Lighting = 0x10000000u,
		Inside = 0x20000000u,
		StyleBreak = 0x40000000u,
		Elevated = 0x10000u,
		Tunnel = 0x20000u,
		MiddlePlatform = 0x40000u,
		WideMedian = 0x80000u,
		PrimaryMiddleBeautification = 0x100000u,
		SecondaryMiddleBeautification = 0x200000u,
		FixedNodeSize = 0x400000u,
		StraightNodeEnd = 0x800000u
	}

	[Flags]
	public enum Side : uint
	{
		Raised = 1u,
		Lowered = 2u,
		LowTransition = 4u,
		HighTransition = 8u,
		PrimaryTrack = 0x10u,
		SecondaryTrack = 0x20u,
		TertiaryTrack = 0x40u,
		QuaternaryTrack = 0x80u,
		PrimaryStop = 0x100u,
		SecondaryStop = 0x200u,
		TertiaryStop = 0x400u,
		PrimaryBeautification = 0x1000u,
		SecondaryBeautification = 0x2000u,
		AbruptEnd = 0x4000u,
		Gate = 0x8000u,
		Sidewalk = 0x10000u,
		WideSidewalk = 0x20000u,
		ParkingSpaces = 0x40000u,
		SoundBarrier = 0x80000u,
		PrimaryLane = 0x100000u,
		SecondaryLane = 0x200000u,
		TertiaryLane = 0x400000u,
		QuaternaryLane = 0x800000u,
		ForbidLeftTurn = 0x1000000u,
		ForbidRightTurn = 0x2000000u,
		AddCrosswalk = 0x4000000u,
		RemoveCrosswalk = 0x8000000u,
		ForbidStraight = 0x10000000u,
		ForbidSecondary = 0x20000000u
	}

	public General m_General;

	public Side m_Left;

	public Side m_Right;

	private const General NODE_MASK_GENERAL = General.Node | General.DeadEnd | General.Intersection | General.Roundabout | General.LevelCrossing | General.MedianBreak | General.TrafficLights | General.RemoveTrafficLights | General.AllWayStop | General.FixedNodeSize | General.StraightNodeEnd;

	private const General OPTION_MASK_GENERAL = General.WideMedian | General.PrimaryMiddleBeautification | General.SecondaryMiddleBeautification;

	private const General DIRECTIONAL_MASK_GENERAL = General.TrafficLights | General.RemoveTrafficLights;

	private const Side NODE_MASK_SIDE = Side.LowTransition | Side.HighTransition;

	private const Side OPTION_MASK_SIDE = Side.PrimaryBeautification | Side.SecondaryBeautification | Side.WideSidewalk;

	private const Side DIRECTIONAL_MASK_SIDE = Side.PrimaryTrack | Side.SecondaryTrack | Side.TertiaryTrack | Side.QuaternaryTrack | Side.PrimaryLane | Side.SecondaryLane | Side.TertiaryLane | Side.QuaternaryLane | Side.ForbidLeftTurn | Side.ForbidRightTurn | Side.AddCrosswalk | Side.RemoveCrosswalk | Side.ForbidStraight | Side.ForbidSecondary;

	public static CompositionFlags nodeMask => new CompositionFlags(General.Node | General.DeadEnd | General.Intersection | General.Roundabout | General.LevelCrossing | General.MedianBreak | General.TrafficLights | General.RemoveTrafficLights | General.AllWayStop | General.FixedNodeSize | General.StraightNodeEnd, Side.LowTransition | Side.HighTransition, Side.LowTransition | Side.HighTransition);

	public static CompositionFlags optionMask => new CompositionFlags(General.WideMedian | General.PrimaryMiddleBeautification | General.SecondaryMiddleBeautification, Side.PrimaryBeautification | Side.SecondaryBeautification | Side.WideSidewalk, Side.PrimaryBeautification | Side.SecondaryBeautification | Side.WideSidewalk);

	public static CompositionFlags directionalMask => new CompositionFlags(General.TrafficLights | General.RemoveTrafficLights, Side.PrimaryTrack | Side.SecondaryTrack | Side.TertiaryTrack | Side.QuaternaryTrack | Side.PrimaryLane | Side.SecondaryLane | Side.TertiaryLane | Side.QuaternaryLane | Side.ForbidLeftTurn | Side.ForbidRightTurn | Side.AddCrosswalk | Side.RemoveCrosswalk | Side.ForbidStraight | Side.ForbidSecondary, Side.PrimaryTrack | Side.SecondaryTrack | Side.TertiaryTrack | Side.QuaternaryTrack | Side.PrimaryLane | Side.SecondaryLane | Side.TertiaryLane | Side.QuaternaryLane | Side.ForbidLeftTurn | Side.ForbidRightTurn | Side.AddCrosswalk | Side.RemoveCrosswalk | Side.ForbidStraight | Side.ForbidSecondary);

	public CompositionFlags(General general, Side left, Side right)
	{
		m_General = general;
		m_Left = left;
		m_Right = right;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		General general = m_General;
		writer.Write((uint)general);
		Side left = m_Left;
		writer.Write((uint)left);
		Side right = m_Right;
		writer.Write((uint)right);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.compositionFlagRefactoring)
		{
			reader.Read(out uint value);
			reader.Read(out uint value2);
			reader.Read(out uint value3);
			m_General = (General)value;
			m_Left = (Side)value2;
			m_Right = (Side)value3;
			return;
		}
		reader.Read(out uint value4);
		if ((value4 & 0x1000) != 0)
		{
			m_Right |= Side.PrimaryTrack;
		}
		if ((value4 & 0x2000) != 0)
		{
			m_Left |= Side.PrimaryTrack;
		}
	}

	public static CompositionFlags operator |(CompositionFlags lhs, CompositionFlags rhs)
	{
		return new CompositionFlags(lhs.m_General | rhs.m_General, lhs.m_Left | rhs.m_Left, lhs.m_Right | rhs.m_Right);
	}

	public static CompositionFlags operator &(CompositionFlags lhs, CompositionFlags rhs)
	{
		return new CompositionFlags(lhs.m_General & rhs.m_General, lhs.m_Left & rhs.m_Left, lhs.m_Right & rhs.m_Right);
	}

	public static CompositionFlags operator ^(CompositionFlags lhs, CompositionFlags rhs)
	{
		return new CompositionFlags(lhs.m_General ^ rhs.m_General, lhs.m_Left ^ rhs.m_Left, lhs.m_Right ^ rhs.m_Right);
	}

	public static bool operator ==(CompositionFlags lhs, CompositionFlags rhs)
	{
		if (lhs.m_General == rhs.m_General && lhs.m_Left == rhs.m_Left)
		{
			return lhs.m_Right == rhs.m_Right;
		}
		return false;
	}

	public static bool operator !=(CompositionFlags lhs, CompositionFlags rhs)
	{
		if (lhs.m_General == rhs.m_General && lhs.m_Left == rhs.m_Left)
		{
			return lhs.m_Right != rhs.m_Right;
		}
		return true;
	}

	public static CompositionFlags operator ~(CompositionFlags rhs)
	{
		return new CompositionFlags(~rhs.m_General, ~rhs.m_Left, ~rhs.m_Right);
	}

	public bool Equals(CompositionFlags other)
	{
		return this == other;
	}

	public override bool Equals(object obj)
	{
		if (obj is CompositionFlags other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		uint general = (uint)m_General;
		int num = general.GetHashCode() * 31;
		general = (uint)m_Left;
		int num2 = (num + general.GetHashCode()) * 31;
		general = (uint)m_Right;
		return num2 + general.GetHashCode();
	}
}
