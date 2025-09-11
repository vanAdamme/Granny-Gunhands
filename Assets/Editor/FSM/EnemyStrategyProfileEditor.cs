#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyStrategyProfile))]
public class EnemyStrategyProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var profile = (EnemyStrategyProfile)target;

        EditorGUILayout.HelpBox("Pick concrete strategy types to save in this profile.", MessageType.Info);

        DrawTypeDropdown("Move",   ref profile.Move,   StrategyTypeCache.MovementTypes);
        DrawTypeDropdown("Attack", ref profile.Attack, StrategyTypeCache.AttackTypes);
        DrawTypeDropdown("Target", ref profile.Target, StrategyTypeCache.TargetTypes);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(profile);
        }
    }

    private void DrawTypeDropdown(string label, ref StrategySlot slot, Type[] options)
    {
        var currentType = slot.ResolveType();

        var display = new string[options.Length + 1];
        display[0] = "(None)";
        var index = 0;

        for (int i = 0; i < options.Length; i++)
        {
            display[i + 1] = options[i].Name;
            if (options[i] == currentType) index = i + 1;
        }

        var newIndex = EditorGUILayout.Popup(label, index, display);
        if (newIndex != index)
        {
            slot.SetType(newIndex == 0 ? null : options[newIndex - 1]);
        }

        // Show the fully-qualified name (read-only) for debugging/versioning clarity
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Assembly Qualified Name", slot.AssemblyQualifiedName ?? "");
            EditorGUILayout.TextField("Nice Name", slot.NiceName ?? "");
        }
    }
}
#endif