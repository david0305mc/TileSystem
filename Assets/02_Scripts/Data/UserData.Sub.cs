
using R3;
using UnityEngine;

public sealed class UserCurrencyData : IDtoConvertible<UserCurrencyDataDto>
{
    public ReactiveProperty<long> Gold { get; } = new(0);
    public ReactiveProperty<long> Gem { get; } = new(0);
    public ReactiveProperty<long> Heart { get; } = new(0);

    public void ApplyDto(UserCurrencyDataDto dto)
    {
        if (dto == null)
            return;

        Gold.Value = dto.Gold;
        Gem.Value = dto.Gem;
        Heart.Value = dto.Heart;
    }

    public UserCurrencyDataDto ToDto()
    {
        return new UserCurrencyDataDto
        {
            Gold = Gold.Value,
            Gem = Gem.Value,
            Heart = Heart.Value
        };
    }
}

public sealed class UserCurrencyDataDto
{
    public long Gold;
    public long Gem;
    public long Heart;
}

public sealed class SkillData : IDtoConvertible<SkillDataDto>
{
    public int SkillID { get; private set; }
    public ReactiveProperty<long> Level { get; } = new(0);
    public SkillData(int skillId)
    {
        SkillID = skillId;
    }

    public void ApplyDto(SkillDataDto dto)
    {
        if (dto == null)
            return;

        SkillID = dto.SkillID;
        Level.Value = dto.Level;
    }

    public SkillDataDto ToDto()
    {
        return new SkillDataDto
        {
            SkillID = SkillID,
            Level = Level.Value
        };
    }
}

public sealed class SkillDataDto
{
    public int SkillID;
    public long Level;
}

public class ChairObjData : PlaceableObjData
{
    public ReactiveProperty<long> ConnectedTableUid { get; set; } = new();

    internal ChairObjData(PlaceableObjDataDto dto) : base(dto)
    {
    }

    public override void UpdateStatus()
    {
        base.UpdateStatus();
    }
}

public class PlaceableObjData : IDtoConvertible<PlaceableObjDataDto>
{
    public long Uid;
    public int TableID;
    public int GridX;
    public int GridY;

    public bool IsReserved => ReservedNpcUid != 0;
    public bool IsOccupied => OccupiedNpcUId != 0;
    public long ReservedNpcUid { get; set; }
    public long OccupiedNpcUId { get; set; }
    public DataManager.Furniture TableData { get; private set; }

    protected PlaceableObjData(PlaceableObjDataDto dto)
    {
        ApplyDto(dto);
    }

    public virtual void UpdateStatus()
    {
    }

    public static bool TryCreate(PlaceableObjDataDto dto, out PlaceableObjData placeableObjData)
    {
        placeableObjData = null;
        if (dto == null)
            return false;

        var tableData = DataManager.Instance.GetFurnitureData(dto.TableID);
        if (tableData == null)
            return false;

        switch (tableData.furnituretype)
        {
            case FURNITURETYPE.CHAIR:
                placeableObjData = new ChairObjData(dto);
                break;
            default:
                placeableObjData = new PlaceableObjData(dto);
                break;
        }

        placeableObjData.TableData = tableData;
        return true;
    }

    public void ApplyDto(PlaceableObjDataDto dto)
    {
        if (dto == null)
            return;

        Uid = dto.Uid;
        TableID = dto.TableID;
        GridX = dto.GridX;
        GridY = dto.GridY;
    }

    public PlaceableObjDataDto ToDto()
    {
        return new PlaceableObjDataDto()
        {
            Uid = Uid,
            TableID = TableID,
            GridX = GridX,
            GridY = GridY
        };
    }
}

public sealed class PlaceableObjDataDto
{
    public long Uid;
    public int TableID;
    public int GridX;
    public int GridY;
}


public sealed class HeroData : IDtoConvertible<HeroDataDto>
{
    public long UID { get; private set; }
    public int TableID { get; private set; }
    public ReactiveProperty<long> Level { get; private set; } = new();

    public void ApplyDto(HeroDataDto dto)
    {
        UID = dto.UID;
        TableID = dto.TableID;
        Level.Value = dto.Level;
    }

    public HeroDataDto ToDto()
    {
        return new HeroDataDto()
        {
            UID = UID,
            TableID = TableID,
            Level = Level.Value
        };
    }
}
public sealed class HeroDataDto
{
    public long UID;
    public int TableID;
    public long Level;
}

public sealed class TileData : IDtoConvertible<TileDataDto>
{
    public Vector2Int Position { get; }

    public int TableID { get; set; }

    public long FurnitureUid { get; set; }

    public bool IsUnlocked { get; set; }

    public bool IsOccupied => FurnitureUid != 0;

    public bool IsWalkable => IsUnlocked && !IsOccupied;

    public TileData(Vector2Int position)
    {
        Position = position;
    }

    public void ApplyDto(TileDataDto dto)
    {
        TableID = dto.TableID;
        FurnitureUid = dto.FurnitureUid;
        IsUnlocked = dto.IsUnlocked;
    }

    public TileDataDto ToDto()
    {
        return new TileDataDto()
        {
            X = Position.x,
            Y = Position.y,
            TableID = TableID,
            FurnitureUid = FurnitureUid,
            IsUnlocked = IsUnlocked
        };
    }
}

public sealed class TileDataDto
{
    public int X;
    public int Y;

    public int TableID;
    public long FurnitureUid;

    public bool IsUnlocked;
}
