using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class PageView : LayoutContainer
{
	private int m_CurrentPage;

	public int currentPage
	{
		get
		{
			return m_CurrentPage;
		}
		set
		{
			if (value != m_CurrentPage)
			{
				m_CurrentPage = value;
				SetPropertiesChanged();
			}
		}
	}

	public PageView()
	{
		base.flex = FlexLayout.Fill;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("currentPage");
		writer.Write(currentPage);
	}
}
