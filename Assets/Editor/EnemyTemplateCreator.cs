#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates/Converts an Enemy prefab with this hierarchy:
/// Enemy (root: physics, visuals, core scripts)
///  └─ FSM (child: EnemyContext, EnemyStateMachine, BribedAI optional)
/// Also (optionally) adds A* components if present (AIPath, AIDestinationSetter) without compile-time deps.
/// Idempotent: re-running only fills missing parts.
/// </summary>
public static class EnemyTemplateCreator
{
    private const string DefaultPrefabDir  = "Assets/Prefabs/Enemies";
    private const string DefaultPrefabName = "EnemyTemplate.prefab";

    [MenuItem("Assets/Create/Enemy/Enemy Template Prefab", priority = 0)]
    public static void CreateEnemyTemplatePrefab()
    {
        var root = new GameObject("Enemy");
        EnsureEnemySetup(root);

        EnsureFolder(DefaultPrefabDir);
        var prefabPath = Path.Combine(DefaultPrefabDir, DefaultPrefabName).Replace("\\", "/");
        var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Selection.activeObject = saved;
        Debug.Log($"[EnemyTemplateCreator] Created prefab at: {prefabPath}", saved);
    }

    [MenuItem("GameObject/Enemy/Convert To Enemy (Root + FSM Child)", false, priority = 10)]
    public static void ConvertSelectionToEnemy()
    {
        var go = Selection.activeGameObject;
        if (!go)
        {
            EditorUtility.DisplayDialog("Convert To Enemy", "Select a GameObject in the Scene or a Prefab in the Project.", "OK");
            return;
        }

        EnsureEnemySetup(go);
        Debug.Log($"[EnemyTemplateCreator] Converted '{go.name}' into Enemy (with FSM child).", go);
    }

    // ---------- Core Builder ----------

    private static void EnsureEnemySetup(GameObject root)
    {
        TrySetTag(root, "Enemy");
        TrySetLayer(root, "Enemy", includeChildren: true);

        // Physics (root)
        var rb = AddIfMissing<Rigidbody2D>(root);
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // Collider (root)
        var col = AddIfMissing<CapsuleCollider2D>(root);
        if (col.size == Vector2.zero) col.size = new Vector2(0.6f, 1.0f);

        // Visuals (child)
        var sprite = FindOrCreateChild(root.transform, "Sprite");
        var sr     = AddIfMissing<SpriteRenderer>(sprite);

        // Apply consistent sprite renderer settings
        if (sr)
        {
            sr.sortingLayerName = "Objects";         // make sure this layer exists in Project Settings
            sr.sortingOrder     = 0;
            sr.spriteSortPoint  = SpriteSortPoint.Pivot;
        }

        // Utility children (optional buckets)
        FindOrCreateChild(root.transform, "FX");
        FindOrCreateChild(root.transform, "Sensors");

        // Animator (root)
        var animator = AddIfMissing<Animator>(root);

        // Core gameplay scripts (root) — by simple names to avoid hard deps
        AddIfMissingByName(root, "Enemy");
        AddIfMissingByName(root, "EnemyEvents");

        // Optional A* Pathfinding (root) without compile-time dependency
        // Tries both unqualified and qualified names.
        var path = AddIfMissingByName(root, "AIPath") ?? AddIfMissingByName(root, "Pathfinding.AIPath");
        var dest = AddIfMissingByName(root, "AIDestinationSetter") ?? AddIfMissingByName(root, "Pathfinding.AIDestinationSetter");

        // If we managed to add AIPath, configure defaults
        if (path)
        {
            var so = new SerializedObject(path);

            // Bump speed if too low
            var maxSpeedProp = so.FindProperty("maxSpeed");
            if (maxSpeedProp != null && maxSpeedProp.propertyType == SerializedPropertyType.Float)
            {
                if (maxSpeedProp.floatValue < 2.5f) maxSpeedProp.floatValue = 2.5f;
            }

            // Force flags
            TrySetBool(so, "canSearch", true);
            TrySetBool(so, "canMove", true);

            // Orientation: set to YAxisForward if the property exists
            var orientProp = so.FindProperty("orientation");
            if (orientProp != null && orientProp.propertyType == SerializedPropertyType.Enum)
            {
                // OrientationMode.YAxisForward is index 1 in current A* builds
                orientProp.enumValueIndex = 1;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(path);
        }

        // FSM (child) — always created and used for all brain components
        var fsmGO = FindOrCreateChild(root.transform, "FSM");

        var ctx   = AddIfMissingByName(fsmGO, "EnemyContext");
        var sm    = AddIfMissingByName(fsmGO, "EnemyStateMachine");
        var bribed= AddIfMissingByName(fsmGO, "BribedAI");   // optional

        // Wire EnemyContext <-> EnemyStateMachine if both exist
        WireObjectField(ctx, "fsm", sm);
        WireObjectField(sm,  "contextSource", ctx);

        // Gentle auto-wiring by common field names across all components
        AutoWireCommonFields(root, rb, col, animator, sr, path, dest);
    }

    // ---------- FSM wiring & generic field set ----------

    private static void WireObjectField(Component comp, string fieldName, Component target)
    {
        if (!comp || !target) return;

        var so = new SerializedObject(comp);
        var prop = so.FindProperty(fieldName);
        if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue == null)
        {
            prop.objectReferenceValue = target;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
        }
    }

