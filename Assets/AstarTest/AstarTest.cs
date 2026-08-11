using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AstarTest : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField, Min(2)] private int _gridWidth = 18;
    [SerializeField, Min(2)] private int _gridHeight = 11;
    [SerializeField, Min(0.1f)] private float _cellSize = 0.65f;
    [SerializeField] private Vector2Int _startCell = new Vector2Int(1, 1);
    [SerializeField] private Vector2Int _targetCell = new Vector2Int(16, 9);

    [Header("A* Visualization")]
    [SerializeField, Min(0f)] private float _searchStepInterval = 0.025f;
    [SerializeField, Min(0.1f)] private float _agentMoveSpeed = 3f;

    [Header("Demo")]
    [SerializeField] private bool _loadDemoObstacles = true;
    [SerializeField, Range(0f, 0.6f)] private float _randomObstacleChance = 0.24f;
    [SerializeField] private int _randomSeed = 12345;
    [SerializeField] private bool _showEditorPreview = true;

    [Header("Colors")]
    [SerializeField] private Color _emptyColor = new Color(0.16f, 0.20f, 0.27f, 1f);
    [SerializeField] private Color _obstacleColor = new Color(0.42f, 0.10f, 0.13f, 1f);
    [SerializeField] private Color _openColor = new Color(0.95f, 0.58f, 0.15f, 1f);
    [SerializeField] private Color _closedColor = new Color(0.18f, 0.42f, 0.68f, 1f);
    [SerializeField] private Color _pathColor = new Color(1f, 0.88f, 0.16f, 1f);
    [SerializeField] private Color _startColor = new Color(0.18f, 0.88f, 0.38f, 1f);
    [SerializeField] private Color _targetColor = new Color(0.95f, 0.20f, 0.60f, 1f);
    [SerializeField] private Color _agentColor = Color.white;

    private const float HudWidth = 560f;
    private const float HudHeight = 172f;

    private bool[,] _walkableCells;
    private SpriteRenderer[,] _cellRenderers;
    private AStarPathfinder _pathfinder;
    private AStarSearchSession _search;
    private IReadOnlyList<Vector2Int> _path;

    private Transform _visualRoot;
    private Sprite _squareSprite;
    private Texture2D _squareTexture;
    private SpriteRenderer _startMarker;
    private SpriteRenderer _targetMarker;
    private SpriteRenderer _agentMarker;
    private LineRenderer _pathLine;
    private Material _lineMaterial;

    private Camera _worldCamera;
    private bool _isPaused;
    private float _searchTimer;
    private int _agentPathIndex;

    private void Awake()
    {
        ClampSettings();
        _worldCamera = Camera.main;

        BuildGrid();

        if (_loadDemoObstacles)
        {
            ApplyDemoObstacles();
        }

        BeginSearch();
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandlePointerInput();
        UpdateSearch();
        UpdateAgentMovement();
    }

    private void OnDestroy()
    {
        if (_lineMaterial != null)
        {
            Destroy(_lineMaterial);
        }

        if (_squareSprite != null)
        {
            Destroy(_squareSprite);
        }

        if (_squareTexture != null)
        {
            Destroy(_squareTexture);
        }
    }

    private void OnValidate()
    {
        ClampSettings();
    }

    private void ClampSettings()
    {
        _gridWidth = Mathf.Max(2, _gridWidth);
        _gridHeight = Mathf.Max(2, _gridHeight);
        _cellSize = Mathf.Max(0.1f, _cellSize);
        _searchStepInterval = Mathf.Max(0f, _searchStepInterval);
        _agentMoveSpeed = Mathf.Max(0.1f, _agentMoveSpeed);
        _startCell = ClampCell(_startCell);
        _targetCell = ClampCell(_targetCell);
    }

    private Vector2Int ClampCell(Vector2Int cell)
    {
        return new Vector2Int(
            Mathf.Clamp(cell.x, 0, _gridWidth - 1),
            Mathf.Clamp(cell.y, 0, _gridHeight - 1));
    }

    private void BuildGrid()
    {
        _walkableCells = new bool[_gridWidth, _gridHeight];
        _cellRenderers = new SpriteRenderer[_gridWidth, _gridHeight];

        _squareTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "AStar Runtime Pixel",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        _squareTexture.SetPixel(0, 0, Color.white);
        _squareTexture.Apply();

        _squareSprite = Sprite.Create(
            _squareTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        _squareSprite.name = "AStar Runtime Square";

        var visualRootObject = new GameObject("Runtime AStar Visuals");
        _visualRoot = visualRootObject.transform;
        _visualRoot.SetParent(transform, false);

        var cellScale = Mathf.Max(0.02f, _cellSize - 0.055f);

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                _walkableCells[x, y] = true;

                var cellObject = new GameObject($"Cell_{x}_{y}");
                cellObject.transform.SetParent(_visualRoot, false);
                cellObject.transform.position = CellToWorld(new Vector2Int(x, y));
                cellObject.transform.localScale = new Vector3(cellScale, cellScale, 1f);

                var spriteRenderer = cellObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = _squareSprite;
                spriteRenderer.color = _emptyColor;
                spriteRenderer.sortingOrder = 0;
                _cellRenderers[x, y] = spriteRenderer;
            }
        }

        _pathfinder = new AStarPathfinder(_gridWidth, _gridHeight, IsWalkable);
        _pathLine = CreatePathLine();
        _startMarker = CreateMarker("Start Marker", _startColor, 10);
        _targetMarker = CreateMarker("Target Marker", _targetColor, 10);
        _agentMarker = CreateMarker("Agent", _agentColor, 20);
        _agentMarker.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private SpriteRenderer CreateMarker(string markerName, Color color, int sortingOrder)
    {
        var markerObject = new GameObject(markerName);
        markerObject.transform.SetParent(_visualRoot, false);
        markerObject.transform.localScale = Vector3.one * (_cellSize * 0.34f);

        var marker = markerObject.AddComponent<SpriteRenderer>();
        marker.sprite = _squareSprite;
        marker.color = color;
        marker.sortingOrder = sortingOrder;
        return marker;
    }

    private LineRenderer CreatePathLine()
    {
        var lineObject = new GameObject("Final Path");
        lineObject.transform.SetParent(_visualRoot, false);

        var line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = false;
        line.startWidth = _cellSize * 0.12f;
        line.endWidth = _cellSize * 0.12f;
        line.startColor = _pathColor;
        line.endColor = _pathColor;
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
        line.positionCount = 0;
        line.sortingOrder = 5;

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            _lineMaterial = new Material(shader)
            {
                name = "AStar Runtime Line Material",
                color = Color.white
            };
            line.sharedMaterial = _lineMaterial;
        }

        return line;
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        var offsetX = (cell.x - (_gridWidth - 1) * 0.5f) * _cellSize;
        var offsetY = (cell.y - (_gridHeight - 1) * 0.5f) * _cellSize;
        return transform.position + new Vector3(offsetX, offsetY, 0f);
    }

    private bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cell)
    {
        var bottomLeft = transform.position - new Vector3(
            _gridWidth * _cellSize * 0.5f,
            _gridHeight * _cellSize * 0.5f,
            0f);

        var x = Mathf.FloorToInt((worldPosition.x - bottomLeft.x) / _cellSize);
        var y = Mathf.FloorToInt((worldPosition.y - bottomLeft.y) / _cellSize);

        if (!_pathfinder.IsInsideGrid(new Vector2Int(x, y)))
        {
            cell = default;
            return false;
        }

        cell = new Vector2Int(x, y);
        return true;
    }

    private bool IsWalkable(Vector2Int cell)
    {
        return _walkableCells[cell.x, cell.y];
    }

    private void ApplyDemoObstacles()
    {
        SetAllWalkable(true);

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                if (IsDemoObstacle(x, y))
                {
                    _walkableCells[x, y] = false;
                }
            }
        }

        EnsureEndpointsWalkable();
        RefreshAllCells();
    }

    private bool IsDemoObstacle(int x, int y)
    {
        if (new Vector2Int(x, y) == _startCell || new Vector2Int(x, y) == _targetCell)
        {
            return false;
        }

        var firstWall = x == 4 && y >= 1 && y <= 9 && y != 3;
        var secondWall = x == 9 && y >= 0 && y <= 8 && y != 6;
        var thirdWall = x == 13 && y >= 2 && y <= 10 && y != 4;
        var lowerShelf = y == 5 && x >= 0 && x <= 3 && x != 2;
        var upperShelf = y == 8 && x >= 5 && x <= 8 && x != 7;
        var middleShelf = y == 2 && x >= 10 && x <= 12 && x != 11;

        return firstWall || secondWall || thirdWall || lowerShelf || upperShelf || middleShelf;
    }

    private void RandomizeObstacles()
    {
        var random = new System.Random(_randomSeed++);

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                var nearStart = Mathf.Abs(x - _startCell.x) + Mathf.Abs(y - _startCell.y) <= 1;
                var nearTarget = Mathf.Abs(x - _targetCell.x) + Mathf.Abs(y - _targetCell.y) <= 1;
                _walkableCells[x, y] = nearStart || nearTarget ||
                                       random.NextDouble() >= _randomObstacleChance;
            }
        }

        BeginSearch();
    }

    private void SetAllWalkable(bool walkable)
    {
        if (_walkableCells == null)
        {
            return;
        }

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                _walkableCells[x, y] = walkable;
            }
        }
    }

    private void EnsureEndpointsWalkable()
    {
        _walkableCells[_startCell.x, _startCell.y] = true;
        _walkableCells[_targetCell.x, _targetCell.y] = true;
    }

    private void BeginSearch()
    {
        _startCell = ClampCell(_startCell);
        _targetCell = ClampCell(_targetCell);
        EnsureEndpointsWalkable();

        _search = _pathfinder.BeginSearch(_startCell, _targetCell);
        _path = null;
        _pathLine.positionCount = 0;
        _searchTimer = 0f;
        _isPaused = false;
        _agentPathIndex = 0;

        PositionMarker(_startMarker, CellToWorld(_startCell), -0.10f);
        PositionMarker(_targetMarker, CellToWorld(_targetCell), -0.10f);
        PositionMarker(_agentMarker, CellToWorld(_startCell), -0.20f);
        _agentMarker.gameObject.SetActive(true);

        RefreshAllCells();
        HandleTerminalSearchState();
    }

    private static void PositionMarker(SpriteRenderer marker, Vector3 position, float zOffset)
    {
        marker.transform.position = new Vector3(position.x, position.y, position.z + zOffset);
    }

    private void UpdateSearch()
    {
        if (_search == null ||
            _search.Status != AStarSearchStatus.Searching ||
            _isPaused)
        {
            return;
        }

        if (_searchStepInterval <= 0f)
        {
            _search.Complete();
            RefreshAllCells();
            HandleTerminalSearchState();
            return;
        }

        _searchTimer += Time.unscaledDeltaTime;

        var safetyCount = 0;
        while (_searchTimer >= _searchStepInterval &&
               _search.Status == AStarSearchStatus.Searching &&
               safetyCount++ < 100)
        {
            _searchTimer -= _searchStepInterval;
            StepSearch();
        }
    }

    private void StepSearch()
    {
        if (_search == null || _search.Status != AStarSearchStatus.Searching)
        {
            return;
        }

        _search.Step();
        RefreshAllCells();
        HandleTerminalSearchState();
    }

    private void HandleTerminalSearchState()
    {
        if (_search == null || _search.Status == AStarSearchStatus.Searching)
        {
            return;
        }

        _isPaused = true;

        if (_search.Status != AStarSearchStatus.PathFound)
        {
            _path = null;
            _pathLine.positionCount = 0;
            return;
        }

        _path = _search.Path;
        _agentPathIndex = Mathf.Min(1, _path.Count);
        DrawFinalPath();
    }

    private void DrawFinalPath()
    {
        if (_path == null || _path.Count == 0)
        {
            _pathLine.positionCount = 0;
            return;
        }

        _pathLine.positionCount = _path.Count;

        for (int i = 0; i < _path.Count; i++)
        {
            var point = CellToWorld(_path[i]);
            _pathLine.SetPosition(i, new Vector3(point.x, point.y, point.z - 0.08f));
        }
    }

    private void UpdateAgentMovement()
    {
        if (_search == null ||
            _search.Status != AStarSearchStatus.PathFound ||
            _path == null ||
            _agentPathIndex >= _path.Count)
        {
            return;
        }

        var pathPoint = CellToWorld(_path[_agentPathIndex]);
        var targetPosition = new Vector3(pathPoint.x, pathPoint.y, pathPoint.z - 0.20f);
        _agentMarker.transform.position = Vector3.MoveTowards(
            _agentMarker.transform.position,
            targetPosition,
            _agentMoveSpeed * Time.deltaTime);

        if ((_agentMarker.transform.position - targetPosition).sqrMagnitude <= 0.0001f)
        {
            _agentMarker.transform.position = targetPosition;
            _agentPathIndex++;
        }
    }

    private void HandleKeyboardInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame &&
            _search?.Status == AStarSearchStatus.Searching)
        {
            _isPaused = !_isPaused;
        }

        if (keyboard.nKey.wasPressedThisFrame &&
            _search?.Status == AStarSearchStatus.Searching)
        {
            _isPaused = true;
            StepSearch();
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            ApplyDemoObstacles();
            BeginSearch();
        }

        if (keyboard.cKey.wasPressedThisFrame)
        {
            SetAllWalkable(true);
            BeginSearch();
        }
    }

    private void HandlePointerInput()
    {
        var mouse = Mouse.current;
        if (mouse == null || _worldCamera == null)
        {
            return;
        }

        var leftPressed = mouse.leftButton.wasPressedThisFrame;
        var rightPressed = mouse.rightButton.wasPressedThisFrame;
        var middlePressed = mouse.middleButton.wasPressedThisFrame;
        if (!leftPressed && !rightPressed && !middlePressed)
        {
            return;
        }

        var screenPosition = mouse.position.ReadValue();
        if (IsPointerOverHud(screenPosition))
        {
            return;
        }

        var cameraDistance = Mathf.Abs(_worldCamera.transform.position.z - transform.position.z);
        var worldPosition = _worldCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, cameraDistance));

        if (!TryWorldToCell(worldPosition, out var cell))
        {
            return;
        }

        var keyboard = Keyboard.current;
        var shiftPressed = keyboard != null &&
                           (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);

        if (middlePressed || leftPressed && shiftPressed)
        {
            ToggleObstacle(cell);
        }
        else if (rightPressed)
        {
            SetTargetCell(cell);
        }
        else if (leftPressed)
        {
            SetStartCell(cell);
        }
    }

    private static bool IsPointerOverHud(Vector2 screenPosition)
    {
        return screenPosition.x <= HudWidth && screenPosition.y >= Screen.height - HudHeight;
    }

    private void ToggleObstacle(Vector2Int cell)
    {
        if (cell == _startCell || cell == _targetCell)
        {
            return;
        }

        _walkableCells[cell.x, cell.y] = !_walkableCells[cell.x, cell.y];
        BeginSearch();
    }

    private void SetStartCell(Vector2Int cell)
    {
        _startCell = cell;
        _walkableCells[cell.x, cell.y] = true;
        BeginSearch();
    }

    private void SetTargetCell(Vector2Int cell)
    {
        _targetCell = cell;
        _walkableCells[cell.x, cell.y] = true;
        BeginSearch();
    }

    private void RefreshAllCells()
    {
        if (_walkableCells == null || _cellRenderers == null)
        {
            return;
        }

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                RefreshCell(new Vector2Int(x, y));
            }
        }
    }

    private void RefreshCell(Vector2Int cell)
    {
        var renderer = _cellRenderers[cell.x, cell.y];

        if (cell == _startCell)
        {
            renderer.color = _startColor;
            return;
        }

        if (cell == _targetCell)
        {
            renderer.color = _targetColor;
            return;
        }

        if (!_walkableCells[cell.x, cell.y])
        {
            renderer.color = _obstacleColor;
            return;
        }

        var state = _search?.GetCellState(cell) ?? AStarCellState.None;
        switch (state)
        {
            case AStarCellState.Open:
                renderer.color = _openColor;
                break;
            case AStarCellState.Closed:
                renderer.color = _closedColor;
                break;
            case AStarCellState.Path:
                renderer.color = _pathColor;
                break;
            default:
                renderer.color = _emptyColor;
                break;
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(12f, 12f, HudWidth - 24f, HudHeight - 12f), GUI.skin.box);
        GUILayout.Label("A* PATHFINDING VISUALIZER - 4 DIRECTIONS");
        GUILayout.Label("Left: Start   Right: Goal   Shift+Left / Middle: Wall");
        GUILayout.Label("Space: Pause/Play   N: Single Step   R: Reset Demo   C: Clear Walls");
        GUILayout.Label(GetStatusText());

        GUILayout.BeginHorizontal();

        var pauseLabel = _isPaused ? "Play" : "Pause";
        if (GUILayout.Button(pauseLabel) &&
            _search?.Status == AStarSearchStatus.Searching)
        {
            _isPaused = !_isPaused;
        }

        if (GUILayout.Button("Step") &&
            _search?.Status == AStarSearchStatus.Searching)
        {
            _isPaused = true;
            StepSearch();
        }

        if (GUILayout.Button("Reset Demo"))
        {
            ApplyDemoObstacles();
            BeginSearch();
        }

        if (GUILayout.Button("Clear Walls"))
        {
            SetAllWalkable(true);
            BeginSearch();
        }

        if (GUILayout.Button("Random Walls"))
        {
            RandomizeObstacles();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private string GetStatusText()
    {
        if (_search == null)
        {
            return "READY";
        }

        switch (_search.Status)
        {
            case AStarSearchStatus.Searching:
                return $"SEARCHING  Open: {_search.OpenCount}  Closed: {_search.ClosedCount}" +
                       (_isPaused ? "  [PAUSED]" : string.Empty);
            case AStarSearchStatus.PathFound:
                return $"PATH FOUND  Cost: {_search.TotalCost}  Nodes: {_search.Path.Count}  " +
                       $"Expanded: {_search.ExpandedNodeCount}";
            case AStarSearchStatus.NoPath:
                return $"NO PATH  Expanded: {_search.ExpandedNodeCount}";
            case AStarSearchStatus.InvalidStartOrTarget:
                return "INVALID START OR GOAL";
            default:
                return "READY";
        }
    }

    private void OnDrawGizmos()
    {
        if (!_showEditorPreview || Application.isPlaying)
        {
            return;
        }

        ClampSettings();
        var previewSize = Mathf.Max(0.02f, _cellSize - 0.055f);

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                if (x == _startCell.x && y == _startCell.y)
                {
                    Gizmos.color = _startColor;
                }
                else if (x == _targetCell.x && y == _targetCell.y)
                {
                    Gizmos.color = _targetColor;
                }
                else if (_loadDemoObstacles && IsDemoObstacle(x, y))
                {
                    Gizmos.color = _obstacleColor;
                }
                else
                {
                    Gizmos.color = _emptyColor;
                }

                Gizmos.DrawCube(
                    CellToWorld(new Vector2Int(x, y)),
                    new Vector3(previewSize, previewSize, 0.05f));
            }
        }

        Gizmos.color = new Color(1f, 1f, 1f, 0.55f);
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(_gridWidth * _cellSize, _gridHeight * _cellSize, 0.1f));
    }
}
