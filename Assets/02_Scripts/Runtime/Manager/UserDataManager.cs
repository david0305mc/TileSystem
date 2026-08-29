using System.Collections.Generic;
using Mono.Cecil.Cil;

public partial class UserDataManager : Singleton<UserDataManager>
{
    public UserData User { get; private set; }

    private Dictionary<long, CustomerData> _customers;

    public void Init()
    {
        User = new UserData();

    }
    
}
