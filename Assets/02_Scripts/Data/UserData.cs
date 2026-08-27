using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class UserDataDto
{
    public long NextPersistentUid;
    public UserCurrencyDataDto CurrencyDto = new();
    public Dictionary<int, SkillDataDto> SkillDtos = new();
    public Dictionary<long, PlaceableObjDataDto> Placeables = new();
    public List<TileDataDto> TileDtos = new();
    public HeroDataDto HeroDto = new();
}

public sealed class UserData : IDtoConvertible<UserDataDto>
{
    private const long DefaultPersistentUid = 1000;

    public long NextPersistentUid { get; private set; }

    public UserCurrencyData Currency { get; private set; } = new();

    public Dictionary<int, SkillData> Skills { get; private set; } = new();
    public Dictionary<long, PlaceableObjData> PlaceableObjs { get; private set; } = new();
    public bool TryGetPlaceableObjData(long uid, out PlaceableObjData placeableObjData) => PlaceableObjs.TryGetValue(uid, out placeableObjData);

    public Dictionary<Vector2Int, TileData> Tiles { get; } = new();
    public bool TryGetTileData(int x, int y, out TileData tileData) => Tiles.TryGetValue(new Vector2Int(x, y), out tileData);

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
        PlaceableObjs.Clear();

        Hero.ApplyDto(new HeroDataDto
        {
            UID = GeneratePersistentUid(),
            TableID = 1,
            Level = 1
        });

