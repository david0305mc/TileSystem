using System;
using System.Collections.Generic;
using UnityEngine;

public enum AStarSearchStatus
{
    Searching,
    PathFound,
    NoPath,
    InvalidStartOrTarget
}

public enum AStarCellState
{
    None,
    Open,
    Closed,
    Path
}

/// <summary>
/// 상하좌우와 대각선을 포함한 8방향 그리드에서 동작하는 A* 경로 탐색기입니다.
/// 월드 좌표 변환이나 MonoBehaviour에 의존하지 않습니다.
/// </summary>
public sealed class AStarPathfinder
{
    private readonly Func<Vector2Int, bool> _isWalkable;

    public int Width { get; }
    public int Height { get; }

    public AStarPathfinder(int width, int height, Func<Vector2Int, bool> isWalkable)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
        _isWalkable = isWalkable ?? throw new ArgumentNullException(nameof(isWalkable));
    }

    public bool IsInsideGrid(Vector2Int position)
    {
        return position.x >= 0 && position.x < Width &&
               position.y >= 0 && position.y < Height;
    }

    public bool IsWalkable(Vector2Int position)
    {
        return IsInsideGrid(position) && _isWalkable(position);
    }

    public AStarSearchSession BeginSearch(Vector2Int start, Vector2Int target)
    {
        return new AStarSearchSession(this, start, target);
    }

    /// <summary>
    /// 경로에는 시작 셀과 도착 셀이 모두 포함됩니다.
    /// </summary>
    public bool TryFindPath(Vector2Int start, Vector2Int target, out List<Vector2Int> path)
    {
        var search = BeginSearch(start, target);
        search.Complete();

        if (search.Status != AStarSearchStatus.PathFound)
        {
            path = null;
            return false;
        }

        path = new List<Vector2Int>(search.Path);
        return true;
    }
}

/// <summary>
/// A* 탐색 한 건의 진행 상태입니다. Step을 호출해 시각화하거나 Complete로 즉시 완료할 수 있습니다.
/// </summary>
public sealed class AStarSearchSession
{
    private sealed class SearchNode
    {
        public readonly Vector2Int Position;

        public int GCost = int.MaxValue;
        public int HCost;
        public SearchNode Parent;
        public AStarCellState State;

        public int FCost => GCost == int.MaxValue ? int.MaxValue : GCost + HCost;

