using System.Threading.Tasks;
using TMPro;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class CellInfiniteScroll : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI titleText;

    private CompositeDisposable disposables;

    public void Bind(int index, ReactiveProperty<int> selectedIndex)
    {
        disposables?.Clear();
        disposables?.Dispose();
        disposables = new CompositeDisposable();

        titleText.SetText($"text {index}");

        button.OnClickAsObservable().Subscribe(_ =>
        {
            selectedIndex.Value = index;
        }).AddTo(disposables);
    }
}
