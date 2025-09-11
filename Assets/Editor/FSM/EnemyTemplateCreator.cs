#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Enemy template builder (root + FSM child).
/// Root: Rigidbody2D, CapsuleCollider2D, Animator, SpriteRenderer, Enemy, EnemyEvents (optional), DamageFlash, (optional) AIPath/AIDestinationSetter
/// FSM child: EnemyContext, EnemyStateMachine, (optional) BribedAI
/// Idempotent: re-running only fills missing parts. No compile-time dependency on A* types.
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

    [MenuItem("GameObject/Granny Gunhands/Convert To Enemy (Root + FSM Child)", false, priority = 10)]
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

        // Animator (root)
        var animator = AddIfMissing<Animator>(root);

        // SpriteRenderer (root) – avoid Animator auto-adding duplicates
        var sr = AddIfMissing<SpriteRenderer>(root);
        if (sr)
        {
            sr.sortingLayerName = "Objects";     // ensure this exists in Project Settings
            sr.sortingOrder     = 0;
            sr.spriteSortPoint  = SpriteSortPoint.Pivot;
        }

        // Core gameplay scripts (root) — Enemy already inherits Target/handles health
        AddIfMissingByName(root, "Enemy");
        AddIfMissingByName(root, "EnemyEvents");   // optional
        AddIfMissingByName(root, "DamageFlash");   // requested

        // Optional A* Pathfinding (root) without compile-time dependency
        var path = AddIfMissingByName(root, "AIPath") ?? AddIfMissingByName(root, "Pathfinding.AIPath");
        var dest = AddIfMissingByName(root, "AIDestinationSetter") ?? AddIfMissingByName(root, "Pathfinding.AIDestinationSetter");

        if (path)
        {
            var so = new SerializedObject(path);

            // Speed minimum
            var maxSpeedProp = so.FindProperty("maxSpeed");
            if (maxSpeedProp != null && maxSpeedProp.propertyType == SerializedPropertyType.Float)
                if (maxSpeedProp.floatValue < 2.5f) maxSpeedProp.floatValue = 2.5f;

            // Enable movement/search flags if they exist
            TrySetBool(so, "canSearch", true);
            TrySetBool(so, "canMove",   true);

            // Orientation: YAxisForward (per your current build index = 1)
            var orientProp = so.FindProperty("orientation");
            if (orientProp != null && orientProp.propertyType == SerializedPropertyType.Enum)
                orientProp.enumValueIndex = 1;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(path);
        }

        // FSM (child) — brain components live here
        var fsmGO = FindOrCreateChild(root.transform, "FSM");
        var ctx    = AddIfMissingByName(fsmGO, "EnemyContext");
        var sm     = AddIfMissingByName(fsmGO, "EnemyStateMachine");
        var bribed = AddIfMissingByName(fsmGO, "BribedAI"); // optional

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
        var dmgFlash = GetComponentByName(root, "DamageFlash");

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

            // Damage flash field on Enemy etc.
            changed |= TryAssignRef(so, "damageFlash", dmgFlash);

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

    private static Component GetComponentByName(GameObject go, string typeName)
    {
        var t = FindTypeAnywhere(typeName);
        return t != null ? go.GetComponent(t) : null;
    }
}
#endif