﻿using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Interfaces;
using AssetRipper.SourceGenerated.Classes.ClassID_27;
using AssetRipper.SourceGenerated.Classes.ClassID_43;
using AssetRipper.SourceGenerated.Classes.ClassID_49;
using AssetRipper.SourceGenerated.Classes.ClassID_83;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AssetRipper.Library.Exporters.PathIdMapping;

public sealed class PathIdMapExporter : IPostExporter
{
	public void DoPostExport(Ripper ripper)
	{
		SerializedGameInfo gameInfo = new();
		GameBundle gameBundle = ripper.GameStructure.FileCollection;
		foreach (SerializedAssetCollection collection in gameBundle.FetchAssetCollections().OfType<SerializedAssetCollection>())
		{
			SerializedFileInfo fileInfo = new()
			{
				Name = collection.Name,
			};
			gameInfo.Files.Add(fileInfo);
			foreach (IUnityObjectBase asset in collection)
			{
				if (asset is IMesh or ITexture or IAudioClip or ITextAsset)//Commonly useful asset types
				{
					fileInfo.Assets.Add(new()
					{
						Name = (asset as IHasNameString)?.NameString,
						Type = asset.ClassName,
						PathID = asset.PathID,
					});
				}
			}
		}

		string outputDirectory = ripper.Settings.AuxiliaryFilesPath;
		Directory.CreateDirectory(outputDirectory);
		using FileStream stream = File.Create(Path.Combine(outputDirectory, "path_id_map.json"));
		JsonSerializer.Serialize(stream, gameInfo, SerializedGameInfoSerializerContext.Default.SerializedGameInfo);
		stream.Flush();
	}
}
