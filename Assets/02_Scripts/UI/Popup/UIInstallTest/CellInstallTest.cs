using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CellInstallTest : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image objectImage;


    public void Bind(int id, System.Action<int> buttonCallback)
    {
        var furniture = DataManager.Instance.GetFurnitureData(id);
        titleText.SetText(furniture.namekey);
        objectImage.sprite = ResourceManager.Instance.GetSpriteFromAtlas(furniture.spritepath);
        
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(()=> { buttonCallback?.Invoke(id); });
    }

}
