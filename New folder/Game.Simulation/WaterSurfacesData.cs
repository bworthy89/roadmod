namespace Game.Simulation;

public struct WaterSurfacesData
{
	public WaterSurfaceData<SurfaceWater> depths;

	public WaterSurfaceData<SurfaceWater> downscaledDepths;

	public bool hasBackdrop;

	public WaterSurfacesData(WaterSurfaceData<SurfaceWater> _depths, WaterSurfaceData<SurfaceWater> _downscaledDepths, bool _hasBackdrop)
	{
		depths = _depths;
		downscaledDepths = _downscaledDepths;
		hasBackdrop = _hasBackdrop;
	}
}
