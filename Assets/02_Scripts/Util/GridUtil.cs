using UnityEngine;

public static class GridUtil
{

    // (0, 0) → (0, 0)
    // (1, 0) → (0.5, 0.25)
    // (0, 1) → (-0.5, 0.25)
    // (1, 1) → (0, 0.5)
    //          (1,1)
    //       /         \
    //    (0,1)       (1,0)
    //       \         /
    //          (0,0)
    public static Vector3 GridToWorld(Vector2Int gridPosition, float tileWidth, float tileHeight)
    {
        var worldX = (gridPosition.x - gridPosition.y) * tileWidth * 0.5f;
        var worldY =
            (gridPosition.x + gridPosition.y) *
            tileHeight * 0.5f;

        return new Vector3(worldX, worldY, 0f);
    }

}
