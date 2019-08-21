using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

[CustomEditor(typeof(VariableJoystick))]
public class VariableJoystickEditor : JoystickEditor
{
    private SerializedProperty moveThreshold;
    private SerializedProperty joystickType;

    protected override void OnEnable()
    {
        base.OnEnable();
        moveThreshold = serializedObject.FindProperty("moveThreshold");
        joystickType = serializedObject.FindProperty("joystickType");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (Background != null)
        {
            RectTransform backgroundRect = (RectTransform)Background.objectReferenceValue;
            backgroundRect.pivot = center;
        }
    }

    protected override void DrawValues()
    {
        base.DrawValues();
        EditorGUILayout.PropertyField(moveThreshold, new GUIContent("Move Threshold", "The distance away from the center input has to be before the joystick begins to move."));
        EditorGUILayout.PropertyField(joystickType, new GUIContent("Joystick Type", "The type of joystick the variable joystick is current using."));
    }
}