using System;
using Colossal.Mathematics;
using Game.Net;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public static class NetCompositionHelpers
{
	private struct TempLaneData
	{
		public int m_GroupIndex;
	}

	private struct TempLaneGroup
	{
		public Entity m_Prefab;

		public float3 m_Position;

		public LaneFlags m_Flags;

		public int m_LaneCount;

		public int m_FullLaneCount;

		public int m_CarriagewayIndex;

		public bool IsCompatible(TempLaneGroup other)
		{
			LaneFlags laneFlags = LaneFlags.Invert | LaneFlags.Road;
			return ((m_Flags & laneFlags) == (other.m_Flags & laneFlags)) & ((m_Flags & LaneFlags.Road) != 0);
		}
	}

	public static void GetRequirementFlags(NetPieceRequirements[] requirements, out CompositionFlags compositionFlags, out NetSectionFlags sectionFlags)
	{
		compositionFlags = default(CompositionFlags);
		sectionFlags = (NetSectionFlags)0;
		if (requirements != null)
		{
			for (int i = 0; i < requirements.Length; i++)
			{
				GetRequirementFlags(requirements[i], ref compositionFlags, ref sectionFlags);
			}
		}
	}

	public static void GetRequirementFlags(NetPieceRequirements requirement, ref CompositionFlags compositionFlags, ref NetSectionFlags sectionFlags)
	{
		switch (requirement)
		{
		case NetPieceRequirements.Node:
			compositionFlags.m_General |= CompositionFlags.General.Node;
			break;
		case NetPieceRequirements.Intersection:
			compositionFlags.m_General |= CompositionFlags.General.Intersection;
			break;
		case NetPieceRequirements.DeadEnd:
			compositionFlags.m_General |= CompositionFlags.General.DeadEnd;
			break;
		case NetPieceRequirements.Crosswalk:
			compositionFlags.m_General |= CompositionFlags.General.Crosswalk;
			break;
		case NetPieceRequirements.BusStop:
			compositionFlags.m_Right |= CompositionFlags.Side.PrimaryStop;
			break;
		case NetPieceRequirements.Median:
			sectionFlags |= NetSectionFlags.Median;
			break;
		case NetPieceRequirements.TrainStop:
			compositionFlags.m_Right |= CompositionFlags.Side.PrimaryStop;
			break;
		case NetPieceRequirements.OppositeTrainStop:
			compositionFlags.m_Left |= CompositionFlags.Side.PrimaryStop;
			break;
		case NetPieceRequirements.Inverted:
			sectionFlags |= NetSectionFlags.Invert;
			break;
		case NetPieceRequirements.TaxiStand:
			compositionFlags.m_Right |= CompositionFlags.Side.PrimaryStop;
			break;
		case NetPieceRequirements.LevelCrossing:
			compositionFlags.m_General |= CompositionFlags.General.LevelCrossing;
			break;
		case NetPieceRequirements.Elevated:
			compositionFlags.m_General |= CompositionFlags.General.Elevated;
			break;
		case NetPieceRequirements.Tunnel:
			compositionFlags.m_General |= CompositionFlags.General.Tunnel;
			break;
		case NetPieceRequirements.Raised:
			compositionFlags.m_Right |= CompositionFlags.Side.Raised;
			break;
		case NetPieceRequirements.Lowered:
			compositionFlags.m_Right |= CompositionFlags.Side.Lowered;
			break;
		case NetPieceRequirements.LowTransition:
			compositionFlags.m_Right |= CompositionFlags.Side.LowTransition;
			break;
		case NetPieceRequirements.HighTransition:
			compositionFlags.m_Right |= CompositionFlags.Side.HighTransition;
			break;
		case NetPieceRequirements.WideMedian:
			compositionFlags.m_General |= CompositionFlags.General.WideMedian;
			break;
		case NetPieceRequirements.TramTrack:
			compositionFlags.m_Right |= CompositionFlags.Side.PrimaryTrack;
			break;
		case NetPieceRequirements.TramStop:
			compositionFlags.m_Right |= CompositionFlags.Side.SecondaryStop;
			break;
		case NetPieceRequirements.OppositeTramTrack:
			compositionFlags.m_Left |= CompositionFlags.Side.PrimaryTrack;
			break;
		case NetPieceRequirements.OppositeTramStop:
			compositionFlags.m_Left |= CompositionFlags.Side.SecondaryStop;
			break;
		case NetPieceRequirements.MedianBreak:
			compositionFlags.m_General |= CompositionFlags.General.MedianBreak;
			break;
		case NetPieceRequirements.ShipStop:
			compositionFlags.m_Right |= CompositionFlags.Side.TertiaryStop;
			break;
		case NetPieceRequirements.Sidewalk:
			compositionFlags.m_Right |= CompositionFlags.Side.Sidewalk;
			break;
		case NetPieceRequirements.Edge:
			compositionFlags.m_General |= CompositionFlags.General.Edge;
			break;
		case NetPieceRequirements.SubwayStop:
			compositionFlags.m_Right |= CompositionFlags.Side.PrimaryStop;
			break;
		case NetPieceRequirements.OppositeSubwayStop:
			compositionFlags.m_Left |= CompositionFlags.Side.PrimaryStop;
			break;
		case NetPieceRequirements.MiddlePlatform:
			compositionFlags.m_General |= CompositionFlags.General.MiddlePlatform;
			break;
		case NetPieceRequirements.Underground:
			sectionFlags |= NetSectionFlags.Underground;
			break;
		case NetPieceRequirements.Roundabout:
			compositionFlags.m_General |= CompositionFlags.General.Roundabout;
			break;
		case NetPieceRequirements.OppositeSidewalk:
			compositionFlags.m_Left |= CompositionFlags.Side.Sidewalk;
			break;
		case NetPieceRequirements.SoundBarrier:
			compositionFlags.m_Right |= CompositionFlags.Side.SoundBarrier;
			break;
		case NetPieceRequirements.Overhead:
			sectionFlags |= NetSectionFlags.Overhead;
			break;
		case NetPieceRequirements.TrafficLights:
			compositionFlags.m_General |= CompositionFlags.General.TrafficLights;
			break;
		case NetPieceRequirements.PublicTransportLane:
			compositionFlags.m_Right |= CompositionFlags.Side.PrimaryLane;
			break;
		case NetPieceRequirements.OppositePublicTransportLane:
			compositionFlags.m_Left |= CompositionFlags.Side.PrimaryLane;
			break;
		case NetPieceRequirements.Spillway:
			compositionFlags.m_General |= CompositionFlags.General.Spillway;
			break;
		case NetPieceRequirements.MiddleGrass:
			compositionFlags.m_General |= CompositionFlags.General.PrimaryMiddleBeautification;
			break;
		case NetPieceRequirements.MiddleTrees:
			compositionFlags.m_General |= CompositionFlags.General.SecondaryMiddleBeautification;
			break;
		case NetPieceRequirements.WideSidewalk:
			compositionFlags.m_Right |= CompositionFlags.Side.WideSidewalk;
			break;
		case NetPieceRequirements.SideGrass:
			compositionFlags.m_Right |= CompositionFlags.Side.PrimaryBeautification;
			break;
		case NetPieceRequirements.SideTrees:
			compositionFlags.m_Right |= CompositionFlags.Side.SecondaryBeautification;
			break;
		case NetPieceRequirements.OppositeGrass:
			compositionFlags.m_Left |= CompositionFlags.Side.PrimaryBeautification;
			break;
		case NetPieceRequirements.OppositeTrees:
			compositionFlags.m_Left |= CompositionFlags.Side.SecondaryBeautification;
			break;
		case NetPieceRequirements.Opening:
			compositionFlags.m_General |= CompositionFlags.General.Opening;
			break;
		case NetPieceRequirements.Front:
			compositionFlags.m_General |= CompositionFlags.General.Front;
			break;
		case NetPieceRequirements.Back:
			compositionFlags.m_General |= CompositionFlags.General.Back;
			break;
		case NetPieceRequirements.Flipped:
			sectionFlags |= NetSectionFlags.FlipMesh;
			break;
		case NetPieceRequirements.RemoveTrafficLights:
			compositionFlags.m_General |= CompositionFlags.General.RemoveTrafficLights;
			break;
		case NetPieceRequirements.AllWayStop:
			compositionFlags.m_General |= CompositionFlags.General.AllWayStop;
			break;
		case NetPieceRequirements.Pavement:
			compositionFlags.m_General |= CompositionFlags.General.Pavement;
			break;
		case NetPieceRequirements.Gravel:
			compositionFlags.m_General |= CompositionFlags.General.Gravel;
			break;
		case NetPieceRequirements.Tiles:
			compositionFlags.m_General |= CompositionFlags.General.Tiles;
			break;
		case NetPieceRequirements.ForbidLeftTurn:
			compositionFlags.m_Right |= CompositionFlags.Side.ForbidLeftTurn;
			break;
		case NetPieceRequirements.ForbidRightTurn:
			compositionFlags.m_Right |= CompositionFlags.Side.ForbidRightTurn;
			break;
		case NetPieceRequirements.OppositeWideSidewalk:
			compositionFlags.m_Left |= CompositionFlags.Side.WideSidewalk;
			break;
		case NetPieceRequirements.OppositeForbidLeftTurn:
			compositionFlags.m_Left |= CompositionFlags.Side.ForbidLeftTurn;
			break;
		case NetPieceRequirements.OppositeForbidRightTurn:
			compositionFlags.m_Left |= CompositionFlags.Side.ForbidRightTurn;
			break;
		case NetPieceRequirements.OppositeSoundBarrier:
			compositionFlags.m_Left |= CompositionFlags.Side.SoundBarrier;
			break;
		case NetPieceRequirements.SidePlatform:
			compositionFlags.m_Right |= CompositionFlags.Side.Sidewalk;
			break;
		case NetPieceRequirements.AddCrosswalk:
			compositionFlags.m_Right |= CompositionFlags.Side.AddCrosswalk;
			break;
		case NetPieceRequirements.RemoveCrosswalk:
			compositionFlags.m_Right |= CompositionFlags.Side.RemoveCrosswalk;
			break;
		case NetPieceRequirements.Lighting:
			compositionFlags.m_General |= CompositionFlags.General.Lighting;
			break;
		case NetPieceRequirements.OppositeBusStop:
			compositionFlags.m_Left |= CompositionFlags.Side.PrimaryStop;
			break;
		case NetPieceRequirements.OppositeTaxiStand:
			compositionFlags.m_Left |= CompositionFlags.Side.PrimaryStop;
			break;
		case NetPieceRequirements.OppositeRaised:
			compositionFlags.m_Left |= CompositionFlags.Side.Raised;
			break;
		case NetPieceRequirements.OppositeLowered:
			compositionFlags.m_Left |= CompositionFlags.Side.Lowered;
			break;
		case NetPieceRequirements.OppositeLowTransition:
			compositionFlags.m_Left |= CompositionFlags.Side.LowTransition;
			break;
		case NetPieceRequirements.OppositeHighTransition:
			compositionFlags.m_Left |= CompositionFlags.Side.HighTransition;
			break;
		case NetPieceRequirements.OppositeShipStop:
			compositionFlags.m_Left |= CompositionFlags.Side.TertiaryStop;
			break;
		case NetPieceRequirements.OppositePlatform:
			compositionFlags.m_Left |= CompositionFlags.Side.Sidewalk;
			break;
		case NetPieceRequirements.OppositeAddCrosswalk:
			compositionFlags.m_Left |= CompositionFlags.Side.AddCrosswalk;
			break;
		case NetPieceRequirements.OppositeRemoveCrosswalk:
			compositionFlags.m_Left |= CompositionFlags.Side.RemoveCrosswalk;
			break;
		case NetPieceRequirements.Inside:
			compositionFlags.m_General |= CompositionFlags.General.Inside;
			break;
		case NetPieceRequirements.ForbidStraight:
			compositionFlags.m_Right |= CompositionFlags.Side.ForbidStraight;
			break;
		case NetPieceRequirements.OppositeForbidStraight:
			compositionFlags.m_Left |= CompositionFlags.Side.ForbidStraight;
			break;
		case NetPieceRequirements.Hidden:
			sectionFlags |= NetSectionFlags.Hidden;
			break;
		case NetPieceRequirements.ParkingSpaces:
			compositionFlags.m_Right |= CompositionFlags.Side.ParkingSpaces;
			break;
		case NetPieceRequirements.OppositeParkingSpaces:
			compositionFlags.m_Left |= CompositionFlags.Side.ParkingSpaces;
			break;
		case NetPieceRequirements.FixedNodeSize:
			compositionFlags.m_General |= CompositionFlags.General.FixedNodeSize;
			break;
		case NetPieceRequirements.HalfLength:
			sectionFlags |= NetSectionFlags.HalfLength;
			break;
		case NetPieceRequirements.AbruptEnd:
			compositionFlags.m_Right |= CompositionFlags.Side.AbruptEnd;
			break;
		case NetPieceRequirements.OppositeAbruptEnd:
			compositionFlags.m_Left |= CompositionFlags.Side.AbruptEnd;
			break;
		case NetPieceRequirements.AttachmentTrack:
			compositionFlags.m_Right |= CompositionFlags.Side.SecondaryTrack;
			break;
		case NetPieceRequirements.EnterGate:
			compositionFlags.m_Right |= CompositionFlags.Side.Gate;
			break;
		case NetPieceRequirements.ExitGate:
			compositionFlags.m_Left |= CompositionFlags.Side.Gate;
			break;
		case NetPieceRequirements.StyleBreak:
			compositionFlags.m_General |= CompositionFlags.General.StyleBreak;
			break;
		case NetPieceRequirements.FerryStop:
			compositionFlags.m_Right |= CompositionFlags.Side.TertiaryStop;
			break;
		case NetPieceRequirements.StraightNodeEnd:
			compositionFlags.m_General |= CompositionFlags.General.StraightNodeEnd;
			break;
		case NetPieceRequirements.BicycleLane:
			compositionFlags.m_Right |= CompositionFlags.Side.SecondaryLane;
			break;
		case NetPieceRequirements.OppositeBicycleLane:
			compositionFlags.m_Left |= CompositionFlags.Side.SecondaryLane;
			break;
		case NetPieceRequirements.ForbidBicycles:
			compositionFlags.m_Right |= CompositionFlags.Side.ForbidSecondary;
			break;
		case NetPieceRequirements.OppositeForbidBicycles:
			compositionFlags.m_Left |= CompositionFlags.Side.ForbidSecondary;
			break;
		}
	}

	public static CompositionFlags InvertCompositionFlags(CompositionFlags flags)
	{
		return new CompositionFlags(flags.m_General, flags.m_Right, flags.m_Left);
	}

	public static NetSectionFlags InvertSectionFlags(NetSectionFlags flags)
	{
		return flags;
	}

	public static bool TestSectionFlags(NetGeometrySection section, CompositionFlags compositionFlags)
	{
		if (((section.m_CompositionAll | section.m_CompositionNone) & compositionFlags) != section.m_CompositionAll)
		{
			return false;
		}
		if (section.m_CompositionAny == default(CompositionFlags))
		{
			return true;
		}
		return (section.m_CompositionAny & compositionFlags) != default(CompositionFlags);
	}

	public static bool TestSubSectionFlags(NetSubSection subSection, CompositionFlags compositionFlags, NetSectionFlags sectionFlags)
	{
		if ((sectionFlags & NetSectionFlags.Median) == 0)
		{
			compositionFlags.m_General &= ~CompositionFlags.General.MedianBreak;
		}
		if (((subSection.m_CompositionAll | subSection.m_CompositionNone) & compositionFlags) != subSection.m_CompositionAll)
		{
			return false;
		}
		if (((subSection.m_SectionAll | subSection.m_SectionNone) & sectionFlags) != subSection.m_SectionAll)
		{
			return false;
		}
		if (subSection.m_CompositionAny == default(CompositionFlags) && subSection.m_SectionAny == (NetSectionFlags)0)
		{
			return true;
		}
		if (!((subSection.m_CompositionAny & compositionFlags) != default(CompositionFlags)))
		{
			return (subSection.m_SectionAny & sectionFlags) != 0;
		}
		return true;
	}

	public static bool TestPieceFlags(NetSectionPiece piece, CompositionFlags compositionFlags, NetSectionFlags sectionFlags)
	{
		if ((sectionFlags & NetSectionFlags.Median) == 0)
		{
			compositionFlags.m_General &= ~CompositionFlags.General.MedianBreak;
		}
		if (((piece.m_CompositionAll | piece.m_CompositionNone) & compositionFlags) != piece.m_CompositionAll)
		{
			return false;
		}
		if (((piece.m_SectionAll | piece.m_SectionNone) & sectionFlags) != piece.m_SectionAll)
		{
			return false;
		}
		if (piece.m_CompositionAny == default(CompositionFlags) && piece.m_SectionAny == (NetSectionFlags)0)
		{
			return true;
		}
		if (!((piece.m_CompositionAny & compositionFlags) != default(CompositionFlags)))
		{
			return (piece.m_SectionAny & sectionFlags) != 0;
		}
		return true;
	}

	public static bool TestPieceFlags2(NetSectionPiece piece, CompositionFlags compositionFlags, NetSectionFlags sectionFlags)
	{
		if ((compositionFlags.m_General & CompositionFlags.General.Roundabout) != 0 && (piece.m_Flags & NetPieceFlags.Side) != 0)
		{
			CompositionFlags compositionFlags2 = compositionFlags;
			if ((compositionFlags2.m_General & CompositionFlags.General.Elevated) != 0)
			{
				if ((compositionFlags2.m_Left & CompositionFlags.Side.HighTransition) != 0)
				{
					compositionFlags2.m_General &= ~CompositionFlags.General.Elevated;
					compositionFlags2.m_Left &= ~CompositionFlags.Side.HighTransition;
				}
				else if ((compositionFlags2.m_Left & CompositionFlags.Side.LowTransition) != 0)
				{
					compositionFlags2.m_General &= ~CompositionFlags.General.Elevated;
					compositionFlags2.m_Left &= ~CompositionFlags.Side.LowTransition;
					compositionFlags2.m_Left |= CompositionFlags.Side.Raised;
				}
				if ((compositionFlags2.m_Right & CompositionFlags.Side.HighTransition) != 0)
				{
					compositionFlags2.m_General &= ~CompositionFlags.General.Elevated;
					compositionFlags2.m_Right &= ~CompositionFlags.Side.HighTransition;
				}
				else if ((compositionFlags2.m_Right & CompositionFlags.Side.LowTransition) != 0)
				{
					compositionFlags2.m_General &= ~CompositionFlags.General.Elevated;
					compositionFlags2.m_Right &= ~CompositionFlags.Side.LowTransition;
					compositionFlags2.m_Right |= CompositionFlags.Side.Raised;
				}
			}
			else if ((compositionFlags2.m_General & CompositionFlags.General.Tunnel) != 0)
			{
				if ((compositionFlags2.m_Left & CompositionFlags.Side.HighTransition) != 0)
				{
					compositionFlags2.m_General &= ~CompositionFlags.General.Tunnel;
					compositionFlags2.m_Left &= ~CompositionFlags.Side.HighTransition;
				}
				else if ((compositionFlags2.m_Left & CompositionFlags.Side.LowTransition) != 0)
				{
					compositionFlags2.m_General &= ~CompositionFlags.General.Tunnel;
					compositionFlags2.m_Left &= ~CompositionFlags.Side.LowTransition;
					compositionFlags2.m_Left |= CompositionFlags.Side.Lowered;
				}
				if ((compositionFlags2.m_Right & CompositionFlags.Side.HighTransition) != 0)
				{
					compositionFlags2.m_General &= ~CompositionFlags.General.Tunnel;
					compositionFlags2.m_Right &= ~CompositionFlags.Side.HighTransition;
				}
				else if ((compositionFlags2.m_Right & CompositionFlags.Side.LowTransition) != 0)
				{
					compositionFlags2.m_General &= ~CompositionFlags.General.Tunnel;
					compositionFlags2.m_Right &= ~CompositionFlags.Side.LowTransition;
					compositionFlags2.m_Right |= CompositionFlags.Side.Lowered;
				}
			}
			else
			{
				if ((compositionFlags2.m_Left & CompositionFlags.Side.LowTransition) != 0)
				{
					if ((compositionFlags2.m_Left & CompositionFlags.Side.Raised) != 0)
					{
						compositionFlags2.m_Left &= ~(CompositionFlags.Side.Raised | CompositionFlags.Side.LowTransition);
					}
					else if ((compositionFlags2.m_Left & CompositionFlags.Side.Lowered) != 0)
					{
						compositionFlags2.m_Left &= ~(CompositionFlags.Side.Lowered | CompositionFlags.Side.LowTransition);
					}
					else if ((compositionFlags2.m_Left & CompositionFlags.Side.SoundBarrier) != 0)
					{
						compositionFlags2.m_Left &= ~(CompositionFlags.Side.LowTransition | CompositionFlags.Side.SoundBarrier);
					}
				}
				if ((compositionFlags2.m_Right & CompositionFlags.Side.LowTransition) != 0)
				{
					if ((compositionFlags2.m_Right & CompositionFlags.Side.Raised) != 0)
					{
						compositionFlags2.m_Right &= ~(CompositionFlags.Side.Raised | CompositionFlags.Side.LowTransition);
					}
					else if ((compositionFlags2.m_Right & CompositionFlags.Side.Lowered) != 0)
					{
						compositionFlags2.m_Right &= ~(CompositionFlags.Side.Lowered | CompositionFlags.Side.LowTransition);
					}
					else if ((compositionFlags2.m_Right & CompositionFlags.Side.SoundBarrier) != 0)
					{
						compositionFlags2.m_Right &= ~(CompositionFlags.Side.LowTransition | CompositionFlags.Side.SoundBarrier);
					}
				}
			}
			if (compositionFlags != compositionFlags2)
			{
				return TestPieceFlags(piece, compositionFlags2, sectionFlags);
			}
		}
		return false;
	}

	public static bool TestObjectFlags(NetPieceObject _object, CompositionFlags compositionFlags, NetSectionFlags sectionFlags)
	{
		if ((sectionFlags & NetSectionFlags.Median) == 0)
		{
			compositionFlags.m_General &= ~CompositionFlags.General.MedianBreak;
		}
		if (((_object.m_CompositionAll | _object.m_CompositionNone) & compositionFlags) != _object.m_CompositionAll)
		{
			return false;
		}
		if (((_object.m_SectionAll | _object.m_SectionNone) & sectionFlags) != _object.m_SectionAll)
		{
			return false;
		}
		if (_object.m_CompositionAny == default(CompositionFlags) && _object.m_SectionAny == (NetSectionFlags)0)
		{
			return true;
		}
		if (!((_object.m_CompositionAny & compositionFlags) != default(CompositionFlags)))
		{
			return (_object.m_SectionAny & sectionFlags) != 0;
		}
		return true;
	}

	public static bool TestLaneFlags(AuxiliaryNetLane lane, CompositionFlags compositionFlags)
	{
		if (((lane.m_CompositionAll | lane.m_CompositionNone) & compositionFlags) != lane.m_CompositionAll)
		{
			return false;
		}
		if (lane.m_CompositionAny == default(CompositionFlags))
		{
			return true;
		}
		return (lane.m_CompositionAny & compositionFlags) != default(CompositionFlags);
	}

	public static bool TestEdgeFlags(NetGeometryEdgeState edgeState, CompositionFlags compositionFlags)
	{
		if (((edgeState.m_CompositionAll | edgeState.m_CompositionNone) & compositionFlags) != edgeState.m_CompositionAll)
		{
			return false;
		}
		if (edgeState.m_CompositionAny == default(CompositionFlags))
		{
			return true;
		}
		return (edgeState.m_CompositionAny & compositionFlags) != default(CompositionFlags);
	}

	public static bool TestEdgeFlags(NetGeometryNodeState nodeState, CompositionFlags compositionFlags)
	{
		if (((nodeState.m_CompositionAll | nodeState.m_CompositionNone) & compositionFlags) != nodeState.m_CompositionAll)
		{
			return false;
		}
		if (nodeState.m_CompositionAny == default(CompositionFlags))
		{
			return true;
		}
		return (nodeState.m_CompositionAny & compositionFlags) != default(CompositionFlags);
	}

	public static bool TestEdgeFlags(ElectricityConnectionData electricityConnectionData, CompositionFlags compositionFlags)
	{
		if (((electricityConnectionData.m_CompositionAll | electricityConnectionData.m_CompositionNone) & compositionFlags) != electricityConnectionData.m_CompositionAll)
		{
			return false;
		}
		if (electricityConnectionData.m_CompositionAny == default(CompositionFlags))
		{
			return true;
		}
		return (electricityConnectionData.m_CompositionAny & compositionFlags) != default(CompositionFlags);
	}

	public static bool TestEdgeMatch(NetGeometryNodeState nodeState, bool2 match)
	{
		return nodeState.m_MatchType switch
		{
			NetEdgeMatchType.Both => math.all(match), 
			NetEdgeMatchType.Any => math.any(match), 
			NetEdgeMatchType.Exclusive => match.x != match.y, 
			_ => false, 
		};
	}

	public static void GetCompositionPieces(NativeList<NetCompositionPiece> resultBuffer, NativeArray<NetGeometrySection> geometrySections, CompositionFlags flags, BufferLookup<NetSubSection> subSectionData, BufferLookup<NetSectionPiece> sectionPieceData)
	{
		int num = 0;
		int num2 = 0;
		CompositionFlags compositionFlags = InvertCompositionFlags(flags);
		compositionFlags.m_Left = (CompositionFlags.Side)(((uint)compositionFlags.m_Left & 0xFFFFBFFFu) | (uint)(flags.m_Left & CompositionFlags.Side.AbruptEnd));
		compositionFlags.m_Right = (CompositionFlags.Side)(((uint)compositionFlags.m_Right & 0xFFFFBFFFu) | (uint)(flags.m_Right & CompositionFlags.Side.AbruptEnd));
		for (int i = 0; i < geometrySections.Length; i++)
		{
			NetGeometrySection section;
			if ((flags.m_General & CompositionFlags.General.Invert) != 0)
			{
				section = geometrySections[geometrySections.Length - 1 - i];
				section.m_Flags ^= NetSectionFlags.Invert | NetSectionFlags.FlipLanes;
				if ((section.m_Flags & NetSectionFlags.Left) != 0)
				{
					section.m_Flags &= ~NetSectionFlags.Left;
					section.m_Flags |= NetSectionFlags.Right;
				}
				else if ((section.m_Flags & NetSectionFlags.Right) != 0)
				{
					section.m_Flags &= ~NetSectionFlags.Right;
					section.m_Flags |= NetSectionFlags.Left;
				}
			}
			else
			{
				section = geometrySections[i];
			}
			if ((flags.m_General & CompositionFlags.General.Flip) != 0)
			{
				section.m_Flags ^= NetSectionFlags.FlipLanes;
			}
			CompositionFlags compositionFlags2 = (((section.m_Flags & NetSectionFlags.Invert) != 0) ? compositionFlags : flags);
			NetPieceFlags netPieceFlags = (NetPieceFlags)0;
			if ((section.m_Flags & NetSectionFlags.HiddenSurface) != 0)
			{
				netPieceFlags |= NetPieceFlags.Surface;
			}
			if ((section.m_Flags & NetSectionFlags.HiddenBottom) != 0)
			{
				netPieceFlags |= NetPieceFlags.Bottom;
			}
			if ((section.m_Flags & NetSectionFlags.HiddenTop) != 0)
			{
				netPieceFlags |= NetPieceFlags.Top;
			}
			if ((section.m_Flags & NetSectionFlags.HiddenSide) != 0)
			{
				netPieceFlags |= NetPieceFlags.Side;
			}
			if (!TestSectionFlags(section, compositionFlags2))
			{
				continue;
			}
			NetSectionFlags sectionFlags = (((section.m_Flags & NetSectionFlags.Invert) != 0) ? InvertSectionFlags(section.m_Flags) : section.m_Flags);
			while (true)
			{
				DynamicBuffer<NetSubSection> dynamicBuffer = subSectionData[section.m_Section];
				NetSubSection subSection;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					subSection = dynamicBuffer[j];
					if (TestSubSectionFlags(subSection, compositionFlags2, sectionFlags))
					{
						goto IL_01bb;
					}
				}
				break;
				IL_01bb:
				section.m_Section = subSection.m_SubSection;
			}
			DynamicBuffer<NetSectionPiece> dynamicBuffer2 = sectionPieceData[section.m_Section];
			for (int k = 0; k < dynamicBuffer2.Length; k++)
			{
				NetSectionPiece piece = dynamicBuffer2[k];
				NetPieceFlags netPieceFlags2 = piece.m_Flags;
				if (!TestPieceFlags(piece, compositionFlags2, sectionFlags))
				{
					if (!TestPieceFlags2(piece, compositionFlags2, sectionFlags))
					{
						continue;
					}
					netPieceFlags2 |= NetPieceFlags.SkipBottomHalf;
				}
				NetCompositionPiece value = new NetCompositionPiece
				{
					m_Piece = piece.m_Piece,
					m_SectionFlags = section.m_Flags,
					m_PieceFlags = netPieceFlags2,
					m_SectionIndex = num,
					m_Offset = section.m_Offset + piece.m_Offset
				};
				if ((netPieceFlags & netPieceFlags2) != 0)
				{
					value.m_SectionFlags |= NetSectionFlags.Hidden;
				}
				resultBuffer.Add(in value);
				num2++;
			}
			if (num2 != 0)
			{
				num++;
				num2 = 0;
			}
		}
	}

	public static void CalculateCompositionData(ref NetCompositionData compositionData, NativeArray<NetCompositionPiece> pieces, ComponentLookup<NetPieceData> netPieceData, ComponentLookup<NetVertexMatchData> netVertexMatchData)
	{
		CalculateCompositionPieceOffsets(ref compositionData, pieces, netPieceData);
		CalculateSyncVertexOffsets(ref compositionData, pieces, netVertexMatchData);
	}

	public static void CalculateMinLod(ref NetCompositionData compositionData, NativeArray<NetCompositionPiece> pieces, ComponentLookup<MeshData> meshDatas)
	{
		float num = 0f;
		for (int i = 0; i < pieces.Length; i++)
		{
			num += meshDatas[pieces[i].m_Piece].m_LodBias;
		}
		if (pieces.Length != 0)
		{
			num /= (float)pieces.Length;
		}
		float2 size = new float2(compositionData.m_Width, MathUtils.Size(compositionData.m_HeightRange));
		compositionData.m_MinLod = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(size), num);
	}

	private static void CalculateCompositionPieceOffsets(ref NetCompositionData compositionData, NativeArray<NetCompositionPiece> pieces, ComponentLookup<NetPieceData> netPieceData)
	{
		compositionData.m_Width = 0f;
		compositionData.m_MiddleOffset = 0f;
		compositionData.m_WidthOffset = 0f;
		compositionData.m_NodeOffset = 0f;
		compositionData.m_HeightRange = new Bounds1(float.MaxValue, float.MinValue);
		compositionData.m_SurfaceHeight = new Bounds1(float.MaxValue, float.MinValue);
		bool test = (compositionData.m_Flags.m_General & CompositionFlags.General.Invert) != 0;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		int num8 = 0;
		while (num8 < pieces.Length)
		{
			NetCompositionPiece value = pieces[num8];
			bool flag5 = (value.m_SectionFlags & NetSectionFlags.Underground) != 0;
			bool flag6 = (value.m_SectionFlags & NetSectionFlags.Overhead) != 0;
			bool test2 = (value.m_SectionFlags & NetSectionFlags.Invert) != 0;
			bool test3 = (value.m_SectionFlags & NetSectionFlags.FlipMesh) != 0;
			bool2 test4 = new bool2((value.m_SectionFlags & NetSectionFlags.Left) != 0, (value.m_SectionFlags & NetSectionFlags.Right) != 0);
			NetPieceData netPieceData2 = netPieceData[value.m_Piece];
			float num9 = netPieceData2.m_Width;
			if (!flag5 || (compositionData.m_Flags.m_General & CompositionFlags.General.Elevated) == 0)
			{
				compositionData.m_HeightRange |= value.m_Offset.y + netPieceData2.m_HeightRange;
			}
			if (!flag5 && !flag6 && (value.m_PieceFlags & NetPieceFlags.PreserveShape) == 0)
			{
				if ((value.m_PieceFlags & NetPieceFlags.Side) != 0)
				{
					float4 falseValue = math.select(netPieceData2.m_SurfaceHeights, netPieceData2.m_SurfaceHeights.yxwz, test2);
					falseValue = math.select(falseValue, falseValue.zwxy, test3);
					compositionData.m_EdgeHeights = math.select(compositionData.m_EdgeHeights, falseValue, test4.xyxy);
					compositionData.m_SideConnectionOffset = math.select(compositionData.m_SideConnectionOffset, netPieceData2.m_SideConnectionOffset, test4);
				}
				if ((value.m_PieceFlags & NetPieceFlags.Surface) != 0)
				{
					compositionData.m_SurfaceHeight.min = math.min(compositionData.m_SurfaceHeight.min, value.m_Offset.y + math.cmin(netPieceData2.m_SurfaceHeights));
					compositionData.m_SurfaceHeight.max = math.max(compositionData.m_SurfaceHeight.max, value.m_Offset.y + math.cmax(netPieceData2.m_SurfaceHeights));
					flag = true;
				}
			}
			compositionData.m_WidthOffset = math.max(compositionData.m_WidthOffset, netPieceData2.m_WidthOffset);
			compositionData.m_NodeOffset = math.max(compositionData.m_NodeOffset, netPieceData2.m_NodeOffset);
			value.m_Size.x = netPieceData2.m_Width;
			value.m_Size.y = netPieceData2.m_HeightRange.max - netPieceData2.m_HeightRange.min;
			value.m_Size.z = netPieceData2.m_Length;
			int i;
			for (i = num8 + 1; i < pieces.Length; i++)
			{
				NetCompositionPiece value2 = pieces[i];
				if (value2.m_SectionIndex != value.m_SectionIndex)
				{
					break;
				}
				NetPieceData netPieceData3 = netPieceData[value2.m_Piece];
				num9 = math.max(num9, netPieceData3.m_Width);
				if (!flag5 || (compositionData.m_Flags.m_General & CompositionFlags.General.Elevated) == 0)
				{
					compositionData.m_HeightRange |= value2.m_Offset.y + netPieceData3.m_HeightRange;
				}
				if (!flag5 && !flag6 && (value2.m_PieceFlags & NetPieceFlags.PreserveShape) == 0)
				{
					if ((value2.m_PieceFlags & NetPieceFlags.Side) != 0)
					{
						float4 falseValue2 = math.select(netPieceData3.m_SurfaceHeights, netPieceData3.m_SurfaceHeights.yxwz, test2);
						falseValue2 = math.select(falseValue2, falseValue2.zwxy, test3);
						compositionData.m_EdgeHeights = math.select(compositionData.m_EdgeHeights, falseValue2, test4.xyxy);
					}
					if ((value2.m_PieceFlags & NetPieceFlags.Surface) != 0)
					{
						compositionData.m_SurfaceHeight.min = math.min(compositionData.m_SurfaceHeight.min, value2.m_Offset.y + math.cmin(netPieceData3.m_SurfaceHeights));
						compositionData.m_SurfaceHeight.max = math.max(compositionData.m_SurfaceHeight.max, value2.m_Offset.y + math.cmax(netPieceData3.m_SurfaceHeights));
						flag = true;
					}
				}
				compositionData.m_WidthOffset = math.max(compositionData.m_WidthOffset, netPieceData3.m_WidthOffset);
				compositionData.m_NodeOffset = math.max(compositionData.m_NodeOffset, netPieceData3.m_NodeOffset);
				value2.m_Size.x = netPieceData3.m_Width;
				value2.m_Size.y = netPieceData3.m_HeightRange.max - netPieceData3.m_HeightRange.min;
				value2.m_Size.z = netPieceData3.m_Length;
				pieces[i] = value2;
			}
			float x = value.m_Offset.x;
			if (flag5)
			{
				value.m_Offset.x += num + num9 * 0.5f;
				num += num9;
				if ((value.m_SectionFlags & (NetSectionFlags.Median | NetSectionFlags.AlignCenter)) == NetSectionFlags.Median)
				{
					num2 = value.m_Offset.x - math.select(x * 2f, 0f, test);
					num3 = x;
					flag3 = true;
				}
				else if ((value.m_SectionFlags & (NetSectionFlags.Right | NetSectionFlags.AlignCenter)) == NetSectionFlags.Right && !flag3)
				{
					num2 = value.m_Offset.x - value.m_Size.x * 0.5f - math.select(x * 2f, 0f, test);
					num3 = x;
					flag3 = true;
				}
			}
			else if (flag6)
			{
				value.m_Offset.x += num4 + num9 * 0.5f;
				num4 += num9;
				if ((value.m_SectionFlags & (NetSectionFlags.Median | NetSectionFlags.AlignCenter)) == NetSectionFlags.Median)
				{
					num5 = value.m_Offset.x - math.select(x * 2f, 0f, test);
					num6 = x;
					flag4 = true;
				}
				else if ((value.m_SectionFlags & (NetSectionFlags.Right | NetSectionFlags.AlignCenter)) == NetSectionFlags.Right && !flag4)
				{
					num5 = value.m_Offset.x - value.m_Size.x * 0.5f - math.select(x * 2f, 0f, test);
					num6 = x;
					flag4 = true;
				}
			}
			else
			{
				value.m_Offset.x += compositionData.m_Width + num9 * 0.5f;
				compositionData.m_Width += num9;
				if ((value.m_SectionFlags & (NetSectionFlags.Median | NetSectionFlags.AlignCenter)) == NetSectionFlags.Median)
				{
					compositionData.m_MiddleOffset = value.m_Offset.x - math.select(x * 2f, 0f, test);
					num7 = x;
					flag2 = true;
				}
				else if ((value.m_SectionFlags & (NetSectionFlags.Right | NetSectionFlags.AlignCenter)) == NetSectionFlags.Right && !flag2)
				{
					compositionData.m_MiddleOffset = value.m_Offset.x - value.m_Size.x * 0.5f - math.select(x * 2f, 0f, test);
					num7 = x;
					flag2 = true;
				}
			}
			pieces[num8] = value;
			for (int j = num8 + 1; j < i; j++)
			{
				NetCompositionPiece value3 = pieces[j];
				value3.m_Offset.x = value.m_Offset.x;
				pieces[j] = value3;
			}
			if ((value.m_PieceFlags & NetPieceFlags.Side) != 0 && i > num8 + 1 && (value.m_SectionFlags & (NetSectionFlags.Left | NetSectionFlags.Right)) != 0)
			{
				bool test5 = (value.m_SectionFlags & NetSectionFlags.Right) != 0;
				for (int k = num8; k < i; k++)
				{
					NetCompositionPiece value4 = pieces[k];
					float num10 = (netPieceData[value4.m_Piece].m_Width - num9) * 0.5f;
					value4.m_Offset.x += math.select(0f - num10, num10, test5);
					pieces[k] = value4;
				}
			}
			num8 = i;
		}
		if (flag3)
		{
			num2 -= num * 0.5f;
		}
		if (flag4)
		{
			num5 -= num4 * 0.5f;
		}
		if (flag2)
		{
			compositionData.m_MiddleOffset -= compositionData.m_Width * 0.5f;
		}
		if ((compositionData.m_Flags.m_General & (CompositionFlags.General.DeadEnd | CompositionFlags.General.LevelCrossing)) == CompositionFlags.General.LevelCrossing || (compositionData.m_Flags.m_General & (CompositionFlags.General.DeadEnd | CompositionFlags.General.Intersection | CompositionFlags.General.Crosswalk)) == CompositionFlags.General.Crosswalk || (compositionData.m_Flags.m_Right & CompositionFlags.Side.AbruptEnd) != 0)
		{
			compositionData.m_State |= CompositionState.BlockUTurn;
		}
		for (int l = 0; l < pieces.Length; l++)
		{
			NetCompositionPiece value5 = pieces[l];
			bool num11 = (value5.m_SectionFlags & NetSectionFlags.Underground) != 0;
			bool flag7 = (value5.m_SectionFlags & NetSectionFlags.Overhead) != 0;
			if ((value5.m_PieceFlags & (NetPieceFlags.PreserveShape | NetPieceFlags.BlockTraffic)) == NetPieceFlags.BlockTraffic)
			{
				compositionData.m_State |= CompositionState.BlockUTurn;
			}
			if ((value5.m_PieceFlags & NetPieceFlags.LowerBottomToTerrain) != 0)
			{
				compositionData.m_State |= CompositionState.LowerToTerrain;
			}
			if ((value5.m_PieceFlags & NetPieceFlags.RaiseTopToTerrain) != 0)
			{
				compositionData.m_State |= CompositionState.RaiseToTerrain;
			}
			if ((value5.m_SectionFlags & NetSectionFlags.HalfLength) != 0)
			{
				compositionData.m_State |= CompositionState.HalfLength;
			}
			if ((value5.m_SectionFlags & NetSectionFlags.Hidden) != 0)
			{
				compositionData.m_State |= CompositionState.Hidden;
			}
			if (num11)
			{
				value5.m_Offset.x -= num * 0.5f + num3;
				value5.m_Offset.x += compositionData.m_MiddleOffset - num2;
			}
			else if (flag7)
			{
				value5.m_Offset.x -= num4 * 0.5f + num6;
				value5.m_Offset.x += compositionData.m_MiddleOffset - num5;
			}
			else
			{
				value5.m_Offset.x -= compositionData.m_Width * 0.5f + num7;
			}
			pieces[l] = value5;
		}
		compositionData.m_Width = math.max(compositionData.m_Width, math.max(num, num4));
		if (compositionData.m_HeightRange.min > compositionData.m_HeightRange.max)
		{
			compositionData.m_HeightRange = default(Bounds1);
		}
		if (flag)
		{
			compositionData.m_State |= CompositionState.HasSurface;
			if ((compositionData.m_Flags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) == 0)
			{
				compositionData.m_State |= CompositionState.ExclusiveGround;
			}
		}
		else
		{
			float num12 = MathUtils.Center(compositionData.m_HeightRange);
			compositionData.m_SurfaceHeight = new Bounds1(num12, num12);
		}
	}

	private static void CalculateSyncVertexOffsets(ref NetCompositionData compositionData, NativeArray<NetCompositionPiece> pieces, ComponentLookup<NetVertexMatchData> netVertexMatchData)
	{
		float4 syncVertexOffsetsLeft = new float4(0f, 0f, 0f, 1f);
		float4 syncVertexOffsetsRight = new float4(0f, 1f, 1f, 1f);
		float middleOffset = compositionData.m_MiddleOffset;
		float num = compositionData.m_Width * 0.5f + middleOffset;
		float num2 = compositionData.m_Width * 0.5f - middleOffset;
		bool flag = false;
		float2 @float = default(float2);
		for (int i = 0; i < pieces.Length; i++)
		{
			NetCompositionPiece netCompositionPiece = pieces[i];
			if ((netCompositionPiece.m_SectionFlags & (NetSectionFlags.Underground | NetSectionFlags.Overhead)) != 0)
			{
				continue;
			}
			if ((netCompositionPiece.m_SectionFlags & NetSectionFlags.Median) != 0)
			{
				if (netVertexMatchData.HasComponent(netCompositionPiece.m_Piece))
				{
					NetVertexMatchData netVertexMatchData2 = netVertexMatchData[netCompositionPiece.m_Piece];
					if (!math.any(math.isnan(netVertexMatchData2.m_Offsets.xy)))
					{
						@float.x = netVertexMatchData2.m_Offsets.x;
						@float.y = math.select(netVertexMatchData2.m_Offsets.z, netVertexMatchData2.m_Offsets.y, math.isnan(netVertexMatchData2.m_Offsets.z));
						if ((netCompositionPiece.m_SectionFlags & NetSectionFlags.Invert) != 0)
						{
							@float = -@float.yx;
						}
						@float += netCompositionPiece.m_Offset.x;
						if (num > 0f)
						{
							syncVertexOffsetsLeft.w = (@float.x - middleOffset) / num + 1f;
						}
						if (num2 > 0f)
						{
							syncVertexOffsetsRight.x = (@float.y - middleOffset) / num2;
						}
						flag = true;
					}
				}
				if (!flag)
				{
					float2 float2 = netCompositionPiece.m_Offset.x;
					float2.x -= netCompositionPiece.m_Size.x * 0.5f;
					float2.y += netCompositionPiece.m_Size.x * 0.5f;
					if (num > 0f)
					{
						syncVertexOffsetsLeft.w = (float2.x - middleOffset) / num + 1f;
					}
					if (num2 > 0f)
					{
						syncVertexOffsetsRight.x = (float2.y - middleOffset) / num2;
					}
				}
			}
			else
			{
				if (!netVertexMatchData.HasComponent(netCompositionPiece.m_Piece))
				{
					continue;
				}
				NetVertexMatchData netVertexMatchData3 = netVertexMatchData[netCompositionPiece.m_Piece];
				if (math.isnan(netVertexMatchData3.m_Offsets.x))
				{
					continue;
				}
				float num3 = netVertexMatchData3.m_Offsets.x;
				for (int j = 0; j < 3; j++)
				{
					if ((netCompositionPiece.m_SectionFlags & NetSectionFlags.Invert) != 0)
					{
						num3 = 0f - num3;
					}
					num3 += netCompositionPiece.m_Offset.x;
					if ((netCompositionPiece.m_SectionFlags & NetSectionFlags.Right) != 0)
					{
						num3 = (num3 - middleOffset) / num2;
						if (syncVertexOffsetsRight.z != 1f)
						{
							syncVertexOffsetsRight.w = num3;
						}
						else if (syncVertexOffsetsRight.y != 1f)
						{
							syncVertexOffsetsRight.z = num3;
						}
						else
						{
							syncVertexOffsetsRight.y = num3;
						}
					}
					else
					{
						num3 = (num3 - middleOffset) / num + 1f;
						if (syncVertexOffsetsLeft.y != 0f)
						{
							syncVertexOffsetsLeft.x = num3;
						}
						else if (syncVertexOffsetsLeft.z != 0f)
						{
							syncVertexOffsetsLeft.y = num3;
						}
						else
						{
							syncVertexOffsetsLeft.z = num3;
						}
					}
					if (j == 0)
					{
						if (math.isnan(netVertexMatchData3.m_Offsets.y))
						{
							break;
						}
						num3 = netVertexMatchData3.m_Offsets.y;
					}
					else
					{
						if (math.isnan(netVertexMatchData3.m_Offsets.z))
						{
							break;
						}
						num3 = netVertexMatchData3.m_Offsets.z;
					}
				}
			}
		}
		if (syncVertexOffsetsLeft.x > syncVertexOffsetsLeft.y)
		{
			syncVertexOffsetsLeft.xy = syncVertexOffsetsLeft.yx;
		}
		if (syncVertexOffsetsRight.z > syncVertexOffsetsRight.w)
		{
			syncVertexOffsetsRight.zw = syncVertexOffsetsRight.wz;
		}
		if (syncVertexOffsetsLeft.y > syncVertexOffsetsLeft.z)
		{
			syncVertexOffsetsLeft.yz = syncVertexOffsetsLeft.zy;
		}
		if (syncVertexOffsetsRight.y > syncVertexOffsetsRight.z)
		{
			syncVertexOffsetsRight.yz = syncVertexOffsetsRight.zy;
		}
		if (syncVertexOffsetsLeft.x > syncVertexOffsetsLeft.y)
		{
			syncVertexOffsetsLeft.xy = syncVertexOffsetsLeft.yx;
		}
		if (syncVertexOffsetsRight.z > syncVertexOffsetsRight.w)
		{
			syncVertexOffsetsRight.zw = syncVertexOffsetsRight.wz;
		}
		if (syncVertexOffsetsLeft.z <= syncVertexOffsetsLeft.x)
		{
			syncVertexOffsetsLeft.z = math.lerp(syncVertexOffsetsLeft.x, syncVertexOffsetsLeft.w, 2f / 3f);
		}
		if (syncVertexOffsetsRight.y >= syncVertexOffsetsRight.w)
		{
			syncVertexOffsetsRight.y = math.lerp(syncVertexOffsetsRight.w, syncVertexOffsetsRight.x, 2f / 3f);
		}
		if (syncVertexOffsetsLeft.y <= syncVertexOffsetsLeft.x)
		{
			syncVertexOffsetsLeft.y = math.lerp(syncVertexOffsetsLeft.x, syncVertexOffsetsLeft.z, 0.5f);
		}
		if (syncVertexOffsetsRight.z >= syncVertexOffsetsRight.w)
		{
			syncVertexOffsetsRight.z = math.lerp(syncVertexOffsetsRight.w, syncVertexOffsetsRight.y, 0.5f);
		}
		if (syncVertexOffsetsLeft.y < syncVertexOffsetsLeft.x + 1E-05f)
		{
			syncVertexOffsetsLeft.y = syncVertexOffsetsLeft.x;
		}
		if (syncVertexOffsetsLeft.w < syncVertexOffsetsLeft.z + 1E-05f)
		{
			syncVertexOffsetsLeft.z = syncVertexOffsetsLeft.w;
		}
		if (syncVertexOffsetsRight.y < syncVertexOffsetsRight.x + 1E-05f)
		{
			syncVertexOffsetsRight.y = syncVertexOffsetsRight.x;
		}
		if (syncVertexOffsetsRight.w < syncVertexOffsetsRight.z + 1E-05f)
		{
			syncVertexOffsetsRight.z = syncVertexOffsetsRight.w;
		}
		compositionData.m_SyncVertexOffsetsLeft = syncVertexOffsetsLeft;
		compositionData.m_SyncVertexOffsetsRight = syncVertexOffsetsRight;
	}

	public static void CalculatePlaceableData(ref PlaceableNetComposition placeableData, NativeArray<NetCompositionPiece> pieces, ComponentLookup<PlaceableNetPieceData> placeableNetPieceData)
	{
		placeableData.m_ConstructionCost = 0u;
		placeableData.m_UpkeepCost = 0f;
		for (int i = 0; i < pieces.Length; i++)
		{
			NetCompositionPiece netCompositionPiece = pieces[i];
			if (placeableNetPieceData.HasComponent(netCompositionPiece.m_Piece))
			{
				PlaceableNetPieceData placeableNetPieceData2 = placeableNetPieceData[netCompositionPiece.m_Piece];
				placeableData.m_ConstructionCost += placeableNetPieceData2.m_ConstructionCost;
				placeableData.m_ElevationCost += placeableNetPieceData2.m_ElevationCost;
				placeableData.m_UpkeepCost += placeableNetPieceData2.m_UpkeepCost;
			}
		}
	}

	public static void AddCompositionLanes<TNetCompositionPieceList>(Entity entity, ref NetCompositionData compositionData, TNetCompositionPieceList pieces, NativeList<NetCompositionLane> netLanes, DynamicBuffer<NetCompositionCarriageway> carriageways, ComponentLookup<NetLaneData> netLaneData, BufferLookup<NetPieceLane> netPieceLanes) where TNetCompositionPieceList : INativeList<NetCompositionPiece>
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		bool flag = true;
		Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
		Bounds3 bounds2 = new Bounds3(float.MaxValue, float.MinValue);
		Bounds3 bounds3 = new Bounds3(float.MaxValue, float.MinValue);
		bool2 @bool = default(bool2);
		bool2 bool2 = default(bool2);
		bool2 bool3 = default(bool2);
		NetCompositionCarriageway elem = default(NetCompositionCarriageway);
		NativeList<TempLaneData> nativeList = new NativeList<TempLaneData>(256, Allocator.Temp);
		NativeList<TempLaneGroup> nativeList2 = new NativeList<TempLaneGroup>(32, Allocator.Temp);
		for (int i = 0; i < pieces.Length; i++)
		{
			NetCompositionPiece netCompositionPiece = pieces[i];
			if ((netCompositionPiece.m_PieceFlags & NetPieceFlags.BlockTraffic) != 0 && num3 != 0)
			{
				num2++;
				num3 = 0;
				if (!math.any(bool2))
				{
					bool2 = bool3;
					bounds2 = bounds3;
				}
				if (math.all(bool2))
				{
					flag = false;
				}
				if (math.any(@bool | bool2) && carriageways.IsCreated)
				{
					Bounds3 bounds4 = bounds | bounds2;
					bool2 x = @bool | bool2;
					if (math.any(@bool != bool2))
					{
						if (math.all(bool2) & math.any(@bool))
						{
							bounds4 = bounds;
							x = @bool;
						}
						else if (math.any(bool2))
						{
							bounds4 = bounds2;
							x = bool2;
						}
					}
					elem.m_Position = MathUtils.Center(bounds4);
					elem.m_Width = MathUtils.Size(bounds4).x;
					if (math.all(x))
					{
						elem.m_Flags &= ~LaneFlags.Invert;
						elem.m_Flags |= LaneFlags.Twoway;
					}
					else if (x.x)
					{
						elem.m_Flags &= ~(LaneFlags.Invert | LaneFlags.Twoway);
					}
					else
					{
						elem.m_Flags &= ~LaneFlags.Twoway;
						elem.m_Flags |= LaneFlags.Invert;
					}
					carriageways.Add(elem);
				}
				bounds = new Bounds3(float.MaxValue, float.MinValue);
				bounds2 = new Bounds3(float.MaxValue, float.MinValue);
				bounds3 = new Bounds3(float.MaxValue, float.MinValue);
				@bool = default(bool2);
				bool2 = default(bool2);
				bool3 = default(bool2);
				elem = default(NetCompositionCarriageway);
			}
			if (!netPieceLanes.HasBuffer(netCompositionPiece.m_Piece))
			{
				continue;
			}
			DynamicBuffer<NetPieceLane> dynamicBuffer = netPieceLanes[netCompositionPiece.m_Piece];
			bool flag2 = (netCompositionPiece.m_SectionFlags & NetSectionFlags.Invert) != 0;
			bool flag3 = (netCompositionPiece.m_SectionFlags & NetSectionFlags.FlipLanes) != 0;
			for (int j = 0; j < dynamicBuffer.Length; j++)
			{
				NetPieceLane netPieceLane = dynamicBuffer[math.select(j, dynamicBuffer.Length - j - 1, flag2)];
				NetLaneData netLaneData2 = netLaneData[netPieceLane.m_Lane];
				TempLaneData value = default(TempLaneData);
				TempLaneGroup value2 = default(TempLaneGroup);
				netLaneData2.m_Flags |= netPieceLane.m_ExtraFlags;
				if (flag2)
				{
					netPieceLane.m_Position.x = 0f - netPieceLane.m_Position.x;
				}
				if ((netLaneData2.m_Flags & LaneFlags.Twoway) != 0)
				{
					@bool |= (netLaneData2.m_Flags & LaneFlags.Track) != 0;
					bool2 |= (netLaneData2.m_Flags & (LaneFlags.Road | LaneFlags.BicyclesOnly)) == LaneFlags.Road;
					bool3 |= (netLaneData2.m_Flags & (LaneFlags.Road | LaneFlags.BicyclesOnly)) == (LaneFlags.Road | LaneFlags.BicyclesOnly);
				}
				else if (flag2 != flag3)
				{
					netLaneData2.m_Flags |= LaneFlags.Invert;
					@bool.y |= (netLaneData2.m_Flags & LaneFlags.Track) != 0;
					bool2.y |= (netLaneData2.m_Flags & (LaneFlags.Road | LaneFlags.BicyclesOnly)) == LaneFlags.Road;
					bool3.y |= (netLaneData2.m_Flags & (LaneFlags.Road | LaneFlags.BicyclesOnly)) == (LaneFlags.Road | LaneFlags.BicyclesOnly);
				}
				else
				{
					@bool.x |= (netLaneData2.m_Flags & LaneFlags.Track) != 0;
					bool2.x |= (netLaneData2.m_Flags & (LaneFlags.Road | LaneFlags.BicyclesOnly)) == LaneFlags.Road;
					bool3.x |= (netLaneData2.m_Flags & (LaneFlags.Road | LaneFlags.BicyclesOnly)) == (LaneFlags.Road | LaneFlags.BicyclesOnly);
				}
				float3 @float = netCompositionPiece.m_Offset + netPieceLane.m_Position;
				value2.m_Flags = netLaneData2.m_Flags;
				if ((netLaneData2.m_Flags & (LaneFlags.Road | LaneFlags.BicyclesOnly)) == LaneFlags.Road)
				{
					float3 float2 = new float3(netLaneData2.m_Width * 0.5f, 0f, 0f);
					bounds2 |= new Bounds3(@float - float2, @float + float2);
					elem.m_Flags |= netLaneData2.m_Flags;
				}
				if ((netLaneData2.m_Flags & (LaneFlags.Road | LaneFlags.BicyclesOnly)) == (LaneFlags.Road | LaneFlags.BicyclesOnly))
				{
					float3 float3 = new float3(netLaneData2.m_Width * 0.5f, 0f, 0f);
					bounds3 |= new Bounds3(@float - float3, @float + float3);
					elem.m_Flags |= netLaneData2.m_Flags;
				}
				if ((netLaneData2.m_Flags & LaneFlags.Track) != 0)
				{
					float3 float4 = new float3(netLaneData2.m_Width * 0.5f, 0f, 0f);
					bounds |= new Bounds3(@float - float4, @float + float4);
					elem.m_Flags |= netLaneData2.m_Flags;
				}
				if (num3 != 0)
				{
					TempLaneGroup tempLaneGroup = nativeList2[nativeList2.Length - 1];
					if (value2.IsCompatible(tempLaneGroup))
					{
						value.m_GroupIndex = nativeList2.Length - 1;
						nativeList.Add(in value);
						if ((tempLaneGroup.m_Flags & (LaneFlags.DisconnectedStart | LaneFlags.DisconnectedEnd | LaneFlags.BicyclesOnly)) != 0)
						{
							tempLaneGroup.m_Prefab = netPieceLane.m_Lane;
							tempLaneGroup.m_Flags = value2.m_Flags & (tempLaneGroup.m_Flags | LaneFlags.Track);
							tempLaneGroup.m_Position = @float;
							tempLaneGroup.m_FullLaneCount = 1;
						}
						else
						{
							tempLaneGroup.m_Flags &= value2.m_Flags | LaneFlags.Track;
							if ((value2.m_Flags & (LaneFlags.DisconnectedStart | LaneFlags.DisconnectedEnd | LaneFlags.BicyclesOnly)) == 0)
							{
								tempLaneGroup.m_Position += @float;
								tempLaneGroup.m_FullLaneCount++;
							}
						}
						tempLaneGroup.m_LaneCount++;
						nativeList2[nativeList2.Length - 1] = tempLaneGroup;
						continue;
					}
				}
				value.m_GroupIndex = num++;
				nativeList.Add(in value);
				value2.m_Prefab = netPieceLane.m_Lane;
				value2.m_Position = @float;
				value2.m_LaneCount = 1;
				value2.m_FullLaneCount = 1;
				value2.m_CarriagewayIndex = num2;
				nativeList2.Add(in value2);
				num3++;
			}
		}
		if (num3 != 0)
		{
			if (!math.any(bool2))
			{
				bool2 = bool3;
				bounds2 = bounds3;
			}
			if (math.all(bool2))
			{
				flag = false;
			}
			if (math.any(@bool | bool2) && carriageways.IsCreated)
			{
				Bounds3 bounds5 = bounds | bounds2;
				bool2 x2 = @bool | bool2;
				if (math.any(@bool != bool2))
				{
					if (math.all(bool2) & math.any(@bool))
					{
						bounds5 = bounds;
						x2 = @bool;
					}
					else if (math.any(bool2))
					{
						bounds5 = bounds2;
						x2 = bool2;
					}
				}
				elem.m_Position = MathUtils.Center(bounds5);
				elem.m_Width = MathUtils.Size(bounds5).x;
				if (math.all(x2))
				{
					elem.m_Flags &= ~LaneFlags.Invert;
					elem.m_Flags |= LaneFlags.Twoway;
				}
				else if (x2.x)
				{
					elem.m_Flags &= ~(LaneFlags.Invert | LaneFlags.Twoway);
				}
				else
				{
					elem.m_Flags &= ~LaneFlags.Twoway;
					elem.m_Flags |= LaneFlags.Invert;
				}
				carriageways.Add(elem);
			}
		}
		if (flag)
		{
			compositionData.m_State |= CompositionState.SeparatedCarriageways;
		}
		int num4 = 0;
		int num5 = -1;
		int num6 = 0;
		for (int k = 0; k < pieces.Length; k++)
		{
			NetCompositionPiece netCompositionPiece2 = pieces[k];
			if (!netPieceLanes.HasBuffer(netCompositionPiece2.m_Piece))
			{
				continue;
			}
			DynamicBuffer<NetPieceLane> dynamicBuffer2 = netPieceLanes[netCompositionPiece2.m_Piece];
			bool flag4 = (netCompositionPiece2.m_SectionFlags & NetSectionFlags.Invert) != 0;
			bool flag5 = (netCompositionPiece2.m_SectionFlags & NetSectionFlags.FlipLanes) != 0;
			for (int l = 0; l < dynamicBuffer2.Length; l++)
			{
				NetPieceLane netPieceLane2 = dynamicBuffer2[math.select(l, dynamicBuffer2.Length - l - 1, flag4)];
				NetLaneData netLaneData3 = netLaneData[netPieceLane2.m_Lane];
				TempLaneData tempLaneData = nativeList[num4];
				TempLaneGroup tempLaneGroup2 = nativeList2[tempLaneData.m_GroupIndex];
				netLaneData3.m_Flags |= netPieceLane2.m_ExtraFlags;
				if (flag4)
				{
					netPieceLane2.m_Position.x = 0f - netPieceLane2.m_Position.x;
				}
				if (flag4 != flag5)
				{
					netLaneData3.m_Flags |= LaneFlags.Invert;
				}
				NetCompositionLane value3 = new NetCompositionLane
				{
					m_Lane = netPieceLane2.m_Lane,
					m_Position = netCompositionPiece2.m_Offset + netPieceLane2.m_Position,
					m_Carriageway = (byte)tempLaneGroup2.m_CarriagewayIndex,
					m_Group = (byte)tempLaneData.m_GroupIndex,
					m_Index = (byte)num4,
					m_Flags = netLaneData3.m_Flags
				};
				if (tempLaneGroup2.m_LaneCount > 1)
				{
					value3.m_Flags |= LaneFlags.Slave;
				}
				if (tempLaneData.m_GroupIndex != num5)
				{
					num5 = tempLaneData.m_GroupIndex;
					value3.m_Flags |= (LaneFlags)((flag4 != flag5) ? 524288 : 262144);
					num6 = 0;
				}
				if (++num6 == tempLaneGroup2.m_LaneCount)
				{
					value3.m_Flags |= (LaneFlags)((flag4 != flag5) ? 262144 : 524288);
				}
				netLanes.Add(in value3);
				if ((netLaneData3.m_Flags & LaneFlags.Road) != 0)
				{
					netCompositionPiece2.m_PieceFlags |= (NetPieceFlags)(((value3.m_Flags & LaneFlags.BicyclesOnly) != 0) ? 65536 : 32768);
				}
				num4++;
			}
			int index = k;
			NetCompositionPiece value4 = netCompositionPiece2;
			pieces[index] = value4;
		}
		int2 @int = 0;
		int2 int2 = 0;
		for (int m = 0; m < netLanes.Length; m++)
		{
			NetCompositionLane value5 = netLanes[m];
			if ((value5.m_Flags & LaneFlags.Parking) != 0)
			{
				int num7 = FindClosestLane(netLanes, m, value5.m_Position, LaneFlags.Road | LaneFlags.BicyclesOnly, LaneFlags.Road);
				if (num7 != -1)
				{
					NetCompositionLane value6 = netLanes[num7];
					if (((value5.m_Flags ^ value6.m_Flags) & LaneFlags.Invert) != 0)
					{
						value5.m_Flags ^= LaneFlags.Invert;
					}
					LaneFlags laneFlags = ((num7 < m != ((value6.m_Flags & LaneFlags.Invert) != 0)) ? LaneFlags.ParkingRight : LaneFlags.ParkingLeft);
					value5.m_Flags |= laneFlags;
					netLanes[m] = value5;
					if ((value5.m_Flags & LaneFlags.Virtual) == 0)
					{
						value6.m_Flags |= laneFlags;
						netLanes[num7] = value6;
					}
				}
			}
			if ((value5.m_Flags & (LaneFlags.Road | LaneFlags.BicyclesOnly)) == LaneFlags.Road)
			{
				if ((value5.m_Flags & LaneFlags.Invert) != 0)
				{
					compositionData.m_State |= CompositionState.HasBackwardRoadLanes;
					int2.x++;
				}
				else
				{
					compositionData.m_State |= CompositionState.HasForwardRoadLanes;
					@int.x++;
				}
			}
			if ((value5.m_Flags & LaneFlags.Track) != 0)
			{
				if ((value5.m_Flags & LaneFlags.Invert) != 0)
				{
					compositionData.m_State |= CompositionState.HasBackwardTrackLanes;
					int2.y++;
				}
				else
				{
					compositionData.m_State |= CompositionState.HasForwardTrackLanes;
					@int.y++;
				}
			}
			if ((value5.m_Flags & LaneFlags.Pedestrian) != 0)
			{
				compositionData.m_State |= CompositionState.HasPedestrianLanes;
			}
		}
		if (math.any(@int != int2))
		{
			compositionData.m_State |= CompositionState.Asymmetric;
		}
		if (math.any(@int > 1) | math.any(int2 > 1))
		{
			compositionData.m_State |= CompositionState.Multilane;
		}
		for (int n = 0; n < nativeList2.Length; n++)
		{
			TempLaneGroup tempLaneGroup3 = nativeList2[n];
			if (tempLaneGroup3.m_LaneCount > 1)
			{
				NetCompositionLane value7 = new NetCompositionLane
				{
					m_Lane = tempLaneGroup3.m_Prefab,
					m_Position = tempLaneGroup3.m_Position / tempLaneGroup3.m_FullLaneCount,
					m_Carriageway = (byte)tempLaneGroup3.m_CarriagewayIndex,
					m_Group = (byte)n,
					m_Index = (byte)num4,
					m_Flags = (tempLaneGroup3.m_Flags | LaneFlags.Master)
				};
				netLanes.Add(in value7);
				num4++;
			}
		}
		nativeList.Dispose();
		nativeList2.Dispose();
		if (num4 >= 256)
		{
			throw new Exception($"Too many lanes: {entity.Index}");
		}
	}

	private static int FindClosestLane(NativeList<NetCompositionLane> lanes, int startIndex, float3 position, LaneFlags flagMask, LaneFlags flags)
	{
		int num = startIndex - 1;
		int num2 = startIndex + 1;
		while (true)
		{
			if (num >= 0 && num2 < lanes.Length)
			{
				NetCompositionLane netCompositionLane = lanes[num];
				NetCompositionLane netCompositionLane2 = lanes[num2];
				if (math.lengthsq(netCompositionLane.m_Position - position) <= math.lengthsq(netCompositionLane2.m_Position - position))
				{
					if ((netCompositionLane.m_Flags & flagMask) == flags)
					{
						return num;
					}
					num--;
				}
				else
				{
					if ((netCompositionLane2.m_Flags & flagMask) == flags)
					{
						return num2;
					}
					num2++;
				}
			}
			else if (num >= 0)
			{
				if ((lanes[num].m_Flags & flagMask) == flags)
				{
					return num;
				}
				num--;
			}
			else
			{
				if (num2 >= lanes.Length)
				{
					break;
				}
				if ((lanes[num2].m_Flags & flagMask) == flags)
				{
					return num2;
				}
				num2++;
			}
		}
		return -1;
	}

	public static float2 CalculateRoundaboutSize(NetCompositionData compositionData, DynamicBuffer<NetCompositionPiece> pieces)
	{
		float2 @float = 0f;
		float2 float2 = float.MaxValue;
		float4 float3 = 0f;
		float2 y = 0f;
		float2 float4 = compositionData.m_Width * 0.5f;
		int2 @int = new int2(pieces.Length, -1);
		int2 int2 = new int2(pieces.Length, -1);
		for (int i = 0; i < pieces.Length; i++)
		{
			NetCompositionPiece netCompositionPiece = pieces[i];
			if ((netCompositionPiece.m_PieceFlags & NetPieceFlags.HasRoadLanes) != 0)
			{
				@int.x = math.min(@int.x, i);
				@int.y = i;
			}
			else if ((netCompositionPiece.m_PieceFlags & NetPieceFlags.HasBicycleLanes) != 0)
			{
				int2.x = math.min(int2.x, i);
				int2.y = i;
			}
		}
		NetPieceFlags netPieceFlags = NetPieceFlags.HasRoadLanes;
		if (@int.y < @int.x && int2.y >= int2.x)
		{
			@int = int2;
			netPieceFlags = NetPieceFlags.HasBicycleLanes;
		}
		for (int j = @int.x; j <= @int.y; j++)
		{
			NetCompositionPiece netCompositionPiece2 = pieces[j];
			if (j == @int.x || (netCompositionPiece2.m_PieceFlags & (netPieceFlags | NetPieceFlags.BlockTraffic)) == NetPieceFlags.BlockTraffic)
			{
				float4.y = compositionData.m_Width * 0.5f;
				float num = 0f;
				for (int k = j + 1; k < @int.y; k++)
				{
					NetCompositionPiece netCompositionPiece3 = pieces[k];
					if ((netCompositionPiece3.m_PieceFlags & netPieceFlags) == 0)
					{
						if ((netCompositionPiece3.m_PieceFlags & NetPieceFlags.BlockTraffic) != 0)
						{
							float4.y = netCompositionPiece3.m_Offset.x - netCompositionPiece3.m_Size.x * 0.5f;
							break;
						}
						num += netCompositionPiece3.m_Size.x;
					}
				}
				float4.y -= num;
			}
			if ((netCompositionPiece2.m_PieceFlags & netPieceFlags) != 0)
			{
				float y2 = (((netCompositionPiece2.m_SectionFlags & NetSectionFlags.Invert) == 0) ? (float4.y + netCompositionPiece2.m_Size.x * 0.5f - netCompositionPiece2.m_Offset.x) : (float4.x + netCompositionPiece2.m_Size.x * 0.5f + netCompositionPiece2.m_Offset.x));
				if ((netCompositionPiece2.m_SectionFlags & NetSectionFlags.Invert) != 0 != ((netCompositionPiece2.m_SectionFlags & NetSectionFlags.FlipLanes) != 0))
				{
					@float.x = math.max(@float.x, y2);
					float2.y = math.min(float2.y, y2);
					y.x += netCompositionPiece2.m_Size.x;
					float3.w = math.max(float3.w, netCompositionPiece2.m_Size.x);
				}
				else
				{
					@float.y = math.max(@float.y, y2);
					float2.x = math.min(float2.x, y2);
					y.y += netCompositionPiece2.m_Size.x;
					float3.z = math.max(float3.z, netCompositionPiece2.m_Size.x);
				}
			}
			else if ((netCompositionPiece2.m_PieceFlags & NetPieceFlags.BlockTraffic) != 0)
			{
				float4.x = netCompositionPiece2.m_Size.x * 0.5f - netCompositionPiece2.m_Offset.x;
				float3.xy = math.max(float3.xy, y);
				y = 0f;
			}
			else
			{
				float4.x -= netCompositionPiece2.m_Size.x;
				float4.y += netCompositionPiece2.m_Size.x;
			}
		}
		float3.xy = math.max(float3.xy, y);
		float2 float5 = math.select(@float, math.max(float2, @float), float2 < float.MaxValue);
		float5 = math.select(float5, compositionData.m_Width * 0.5f, float5 == 0f);
		float5 += math.max(float3.xy, float3.zw) / 3f;
		if (netPieceFlags == NetPieceFlags.HasRoadLanes && int2.y >= int2.x)
		{
			float5 += 1.5f;
		}
		return float5;
	}

	public static CompositionFlags GetElevationFlags(Elevation startElevation, Elevation middleElevation, Elevation endElevation, NetGeometryData prefabGeometryData)
	{
		CompositionFlags result = default(CompositionFlags);
		float2 @float = math.max(math.max(math.cmin(startElevation.m_Elevation), math.cmin(endElevation.m_Elevation)), middleElevation.m_Elevation);
		float3 x = new float3(math.cmin(startElevation.m_Elevation), math.cmin(endElevation.m_Elevation), math.cmin(middleElevation.m_Elevation));
		float3 x2 = new float3(math.cmax(startElevation.m_Elevation), math.cmax(endElevation.m_Elevation), math.cmax(middleElevation.m_Elevation));
		float2 float2 = math.min(math.min(math.cmax(startElevation.m_Elevation), math.cmax(endElevation.m_Elevation)), middleElevation.m_Elevation);
		if (math.all(@float >= prefabGeometryData.m_ElevationLimit * 2f) || (prefabGeometryData.m_Flags & GeometryFlags.RequireElevated) != 0)
		{
			if ((prefabGeometryData.m_Flags & GeometryFlags.ElevatedIsRaised) != 0)
			{
				result.m_Left |= CompositionFlags.Side.Raised;
				result.m_Right |= CompositionFlags.Side.Raised;
			}
			else
			{
				result.m_General |= CompositionFlags.General.Elevated;
			}
		}
		else if (math.cmax(x) <= prefabGeometryData.m_ElevationLimit * -2f && math.cmin(x2) <= prefabGeometryData.m_ElevationLimit * -3f)
		{
			result.m_General |= CompositionFlags.General.Tunnel;
		}
		else
		{
			if (@float.x >= prefabGeometryData.m_ElevationLimit)
			{
				if ((prefabGeometryData.m_Flags & GeometryFlags.RaisedIsElevated) != 0)
				{
					result.m_General |= CompositionFlags.General.Elevated;
				}
				else
				{
					result.m_Left |= CompositionFlags.Side.Raised;
				}
			}
			else if (float2.x <= 0f - prefabGeometryData.m_ElevationLimit)
			{
				if ((prefabGeometryData.m_Flags & GeometryFlags.LoweredIsTunnel) != 0)
				{
					result.m_General |= CompositionFlags.General.Tunnel;
				}
				else
				{
					result.m_Left |= CompositionFlags.Side.Lowered;
				}
			}
			if (@float.y >= prefabGeometryData.m_ElevationLimit)
			{
				if ((prefabGeometryData.m_Flags & GeometryFlags.RaisedIsElevated) != 0)
				{
					result.m_General |= CompositionFlags.General.Elevated;
				}
				else
				{
					result.m_Right |= CompositionFlags.Side.Raised;
				}
			}
			else if (float2.y <= 0f - prefabGeometryData.m_ElevationLimit)
			{
				if ((prefabGeometryData.m_Flags & GeometryFlags.LoweredIsTunnel) != 0)
				{
					result.m_General |= CompositionFlags.General.Tunnel;
				}
				else
				{
					result.m_Right |= CompositionFlags.Side.Lowered;
				}
			}
		}
		return result;
	}
}
