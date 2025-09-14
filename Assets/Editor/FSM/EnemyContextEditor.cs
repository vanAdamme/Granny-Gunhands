#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonoBehaviour), true)]
public class EnemyContextEditor : Editor
{
    // Match EnemyContext.cs exactly so auto-wiring works
    private static readonly string[] MoveFieldNames   = { "movementSource", "moveStrategy", "move", "movement", "movementStrategy" };
    private static readonly string[] AttackFieldNames = { "attackSource", "attackStrategy", "attack", "attacker", "attackController" };
    private static readonly string[] TargetFieldNames = { "targetProviderSource", "targetProvider", "targeting", "targetStrategy" };
    private static EnemyStrategyProfile lastProfile;

    public override bool RequiresConstantRepaint() => false;

    public override void OnInspectorGUI()
    {
        var mb = (MonoBehaviour)target;
        if (mb is EnemyContext ctx)
        {
            if (!(ctx.TargetProvider is AggroTargetProvider))
            {
                EditorGUILayout.HelpBox(
                    "Target provider is not AggroTargetProvider.\n" +
                    "Enemy will aggro immediately instead of waiting for range/damage.",
                    MessageType.Warning);
            }
        }

        var t  = mb.GetType();
        if (t.Name != "EnemyContext")
        {
            base.OnInspectorGUI();
            return;
        }

        // Draw default first so all serialized fields remain visible
        base.OnInspectorGUI();
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("FSM Strategies", EditorStyles.boldLabel);

        // NOTE: StrategyTypeCache exposes MovementTypes (not MoveTypes)
        DrawStrategyDropdown("Move Strategy",   StrategyTypeCache.MovementTypes, mb, MoveFieldNames);
        DrawStrategyDropdown("Attack Strategy", StrategyTypeCache.AttackTypes,   mb, AttackFieldNames);
        DrawStrategyDropdown("Target Provider", StrategyTypeCache.TargetTypes,   mb, TargetFieldNames);

        EditorGUILayout.Space(8);
        DrawPresetSection(mb);
    }

    private void DrawPresetSection(MonoBehaviour context)
    {
        EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            // Draw a real persistent ObjectField
            lastProfile = (EnemyStrategyProfile)EditorGUILayout.ObjectField(
                lastProfile, typeof(EnemyStrategyProfile), false);

            // Asset picker shortcut
            if (GUILayout.Button("Choose…", GUILayout.Width(80)))
            {
                EditorGUIUtility.ShowObjectPicker<EnemyStrategyProfile>(lastProfile, false, "", 0);
            }

            // Apply button
            EditorGUI.BeginDisabledGroup(!lastProfile);
            if (GUILayout.Button("Apply", GUILayout.Width(70)))
            {
                ApplyTypeToCategory(context, lastProfile.Move.ResolveType(),   StrategyTypeCache.MovementTypes, MoveFieldNames);
                ApplyTypeToCategory(context, lastProfile.Attack.ResolveType(), StrategyTypeCache.AttackTypes,   AttackFieldNames);
                ApplyTypeToCategory(context, lastProfile.Target.ResolveType(), StrategyTypeCache.TargetTypes,   TargetFieldNames);
            }
            EditorGUI.EndDisabledGroup();
        }

        // Handle selection from the object picker
        if (Event.current.commandName == "ObjectSelectorUpdated")
        {
            var picked = EditorGUIUtility.GetObjectPickerObject() as EnemyStrategyProfile;
            if (picked) { lastProfile = picked; Repaint(); }
        }

        // Nice big dropzone
        var dropRect = GUILayoutUtility.GetRect(0, 36, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "Drop EnemyStrategyProfile here", EditorStyles.helpBox);

        var e = Event.current;
        if (dropRect.Contains(e.mousePosition))
        {
            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
                var valid = DragAndDrop.objectReferences.OfType<EnemyStrategyProfile>().FirstOrDefault();
                if (valid)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        lastProfile = valid;
                        Repaint();
                    }
                    e.Use();
                }
            }
        }
    }

    private void DrawStrategyDropdown(string label, Type[] options, MonoBehaviour context, string[] fieldNames)
    {
        var current = GetCurrentAssignedType(context, options, fieldNames);
        var names   = new[] { "(None)" }.Concat(options.Select(o => o.Name)).ToArray();
        var index   = 0;

        if (current != null)
        {
            var idx = Array.FindIndex(options, t => t == current);
            if (idx >= 0) index = idx + 1;
        }

        var newIndex = EditorGUILayout.Popup(label, index, names);
        if (newIndex == index) return;

        Type chosen = (newIndex <= 0) ? null : options[newIndex - 1];
        ApplyTypeToCategory(context, chosen, options, fieldNames);
    }

    private Type GetCurrentAssignedType(MonoBehaviour context, Type[] candidates, string[] fieldNames)
    {
        var hostGO  = context.gameObject;
        var onHost  = hostGO.GetComponents<MonoBehaviour>();
        var haveOne = onHost.FirstOrDefault(m => m && candidates.Any(c => c == m.GetType()));
        if (!haveOne) return null;

        var so = new SerializedObject(context);
        foreach (var name in fieldNames)
        {
            var p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue is MonoBehaviour mb)
            {
                var mt = mb.GetType();
                if (candidates.Any(c => c == mt))
                    return mt;
            }
        }

        return haveOne.GetType();
    }

    private void ApplyTypeToCategory(MonoBehaviour context, Type chosen, Type[] categoryTypes, string[] fieldNames)
    {
        var hostGO = context.gameObject;

        // Remove others in the same category
        foreach (var c in hostGO.GetComponents<MonoBehaviour>())
        {
            if (!c) continue;
            var ct = c.GetType();
            if (categoryTypes.Any(x => x == ct) && (chosen == null || ct != chosen))
            {
                Undo.DestroyObjectImmediate(c);
            }
        }

        MonoBehaviour newComp = null;
        if (chosen != null)
        {
            newComp = (MonoBehaviour)hostGO.GetComponent(chosen);
            if (!newComp)
            {
                Undo.AddComponent(hostGO, chosen);
                newComp = (MonoBehaviour)hostGO.GetComponent(chosen);
            }
        }

        // Auto-wire serialized ref on EnemyContext
        var so = new SerializedObject(context);
        bool changed = false;
        foreach (var name in fieldNames)
        {
            var p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (p.objectReferenceValue != newComp)
                {
                    p.objectReferenceValue = newComp;
                    changed = true;
                }
                break; // only set the first matching field
            }
        }
        if (changed)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(context);
        }

        if (newComp) EditorUtility.SetDirty(newComp);
    }
}
#endif