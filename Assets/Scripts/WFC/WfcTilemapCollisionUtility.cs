using UnityEngine;
using UnityEngine.Tilemaps;

namespace WFC
{
    /// <summary>
    /// Configures 2D collision for a WFC / room-tree tilemap so <see cref="Rigidbody2D.MovePosition"/> actors hit walls.
    /// Uses the Unity-recommended stack: <see cref="RigidbodyType2D.Static"/> + <see cref="CompositeCollider2D"/> +
    /// <see cref="TilemapCollider2D"/> (<see cref="Collider2D.CompositeOperation.Merge"/> in Unity 6), which bakes merged outlines
    /// and registers reliably in Play mode. Walkable tiles (<c>Tile.colliderType = None</c>) leave holes in the merged polygon.
    /// </summary>
    public static class WfcTilemapCollisionUtility
    {
        public static void EnsureTilemapCollider2D(Tilemap tilemap)
        {
            if (tilemap == null) return;

            var rb = tilemap.GetComponent<Rigidbody2D>();
            if (rb == null)
                rb = tilemap.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            rb.simulated = true;
            rb.gravityScale = 0f;

            var composite = tilemap.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = tilemap.gameObject.AddComponent<CompositeCollider2D>();
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            }

            var tileCollider = tilemap.GetComponent<TilemapCollider2D>();
            if (tileCollider == null)
                tileCollider = tilemap.gameObject.AddComponent<TilemapCollider2D>();
            // Unity 6+: replaces deprecated TilemapCollider2D.usedByComposite.
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            // 1) Refresh tile flags so TilemapCollider2D knows shapes changed.
            tilemap.RefreshAllTiles();
            // 2) Force TilemapCollider2D to convert pending tile changes into collision shapes
            //    RIGHT NOW (without this the composite has no shapes to merge after a bulk SetTilesBlock).
            tileCollider.ProcessTilemapChanges();
            // 3) Re-bake merged geometry from the freshly populated tile collider.
            composite.GenerateGeometry();

            Debug.Log(
                $"[WfcCollision] Configured collider on '{tilemap.name}': " +
                $"tileShapes={tileCollider.shapeCount}, compositeShapes={composite.shapeCount}, " +
                $"compositePath(s)={composite.pathCount}, bounds={composite.bounds}, " +
                $"rb.bodyType={rb.bodyType}, rb.simulated={rb.simulated}, " +
                $"tileCollider.isTrigger={tileCollider.isTrigger}, composite.isTrigger={composite.isTrigger}, " +
                $"layer={LayerMask.LayerToName(tilemap.gameObject.layer)}({tilemap.gameObject.layer})");

            if (composite.shapeCount == 0)
            {
                Debug.LogError(
                    $"[WfcCollision] '{tilemap.name}' composite has 0 shapes — nothing will block actors! " +
                    "Likely causes: (a) tilemap has 0 tiles with colliderType != None; " +
                    "(b) all tiles were nulled; (c) the placed TileBase is a RuleTile returning no shape.");
            }
        }
    }
}
