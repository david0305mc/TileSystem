using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;
using System.Collections.Generic;
using System.Linq;

public class ObjectInstallTestPopup : PopupBase<Unit>
{
    [SerializeField] private InfiniteScroll infiniteScroll;

    List<DataManager.Furniture> furnitures;
    public override UniTask Show()
    {
        furnitures = DataManager.Instance.FurnitureArray.ToList();
        base.Show();
        infiniteScroll.Init(furnitures.Count, Bind);
        return UniTask.CompletedTask;
    }

    private void Bind(Transform transform, int index)
    {
        var cell = transform.GetComponent<CellInstallTest>();
        cell.Bind(furnitures[index].id, OnCellClicked);
    }

    private void OnCellClicked(int index)
    {
        Debug.Log($"touch {index}");
    }
}