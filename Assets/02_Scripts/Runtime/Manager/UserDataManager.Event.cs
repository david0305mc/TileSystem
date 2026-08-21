using Cysharp.Threading.Tasks;

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
                tileData.FurnitureId = placeableObjData.Uid;
                tileData.BlocksMovement = blocksMovement;
            }
        }

        SaveLocalDataAsync().Forget();
        return placeableObjData;
    }
}
