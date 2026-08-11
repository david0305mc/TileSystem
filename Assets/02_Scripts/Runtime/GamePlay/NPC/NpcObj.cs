using System.Collections.Generic;
using UnityEngine;

public class NpcObj : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float _moveSpeed = 2f;
    [SerializeField, Min(0.001f)] private float _arrivalDistance = 0.02f;

    private List<Vector3> _worldPath;
    private int _pathIndex;

    public Vector2Int CurrentGridPosition { get; private set; }
    public bool IsMoving => _worldPath != null && _pathIndex < _worldPath.Count;

    public void Initialize(Vector2Int gridPosition)
    {
        Stop();
        CurrentGridPosition = gridPosition;

        if (GridManager.HasInstance)
        {
            transform.position = GridManager.Instance.GridToWorldPosition(gridPosition);
        }
    }

    /// <summary>
    /// A* 경로를 찾아 지정한 그리드 셀까지 이동을 시작합니다.
    /// </summary>
    public bool MoveTo(Vector2Int targetGridPosition)
    {
        if (!GridManager.HasInstance)
        {
            return false;
        }

        var gridManager = GridManager.Instance;
        if (!gridManager.TryWorldToGridPosition(transform.position, out var startGridPosition) ||
            !gridManager.TryFindPath(startGridPosition, targetGridPosition, out var gridPath))
        {
            Stop();
            return false;
        }

        _worldPath = new List<Vector3>(gridPath.Count);
        foreach (var gridPosition in gridPath)
        {
            _worldPath.Add(gridManager.GridToWorldPosition(gridPosition));
        }

        CurrentGridPosition = startGridPosition;
        _pathIndex = 0;
        return true;
    }

    public bool MoveTo(Vector3 targetWorldPosition)
    {
        if (!GridManager.HasInstance ||
            !GridManager.Instance.TryWorldToGridPosition(targetWorldPosition, out var targetGridPosition))
        {
            return false;
        }

        return MoveTo(targetGridPosition);
    }

    public void Stop()
    {
        _worldPath = null;
        _pathIndex = 0;
    }

    private void Update()
    {
        if (!IsMoving)
        {
            return;
        }

        var targetPosition = _worldPath[_pathIndex];
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            _moveSpeed * Time.deltaTime);

        if ((transform.position - targetPosition).sqrMagnitude >
            _arrivalDistance * _arrivalDistance)
        {
            return;
        }

        transform.position = targetPosition;

        if (GridManager.HasInstance &&
            GridManager.Instance.TryWorldToGridPosition(targetPosition, out var gridPosition))
        {
            CurrentGridPosition = gridPosition;
        }

        _pathIndex++;
        if (_pathIndex >= _worldPath.Count)
        {
            Stop();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_worldPath == null || _pathIndex >= _worldPath.Count)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        var previousPosition = transform.position;

        for (int i = _pathIndex; i < _worldPath.Count; i++)
        {
            Gizmos.DrawLine(previousPosition, _worldPath[i]);
            previousPosition = _worldPath[i];
        }
    }
}
