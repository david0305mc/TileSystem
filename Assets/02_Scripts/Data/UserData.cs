using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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

public partial class UserData : IDtoConvertible<UserDataDto>
{
    private const long DefaultPersistentUid = 1000;

    public long NextPersistentUid { get; private set; }

    public UserCurrencyData Currency { get; private set; } = new();

    public Dictionary<int, SkillData> Skills { get; private set; } = new();
    public Dictionary<long, PlaceableObjData> PlaceableObjs { get; private set; } = new();
    public bool TryGetPlaceableObjData(long uid, out PlaceableObjData placeableObjData) => PlaceableObjs.TryGetValue(uid, out placeableObjData);

    public Dictionary<Vector2Int, TileData> Tiles { get; } = new();
    public bool TryGetTileData(Vector2Int gridPos, out TileData tileData) => Tiles.TryGetValue(gridPos, out tileData);

    public HeroData Hero { get; private set; } = new();
    public PlaceableObjData DisplayStandData { get; private set; } = default;

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
        ApplyPlaceableDtos(dto.Placeables);

        ApplyTileDtos(dto.TileDtos);
        RebuildPlaceableConnection();

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
            if (tileData.FurnitureUid != 0 && !PlaceableObjs.ContainsKey(tileData.FurnitureUid))
                tileData.FurnitureUid = 0;