        CreateDefaultTiles();
        CreateDefaultPlaceableObjs();
    }

    public void ApplyDto(UserDataDto dto)
    {
        if (dto == null)
            return;

        NextPersistentUid = dto.NextPersistentUid > 0
            ? dto.NextPersistentUid
            : DefaultPersistentUid;

        if (dto.CurrencyDto != null)
            Currency.ApplyDto(dto.CurrencyDto);

        DataMapperUtil.ApplyDtoDictionary(
            Skills, dto.SkillDtos, dtoValue =>
            {
                var skillData = new SkillData(dtoValue.SkillID);
                skillData.ApplyDto(dtoValue);
                return skillData;
            });
        DataMapperUtil.ApplyDtoDictionary(
            PlaceableObjs, dto.Placeables, dtoValue =>
            {
                return new PlaceableObjData(dtoValue);
            }
        );

        ApplyTileDtos(dto.TileDtos);

        if (dto.HeroDto != null)
            Hero.ApplyDto(dto.HeroDto);
    }

    public UserDataDto ToDto()
    {
        return new UserDataDto
        {
            NextPersistentUid = NextPersistentUid,
            CurrencyDto = Currency.ToDto(),
            SkillDtos = DataMapperUtil.ToDtoDictionary<int, SkillData, SkillDataDto>(Skills),
            Placeables = DataMapperUtil.ToDtoDictionary<long, PlaceableObjData, PlaceableObjDataDto>(PlaceableObjs),
            TileDtos = ToTileDtos(),
            HeroDto = Hero.ToDto()
        };
    }

    public long GeneratePersistentUid()
    {
        if (NextPersistentUid <= 0)
            NextPersistentUid = DefaultPersistentUid;

        return NextPersistentUid++;
    }

    private void ApplyTileDtos(List<TileDataDto> tileDtos)
    {
        Tiles.Clear();

        if (tileDtos == null)
            return;

        foreach (var tileDto in tileDtos)
        {
            if (tileDto == null)
                continue;

            var position = new Vector2Int(tileDto.X, tileDto.Y);
            var tileData = new TileData(position);

            tileData.ApplyDto(tileDto);
            Tiles[position] = tileData;
        }
    }

    private List<TileDataDto> ToTileDtos()
    {
        var result = new List<TileDataDto>(Tiles.Count);

        foreach (var tileData in Tiles.Values)
        {
            if (tileData == null)
                continue;

            result.Add(tileData.ToDto());
        }

        return result;
    }

    private void CreateDefaultTiles()
    {
        int width = GameDefine.GridWidth;
        int height = GameDefine.GridHeight;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var position = new Vector2Int(x, y);

                Tiles[position] = new TileData(position)
                {
                    TableID = 1,
                    FurnitureUid = 0,
                    IsUnlocked = true
                };
            }
        }
    }
    private void CreateDefaultPlaceableObjs()
    {
        CreatePlaceableObj(100001, 5, 5);
        CreatePlaceableObj(100002, 7, 5);
        CreatePlaceableObj(100003, 8, 3);
        CreatePlaceableObj(100004, 6, 5);
    }
    public bool RemovePlaceableObj(long uid)
    {
        if (!TryGetPlaceableObjData(uid, out PlaceableObjData placeableObjData))
        {
            return false;
        }

        if (!TryGetPlaceableTiles(placeableObjData.TableData.id, placeableObjData.GridX, placeableObjData.GridY, out var tiles))
        {
            return false;
        }

        foreach (var tileData in tiles)
        {
            if (tileData.FurnitureUid == uid)
            {
                tileData.FurnitureUid = 0;
            }
        }
        PlaceableObjs.Remove(uid);
        return true;

    }
    public bool TryGetPlaceableTiles(int furnitureId, int gridX, int gridY, out List<TileData> tiles)
    {
        tiles = null;

        var furnitureData = DataManager.Instance.GetFurnitureData(furnitureId);
        if (furnitureData == null)
            return false;

        int sizeX = Mathf.Max(1, furnitureData.sizex);
        int sizeY = Mathf.Max(1, furnitureData.sizey);

        var result = new List<TileData>(sizeX * sizeY);

        for (int x = gridX; x < gridX + sizeX; x++)
        {
            for (int y = gridY; y < gridY + sizeY; y++)
            {
                if (!TryGetTileData(x, y, out var tileData))
                    return false;

                result.Add(tileData);
            }
        }

        tiles = result;
        return true;
    }
    public PlaceableObjData CreatePlaceableObj(int tid, int gridX, int gridY)
    {
        if (!TryGetAvailablePlaceableTiles(tid, new Vector2Int(gridX, gridY), 0, out var tileDatas))
        {
            return null;
        }

        var furnitureData = DataManager.Instance.GetFurnitureData(tid);
        if (furnitureData == null)
            return null;

        long uid = GeneratePersistentUid();
        var placeableObjData = new PlaceableObjData(new PlaceableObjDataDto()
        {
            Uid = uid,
            TableID = tid,
            GridX = gridX,
            GridY = gridY
        });
        PlaceableObjs.Add(uid, placeableObjData);
        foreach (var tileData in tileDatas)
        {
            tileData.FurnitureUid = placeableObjData.Uid;
        }
        return placeableObjData;
    }

    public bool TryMovePlaceableObj(long placeableUid, Vector2Int targetGridPos)
    {
        if (!TryGetPlaceableObjData(placeableUid, out var placeableObjData))
        {
            return false;
        }
        if (!TryGetAvailablePlaceableTiles(placeableObjData.TableID, targetGridPos, placeableUid, out var targetTiles))
        {
            return false;
        }
        if (!TryGetPlaceableTiles(placeableObjData.TableID, placeableObjData.GridX, placeableObjData.GridY, out var currentTiles))
        {
            return false;
        }

        foreach (var tileData in currentTiles)
        {
            tileData.FurnitureUid = 0;
        }

        placeableObjData.GridX = targetGridPos.x;
        placeableObjData.GridY = targetGridPos.y;

        foreach (var targetTileData in targetTiles)
        {
            targetTileData.FurnitureUid = placeableUid;
        }
        return true;
    }

    public bool CanPlaceFurniture(int furnitureId, Vector2Int gridPosition, long ignoredFurnitureUid = 0)
    {
        return TryGetAvailablePlaceableTiles(furnitureId, gridPosition, ignoredFurnitureUid, out _);
    }

    private bool TryGetAvailablePlaceableTiles(int furnitureId, Vector2Int gridPosition, long ignoredFurnitureUid, out List<TileData> tiles)
    {
        if (!TryGetPlaceableTiles(furnitureId, gridPosition.x, gridPosition.y, out tiles))
        {
            return false;
        }

        return tiles.All(item =>
            item.IsUnlocked &&
            (!item.IsOccupied || item.FurnitureUid == ignoredFurnitureUid));
    }
    public bool TryFindEmptyChair(out PlaceableObjData chair)
    {
        foreach (var placeableObj in PlaceableObjs.Values)
        {
            if (placeableObj.TableData.furnituretype == FURNITURETYPE.CHAIR &&
                !placeableObj.IsOccupied &&
                !placeableObj.IsReserved)
            {
                chair = placeableObj;
                return true;
            }
        }

        chair = default;
        return false;
    }
}
