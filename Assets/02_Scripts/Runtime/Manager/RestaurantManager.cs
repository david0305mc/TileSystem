using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RestaurantManager : SingletonMono<RestaurantManager>
{
    [SerializeField] private CustomerObj _customerObjPrefab;
    [SerializeField] private WaiterObj _waiterObjPrefab;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private OverHeadUIManager _overHeadUIManager;

    private readonly Dictionary<long, CustomerObj> _customerObjs = new();
    private readonly Dictionary<long, WaiterObj> _waiterObjs = new();

    private readonly List<WaiterTask> waiterTasks = new();

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
        CreateWaiterObj(new Vector2Int(3, 4));
    }

    private void GenerateCustomerRandom()
    {
        var attemptCount = 0;
        var createdCount = 0;
        while (createdCount < 5 && attemptCount++ < 100)
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
            // Assign Waiter 
            var waiterTask = waiterTasks.FirstOrDefault(item => !item.IsAssigned);
            if (waiterTask != default)
            {
                waiterTask.WaiterUid = waiterData.Uid;
                waiterTask.IsAssigned = true;
            }
            return waiterTask;
        }, waiterTask =>
        {
            // Serve Food
            ServeFoodWaiterTask(waiterData.Uid, waiterTask.OrderUid);

            // To Do serve Food
            // To Do 해당 테이블에 말풍선 띄우고
            // To Do 고객은 식사 상태로 변환
        }, waiterTask =>
        {
            ReleaseWaiterTask(waiterData.Uid, waiterTask);
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
            if (customerObj.BaseHud != null)
            {
                return;
            }

            customerObj.BaseHud = _overHeadUIManager.ShowHud(customerObj);
        }, () =>
        {
            BaseWorldHud baseHud = customerObj.BaseHud;
            if (baseHud == null)
            {
                return;
            }
            _overHeadUIManager.HideHud(customerObj);
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
            // Sit
            customerData.OrderId = CreateWaiterTask(WaiterTaskType.ServeFood, customerData.Uid, chairUid);
        }, () =>
        {
            // Exit
            var waiterTask = waiterTasks.FirstOrDefault(item => item.OrderUid == customerData.OrderId);
            if (waiterTask != null)
            {
                var tableObj = _gridManager.TryGetPlaceableObj(waiterTask.TableUid);
                if (tableObj != null)
                {
                    _overHeadUIManager.HideHud(tableObj);
                }
            }
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
    private long CreateWaiterTask(WaiterTaskType waiterTaskType, long customerUid, long chairUid)
    {
        if (!UserDataManager.Instance.User.TryGetPlaceableObjData(chairUid, out var placeableObjData)
        || placeableObjData is not ChairObjData chairObjData)
        {
            return 0;
        }

        WaiterTask waiterTask = new WaiterTask
        {
            OrderUid = UserDataManager.Instance.User.GenerateRuntimeUid(),
            CustomerUid = customerUid,
            TableUid = chairObjData.ConnectedTableUid.Value,
            Type = waiterTaskType,
        };
        waiterTasks.Add(waiterTask);
        return waiterTask.OrderUid;
    }
    private void ServeFoodWaiterTask(long waiterUid, long orderUid)
    {
        WaiterTask waiterTask = waiterTasks.FirstOrDefault(item => item.OrderUid == orderUid);
        if (waiterTask.WaiterUid != waiterUid
        || waiterTask.IsAssigned != true)
        {
            return;
        }

        var tableObj = _gridManager.TryGetPlaceableObj(waiterTask.TableUid);
        if (tableObj != null)
        {
            _overHeadUIManager.ShowHud(tableObj);
        }
    }
    private void CompleteWaiterTask(long waiterUid, WaiterTask waiterTask)
    {
        if (waiterTask.WaiterUid != waiterUid
            || waiterTask.IsAssigned != true
            || waiterTasks.Remove(waiterTask))
        {
            return;
        }
        var tableObj = _gridManager.TryGetPlaceableObj(waiterTask.TableUid);
        if (tableObj != null)
        {
            _overHeadUIManager.HideHud(tableObj);
        }

        waiterTask.IsAssigned = false;
        waiterTask.WaiterUid = 0;
    }
    private void ReleaseWaiterTask(long waiterUid, WaiterTask waiterTask)
    {
        if (waiterTask.WaiterUid != waiterUid
            || !waiterTask.IsAssigned)
        {
            return;
        }
        var taskIndex = waiterTasks.IndexOf(waiterTask);
        if (taskIndex < 0)
        {
            return;
        }
        if (taskIndex < waiterTasks.Count - 1)
        {
            waiterTasks.RemoveAt(taskIndex);
            waiterTasks.Add(waiterTask);
        }

        waiterTask.IsAssigned = false;
        waiterTask.WaiterUid = 0;
        return;
    }
}
