namespace Game.Rendering;

public struct ViewerDistances
{
	public float focus { get; set; }

	public float closestSurface { get; set; }

	public float farthestSurface { get; set; }

	public float averageSurface { get; set; }

	public float center { get; set; }

	public float ground { get; set; }

	public float maxDistanceToSeaLevel { get; set; }
}
