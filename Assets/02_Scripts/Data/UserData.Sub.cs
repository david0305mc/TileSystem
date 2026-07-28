
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

    public long FurnitureId { get; set; }

    public bool IsUnlocked { get; set; }

    public bool BlocksMovement { get; set; }

    public bool IsOccupied => FurnitureId != 0;

    public bool IsWalkable =>
        IsUnlocked &&
        !BlocksMovement;

    public TileData(Vector2Int position)
    {
        Position = position;
    }

    public void ApplyDto(TileDataDto dto)
    {
        TableID = dto.TableID;
        FurnitureId = dto.FurnitureId;
        IsUnlocked = dto.IsUnlocked;
        BlocksMovement = dto.BlocksMovement;
    }

    public TileDataDto ToDto()
    {
        return new TileDataDto()
        {
            X = Position.x,
            Y = Position.y,
            TableID = TableID,
            FurnitureId = FurnitureId,
            IsUnlocked = IsUnlocked,
            BlocksMovement = BlocksMovement
        };
    }
}

public sealed class TileDataDto
{
    public int X;
    public int Y;

    public int TableID;
    public long FurnitureId;

    public bool IsUnlocked;
    public bool BlocksMovement;
}