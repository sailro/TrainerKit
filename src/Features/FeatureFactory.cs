using System;
using System.Linq;

#nullable enable

namespace TrainerKit.Features;

internal static class FeatureFactory
{
	private static Feature[]? _features = null;
	private static readonly Lazy<Type[]> Types = new(() => [.. typeof(FeatureFactory)
		.Assembly
		.GetTypes()
		.Where(t => t.IsSubclassOf(typeof(Feature)) && !t.IsAbstract)]);

	public static Feature[] RegisterAllFeatures()
	{
		_features = [.. GetAllFeatureTypes()
			.Select(Activator.CreateInstance)
			.OfType<Feature>()];

		return _features;
	}

	public static Type[] GetAllFeatureTypes()
	{
		return Types.Value;
	}

	public static T? GetFeature<T>() where T : Feature
	{
		return GetAllFeatures()
			.OfType<T>()
			.FirstOrDefault();
	}

	public static Feature[] GetAllFeatures()
	{
		return _features ?? [];
	}

	public static ToggleFeature[] GetAllToggleableFeatures()
	{
		return [.. GetAllFeatures().OfType<ToggleFeature>()];
	}
}
