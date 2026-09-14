using System;
using System.ComponentModel;
using JetBrains.Annotations;

namespace ServerSync;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
internal class ConfigurationManagerAttributes : Attribute
{
	[UsedImplicitly]
	public bool? ShowRangeButtons;

	[UsedImplicitly]
	public string? Category;

	[UsedImplicitly]
	public string? Name;

	[UsedImplicitly]
	public string? Description;

	[UsedImplicitly]
	public int? Order;

	[UsedImplicitly]
	public bool? ReadOnly;

	[UsedImplicitly]
	public bool? Browsable;

	[UsedImplicitly]
	public string? CustomDrawer;
}
