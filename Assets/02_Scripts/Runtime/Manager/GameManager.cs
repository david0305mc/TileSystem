using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

public class GameManager : SingletonMono<GameManager>
{
    private readonly ReactiveProperty<global::GameMode> _gameMode = new(global::GameMode.Normal);
    public ReactiveProperty<global::GameMode> GameMode => _gameMode;

    private bool _isStartingGame;

    public void EnterEditMode()
    {
        _gameMode.Value = global::GameMode.Edit;
    }

    public void CancelEditMode()
    {
        _gameMode.Value = global::GameMode.Normal;
    }

    public async UniTask StartGame()
    {
        if (_isStartingGame)
        {
            Debug.LogWarning($"_isStartingGame {_isStartingGame}");
            return;
        }

        _isStartingGame = true;

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
