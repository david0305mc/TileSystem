using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RestaurantManager : SingletonMono<RestaurantManager>
{
    [SerializeField] private CustomerObj _customerObjPrefab;
    [SerializeField] private WaiterObj _waiterObjPrefab;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private OverHeadUIManager _overHeadUIManager;

    private readonly Dictionary<long, CustomerObj> _customerObjs = new();
    private readonly Dictionary<long, WaiterObj> _waiterObjs = new();

    private Queue<WaiterTask> waiterTasks = new Queue<WaiterTask>();

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
        GenerateCustomerRandom();
        CreateWaiterObj(new Vector2Int(3, 3));
    }

    private void GenerateCustomerRandom()
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

            if (CreateCustomerObj(gridPosition) == null)
            {
                continue;
            }

            createdCount++;
        }
    }
    public WaiterObj CreateWaiterObj(Vector2Int gridPosition)
    {
        if (_waiterObjPrefab == null
            || _gridManager == null
            || !_gridManager.IsWalkable(gridPosition))
        {
            return null;
        }

        var waiterData = UserDataManager.Instance.User.CreateWaiter();
        var waiterObj = Lean.Pool.LeanPool.Spawn(_waiterObjPrefab, _gridManager.GridRoot);

        waiterObj.transform.localPosition = _gridManager.GridToLocalPosition(gridPosition, Vector2Int.one);
        waiterObj.transform.localRotation = Quaternion.identity;
        waiterObj.Initialize(waiterData.Uid, gridPosition, () =>
        {
            return DequeueWaiterTask();
        });
        _waiterObjs.Add(waiterObj.Uid, waiterObj);

        return waiterObj;
    }

    public bool TryGetWaiterObj(long uid, out WaiterObj waiterObj)
    {
        return _waiterObjs.TryGetValue(uid, out waiterObj);
    }

    public bool RemoveWaiterObj(long uid)
    {
        if (!_waiterObjs.Remove(uid, out var waiterObj))
        {
            return false;
        }

        UserDataManager.Instance.User.DeleteWaiter(uid);

        if (waiterObj != null)
        {
            waiterObj.Deinitialize();
            Lean.Pool.LeanPool.Despawn(waiterObj);
        }

        return true;
    }

    public CustomerObj CreateCustomerObj(Vector2Int gridPosition)
    {
        if (_customerObjPrefab == null || !_gridManager.IsWalkable(gridPosition))
        {
            return null;
        }

        var customerData = UserDataManager.Instance.User.CreateCustomer();
        var customerObj = Lean.Pool.LeanPool.Spawn(_customerObjPrefab, _gridManager.GridRoot);

        customerObj.transform.localPosition = _gridManager.GridToLocalPosition(gridPosition, Vector2Int.one);
        customerObj.transform.localRotation = Quaternion.identity;
        customerObj.Initialize(customerData.Uid, gridPosition, () =>
        {
            if (customerObj.NpcHud != null)
            {
                return;
            }

            customerObj.NpcHud = _overHeadUIManager.AttachNpcHud(customerObj);
        }, () =>
        {
            NpcHud npcHud = customerObj.NpcHud;

            if (npcHud == null)
            {
                return;
            }

            customerObj.NpcHud = null;
            _overHeadUIManager.DetachNpcHud(npcHud);
        }, (out PlaceableObj targetChair, out Vector2Int targetGrid) =>
        {
            targetChair = default;
            targetGrid = default;
            if (TryFindReachableEmptyChair(customerObj, out var chair, out var approachGridPos))
            {
                var chairObj = _gridManager.TryGetPlaceableObj(chair.Uid);
                if (chairObj == null)
                {
                    return false;
                }

                if (!UserDataManager.Instance.User.TryReserveChair(chair.Uid, customerData.Uid))
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
        }, (chairUid) =>
        {
            CreateWaiterTask(WaiterTaskType.ServeFood, customerData.Uid, chairUid);
        }, () =>
        {
            UserDataManager.Instance.User.TryReleaseChair(customerData.Uid);
        });
        _customerObjs.Add(customerObj.Uid, customerObj);
        return customerObj;
    }

    public bool TryGetCustomerObj(long uid, out CustomerObj customerObj)
    {
        return _customerObjs.TryGetValue(uid, out customerObj);
    }

    public bool RemoveCustomerObj(long uid)
    {
        if (!_customerObjs.Remove(uid, out var customerObj))
        {
            return false;
        }

        UserDataManager.Instance.User.DeleteCustomer(uid);

        if (customerObj != null)
        {
            customerObj.Deinitialize();
            Lean.Pool.LeanPool.Despawn(customerObj);
        }

        return true;
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

            foreach (var gridPos in user.GetApproachGridPositions(placeableData))
            {
                if (!_gridManager.TryFindWorldPath(npcObj.transform.position, _gridManager.GridToWorldPosition(gridPos), out var _param))
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
    private void CreateWaiterTask(WaiterTaskType waiterTaskType, long customerUid, long tableUid)
    {
        WaiterTask waiterTask = new WaiterTask();
        waiterTask.CustomerUid = customerUid;
        waiterTask.TableUid = tableUid;
        waiterTask.WaiterTaskType = waiterTaskType;
        waiterTasks.Enqueue(waiterTask);
    }
    private WaiterTask DequeueWaiterTask()
    {
        if( waiterTasks.TryDequeue(out var result))
        {
            return result;
        }
        return default;
    }
}
