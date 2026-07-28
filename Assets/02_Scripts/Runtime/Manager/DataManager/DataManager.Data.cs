#pragma warning disable 114
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

public partial class DataManager : Singleton<DataManager> {
	[Preserve]
	public partial class Furniture {
		public int id;
		public string namekey;
		public string furnituretype;
		public string prefabkey;
		public int sizex;
		public int sizey;
		public int price;
		public int unlocklevel;
		public int capacity;
		public int blocksmovement;
	}
	public Furniture[] FurnitureArray { get; private set; }
	public Dictionary<int, Furniture> FurnitureDic { get; private set; }
	[Preserve]
	public void BindFurnitureData(Type type, string text) {
		var deserializedData = CSVSerializer.Deserialize(text, type, new CSVSerializer.Options() { SkipDataRows = 1 });
		GetType().GetProperty(nameof(FurnitureArray))?.SetValue(this, deserializedData, null);
		FurnitureDic = FurnitureArray?.ToDictionary(i => i.id) ?? new Dictionary<int, Furniture>();
	}
	[Preserve]
	public Furniture GetFurnitureData(int _id) {
		if (FurnitureDic != null && FurnitureDic.TryGetValue(_id, out Furniture value)) {
			return value;
		}
		Debug.LogError($"테이블에 ID가 없습니다: {_id}");
		return null;
	}
	[Preserve]
	public partial class Recipe {
		public int id;
		public string namekey;
		public string iconkey;
		public int cookingfurnitureid;
		public int cookingtime;
		public int sellprice;
		public int exp;
		public int unlocklevel;
	}
	public Recipe[] RecipeArray { get; private set; }
	public Dictionary<int, Recipe> RecipeDic { get; private set; }
	[Preserve]
	public void BindRecipeData(Type type, string text) {
		var deserializedData = CSVSerializer.Deserialize(text, type, new CSVSerializer.Options() { SkipDataRows = 1 });
		GetType().GetProperty(nameof(RecipeArray))?.SetValue(this, deserializedData, null);
		RecipeDic = RecipeArray?.ToDictionary(i => i.id) ?? new Dictionary<int, Recipe>();
	}
	[Preserve]
	public Recipe GetRecipeData(int _id) {
		if (RecipeDic != null && RecipeDic.TryGetValue(_id, out Recipe value)) {
			return value;
		}
		Debug.LogError($"테이블에 ID가 없습니다: {_id}");
		return null;
	}
	[Preserve]
	public partial class Customer {
		public int id;
		public string namekey;
		public string prefabkey;
		public string movespeed;
		public int eattime;
		public int patiencetime;
		public int ordergroupid;
		public int rewardmultiplier;
	}
	public Customer[] CustomerArray { get; private set; }
	public Dictionary<int, Customer> CustomerDic { get; private set; }
	[Preserve]
	public void BindCustomerData(Type type, string text) {
		var deserializedData = CSVSerializer.Deserialize(text, type, new CSVSerializer.Options() { SkipDataRows = 1 });
		GetType().GetProperty(nameof(CustomerArray))?.SetValue(this, deserializedData, null);
		CustomerDic = CustomerArray?.ToDictionary(i => i.id) ?? new Dictionary<int, Customer>();
	}
	[Preserve]
	public Customer GetCustomerData(int _id) {
		if (CustomerDic != null && CustomerDic.TryGetValue(_id, out Customer value)) {
			return value;
		}
		Debug.LogError($"테이블에 ID가 없습니다: {_id}");
		return null;
	}
	[Preserve]
	public partial class Level {
		public int id;
		public int requiredexp;
		public int rewardgold;
		public int maxtablecount;
		public int maxstaffcount;
	}
	public Level[] LevelArray { get; private set; }
	public Dictionary<int, Level> LevelDic { get; private set; }
	[Preserve]
	public void BindLevelData(Type type, string text) {
		var deserializedData = CSVSerializer.Deserialize(text, type, new CSVSerializer.Options() { SkipDataRows = 1 });
		GetType().GetProperty(nameof(LevelArray))?.SetValue(this, deserializedData, null);
		LevelDic = LevelArray?.ToDictionary(i => i.id) ?? new Dictionary<int, Level>();
	}
	[Preserve]
	public Level GetLevelData(int _id) {
		if (LevelDic != null && LevelDic.TryGetValue(_id, out Level value)) {
			return value;
		}
		Debug.LogError($"테이블에 ID가 없습니다: {_id}");
		return null;
	}
}
