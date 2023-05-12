using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FrontlaneDeployer))]
public class FrontlaneDeployerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FrontlaneDeployer fd = (FrontlaneDeployer)target;

        if (GUILayout.Button("Update"))
        {
            fd.FillPlaceholders();
        }
    }
}
