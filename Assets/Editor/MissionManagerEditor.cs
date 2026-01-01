#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MissionManager))]
public class MissionManagerEditor : Editor
{
    private SerializedProperty allMissionsProperty;
    private SerializedProperty availableDescriptionsProperty;
    private SerializedProperty availableTargetCountsProperty;
    private SerializedProperty availableRewardItemsProperty;
    private SerializedProperty availableEnemyTypesProperty;

    // Cache for reward item lookups
    private Dictionary<string, ItemObject> rewardItemCache;

    void OnEnable()
    {
        allMissionsProperty = serializedObject.FindProperty("allMissions");
        availableDescriptionsProperty = serializedObject.FindProperty("availableDescriptions");
        availableTargetCountsProperty = serializedObject.FindProperty("availableTargetCounts");
        availableRewardItemsProperty = serializedObject.FindProperty("availableRewardItems");
        availableEnemyTypesProperty = serializedObject.FindProperty("availableEnemyTypes");

        BuildRewardItemCache();
    }

    void BuildRewardItemCache()
    {
        rewardItemCache = new Dictionary<string, ItemObject>();

        var manager = target as MissionManager;
        if (manager != null && manager.availableRewardItems != null)
        {
            foreach (var item in manager.availableRewardItems)
            {
                if (item != null)
                {
                    // Cache items by type name (e.g., "Cherries", "Leaf")
                    string typeName = item.itemName.ToLower();
                    if (!rewardItemCache.ContainsKey(typeName))
                    {
                        rewardItemCache[typeName] = item;
                    }
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var manager = target as MissionManager;

        // Draw default inspector for non-mission fields
        EditorGUILayout.PropertyField(serializedObject.FindProperty("missionCanvas"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("missionListScrollView"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("missionItemPrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("missionListContent"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layout Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemSpacing"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("paddingLeft"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("paddingRight"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("paddingTop"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("paddingBottom"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Enemy Types (optional)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(availableEnemyTypesProperty, true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dropdown Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(availableDescriptionsProperty, true);
        EditorGUILayout.PropertyField(availableTargetCountsProperty, true);
        EditorGUILayout.PropertyField(availableRewardItemsProperty, new GUIContent("Available Reward Items (for auto-assignment)"), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mission Data", EditorStyles.boldLabel);

        // Draw missions with custom dropdowns
        DrawMissionList(manager);

        // Add mission button
        if (GUILayout.Button("Add New Mission"))
        {
            allMissionsProperty.arraySize++;
            var newMission = allMissionsProperty.GetArrayElementAtIndex(allMissionsProperty.arraySize - 1);
            InitializeNewMission(newMission);
        }

        serializedObject.ApplyModifiedProperties();

        // Rebuild cache if reward items changed
        if (GUI.changed)
        {
            BuildRewardItemCache();
        }
    }

    void DrawMissionList(MissionManager manager)
    {
        for (int i = 0; i < allMissionsProperty.arraySize; i++)
        {
            var missionProp = allMissionsProperty.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical("box");

            // Mission header with delete button
            EditorGUILayout.BeginHorizontal();
            var idProp = missionProp.FindPropertyRelative("id");
            idProp.isExpanded = EditorGUILayout.Foldout(idProp.isExpanded, $"Mission: {idProp.stringValue}", true);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                allMissionsProperty.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (idProp.isExpanded)
            {
                EditorGUI.indentLevel++;

                // Mission ID
                EditorGUILayout.PropertyField(idProp, new GUIContent("Mission ID"));

                // Description with dropdown
                DrawDescriptionDropdown(missionProp, manager);

                // Target count with dropdown
                DrawTargetCountDropdown(missionProp, manager);

                // Current count
                EditorGUILayout.PropertyField(missionProp.FindPropertyRelative("currentCount"));

                // Enemy type with dropdown
                DrawEnemyTypeDropdown(missionProp, manager);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Reward Settings", EditorStyles.boldLabel);

                // Reward type
                var rewardTypeProp = missionProp.FindPropertyRelative("rewardType");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(rewardTypeProp);

                // Auto-assign reward item when reward type changes
                if (EditorGUI.EndChangeCheck())
                {
                    AutoAssignRewardItem(missionProp, rewardTypeProp.enumValueIndex);
                }

                // Reward amount
                EditorGUILayout.PropertyField(missionProp.FindPropertyRelative("rewardAmount"));

                // Reward item (with auto-assignment hint)
                var rewardItemProp = missionProp.FindPropertyRelative("rewardItem");
                EditorGUILayout.PropertyField(rewardItemProp, new GUIContent("Reward Item (auto-assigned)"));

                // Mission status
                EditorGUILayout.PropertyField(missionProp.FindPropertyRelative("status"));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }

    void DrawDescriptionDropdown(SerializedProperty missionProp, MissionManager manager)
    {
        var descriptionProp = missionProp.FindPropertyRelative("description");

        if (manager.availableDescriptions != null && manager.availableDescriptions.Count > 0)
        {
            // Create dropdown options
            string[] options = new string[manager.availableDescriptions.Count + 1];
            options[0] = "Custom...";
            for (int i = 0; i < manager.availableDescriptions.Count; i++)
            {
                options[i + 1] = manager.availableDescriptions[i];
            }

            // Find current selection
            int selectedIndex = 0;
            for (int i = 0; i < manager.availableDescriptions.Count; i++)
            {
                if (descriptionProp.stringValue == manager.availableDescriptions[i])
                {
                    selectedIndex = i + 1;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("Description", selectedIndex, options);

            if (EditorGUI.EndChangeCheck())
            {
                if (selectedIndex > 0)
                {
                    descriptionProp.stringValue = manager.availableDescriptions[selectedIndex - 1];
                }
            }

            // Show text field if "Custom" is selected
            if (selectedIndex == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(descriptionProp, new GUIContent("Custom Description"));
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            // No dropdown options, show regular text field
            EditorGUILayout.PropertyField(descriptionProp);
            EditorGUILayout.HelpBox("Add descriptions to 'Available Descriptions' list to enable dropdown.", MessageType.Info);
        }
    }

    void DrawTargetCountDropdown(SerializedProperty missionProp, MissionManager manager)
    {
        var targetCountProp = missionProp.FindPropertyRelative("targetCount");

        if (manager.availableTargetCounts != null && manager.availableTargetCounts.Count > 0)
        {
            // Create dropdown options
            string[] options = new string[manager.availableTargetCounts.Count + 1];
            options[0] = "Custom...";
            for (int i = 0; i < manager.availableTargetCounts.Count; i++)
            {
                options[i + 1] = manager.availableTargetCounts[i].ToString();
            }

            // Find current selection
            int selectedIndex = 0;
            for (int i = 0; i < manager.availableTargetCounts.Count; i++)
            {
                if (targetCountProp.intValue == manager.availableTargetCounts[i])
                {
                    selectedIndex = i + 1;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("Target Count", selectedIndex, options);

            if (EditorGUI.EndChangeCheck())
            {
                if (selectedIndex > 0)
                {
                    targetCountProp.intValue = manager.availableTargetCounts[selectedIndex - 1];
                }
            }

            // Show int field if "Custom" is selected
            if (selectedIndex == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(targetCountProp, new GUIContent("Custom Count"));
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            // No dropdown options, show regular int field
            EditorGUILayout.PropertyField(targetCountProp);
        }
    }

    void DrawEnemyTypeDropdown(SerializedProperty missionProp, MissionManager manager)
    {
        var enemyTypeProp = missionProp.FindPropertyRelative("enemyType");

        if (manager.availableEnemyTypes != null && manager.availableEnemyTypes.Count > 0)
        {
            // Create dropdown options
            string[] options = new string[manager.availableEnemyTypes.Count + 1];
            options[0] = "None (Custom)";
            for (int i = 0; i < manager.availableEnemyTypes.Count; i++)
            {
                var enemyType = manager.availableEnemyTypes[i];
                options[i + 1] = enemyType != null ? enemyType.displayName : "Null";
            }

            // Find current selection
            int selectedIndex = 0;
            var currentEnemyType = enemyTypeProp.objectReferenceValue as EnemyType;
            
            if (currentEnemyType != null)
            {
                for (int i = 0; i < manager.availableEnemyTypes.Count; i++)
                {
                    if (manager.availableEnemyTypes[i] == currentEnemyType)
                    {
                        selectedIndex = i + 1;
                        break;
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("Enemy Type", selectedIndex, options);

            if (EditorGUI.EndChangeCheck())
            {
                if (selectedIndex > 0)
                {
                    enemyTypeProp.objectReferenceValue = manager.availableEnemyTypes[selectedIndex - 1];
                }
                else
                {
                    enemyTypeProp.objectReferenceValue = null;
                }
            }

            // Show object field if "None (Custom)" is selected
            if (selectedIndex == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(enemyTypeProp, new GUIContent("Custom Enemy Type"));
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            // No dropdown options, show regular object field
            EditorGUILayout.PropertyField(enemyTypeProp);
            EditorGUILayout.HelpBox("Add Enemy Types to 'Available Enemy Types' list to enable dropdown.", MessageType.Info);
        }
    }

    void AutoAssignRewardItem(SerializedProperty missionProp, int rewardTypeIndex)
    {
        var rewardItemProp = missionProp.FindPropertyRelative("rewardItem");
        var manager = target as MissionManager;

        // Get the reward type enum value
        Mission.RewardType rewardType = (Mission.RewardType)rewardTypeIndex;

        ItemObject itemToAssign = null;

        switch (rewardType)
        {
            case Mission.RewardType.Cherries:
                // Look for cherry item in cache
                if (rewardItemCache.ContainsKey("cherries") || rewardItemCache.ContainsKey("cherry"))
                {
                    itemToAssign = rewardItemCache.ContainsKey("cherries") ?
                                   rewardItemCache["cherries"] :
                                   rewardItemCache["cherry"];
                }
                // Fallback: search in available reward items
                else if (manager.availableRewardItems != null)
                {
                    foreach (var item in manager.availableRewardItems)
                    {
                        if (item != null && item.itemName.ToLower().Contains("cherr"))
                        {
                            itemToAssign = item;
                            break;
                        }
                    }
                }
                break;

            case Mission.RewardType.Leaf:
                // For leaf/money rewards, we typically don't need an item object
                // But if you have a "Leaf" item, we can assign it
                if (rewardItemCache.ContainsKey("leaf") || rewardItemCache.ContainsKey("leaves"))
                {
                    itemToAssign = rewardItemCache.ContainsKey("leaf") ?
                                   rewardItemCache["leaf"] :
                                   rewardItemCache["leaves"];
                }
                else
                {
                    // Clear the item since Leaf rewards use MoneyManager, not items
                    itemToAssign = null;
                }
                break;

            case Mission.RewardType.Item:
                // For generic items, don't auto-assign - let user choose
                // But we can suggest the first available item if none is set
                if (rewardItemProp.objectReferenceValue == null &&
                    manager.availableRewardItems != null &&
                    manager.availableRewardItems.Count > 0)
                {
                    itemToAssign = manager.availableRewardItems[0];
                }
                break;

            case Mission.RewardType.None:
                // Clear the item
                itemToAssign = null;
                break;
        }

        // Assign the item
        rewardItemProp.objectReferenceValue = itemToAssign;

        if (itemToAssign != null)
        {
            Debug.Log($"Auto-assigned reward item: {itemToAssign.itemName} for reward type: {rewardType}");
        }
        else if (rewardType != Mission.RewardType.None && rewardType != Mission.RewardType.Leaf)
        {
            Debug.LogWarning($"Could not auto-assign reward item for type: {rewardType}. Please assign manually or add appropriate items to 'Available Reward Items'.");
        }
    }

    void InitializeNewMission(SerializedProperty missionProp)
    {
        missionProp.FindPropertyRelative("id").stringValue = "mission_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        missionProp.FindPropertyRelative("description").stringValue = "New Mission";
        missionProp.FindPropertyRelative("targetCount").intValue = 5;
        missionProp.FindPropertyRelative("currentCount").intValue = 0;
        missionProp.FindPropertyRelative("rewardType").enumValueIndex = 0;
        missionProp.FindPropertyRelative("rewardAmount").intValue = 0;
        missionProp.FindPropertyRelative("rewardItem").objectReferenceValue = null;
        missionProp.FindPropertyRelative("status").enumValueIndex = 0;

        // Set foldout to expanded for new mission
        missionProp.FindPropertyRelative("id").isExpanded = true;
    }
}
#endif