using System.Collections.Generic;

public class CustomerData
{
    public long Uid { get; }
    public long AssignedFurnitureUid { get; set; }
    public CustomerData(long uid)
    {
        Uid = uid;
    }
}
public class WaiterData
{
    public long Uid { get; }
    public WaiterData(long uid)
    {
        Uid = uid;
    }
}

public partial class UserData
{
    private const long DefaultRuntimeUid = 10000;
    public long NextRuntimeUid { get; private set; } = DefaultRuntimeUid;

    private Dictionary<long, CustomerData> _customers = new Dictionary<long, CustomerData>();
    private Dictionary<long, WaiterData> _waiters = new Dictionary<long, WaiterData>();
    public long GenerateRuntimeUid()
    {
        if (NextRuntimeUid <= 0)
            NextRuntimeUid = DefaultRuntimeUid;

        return NextRuntimeUid++;
    }
    public void ResetRuntimeData()
    {
        NextRuntimeUid = DefaultRuntimeUid;
        _customers.Clear();
        _waiters.Clear();
        foreach(var placeableObj in PlaceableObjs.Values)
        {
            placeableObj.ReservedNpcUid = 0;
            placeableObj.OccupiedNpcUId = 0;
        }
    }

    public CustomerData TryGetCustomer(long uid)
    {
        if (_customers.TryGetValue(uid, out var customerData))
        {
            return customerData;
        }
        return default;
    }
    public CustomerData CreateCustomer()
    {
        CustomerData customerData = new CustomerData(GenerateRuntimeUid());
        _customers.Add(customerData.Uid, customerData);
        return customerData;
    }
    public void DeleteCustomer(long uid)
    {
        TryReleaseChair(uid);
        _customers.Remove(uid);
    }
    public WaiterData TryGetWaiter(long uid)
    {
        if (_waiters.TryGetValue(uid, out var waiterData))
        {
            return waiterData;
        }
        return default;
    }
    public WaiterData CreateWaiter()
    {
        WaiterData waiterData = new WaiterData(GenerateRuntimeUid());
        _waiters.Add(waiterData.Uid, waiterData);
        return waiterData;
    }
    public void DeleteWaiter(long uid)
    {
        _waiters.Remove(uid);
    }
}