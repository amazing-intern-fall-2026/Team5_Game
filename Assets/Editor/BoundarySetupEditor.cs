#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BoundarySetupEditor
{
    private const string RootFolder = "Assets/art/EnvironmentBoundary";
    private const string TileFolder = RootFolder + "/Tiles";
    private const string PaletteFolder = RootFolder + "/Palette";
    private const string ScenePath = "Assets/Scenes/demo1.unity";

    private static readonly string[] SpriteNames =
    {
        "boundary-fence-horizontal",
        "boundary-fence-vertical",
        "boundary-corner-top-left",
        "boundary-corner-top-right",
        "boundary-corner-bottom-left",
        "boundary-corner-bottom-right",
        "boundary-fence-gate",
        "boundary-large-rock",
        "boundary-bush",
        "boundary-water-cliff"
    };

    [InitializeOnLoadMethod]
    private static void ScheduleAutomaticSetup()
    {
        EditorApplication.delayCall += GenerateIfNeeded;
    }

    [MenuItem("Farm Game/Environment/Generate Boundary Map")]
    public static void GenerateBoundaryMap()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(TileFolder);
        EnsureFolder(PaletteFolder);

        ConfigureTextureImports();
        AssetDatabase.Refresh();

        Tile[] tiles = CreateTiles();
        CreatePalette(tiles);
        CreateSceneBoundary(tiles);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Boundary] Đã tạo Tile Palette, BoundaryTilemap và collider bao kín map.");
    }

    private static void GenerateIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode
            || !File.Exists(ScenePath))
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            return;
        }

        GameObject grid = GameObject.Find("Grid");
        if (grid != null && grid.transform.Find("BoundaryTilemap") != null)
        {
            return;
        }

        GenerateBoundaryMap();
    }

    private static void ConfigureTextureImports()
    {
        foreach (string spriteName in SpriteNames)
        {
            string path = RootFolder + "/" + spriteName + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[Boundary] Không tìm thấy sprite: " + path);
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1024f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static Tile[] CreateTiles()
    {
        Tile[] tiles = new Tile[SpriteNames.Length];

        for (int i = 0; i < SpriteNames.Length; i++)
        {
            string spritePath = RootFolder + "/" + SpriteNames[i] + ".png";
            string tilePath = TileFolder + "/" + SpriteNames[i] + ".asset";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);

            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.name = SpriteNames[i];
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.transform = Matrix4x4.identity;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            tiles[i] = tile;
        }

        return tiles;
    }

    private static void CreatePalette(Tile[] tiles)
    {
        const string palettePath = PaletteFolder + "/BoundaryPalette.prefab";
        GameObject paletteRoot = new GameObject("BoundaryPalette", typeof(Grid));
        GameObject tilemapObject = new GameObject(
            "BoundaryPaletteTilemap",
            typeof(Tilemap),
            typeof(TilemapRenderer)
        );
        tilemapObject.transform.SetParent(paletteRoot.transform, false);

        Tilemap paletteTilemap = tilemapObject.GetComponent<Tilemap>();
        for (int i = 0; i < tiles.Length; i++)
        {
            paletteTilemap.SetTile(new Vector3Int(i % 5, -(i / 5), 0), tiles[i]);
        }

        GameObject palette = PrefabUtility.SaveAsPrefabAsset(paletteRoot, palettePath);
        Object.DestroyImmediate(paletteRoot);
        AssetDatabase.SetLabels(palette, new[] { "TilePalette" });
    }

    private static void CreateSceneBoundary(Tile[] tiles)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject grid = GameObject.Find("Grid");
        if (grid == null)
        {
            Debug.LogError("[Boundary] Scene demo1 không có Grid.");
            return;
        }

        Tilemap ground = grid.transform.Find("GroundTilemap")?.GetComponent<Tilemap>();
        if (ground == null)
        {
            Debug.LogError("[Boundary] Không tìm thấy GroundTilemap.");
            return;
        }

        Transform existing = grid.transform.Find("BoundaryTilemap");
        GameObject boundaryObject = existing != null
            ? existing.gameObject
            : new GameObject(
                "BoundaryTilemap",
                typeof(Tilemap),
                typeof(TilemapRenderer),
                typeof(TilemapCollider2D)
            );

        if (existing == null)
        {
            boundaryObject.transform.SetParent(grid.transform, false);
        }

        Tilemap boundary = boundaryObject.GetComponent<Tilemap>();
        TilemapRenderer renderer = boundaryObject.GetComponent<TilemapRenderer>();
        TilemapCollider2D boundaryCollider = boundaryObject.GetComponent<TilemapCollider2D>();
        if (boundaryCollider == null)
        {
            boundaryCollider = boundaryObject.AddComponent<TilemapCollider2D>();
        }
        boundary.ClearAllTiles();
        renderer.sortingOrder = 2;
        Material boundaryMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/"
            + "Sprite-Unlit-Default.mat"
        );
        if (boundaryMaterial != null)
        {
            renderer.sharedMaterial = boundaryMaterial;
        }
        boundaryCollider.enabled = true;

        BoundsInt grassBounds = ground.cellBounds;
        int minX = grassBounds.xMin - 1;
        int maxX = grassBounds.xMax;
        int minY = grassBounds.yMin - 1;
        int maxY = grassBounds.yMax;

        Tile horizontal = tiles[0];
        Tile vertical = tiles[1];
        Tile topLeft = tiles[2];
        Tile topRight = tiles[3];
        Tile bottomLeft = tiles[4];
        Tile bottomRight = tiles[5];
        Tile gate = tiles[6];
        Tile rock = tiles[7];
        Tile bush = tiles[8];
        Tile waterCliff = tiles[9];

        int outerMinX = minX - 1;
        int outerMaxX = maxX + 1;
        int outerMinY = minY - 1;
        int outerMaxY = maxY + 1;

        for (int x = outerMinX; x <= outerMaxX; x++)
        {
            boundary.SetTile(new Vector3Int(x, outerMinY, 0), waterCliff);
            boundary.SetTile(new Vector3Int(x, outerMaxY, 0), waterCliff);
        }

        for (int y = outerMinY + 1; y < outerMaxY; y++)
        {
            boundary.SetTile(new Vector3Int(outerMinX, y, 0), waterCliff);
            boundary.SetTile(new Vector3Int(outerMaxX, y, 0), waterCliff);
        }

        for (int x = minX + 1; x < maxX; x++)
        {
            boundary.SetTile(new Vector3Int(x, maxY, 0), horizontal);
            boundary.SetTile(
                new Vector3Int(x, minY, 0),
                x == 0 ? gate : horizontal
            );
        }

        for (int y = minY + 1; y < maxY; y++)
        {
            boundary.SetTile(new Vector3Int(minX, y, 0), vertical);
            boundary.SetTile(new Vector3Int(maxX, y, 0), vertical);
        }

        boundary.SetTile(new Vector3Int(minX, maxY, 0), topLeft);
        boundary.SetTile(new Vector3Int(maxX, maxY, 0), topRight);
        boundary.SetTile(new Vector3Int(minX, minY, 0), bottomLeft);
        boundary.SetTile(new Vector3Int(maxX, minY, 0), bottomRight);

        boundary.SetTile(new Vector3Int(minX, 2, 0), rock);
        boundary.SetTile(new Vector3Int(maxX, -2, 0), bush);
        boundary.RefreshAllTiles();
        boundary.CompressBounds();

        ConfigurePlayerPhysics();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigurePlayerPhysics()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[Boundary] Không tìm thấy Player.");
            return;
        }

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = player.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;

        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        if (playerCollider == null)
        {
            playerCollider = player.AddComponent<BoxCollider2D>();
        }

        playerCollider.isTrigger = false;
        playerCollider.size = new Vector2(0.62f, 0.82f);
        playerCollider.offset = new Vector2(0f, -0.18f);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string name = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
