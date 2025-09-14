#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RequireInterfaceAttribute))]
public class RequireInterfaceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attrib = (RequireInterfaceAttribute)attribute;

        if (property.propertyType == SerializedPropertyType.ObjectReference)
        {
            EditorGUI.BeginChangeCheck();
            var obj = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(Component), true);
            if (EditorGUI.EndChangeCheck())
            {
                if (obj == null || attrib.RequiredType.IsAssignableFrom(obj.GetType()))
                {
                    property.objectReferenceValue = obj;
                }
                else
                {
                    // If it's a Component, check its interfaces
                    if (obj is Component comp && attrib.RequiredType.IsAssignableFrom(comp.GetType()))
                        property.objectReferenceValue = comp;
                    else
                        Debug.LogError($"{obj.name} does not implement {attrib.RequiredType.Name}");
                }
            }
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use [RequireInterface] on a Component field.");
        }
    }
}
#endif