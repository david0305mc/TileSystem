using System.Collections.Generic;

public class CustomerData
{
    public long Uid=> _uid;
    public long _uid;
    public long AssignedFurnitureUid {get; set;}
    public CustomerData(long uid)
    {
        _uid = uid;
    }
}

public partial class UserData
{
    private const long DefaultRuntimeUid = 10000;
    public long NextRuntimeUid { get; private set; } = DefaultRuntimeUid;

    private Dictionary<long, CustomerData> _customers = new Dictionary<long, CustomerData>();    
    public long GenerateRuntimeUid()
    {
        if (NextRuntimeUid <= 0)
            NextRuntimeUid = DefaultPersistentUid;

        return NextRuntimeUid++;
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
        _customers.Remove(uid);
    }
}