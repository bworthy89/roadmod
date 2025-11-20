using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Game.UI.Widgets;

public class Breadcrumbs : Widget, IEnumerable<Label>, IEnumerable, IContainerWidget
{
	private List<IWidget> m_Labels = new List<IWidget>();

	public int labelCount => m_Labels.Count;

	public IList<IWidget> children => m_Labels;

	public override IList<IWidget> visibleChildren => m_Labels;

	public Breadcrumbs WithLabel(Label label)
	{
		m_Labels.Add(label);
		return this;
	}

	public Breadcrumbs WithOutLabel(Label label)
	{
		m_Labels.Remove(label);
		return this;
	}

	public IEnumerator<Label> GetEnumerator()
	{
		return m_Labels.OfType<Label>().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
