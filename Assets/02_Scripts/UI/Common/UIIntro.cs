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
            // var result = await PopupManager.Instance.ShowPopup<PopupOK, bool>();
            // await SceneTransition.Instance.LoadSceneWithFadeAsync(GameDefine.MainSceneName);
            await DataManager.Instance.LoadDataAsync();

            GameManager.Instance.StartGame().Forget();
            button.gameObject.SetActive(false);
        });
    }

    void OnEnable()
    {
        button.gameObject.SetActive(true);
    }



}
