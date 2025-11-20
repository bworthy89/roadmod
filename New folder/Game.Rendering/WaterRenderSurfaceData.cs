using Unity.Collections;

namespace Game.Rendering;

public struct WaterRenderSurfaceData
{
	public NativeArray<WaterHeightRequest> waterHeightsResults { get; private set; }

	public WaterRenderSurfaceData(NativeArray<WaterHeightRequest> _waterHeightsResults)
	{
		waterHeightsResults = _waterHeightsResults;
	}

	public bool GetWaterRenderHeight(int enitityId, int queryIndex, out float height)
	{
		height = 0f;
		for (int i = 0; i < waterHeightsResults.Length; i++)
		{
			WaterHeightRequest waterHeightRequest = waterHeightsResults[i];
			if (waterHeightRequest.entityId == enitityId && waterHeightRequest.queryId == queryIndex)
			{
				height = waterHeightRequest.position.y;
				return true;
			}
		}
		return false;
	}
}
