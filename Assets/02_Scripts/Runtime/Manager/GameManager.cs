using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

public class GameManager : SingletonMono<GameManager>
{
    private readonly ReactiveProperty<GameMode> _gameMode = new(global::GameMode.Normal);
    public ReactiveProperty<GameMode> GameMode => _gameMode;

    private readonly ReactiveProperty<EditMode> _editMode = new(global::EditMode.Normal);
    public ReactiveProperty<EditMode> EditMode => _editMode;
    

    private bool _isStartingGame;

    public void EnterEditMode()
    {
        _gameMode.Value = global::GameMode.Edit;
        _editMode.Value = global::EditMode.Normal;
    }

    public void CancelEditMode()
    {
        _gameMode.Value = global::GameMode.Normal;
        _editMode.Value = global::EditMode.Normal;
    }

    public void ToggleEditMode()
    {
        if (_editMode.Value == global::EditMode.Normal)
        {
            _editMode.Value = global::EditMode.Floor;
        }
        else
        {
            _editMode.Value = global::EditMode.Normal;
        }
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
