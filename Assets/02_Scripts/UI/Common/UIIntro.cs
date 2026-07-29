using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
