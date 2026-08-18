using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CellInstallTest : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI titleText;

    public void Bind(int index, System.Action<int> buttonCallback)
    {
        titleText.SetText($"{index}");
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(()=> { buttonCallback?.Invoke(index); });
    }

}