    private static void TrySetBool(SerializedObject so, string fieldName, bool value)
    {
        var p = so.FindProperty(fieldName);
        if (p != null && p.propertyType == SerializedPropertyType.Boolean && p.boolValue != value)
        {
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ---------- Auto-wiring by common names ----------

    private static void AutoWireCommonFields(GameObject root,
                                             Rigidbody2D rb, Collider2D col,
                                             Animator animator, SpriteRenderer sr,
                                             Component path, Component dest)
    {
        var monos = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in monos)
        {
            if (!mb) continue;
            var so = new SerializedObject(mb);
            bool changed = false;

            changed |= TryAssignRef(so, "rb", rb);
            changed |= TryAssignRef(so, "rigidbody", rb);
            changed |= TryAssignRef(so, "rigidbody2D", rb);

            changed |= TryAssignRef(so, "col", col);
            changed |= TryAssignRef(so, "collider", col);
            changed |= TryAssignRef(so, "collider2D", col);

            changed |= TryAssignRef(so, "anim", animator);
            changed |= TryAssignRef(so, "animator", animator);

            changed |= TryAssignRef(so, "spriteRenderer", sr);
            changed |= TryAssignRef(so, "sr", sr);

            // A* convenience names (safe even if components are null)
            changed |= TryAssignRef(so, "path", path);
            changed |= TryAssignRef(so, "aiPath", path);
            changed |= TryAssignRef(so, "destinationSetter", dest);
            changed |= TryAssignRef(so, "aidestinationSetter", dest);

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mb);
            }
        }
    }

    private static bool TryAssignRef(SerializedObject so, string fieldName, Object value)
    {
        if (!value) return false;
        var prop = so.FindProperty(fieldName);
        if (prop == null) return false;
        if (prop.propertyType != SerializedPropertyType.ObjectReference) return false;
        if (prop.objectReferenceValue != null) return false;
        prop.objectReferenceValue = value;
        return true;
    }

    // ---------- Utilities ----------

    private static T AddIfMissing<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c ? c : go.AddComponent<T>();
    }

    /// <summary>
    /// Adds component by simple or qualified type name if it exists in any loaded assembly.
    /// Returns the added component instance (Component) or null if not found.
    /// </summary>
    private static Component AddIfMissingByName(GameObject go, string typeName)
    {
        var have = go.GetComponent(typeName);
        if (have) return have;

        var t = FindTypeAnywhere(typeName);
        if (t == null) return null;

        return go.AddComponent(t);
    }

    private static System.Type FindTypeAnywhere(string typeName)
    {
        // Try exact first
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName, throwOnError: false);
            if (t != null) return t;
        }
        // Fallback: search by short name
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in asm.GetTypes())
            {
                if (t.Name == typeName) return t;
            }
        }
        return null;
    }

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child) return child.gameObject;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void TrySetTag(GameObject go, string tag)
    {
        try
        {
            foreach (var t in UnityEditorInternal.InternalEditorUtility.tags)
                if (t == tag) { go.tag = tag; break; }
        }
        catch { /* ignore if not defined */ }
    }

    private static void TrySetLayer(GameObject go, string layerName, bool includeChildren)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) return;

        if (includeChildren)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }
        else go.layer = layer;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif