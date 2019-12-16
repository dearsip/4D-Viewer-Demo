using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

[CanEditMultipleObjects, CustomEditor(typeof(StepSlider), true)]
public class StepSliderEditor : SliderEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        this.serializedObject.Update();
        EditorGUILayout.PropertyField(this.serializedObject.FindProperty("step"), true);
        this.serializedObject.ApplyModifiedProperties();
    }
}