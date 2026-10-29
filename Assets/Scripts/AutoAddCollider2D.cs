
using UnityEngine;
using UnityEditor;

public class AutoAddCollider2D : Editor
{
    [MenuItem("Tools/2D/Auto Add Colliders")]
    static void AddColliders()
    {
        GameObject[] objects =
            FindObjectsByType<GameObject>(
                FindObjectsSortMode.None
            );

        int addedCount = 0;

        foreach (GameObject obj in objects)
        {
            if (!obj.scene.IsValid())
                continue;

            string objectName = obj.name.ToLower();

            bool isObstacle =
                objectName.Contains("house") ||
                objectName.Contains("tree") ||
                objectName.Contains("rock") ||
                objectName.Contains("stone");

            if (!isObstacle)
                continue;

            SpriteRenderer sprite =
                obj.GetComponent<SpriteRenderer>();

            if (sprite == null)
                continue;

            // Không thêm Collider nếu đã có
            if (obj.GetComponent<Collider2D>() != null)
                continue;

            BoxCollider2D collider =
                obj.AddComponent<BoxCollider2D>();

            // Lấy kích thước Sprite
            collider.size = sprite.localBounds.size;
            collider.offset = sprite.localBounds.center;

            Undo.RegisterCreatedObjectUndo(
                collider,
                "Add Collider 2D"
            );

            addedCount++;
        }

        Debug.Log(
            "Da them " + addedCount +
            " Box Collider 2D!"
        );
    }
}
