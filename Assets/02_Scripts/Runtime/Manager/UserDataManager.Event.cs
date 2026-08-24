using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class UserDataManager : Singleton<UserDataManager>
{
    public void AddLevel(int amt = 1)
    {
        User.Hero.Level.Value += amt;
        SaveLocalDataAsync().Forget();
    }
    public void AddGem(long amount = 1)
    {
        if (amount <= 0)
            return;

        User.Currency.Gem.Value += amount;
        SaveLocalDataAsync().Forget();
    }

    public void AddGold(long amount)
    {
        if (amount <= 0)
            return;

        User.Currency.Gold.Value += amount;
        SaveLocalDataAsync().Forget();
    }

    public void AddHeart(long amount)
    {
        if (amount <= 0)
            return;

        User.Currency.Heart.Value += amount;
        SaveLocalDataAsync().Forget();
    }

    public void SetSkillLevel(int skillId, long level)
    {
        if (skillId <= 0)
            return;

        if (!User.Skills.TryGetValue(skillId, out var skill))
        {
            skill = new SkillData(skillId);
            User.Skills[skillId] = skill;
        }

        skill.Level.Value = level;
        SaveLocalDataAsync().Forget();
    }

    public PlaceableObjData CreatePlaceableObj(int tid, int gridX, int gridY)
    {
        var placeableObjData = User.CreatePlaceableObj(tid, gridX, gridY);
        if (placeableObjData == null)
            return null;

        var furnitureInfo = DataManager.Instance.GetFurnitureData(tid);

        bool blocksMovement = furnitureInfo.blocksmovement != 0;
        for (int x = gridX; x < gridX + furnitureInfo.sizex; x++)
        {
            for (int y = gridY; y < gridY + furnitureInfo.sizey; y++)
            {
                User.TryGetTileData(x, y, out var tileData);
                tileData.FurnitureUid = placeableObjData.Uid;
            }
        }

        SaveLocalDataAsync().Forget();
        return placeableObjData;
    }
    public void MovePlaceableObj(long placeableUid, Vector2Int targetGridPos)
    {

        var placeableData = User.PlaceableObjs[placeableUid];
        var beforeTileData = User.Tiles[new Vector2Int(placeableData.GridX, placeableData.GridY)];
        var afterTileData = User.Tiles[targetGridPos];
        
        beforeTileData.FurnitureUid = 0;
        afterTileData.FurnitureUid = placeableData.Uid;
        placeableData.GridX = targetGridPos.x;
        placeableData.GridY = targetGridPos.y;
        SaveLocalDataAsync().Forget();
    }

}
