using UnityEngine;
using UnityEngine.UI;
using R3;
public class UIMain : MonoBehaviour
{
    [SerializeField] private Button button;

    void Awake()
    {
        button.onClick.AddListener(() =>
        {
            // UserDataManager.Instance.AddGold(1);

            PopupManager.Instance.ShowPopup<ObjectInstallTestPopup, Unit>();
        });
    }
}
