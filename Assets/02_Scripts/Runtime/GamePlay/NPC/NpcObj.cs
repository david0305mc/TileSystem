using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Serialization;

public class NpcObj : MonoBehaviour, IWorldHudTarget
{
    [SerializeField] private Transform _hudAnchor;

    [FormerlySerializedAs("skeletonAnimation")]
    [SerializeField] protected SkeletonAnimation _skeletonAnimation;

    [Header("Appearance")]
    [SerializeField, SpineSkin]
    private string _baseSkin = "skin-base";

    [SerializeField, SpineSkin]
    private string[] _hairSkins =
    {
        "hair/blue",
        "hair/brown",
        "hair/long-blue-with-scarf",
        "hair/pink",
        "hair/short-red",
    };

    [SerializeField, SpineSkin]
    private string[] _eyeSkins =
    {
        "eyes/eyes-blue",
        "eyes/green",
        "eyes/violet",
        "eyes/yellow",
    };

    [SerializeField, SpineSkin]
    private string[] _noseSkins =
    {
        "nose/long",
        "nose/short",
    };

    [SerializeField, SpineSkin]
    private string[] _clothesSkins =
    {
        "clothes/dress-blue",
        "clothes/dress-green",
        "clothes/hoodie-blue-and-scarf",
        "clothes/hoodie-orange",
    };

    [SerializeField, SpineSkin]
    private string[] _legSkins =
    {
        "legs/boots-pink",
        "legs/boots-red",
        "legs/pants-green",
        "legs/pants-jeans",
    };

    [SerializeField, SpineSkin]
    private string[] _accessorySkins =
    {
        "accessories/backpack",
        "accessories/bag",
        "accessories/cape-blue",
        "accessories/cape-red",
        "accessories/hat-pointy-blue-yellow",
        "accessories/hat-red-yellow",
        "accessories/scarf",
    };

    [SerializeField, Range(0f, 1f)]
    private float _accessoryChance = 0.5f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)]
    private float _moveSpeed = 2f;

    [SerializeField, Min(0.001f)]
    private float _arrivalDistance = 0.02f;

    private readonly List<Vector3> _worldPath = new();

    private int _pathIndex;
    private Vector2Int _targetGridPosition;

    public Transform HudAnchor => _hudAnchor;
    public long Uid { get; private set; }
    public Vector2Int CurrentGridPosition { get; private set; }
    public bool IsMoving { get; private set; }

    protected bool HasPath =>
        _worldPath.Count > 0 && _pathIndex < _worldPath.Count;

    public BaseWorldHud BaseHud { get; set; }

    protected void InitializeNpc(long uid, Vector2Int gridPosition)
    {
        StopMovement();

        Uid = uid;
        CurrentGridPosition = gridPosition;
        _targetGridPosition = gridPosition;

        if (GridManager.HasInstance)
        {
            transform.position = GridManager.Instance.GridToWorldPosition(gridPosition);
        }

        RandomizeAppearance();
    }

    protected bool TrySetDestination(Vector2Int targetGridPosition)
    {
        if (!GridManager.HasInstance)
        {
            return false;
        }

        GridManager gridManager = GridManager.Instance;
        Vector3 targetWorldPosition =
            gridManager.GridToWorldPosition(targetGridPosition);

        if (!gridManager.TryFindWorldPath(
                transform.position,
                targetWorldPosition,
                out var path) ||
            path == null ||
            path.Count == 0)
        {
            return false;
        }

        _worldPath.Clear();
        _worldPath.AddRange(path);
        _targetGridPosition = targetGridPosition;
        _pathIndex = 0;

        return true;
    }

    protected async UniTask MoveToDestinationAsync(
        CancellationToken cancellationToken)
    {
        if (!HasPath)
        {
            return;
        }

        float arrivalDistanceSqr = _arrivalDistance * _arrivalDistance;
        IsMoving = true;

        try
        {
            while (_pathIndex < _worldPath.Count)
            {
                Vector3 targetPosition = _worldPath[_pathIndex];

                while ((transform.position - targetPosition).sqrMagnitude >
                       arrivalDistanceSqr)
                {
                    bool isLeft = transform.position.x > targetPosition.x;
                    SetFlip(isLeft);

                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetPosition,
                        _moveSpeed * Time.deltaTime);

                    await UniTask.Yield(
                        PlayerLoopTiming.Update,
                        cancellationToken);
                }

                transform.position = targetPosition;
                _pathIndex++;
            }

            CurrentGridPosition = _targetGridPosition;
        }
        finally
        {
            StopMovement();
        }
    }

    protected void StopMovement()
    {
        IsMoving = false;
        _worldPath.Clear();
        _pathIndex = 0;
    }

    protected void SetAnimation(string animationName)
    {
        if (_skeletonAnimation != null)
        {
            _skeletonAnimation.AnimationName = animationName;
        }
    }

    protected void RandomizeAppearance()
    {
        if (_skeletonAnimation == null)
        {
            return;
        }

        _skeletonAnimation.Initialize(false);

        Skeleton skeleton = _skeletonAnimation.Skeleton;

        if (skeleton == null)
        {
            return;
        }

        SkeletonData skeletonData = skeleton.Data;
        var combinedSkin = new Skin("npc-random");

        AddSkin(combinedSkin, skeletonData, _baseSkin);
        AddRandomSkin(combinedSkin, skeletonData, _hairSkins);
        AddRandomSkin(combinedSkin, skeletonData, _eyeSkins);
        AddRandomSkin(combinedSkin, skeletonData, _noseSkins);
        AddRandomSkin(combinedSkin, skeletonData, _clothesSkins);
        AddRandomSkin(combinedSkin, skeletonData, _legSkins);

        if (Random.value < _accessoryChance)
        {
            AddRandomSkin(combinedSkin, skeletonData, _accessorySkins);
        }

        skeleton.SetSkin(combinedSkin);
        skeleton.SetSlotsToSetupPose();
    }

    protected void SetFlip(bool isLeft)
    {
        if (_skeletonAnimation?.Skeleton != null)
        {
            _skeletonAnimation.Skeleton.ScaleX = isLeft ? -1f : 1f;
        }
    }

    private static void AddRandomSkin(
        Skin combinedSkin,
        SkeletonData skeletonData,
        IReadOnlyList<string> skinNames)
    {
        if (skinNames == null || skinNames.Count == 0)
        {
            return;
        }

        AddSkin(
            combinedSkin,
            skeletonData,
            skinNames[Random.Range(0, skinNames.Count)]);
    }

    private static void AddSkin(
        Skin combinedSkin,
        SkeletonData skeletonData,
        string skinName)
    {
        if (string.IsNullOrEmpty(skinName))
        {
            return;
        }

        Skin skin = skeletonData.FindSkin(skinName);

        if (skin != null)
        {
            combinedSkin.AddSkin(skin);
        }
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if (!HasPath)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Vector3 previousPosition = transform.position;

        for (int i = _pathIndex; i < _worldPath.Count; i++)
        {
            Vector3 targetPosition = _worldPath[i];
            Gizmos.DrawLine(previousPosition, targetPosition);
            previousPosition = targetPosition;
        }
    }


#endif
}
