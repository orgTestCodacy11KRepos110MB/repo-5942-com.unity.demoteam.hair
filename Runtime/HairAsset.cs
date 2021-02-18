﻿using System;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using Unity.DemoTeam.Attributes;

namespace Unity.DemoTeam.Hair
{
	[CreateAssetMenu(menuName = "Hair/Hair Asset", order = 250), PreferBinarySerialization]
	public class HairAsset : ScriptableObject
	{
		public enum Type
		{
			Procedural,
			Alembic,
		}

		public enum MemoryLayout
		{
			Interleaved,
			Sequential,
		}

		[Serializable]
		public struct SettingsBasic
		{
			[Tooltip("Type of generator")]
			public Type type;
			[Tooltip("Memory layout for the generated strands")]
			public MemoryLayout memoryLayout;
			[Tooltip("Material applied to the generated strand groups")]
			public Material material;

			public static readonly SettingsBasic defaults = new SettingsBasic()
			{
				type = Type.Procedural,
				memoryLayout = MemoryLayout.Interleaved,
			};
		}

		[Serializable]
		public struct SettingsAlembic
		{
			[Tooltip("Alembic asset containing at least one set of curves")]
			public AlembicStreamPlayer alembicAsset;

			[LineHeader("Processing")]

			[Tooltip("Resample curves to ensure a specific number of particles along each strand")]
			public bool resampleCurves;
			[Range(3, HairSim.MAX_STRAND_PARTICLE_COUNT), Tooltip("Number of particles along each strand")]
			public int resampleParticleCount;

			public static readonly SettingsAlembic defaults = new SettingsAlembic()
			{
				resampleCurves = true,
				resampleParticleCount = 16,
			};
		}

		[Serializable]
		public struct SettingsProcedural
		{
			public enum PlacementType
			{
				Primitive,
				//Custom,//TODO
				Mesh,
			}

			public enum PrimitiveType
			{
				Curtain,
				Brush,
				Cap,
				StratifiedCurtain,
			}

			public enum CurlSamplingStrategy
			{
				RelaxStrandLength,
				RelaxCurlSlope,
			}

			[LineHeader("Roots")]

			[Tooltip("Placement method")]
			public PlacementType placement;
			[VisibleIf(nameof(placement), PlacementType.Primitive), Tooltip("Place strands using builtin primitive generator")]
			public PrimitiveType placementPrimitive;
			//[VisibleIf(nameof(placement), PlacementType.Custom)]
			//public ScriptableObject placementCustom;
			[VisibleIf(nameof(placement), PlacementType.Mesh), Tooltip("Place strands on specified triangle mesh")]
			public Mesh placementMesh;
			[VisibleIf(nameof(placement), PlacementType.Mesh), Tooltip("Place strands on mesh according to specified density map (where 0 == Empty region, 1 == Fully populated region)")]
			public Texture2D placementDensity;

			//TODO implement
			//[VisibleIf(nameof(placement), PlacementType.Mesh), Tooltip("Source strand direction from specified object space normal map")]
			//public Texture2D paintedDirection;
			//[VisibleIf(nameof(placement), PlacementType.Mesh), Tooltip("Source strand parameter variation from specified 4-channel mask map (where R,G,B,A == Strand length, Strand diameter, Curl radius, Curl slope)")]
			//public Texture2D paintedVariation;

			[LineHeader("Strands")]

			[Range(64, HairSim.MAX_STRAND_COUNT), Tooltip("Number of strands")]
			public int strandCount;
			[Range(3, HairSim.MAX_STRAND_PARTICLE_COUNT), Tooltip("Number of particles along each strand")]
			public int strandParticleCount;
			[Range(0.001f, 5.0f), Tooltip("Strand length (in meters)")]
			public float strandLength;
			[ToggleGroup, Tooltip("Enable this to vary the strand lengths")]
			public bool strandLengthVariation;
			[ToggleGroupItem, Range(0.0f, 1.0f), Tooltip("Amount of variation as fraction of strand length")]
			public float strandLengthVariationAmount;

			[LineHeader("Curls")]

			[ToggleGroup, Tooltip("Enable this to curl the strands")]
			public bool curl;
			[ToggleGroupItem(withLabel = true), Range(0.0f, 10.0f), Tooltip("Curl radius (in centimeters)")]
			public float curlRadius;
			[ToggleGroupItem(withLabel = true), Range(0.0f, 1.0f), Tooltip("Curl slope")]
			public float curlSlope;
			[ToggleGroup, Tooltip("Enable this to vary the curls")]
			public bool curlVariation;
			[ToggleGroupItem(withLabel = true), Range(0.0f, 1.0f), Tooltip("Amount of variation as fraction of curl radius")]
			public float curlVariationRadius;
			[ToggleGroupItem(withLabel = true), Range(0.0f, 1.0f), Tooltip("Amount of variation as fraction of curl slope")]
			public float curlVariationSlope;
			[Tooltip("Choose which parameter to relax if the curls become undersampled (due to a combination of particle count, strand length, curl radius and slope)")]
			public CurlSamplingStrategy curlSamplingStrategy;

			public static readonly SettingsProcedural defaults = new SettingsProcedural()
			{
				placement = PlacementType.Primitive,
				placementPrimitive = PrimitiveType.Curtain,
				placementMesh = null,
				placementDensity = null,

				strandCount = 64,
				strandParticleCount = 32,

				strandLength = 0.25f,
				strandLengthVariation = false,
				strandLengthVariationAmount = 0.2f,

				curl = false,
				curlRadius = 1.0f,
				curlSlope = 0.3f,
				curlVariation = false,
				curlVariationRadius = 0.1f,
				curlVariationSlope = 0.3f,
				curlSamplingStrategy = CurlSamplingStrategy.RelaxStrandLength,
			};
		}

		[Serializable]
		public struct StrandGroup
		{
			public int strandCount;
			public int strandParticleCount;

			public float maxStrandLength;
			public float maxParticleInterval;

			[HideInInspector] public float[] rootScale;
			[HideInInspector] public Vector3[] rootPosition;
			[HideInInspector] public Vector3[] rootDirection;

			[HideInInspector] public Vector3[] particlePosition;
			[HideInInspector] public MemoryLayout particleMemoryLayout;

			[HideInInspector] public Mesh meshAssetLines;
			[HideInInspector] public Mesh meshAssetRoots;
		}

		public Material defaultMaterial;

		public SettingsBasic settingsBasic = SettingsBasic.defaults;
		public SettingsAlembic settingsAlembic = SettingsAlembic.defaults;
		public SettingsProcedural settingsProcedural = SettingsProcedural.defaults;

		public StrandGroup[] strandGroups;
		public bool strandGroupsAutoBuild;

		public string checksum;

		public HairSim.SolverSettings settingsSolver = HairSim.SolverSettings.defaults;
		public HairSim.VolumeSettings settingsVolume = HairSim.VolumeSettings.defaults;
	}
}