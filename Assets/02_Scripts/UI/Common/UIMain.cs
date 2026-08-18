using UnityEngine;
using UnityEngine.UI;
using R3;
public class UIMain : SingletonMono<UIMain>
{
    [SerializeField] private Button button;

    protected override void Awake()
    {
        base.Awake();
        button.onClick.AddListener(() =>
        {
            // UserDataManager.Instance.AddGold(1);

            PopupManager.Instance.ShowPopup<ObjectInstallTestPopup, Unit>();
        });
    }
}
