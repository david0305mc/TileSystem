using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : SingletonMono<GameManager>
{

    private bool _isStartingGame;

    public async UniTask StartGame()
    {
        if (_isStartingGame)
        {
            Debug.LogWarning($"_isStartingGame {_isStartingGame}");
            return;
        }

        try
        {
            await DataManager.Instance.LoadDataAsync();
            UserDataManager.Instance.Init();
            await UserDataManager.Instance.LoadLocalDataAsync();
            await SceneTransition.Instance.LoadSceneWithFadeAsync(GameDefine.MainSceneName);

            LoadComplete();
        }
        finally
        {
            _isStartingGame = false;
        }
    }

    private void LoadComplete()
    {
        foreach (var item in DataManager.Instance.FurnitureDic)
        {
            Debug.Log($"item {item.Value.namekey}");
        }
        
    }
}
