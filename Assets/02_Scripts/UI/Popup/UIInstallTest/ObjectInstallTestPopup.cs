using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

public class ObjectInstallTestPopup : PopupBase<Unit>
{
    [SerializeField] private InfiniteScroll infiniteScroll;

    public override UniTask Show()
    {
        base.Show();

        infiniteScroll.Init(50, Bind);

        return UniTask.CompletedTask;
    }

    private void Bind(Transform transform, int index)
    {
        var cell = transform.GetComponent<CellInstallTest>();
        cell.Bind(index, OnCellClicked);
    }

    private void OnCellClicked(int index)
    {
        Debug.Log($"touch {index}");
    }
}