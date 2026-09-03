using UnityEngine;

public class RestaurantManager : SingletonMono<RestaurantManager>
{
    [SerializeField] private NpcObj _npcObjPrefab;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private OverHeadUIManager _overHeadUIManager;

    protected override void Awake()
    {
        base.Awake();
    }
    protected override void OnInitialize()
    {
        base.OnInitialize();
    }

    void Start()
    {
        GenerateNpcRandom();
    }

    private void GenerateNpcRandom()
    {
        var attemptCount = 0;
        var createdCount = 0;
        while (createdCount < 10 && attemptCount++ < 100)
        {
            int x = Random.Range(0, GameDefine.GridWidth);
            int y = Random.Range(0, GameDefine.GridHeight);
            var gridPosition = new Vector2Int(x, y);

            if (!_gridManager.IsWalkable(gridPosition))
            {
                continue;
            }

            var npcObj = CreateNpc(gridPosition);
            // npcObj.MoveTo(GetRandomGridPos());

            createdCount++;
        }
    }

    public NpcObj CreateNpc(Vector2Int gridPosition)
    {
        if (_npcObjPrefab == null || !_gridManager.IsWalkable(gridPosition))
        {
            return null;
        }

        var customerData = UserDataManager.Instance.User.CreateCustomer();
        var npc = Lean.Pool.LeanPool.Spawn(_npcObjPrefab, _gridManager.GridRoot);

        npc.transform.localPosition = _gridManager.GridToWorld(gridPosition, Vector2Int.one);
        npc.transform.localRotation = Quaternion.identity;
        npc.Initialize(customerData.Uid, gridPosition, () =>
        {
            if (npc.NpcHud != null)
            {
                return;
            }

            npc.NpcHud = _overHeadUIManager.AttachNpcHud(npc);
        }, () =>
        {
            NpcHud npcHud = npc.NpcHud;

            if (npcHud == null)
            {
                return;
            }

            npc.NpcHud = null;
            _overHeadUIManager.DetachNpcHud(npcHud);
        }, (out PlaceableObj targetChair, out Vector2Int targetGrid) =>
        {
            targetChair = default;
            targetGrid = default;
            if (TryFindReachableEmptyChair(npc, out var chair, out var approachGridPos))
            {
                var chairObj = _gridManager.TryGetPlaceableObj(chair.Uid);
                if (chairObj == null)
                {
                    return false;
                }

                if(!UserDataManager.Instance.User.TryReserveChair(chair.Uid, customerData.Uid))
                {
                    return false;
                }
                targetGrid = approachGridPos;
                targetChair = chairObj;
                return true;
            }
            else
            {
                return false;
            }
        }, () =>
        {

        }, () =>
        {
            UserDataManager.Instance.User.TryReleaseChair(customerData.Uid);
        });
        return npc;
    }
    private bool TryFindReachableEmptyChair(NpcObj npcObj, out PlaceableObjData chair, out Vector2Int approachGridPos)
    {
        chair = default;
        approachGridPos = default;
        var user = UserDataManager.Instance.User;
        foreach (var placeableData in user.PlaceableObjs.Values)
        {
            if (placeableData is not ChairObjData chairObjData || chairObjData.ConnectedTableUid.Value == 0)
                continue;
            if (placeableData.IsOccupied || placeableData.IsReserved)
                continue;

            foreach (var gridPos in user.GetApproachGridPos(new Vector2Int(placeableData.GridX, placeableData.GridY)))
            {
                
                if(!_gridManager.TryFindWorldPath(npcObj.transform.position, _gridManager.GridToWorldPosition(gridPos), out var _param))
                {
                    continue;
                }
                chair = placeableData;
                approachGridPos = gridPos;
                return true;
            }
        }
        return false;
    }
}