            Tiles[position] = tileData;
        }
    }

    private void ApplyPlaceableDtos(Dictionary<long, PlaceableObjDataDto> placeableDtos)
    {
        PlaceableObjs.Clear();

        if (placeableDtos == null)
            return;

        foreach (var pair in placeableDtos)
        {
            if (pair.Value == null)
                continue;

            if (!PlaceableObjData.TryCreate(pair.Value, out var placeableObjData))
            {
                Debug.LogWarning($"Skipping placeable {pair.Key}: furniture table ID {pair.Value.TableID} was not found.");
                continue;
            }

            PlaceableObjs[pair.Key] = placeableObjData;
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

        var gridPosition = new Vector2Int(placeableObjData.GridX, placeableObjData.GridY);
        if (!TryGetFootprintTiles(placeableObjData.TableData.id, gridPosition, out var tiles))
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

        DisconnectPlaceableData(placeableObjData);
        PlaceableObjs.Remove(uid);
        UpdatePlaceableConnectAroundTiles(tiles);
        return true;
    }

    public bool TryGetFootprintTiles(int furnitureId, Vector2Int gridPosition, out List<TileData> tiles)
    {
        tiles = null;

        var furnitureData = DataManager.Instance.GetFurnitureData(furnitureId);
        if (furnitureData == null)
            return false;

        int sizeX = Mathf.Max(1, furnitureData.sizex);
        int sizeY = Mathf.Max(1, furnitureData.sizey);

        var result = new List<TileData>(sizeX * sizeY);

        for (int x = gridPosition.x; x < gridPosition.x + sizeX; x++)
        {
            for (int y = gridPosition.y; y < gridPosition.y + sizeY; y++)
            {
                if (!TryGetTileData(new Vector2Int(x, y), out var tileData))
                    return false;

                result.Add(tileData);
            }
        }

        tiles = result;
        return true;
    }
    public bool TryGetPlaceableDataFromGridPos(Vector2Int gridPos, out PlaceableObjData placeableObjData)
    {
        placeableObjData = default;
        if (!TryGetTileData(gridPos, out var tileData))
            return false;
        if (!TryGetPlaceableObjData(tileData.FurnitureUid, out placeableObjData))
            return false;
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
        var dto = new PlaceableObjDataDto
        {
            Uid = uid,
            TableID = tid,
            GridX = gridX,
            GridY = gridY
        };
        if (!PlaceableObjData.TryCreate(dto, out var placeableObjData))
            return null;

        PlaceableObjs.Add(uid, placeableObjData);
        if (placeableObjData.TableData.furnituretype == FURNITURETYPE.DISPLAYSTAND)
        {
            DisplayStandData = placeableObjData;
        }
        foreach (var tileData in tileDatas)
        {
            tileData.FurnitureUid = placeableObjData.Uid;
        }

        UpdatePlaceableConnectAroundTiles(tileDatas, placeableObjData.Uid);
        return placeableObjData;
    }

    public bool TryMovePlaceableObj(long placeableUid, Vector2Int targetGridPos)
    {
        if (!TryGetPlaceableObjData(placeableUid, out var placeableObjData))
        {
            return false;
        }
        if (!TryGetAvailablePlaceableTiles(placeableObjData.TableID, targetGridPos, placeableUid, out var newTiles))
        {
            return false;
        }
        var oldGridPosition = new Vector2Int(placeableObjData.GridX, placeableObjData.GridY);
        if (!TryGetFootprintTiles(placeableObjData.TableID, oldGridPosition, out var oldTiles))
        {
            return false;
        }

        foreach (TileData tileData in oldTiles)
        {
            tileData.FurnitureUid = 0;
        }
        DisconnectPlaceableData(placeableObjData);

        placeableObjData.GridX = targetGridPos.x;
        placeableObjData.GridY = targetGridPos.y;

        foreach (var targetTileData in newTiles)
        {
            targetTileData.FurnitureUid = placeableUid;
        }
        UpdatePlaceableConnectAroundTiles(oldTiles.Concat(newTiles), placeableUid);
        return true;
    }
    private void AddPlaceableUidFromGrid(Vector2Int gridPos, HashSet<long> affectedPlaceableUids)
    {
        if (TryGetPlaceableDataFromGridPos(gridPos, out var placeableObjData))
        {
            affectedPlaceableUids.Add(placeableObjData.Uid);
        }
    }
    private void UpdatePlaceableConnectAroundTiles(IEnumerable<TileData> tileDatas, long priorityUid = 0)
    {
        HashSet<long> affectedPlaceableUids = new HashSet<long>();
        foreach (var tileData in tileDatas)
        {
            AddPlaceableUidFromGrid(tileData.Position, affectedPlaceableUids);
            foreach (var dir in GameDefine.AdjacentDirections)
            {
                AddPlaceableUidFromGrid(tileData.Position + dir, affectedPlaceableUids);
            }
        }
        if (priorityUid != 0)
        {
            UpdatePlaceableConnect(priorityUid);
            affectedPlaceableUids.Remove(priorityUid);
        }

        foreach (var uid in affectedPlaceableUids.OrderBy(uid => uid))
        {
            UpdatePlaceableConnect(uid);
        }
    }

    public bool CanPlaceFurniture(int furnitureId, Vector2Int gridPosition, long ignoredFurnitureUid = 0)
    {
        return TryGetAvailablePlaceableTiles(furnitureId, gridPosition, ignoredFurnitureUid, out _);
    }

    private bool TryGetAvailablePlaceableTiles(int furnitureId, Vector2Int gridPosition, long ignoredFurnitureUid, out List<TileData> tiles)
    {
        if (!TryGetFootprintTiles(furnitureId, gridPosition, out tiles))
        {
            return false;
        }

        return tiles.All(item =>
            item.IsUnlocked &&
            (!item.IsOccupied || item.FurnitureUid == ignoredFurnitureUid));
    }
    public bool TryReserveChair(long chairUid, long customerUid)
    {
        if (!PlaceableObjs.TryGetValue(chairUid, out var chair))
        {
            return false;
        }
        if (!_customers.TryGetValue(customerUid, out var customerData))
        {
            return false;
        }

        customerData.AssignedFurnitureUid = chairUid;
        chair.ReservedNpcUid = customerData.Uid;
        return true;
    }
    public bool TryOccupyChair(long chairUid, long customerUid)
    {
        if (!PlaceableObjs.TryGetValue(chairUid, out var chair))
        {
            return false;
        }
        if (!_customers.TryGetValue(customerUid, out var customerData))
        {
            return false;
        }

        customerData.AssignedFurnitureUid = chairUid;
        chair.OccupiedNpcUId = customerData.Uid;
        chair.ReservedNpcUid = 0;
        return true;
    }

    public bool TryReleaseChair(long customerUid)
    {
        if (!_customers.TryGetValue(customerUid, out var customerData))
        {
            return false;
        }
        long chairUid = customerData.AssignedFurnitureUid;
        if (!PlaceableObjs.TryGetValue(chairUid, out var chair))
        {
            return false;
        }
        customerData.AssignedFurnitureUid = 0;
        chair.ReservedNpcUid = 0;
        chair.OccupiedNpcUId = 0;

        return true;
    }
    public List<Vector2Int> GetApproachGridPositions(PlaceableObjData placeableData)
    {
        var gridPosition = new Vector2Int(
            placeableData.GridX,
            placeableData.GridY);

        if (!TryGetFootprintTiles(
                placeableData.TableData.id,
                gridPosition,
                out var footprintTiles))
        {
            return new List<Vector2Int>();
        }

        var footprintPositions = footprintTiles
            .Select(tile => tile.Position)
            .ToList();

        var approachPositions = new HashSet<Vector2Int>();

        foreach (var footprintPosition in footprintPositions)
        {
            foreach (var direction in GameDefine.AdjacentDirections)
            {
                var approachPosition = footprintPosition + direction;

                // 가구가 차지하고 있는 타일 제외
                if (footprintPositions.Contains(approachPosition))
                    continue;

                // 그리드 밖 제외
                if (!TryGetTileData(approachPosition, out var tileData))
                    continue;

                // NPC가 접근할 수 없는 타일 제외
                if (!tileData.IsWalkable)
                    continue;

                approachPositions.Add(approachPosition);
            }
        }

        return approachPositions.ToList();
    }

    public IEnumerable<Vector2Int> GetApproachGridPos(Vector2Int targetGrid)
    {
        foreach (var directon in GameDefine.ApproachDirections)
        {
            Vector2Int tilePos = targetGrid + directon;
            if (!TryGetTileData(tilePos, out var tileData))
            {
                continue;
            }

            if (!tileData.IsWalkable)
                continue;

            yield return tilePos;
        }
    }

    private void RebuildPlaceableConnection()
    {
        foreach (var placeableObjData in PlaceableObjs.Values)
        {
            ClearPlaceableConnection(placeableObjData);
        }

        foreach (var placeableObjData in PlaceableObjs.Values.OrderBy(item => item.Uid))
        {
            UpdatePlaceableConnect(placeableObjData.Uid);
        }
    }

    private static void ClearPlaceableConnection(PlaceableObjData placeableObjData)
    {
        if (placeableObjData is ChairObjData chairObjData)
        {
            chairObjData.ConnectedTableUid.Value = 0;
        }
        else if (placeableObjData is TableObjData tableObjData)
        {
            tableObjData.ConnectedChairUid.Value = 0;
        }
    }

    private void DisconnectPlaceableData(PlaceableObjData placeableObjData)
    {
        if (placeableObjData is ChairObjData chairObjData)
        {
            var connectedTableUid = chairObjData.ConnectedTableUid.Value;
            chairObjData.ConnectedTableUid.Value = 0;

            if (TryGetPlaceableObjData(connectedTableUid, out var connectedObjData)
                && connectedObjData is TableObjData connectedTableObjData
                && connectedTableObjData.ConnectedChairUid.Value == chairObjData.Uid)
            {
                connectedTableObjData.ConnectedChairUid.Value = 0;
            }
        }
        else if (placeableObjData is TableObjData tableObjData)
        {
            var connectedChairUid = tableObjData.ConnectedChairUid.Value;
            tableObjData.ConnectedChairUid.Value = 0;

            if (TryGetPlaceableObjData(connectedChairUid, out var connectedObjData)
                && connectedObjData is ChairObjData connectedChairObjData
                && connectedChairObjData.ConnectedTableUid.Value == tableObjData.Uid)
            {
                connectedChairObjData.ConnectedTableUid.Value = 0;
            }
        }
    }

    private void UpdatePlaceableConnect(long placeableUid)
    {
        if (!TryGetPlaceableObjData(placeableUid, out var placeableObjData)
            || placeableObjData is not ChairObjData chairObjData
            || chairObjData.ConnectedTableUid.Value != 0)
        {
            return;
        }

        if (!TryGetFootprintTiles(chairObjData.TableData.id, new Vector2Int(chairObjData.GridX, chairObjData.GridY), out var chairTiles))
        {
            return;
        }

        HashSet<long> checkedPlaceableUids = new HashSet<long>();
        foreach (var chairTileData in chairTiles)
        {
            foreach (var dir in GameDefine.AdjacentDirections)
            {
                var gridPos = dir + chairTileData.Position;
                if (!TryGetPlaceableDataFromGridPos(gridPos, out var adjacentPlaceableObj)
                    || !checkedPlaceableUids.Add(adjacentPlaceableObj.Uid)
                    || adjacentPlaceableObj is not TableObjData tableObjData
                    || tableObjData.ConnectedChairUid.Value != 0)
                {
                    continue;
                }

                tableObjData.ConnectedChairUid.Value = chairObjData.Uid;
                chairObjData.ConnectedTableUid.Value = tableObjData.Uid;
                return;
            }
        }
    }

}
