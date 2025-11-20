namespace Game.Areas;

public class AreaTools
{
	public static string GetMapFeatureIconName(MapFeature feature)
	{
		return feature switch
		{
			MapFeature.None => "None", 
			MapFeature.Area => "Area", 
			MapFeature.BuildableLand => "Building", 
			MapFeature.FertileLand => "Fertility", 
			MapFeature.Forest => "Forest", 
			MapFeature.Oil => "Oil", 
			MapFeature.Ore => "Coal", 
			MapFeature.SurfaceWater => "Water", 
			MapFeature.GroundWater => "Water", 
			_ => "None", 
		};
	}
}
