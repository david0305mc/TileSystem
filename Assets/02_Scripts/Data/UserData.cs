using System.Collections.Generic;
using UnityEngine;

public sealed class UserDataDto
{
    public long NextPersistentUid;
    public UserCurrencyDataDto CurrencyDto = new();
    public Dictionary<int, SkillDataDto> SkillDtos = new();
    public Dictionary<Vector2Int, TileDataDto> TileDtos = new();
    public HeroDataDto HeroDto = new();
}

public sealed class UserData : IDtoConvertible<UserDataDto>
{
    private const long DefaultPersistentUid = 1000;

    public long NextPersistentUid { get; private set; }
    public UserCurrencyData Currency { get; private set; } = new();
    public Dictionary<int, SkillData> Skills { get; private set; } = new();
    public Dictionary<Vector2Int, TileData> Tiles { get; private set; } = new Dictionary<Vector2Int, TileData>();
    public HeroData Hero { get; private set; } = new();

    public void CreateNewUser()
    {
        NextPersistentUid = DefaultPersistentUid;

        Currency.ApplyDto(new UserCurrencyDataDto
        {
            Gold = 0,
            Gem = 0,
            Heart = 0
        });

        Skills.Clear();
        Tiles.Clear();

        Hero.ApplyDto(new HeroDataDto
        {
            UID = GeneratePersistentUid(),
            TableID = 1,
            Level = 1
        });
        CreateDefaultTiles();
    }

    public void ApplyDto(UserDataDto dto)
    {
        if (dto == null)
            return;

        if (dto.CurrencyDto != null)
            Currency.ApplyDto(dto.CurrencyDto);

        DataMapperUtil.ApplyDtoDictionary(Skills, dto.SkillDtos, dtoValue =>
        {
            var skillData = new SkillData(dtoValue.SkillID);
            skillData.ApplyDto(dtoValue);
            return skillData;
        });
        DataMapperUtil.ApplyDtoDictionary(Tiles, dto.TileDtos, dtoValue =>
        {
            var tileData = new TileData(new Vector2Int(dtoValue.X, dtoValue.Y));
            tileData.ApplyDto(dtoValue);
            return tileData;
        });

        if (dto.HeroDto != null)
            Hero.ApplyDto(dto.HeroDto);

        NextPersistentUid = dto.NextPersistentUid > 0 ? dto.NextPersistentUid : DefaultPersistentUid;
    }

    public UserDataDto ToDto()
    {
        return new UserDataDto
        {
            NextPersistentUid = NextPersistentUid,
            CurrencyDto = Currency.ToDto(),
            SkillDtos = DataMapperUtil.ToDtoDictionary<int, SkillData, SkillDataDto>(Skills),
            TileDtos = DataMapperUtil.ToDtoDictionary<Vector2Int, TileData, TileDataDto>(Tiles),
            HeroDto = Hero.ToDto()
        };
    }

    public long GeneratePersistentUid()
    {
        if (NextPersistentUid <= 0)
            NextPersistentUid = DefaultPersistentUid;

        return NextPersistentUid++;
    }
    private void CreateDefaultTiles()
    {
        int width = GameDefine.TileWidth;
        int height = GameDefine.TileHeight;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var position = new Vector2Int(x, y);

                Tiles[position] = new TileData(position)
                {
                    TableID = 1,
                    FurnitureId = 0,
                    IsUnlocked = true,
                    BlocksMovement = false
                };
            }
        }
    }
}
