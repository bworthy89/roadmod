namespace Game.Prefabs;

public enum NetObjectPlacement
{
	Node,
	EdgeEndsOrNode,
	EdgeMiddle,
	EdgeEnds,
	CourseStart,
	CourseEnd,
	NodeBeforeFixedSegment,
	NodeBetweenFixedSegment,
	NodeAfterFixedSegment,
	EdgeMiddleFixedSegment,
	EdgeEndsFixedSegment,
	EdgeStartFixedSegment,
	EdgeEndFixedSegment,
	EdgeEndsOrNodeFixedSegment,
	EdgeStartOrNodeFixedSegment,
	EdgeEndOrNodeFixedSegment,
	WaterwayCrossingNode,
	NotWaterwayCrossingNode,
	NotWaterwayCrossingEdgeMiddle,
	NotWaterwayCrossingEdgeEndsOrNode
}