        public SearchNode(Vector2Int position)
        {
            Position = position;
        }
    }

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.up + Vector2Int.right,
        Vector2Int.right,
        Vector2Int.down + Vector2Int.right,
        Vector2Int.down,
        Vector2Int.down + Vector2Int.left,
        Vector2Int.left,
        Vector2Int.up + Vector2Int.left
    };

    private readonly AStarPathfinder _pathfinder;
    private readonly SearchNode[,] _nodes;
    private readonly List<SearchNode> _openSet = new List<SearchNode>();
    private readonly HashSet<SearchNode> _closedSet = new HashSet<SearchNode>();
    private readonly List<Vector2Int> _path = new List<Vector2Int>();
    private readonly SearchNode _startNode;
    private readonly SearchNode _targetNode;

    public Vector2Int Start { get; }
    public Vector2Int Target { get; }
    public AStarSearchStatus Status { get; private set; }
    public IReadOnlyList<Vector2Int> Path => _path;
    public int OpenCount => _openSet.Count;
    public int ClosedCount => _closedSet.Count;
    public int ExpandedNodeCount { get; private set; }
    public int TotalCost => Status == AStarSearchStatus.PathFound ? _targetNode.GCost : 0;

    internal AStarSearchSession(AStarPathfinder pathfinder, Vector2Int start, Vector2Int target)
    {
        _pathfinder = pathfinder;
        Start = start;
        Target = target;
        _nodes = new SearchNode[pathfinder.Width, pathfinder.Height];

        for (int x = 0; x < pathfinder.Width; x++)
        {
            for (int y = 0; y < pathfinder.Height; y++)
            {
                _nodes[x, y] = new SearchNode(new Vector2Int(x, y));
            }
        }

        if(!pathfinder.IsInsideGrid(start))
        {
            Status = AStarSearchStatus.InvalidStartOrTarget;
            return;
        }

        if (!pathfinder.IsWalkable(target))
        {
            Status = AStarSearchStatus.InvalidStartOrTarget;
            return;
        }

        _startNode = GetNode(start);
        _targetNode = GetNode(target);
        _startNode.GCost = 0;
        _startNode.HCost = GetOctileCost(start, target);
        _startNode.State = AStarCellState.Open;
        _openSet.Add(_startNode);
        Status = AStarSearchStatus.Searching;
    }

    public AStarCellState GetCellState(Vector2Int position)
    {
        if (!_pathfinder.IsInsideGrid(position))
        {
            return AStarCellState.None;
        }

        return GetNode(position).State;
    }

    public AStarSearchStatus Step()
    {
        if (Status != AStarSearchStatus.Searching)
        {
            return Status;
        }

        if (_openSet.Count == 0)
        {
            Status = AStarSearchStatus.NoPath;
            return Status;
        }

        var currentIndex = FindBestOpenNodeIndex();
        var currentNode = _openSet[currentIndex];
        _openSet.RemoveAt(currentIndex);
        _closedSet.Add(currentNode);
        currentNode.State = AStarCellState.Closed;
        ExpandedNodeCount++;

        if (currentNode == _targetNode)
        {
            BuildPath();
            Status = AStarSearchStatus.PathFound;
            return Status;
        }

        foreach (var direction in Directions)
        {
            var neighbourPosition = currentNode.Position + direction;
            if (!_pathfinder.IsWalkable(neighbourPosition))
            {
                continue;
            }

            var isDiagonal = direction.x != 0 && direction.y != 0;
            if (isDiagonal &&
                (!_pathfinder.IsWalkable(currentNode.Position + new Vector2Int(direction.x, 0)) ||
                 !_pathfinder.IsWalkable(currentNode.Position + new Vector2Int(0, direction.y))))
            {
                continue;
            }

            var neighbour = GetNode(neighbourPosition);
            if (_closedSet.Contains(neighbour))
            {
                continue;
            }

            var movementCost = isDiagonal ? 14 : 10;
            var newCost = currentNode.GCost + movementCost;
            if (newCost >= neighbour.GCost)
            {
                continue;
            }

            neighbour.GCost = newCost;
            neighbour.HCost = GetOctileCost(neighbour.Position, Target);
            neighbour.Parent = currentNode;

            if (!_openSet.Contains(neighbour))
            {
                neighbour.State = AStarCellState.Open;
                _openSet.Add(neighbour);
            }
        }

        return Status;
    }

    public AStarSearchStatus Complete()
    {
        while (Status == AStarSearchStatus.Searching)
        {
            Step();
        }

        return Status;
    }

    private SearchNode GetNode(Vector2Int position)
    {
        return _nodes[position.x, position.y];
    }

    private int FindBestOpenNodeIndex()
    {
        var bestIndex = 0;

        for (int i = 1; i < _openSet.Count; i++)
        {
            var candidate = _openSet[i];
            var best = _openSet[bestIndex];

            if (candidate.FCost < best.FCost ||
                candidate.FCost == best.FCost && candidate.HCost < best.HCost)
            {
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void BuildPath()
    {
        _path.Clear();
        var current = _targetNode;

        while (current != null)
        {
            _path.Add(current.Position);
            current.State = AStarCellState.Path;
            current = current.Parent;
        }

        _path.Reverse();
    }

    private static int GetOctileCost(Vector2Int from, Vector2Int to)
    {
        var deltaX = Mathf.Abs(from.x - to.x);
        var deltaY = Mathf.Abs(from.y - to.y);
        var diagonalDistance = Mathf.Min(deltaX, deltaY);
        var straightDistance = Mathf.Max(deltaX, deltaY) - diagonalDistance;

        return diagonalDistance * 14 + straightDistance * 10;
    }
}
