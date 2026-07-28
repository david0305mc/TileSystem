using UnityEngine;
using UnityEngine.UI;

public class UIMain : MonoBehaviour
{
    [SerializeField] private Button button;

    void Awake()
    {
        button.onClick.AddListener(() =>
        {
            UserDataManager.Instance.AddGold(1);

        });
    }
}
