using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using R3;

public class UIIntro : MonoBehaviour
{
    [SerializeField] private Button button;

    void Start()
    {
        button.onClick.AddListener(async () =>
        {
            GameManager.Instance.StartGame().Forget();
            button.gameObject.SetActive(false);
        });
    }

    void OnEnable()
    {
        button.gameObject.SetActive(true);
    }



}
