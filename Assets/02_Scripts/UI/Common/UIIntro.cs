using UnityEngine;
using UnityEngine.UI;

public class UIIntro : MonoBehaviour
{
    [SerializeField] private Button button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button.onClick.AddListener(async () =>
        {
            var result = await PopupManager.Instance.ShowPopup<PopupOK, bool>();

        });
    }


}
