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

        var npc = Lean.Pool.LeanPool.Spawn(_npcObjPrefab, _gridManager.GridRoot);
        npc.transform.localPosition = _gridManager.GridToWorld(gridPosition, Vector2Int.one);
        npc.transform.localRotation = Quaternion.identity;
        npc.Initialize(gridPosition, () =>
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
        });
        return npc;
    }
}
