using ColonyBuildingSim.Objectives;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom inspector for ObjectiveDefinition assets.
/// </summary>
[CustomEditor(typeof(ObjectiveDefinition))]
public class ObjectiveDefinitionEditor : Editor
{
    /// <summary>
    /// Draws the default inspector plus validation tools.
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12.0f);
        EditorGUILayout.LabelField("Objective Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Validate Objective"))
        {
            ValidateObjective();
        }
    }

    /// <summary>
    /// Validates the selected ObjectiveDefinition asset.
    /// </summary>
    private void ValidateObjective()
    {
        ObjectiveDefinition objectiveDefinition = target as ObjectiveDefinition;

        if (objectiveDefinition == null)
        {
            Debug.LogError("No ObjectiveDefinition selected.");
            return;
        }

        bool isValid = true;

        if (string.IsNullOrWhiteSpace(objectiveDefinition.ObjectiveTitle))
        {
            Debug.LogError("ObjectiveDefinition validation failed: Objective title is empty.", objectiveDefinition);
            isValid = false;
        }

        if (objectiveDefinition.Requirements == null || objectiveDefinition.Requirements.Count == 0)
        {
            Debug.LogError("ObjectiveDefinition validation failed: Objective has no requirements.", objectiveDefinition);
            isValid = false;
        }
        else
        {
            for (int i = 0; i < objectiveDefinition.Requirements.Count; ++i)
            {
                ObjectiveRequirement requirement = objectiveDefinition.Requirements[i];

                if (requirement == null)
                {
                    Debug.LogError("ObjectiveDefinition validation failed: Requirement " + i + " is null.", objectiveDefinition);
                    isValid = false;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(requirement.DisplayName))
                {
                    Debug.LogError("ObjectiveDefinition validation failed: Requirement " + i + " has no display name.", objectiveDefinition);
                    isValid = false;
                }

                if (requirement.RequiredAmount <= 0)
                {
                    Debug.LogError("ObjectiveDefinition validation failed: Requirement " + i + " has invalid required amount.", objectiveDefinition);
                    isValid = false;
                }
            }
        }

        if (isValid)
        {
            Debug.Log("ObjectiveDefinition validation passed: " + objectiveDefinition.ObjectiveTitle, objectiveDefinition);
        }
    }
}