using System.Collections.Generic;
using System.Linq;
using Colossal.PSI.Common;
using Colossal.UI.Binding;
using Game.UI.Menu;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class ExternalLinkField : Widget
{
	public class Bindings : IWidgetBindingFactory
	{
		public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
		{
			yield return new TriggerBinding<IWidget, int, string, string>(group, "setExternalLink", delegate(IWidget widget, int index, string type, string url)
			{
				if (widget is ExternalLinkField externalLinkField)
				{
					externalLinkField.SetValue(index, type, url);
					onValueChanged(widget);
				}
			}, pathResolver);
			yield return new TriggerBinding<IWidget, int>(group, "removeExternalLink", delegate(IWidget widget, int index)
			{
				if (widget is ExternalLinkField externalLinkField)
				{
					externalLinkField.Remove(index);
					onValueChanged(widget);
				}
			}, pathResolver);
			yield return new TriggerBinding<IWidget>(group, "addExternalLink", delegate(IWidget widget)
			{
				if (widget is ExternalLinkField externalLinkField)
				{
					externalLinkField.Add();
					onValueChanged(widget);
				}
			}, pathResolver);
		}
	}

	private static readonly IModsUploadSupport.ExternalLinkData kDefaultLink = new IModsUploadSupport.ExternalLinkData
	{
		m_Type = IModsUploadSupport.ExternalLinkInfo.kAcceptedTypes[0].m_Type,
		m_URL = string.Empty
	};

	private static readonly string[] kAcceptedTypes = IModsUploadSupport.ExternalLinkInfo.kAcceptedTypes.Select((IModsUploadSupport.ExternalLinkInfo info) => info.m_Type).ToArray();

	public List<IModsUploadSupport.ExternalLinkData> links { get; set; } = new List<IModsUploadSupport.ExternalLinkData>();

	public int maxLinks { get; set; } = 5;

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("links");
		writer.ArrayBegin(links.Count);
		foreach (IModsUploadSupport.ExternalLinkData link in links)
		{
			WriteExternalLink(writer, link);
		}
		writer.ArrayEnd();
		writer.PropertyName("acceptedTypes");
		writer.Write(kAcceptedTypes);
		writer.PropertyName("maxLinks");
		writer.Write(maxLinks);
	}

	private void WriteExternalLink(IJsonWriter writer, IModsUploadSupport.ExternalLinkData link)
	{
		writer.TypeBegin("ExternalLinkData");
		writer.PropertyName("type");
		writer.Write(link.m_Type);
		writer.PropertyName("url");
		writer.Write(link.m_URL);
		writer.PropertyName("error");
		writer.Write(!AssetUploadUtils.ValidateExternalLink(link));
		writer.PropertyName("lockType");
		writer.Write(AssetUploadUtils.LockLinkType(link.m_URL, out var _));
		writer.TypeEnd();
	}

	private void SetValue(int index, string type, string url)
	{
		IModsUploadSupport.ExternalLinkData value = new IModsUploadSupport.ExternalLinkData
		{
			m_Type = type,
			m_URL = url
		};
		if (AssetUploadUtils.LockLinkType(value.m_URL, out var type2))
		{
			value.m_Type = type2;
		}
		links[index] = value;
		SetPropertiesChanged();
	}

	private void Remove(int index)
	{
		links.RemoveAt(index);
		SetPropertiesChanged();
	}

	private void Add()
	{
		links.Add(kDefaultLink);
		SetPropertiesChanged();
	}
}
