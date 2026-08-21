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

    public void CreatePlaceableObj(int tid, int gridX, int gridY)
    {
        if (User == null || tid <= 0)
            return;

        var furnitureData = DataManager.Instance.GetFurnitureData(tid);
        if (furnitureData == null)
            return;

        int sizeX = furnitureData.sizex > 0 ? furnitureData.sizex : 1;
        int sizeY = furnitureData.sizey > 0 ? furnitureData.sizey : 1;

        for (int x = gridX; x < gridX + sizeX; x++)
        {
            for (int y = gridY; y < gridY + sizeY; y++)
            {
                if (!User.TryGetTileData(x, y, out var tileData) ||
                    !tileData.IsUnlocked ||
                    tileData.IsOccupied)
                {
                    return;
                }
            }
        }

        long uid = User.GeneratePersistentUid();
        var placeableObjData = new PlaceableObjData
        {
            Uid = uid,
            TableID = tid,
            GridX = gridX,
            GridY = gridY
        };

        User.PlaceableObjs.Add(uid, placeableObjData);

        bool blocksMovement = furnitureData.blocksmovement != 0;
        for (int x = gridX; x < gridX + sizeX; x++)
        {
            for (int y = gridY; y < gridY + sizeY; y++)
            {
                User.TryGetTileData(x, y, out var tileData);
                tileData.FurnitureId = uid;
                tileData.BlocksMovement = blocksMovement;
            }
        }

        SaveLocalDataAsync().Forget();
    }
}
