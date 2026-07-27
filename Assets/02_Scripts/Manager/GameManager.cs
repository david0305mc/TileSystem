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
            UserDataManager.Instance.Init();
            await UserDataManager.Instance.LoadLocalDataAsync();
            await SceneTransition.Instance.LoadSceneWithFadeAsync(GameDefine.MainSceneName);
        }
        finally
        {
            _isStartingGame = false;
        }
    }
}
