using Colossal.UI.Binding;

namespace Game.UI.InGame;

public class InfoviewsUIUtils
{
	public static void UpdateFiveSlicePieChartData(IJsonWriter binder, int a, int b, int c, int d, int e)
	{
		binder.TypeBegin("infoviews.ChartData");
		binder.PropertyName("values");
		binder.ArrayBegin(5u);
		binder.Write(a);
		binder.Write(b);
		binder.Write(c);
		binder.Write(d);
		binder.Write(e);
		binder.ArrayEnd();
		binder.PropertyName("total");
		binder.Write(a + b + c + d + e);
		binder.TypeEnd();
	}
}
