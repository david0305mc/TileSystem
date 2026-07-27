#pragma warning disable 114
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

public partial class DataManager : Singleton<DataManager> {
	[Preserve]
	public partial class Artifact {
		public int id;
		public string artifactname;
		public string artifactdesc;
		public string artifactlevelupdesc;
		public string iconname;
		public string modelname;
		public int maxlevel;
		public int levelupcost;
		public UNLOCKTYPE unlocktype;
		public int unlocktarget;
		public int unlockvalue;
		public string unlockdesc;
	}
	public Artifact[] ArtifactArray { get; private set; }
	public Dictionary<int, Artifact> ArtifactDic { get; private set; }
	[Preserve]
	public void BindArtifactData(Type type, string text) {
		var deserializedData = CSVSerializer.Deserialize(text, type, new CSVSerializer.Options() { SkipDataRows = 2 });
		GetType().GetProperty(nameof(ArtifactArray))?.SetValue(this, deserializedData, null);
		ArtifactDic = ArtifactArray?.ToDictionary(i => i.id) ?? new Dictionary<int, Artifact>();
	}
	[Preserve]
	public Artifact GetArtifactData(int _id) {
		if (ArtifactDic != null && ArtifactDic.TryGetValue(_id, out Artifact value)) {
			return value;
		}
		Debug.LogError($"테이블에 ID가 없습니다: {_id}");
		return null;
	}
	[Preserve]
	public partial class Localization {
		public string id;
		public string ko;
		public string en;
		public string jp;
	}
	public Localization[] LocalizationArray { get; private set; }
	public Dictionary<string, Localization> LocalizationDic { get; private set; }
	[Preserve]
	public void BindLocalizationData(Type type, string text) {
		var deserializedData = CSVSerializer.Deserialize(text, type, new CSVSerializer.Options() { SkipDataRows = 2 });
		GetType().GetProperty(nameof(LocalizationArray))?.SetValue(this, deserializedData, null);
		LocalizationDic = LocalizationArray?.ToDictionary(i => i.id) ?? new Dictionary<string, Localization>();
	}
	[Preserve]
	public Localization GetLocalizationData(string _id) {
		if (LocalizationDic != null && LocalizationDic.TryGetValue(_id, out Localization value)) {
			return value;
		}
		Debug.LogError($"테이블에 ID가 없습니다: {_id}");
		return null;
	}
}
