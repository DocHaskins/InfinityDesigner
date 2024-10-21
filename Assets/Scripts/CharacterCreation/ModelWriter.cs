using UnityEngine;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Converts JSON data into a custom model format compatible with the Dying Light 2 Engine, focusing on character customization elements like arms, hands, legs, and torso parts. 
/// It reads model properties from a JSON file, processes them to include or exclude specific slots based on predefined requirements, and generates a new structured format. 
/// This includes handling of empty slots, appending necessary metadata, and ensuring compatibility with the target model system. 
/// The script is designed for flexibility in managing character components, facilitating easy updates and extensions to character models within game development workflows.
/// </summary>

namespace doppelganger
{
    public class ModelWriter : MonoBehaviour
    {
        private void Start()
        {
            LoadSlotUidLookup();
        }
        private Dictionary<string, Queue<int>> slotUidLookup = new Dictionary<string, Queue<int>>();
        private void LoadSlotUidLookup()
        {
            // Correcting the path to include the "SlotData" directory
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "SlotData/SlotUidLookup_Empty.json");
            if (File.Exists(jsonPath))
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var lookup = JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(jsonContent);
                slotUidLookup = lookup.ToDictionary(kvp => kvp.Key, kvp => new Queue<int>(kvp.Value));
            }
            else
            {
                Debug.LogError($"SlotUidLookup_Empty.json file not found at path: {jsonPath}");
                slotUidLookup = new Dictionary<string, Queue<int>>(); // Initialize to avoid null reference
            }
        }
        public void ConvertJsonToModelFormat(string jsonInputPath, string modelOutputPath, string category)
        {
            var requiredSlots = new List<string>
        {
            "ARMS", "ARMS_PART_1", "ARMS_PART_2", "ARMS_PART_3", "ARMS_PART_4", "ARMS_PART_5", "ARMS_PART_6",
            "HANDS", "HANDS_PART_1", "HANDS_PART_2", "HANDS_PART_3", "HANDS_PART_4", "HANDS_PART_5", "HANDS_PART_6",
            "HAT", "HEAD", "HEADCOVER", "HEADCOVER_PART_1", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6",
            "LEGS", "LEGS_PART_1", "LEGS_PART_2", "LEGS_PART_3", "LEGS_PART_5", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5",
            "PANTS", "PANTS_PART_1", "PLAYER_GLOVES", "TORSO", "TORSO_PART_1", "TORSO_PART_2", "TORSO_PART_3", "TORSO_PART_4", "TORSO_PART_5", "TORSO_PART_6", "TORSO_PART_7", "TORSO_PART_8", "TORSO_PART_9",
        };
            var requiredPlayerSlots = new List<string>
        {
            "ARMS", "ARMS_PART_1", "ARMS_PART_2", "ARMS_PART_3", "ARMS_PART_4", "ARMS_PART_5", "ARMS_PART_6",
            "HANDS", "HANDS_PART_1", "HANDS_PART_2", "HANDS_PART_3", "HANDS_PART_4", "HANDS_PART_5", "HANDS_PART_6",
            "HAT", "HEAD", "HEADCOVER", "HEADCOVER_PART_1", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6",
            "LEGS", "LEGS_PART_1", "LEGS_PART_2", "LEGS_PART_3", "LEGS_PART_5", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5",
            "PANTS", "PANTS_PART_1", "PLAYER_GLOVES", "TORSO", "TORSO_PART_1", "TORSO_PART_2", "TORSO_PART_3", "TORSO_PART_4", "TORSO_PART_5", "TORSO_PART_6", "TORSO_PART_7", "TORSO_PART_8", "TORSO_PART_9", "SHIELDHOLDER", "PARAGLIDER",
        };
            // Load the model data from JSON input
            string jsonInput = File.ReadAllText(jsonInputPath);
            ModelData modelData = JsonUtility.FromJson<ModelData>(jsonInput);
            if (!modelData.skeletonName.EndsWith(".msh"))
            {
                modelData.skeletonName += ".msh";
            }

            List<int> existingSlotUids = modelData.slotPairs.Select(sp => sp.slotData.slotUid).ToList();

            // Initialize StringBuilder for JSON output
            StringBuilder sb = new StringBuilder();

            //Debug.Log($"skeletonName: {modelData.skeletonName}");
            // Start JSON structure
            sb.AppendLine("{");
            sb.AppendLine("  \"version\": 6,");
            sb.AppendLine("  \"preset\": {");
            sb.AppendLine($"    \"skeletonName\": \"{modelData.skeletonName}\"");
            sb.AppendLine("  },");
            sb.AppendLine("  \"data\": {");

            // Append properties
            AppendProperties(sb, modelData);

            // Begin slots array
            sb.AppendLine("  \"slots\": [");
            bool isFirstSlotAppended = modelData.slotPairs.Count == 0; // True if no slots have been appended yet

            foreach (var slotPair in modelData.slotPairs)
            {
                AppendSlotPair(sb, slotPair, modelData.slotPairs.LastOrDefault().Equals(slotPair));
                isFirstSlotAppended = false; // After appending the first slot, set this to false
            }

            var usedSlotNames = modelData.slotPairs.Select(sp => sp.slotData.name).ToList();
            bool isPreviousSlotAppended = true;
            int existingUIDs = existingSlotUids.Count;

            if (category.Equals("Player", StringComparison.OrdinalIgnoreCase))
            {
                var slotsToCreate = requiredPlayerSlots.Except(usedSlotNames).ToList(); // Slots that actually need to be created
                int totalRequired = slotsToCreate.Count;

                foreach (var requiredSlot in requiredPlayerSlots)
                {
                    if (!usedSlotNames.Contains(requiredSlot))
                    {
                        int nextAvailableSlotUid = DetermineNextAvailableSlotUid(category, requiredSlot, new HashSet<int>(existingSlotUids));
                        bool isLast = slotsToCreate.IndexOf(requiredSlot) == slotsToCreate.Count - 1; // Check if this is the last slot to create

                        // Note: The following log might need adjustment since createdCount isn't updated in this snippet
                        //Debug.Log($"nextAvailableSlotUid: {nextAvailableSlotUid}, total required: {totalRequired}, created: {slotsToCreate.IndexOf(requiredSlot) + 1}, isLast: {isLast}");

                        AppendEmptySlot(sb, requiredSlot, nextAvailableSlotUid, isFirstSlotAppended, isPreviousSlotAppended, isLast);

                        existingSlotUids.Add(nextAvailableSlotUid); // Update the list with the newly used UID
                        isFirstSlotAppended = false; // Adjusted after the first slot creation
                        isPreviousSlotAppended = true; // Ensures correct formatting for JSON
                    }
                }
            }
            else
            {
                var slotsToCreate = requiredSlots.Except(usedSlotNames).ToList(); // Slots that actually need to be created
                int totalRequired = slotsToCreate.Count;

                foreach (var requiredSlot in requiredSlots)
                {
                    if (!usedSlotNames.Contains(requiredSlot))
                    {
                        int nextAvailableSlotUid = DetermineNextAvailableSlotUid(category, requiredSlot, new HashSet<int>(existingSlotUids));
                        bool isLast = slotsToCreate.IndexOf(requiredSlot) == slotsToCreate.Count - 1; // Check if this is the last slot to create

                        // Note: The following log might need adjustment since createdCount isn't updated in this snippet
                        Debug.Log($"nextAvailableSlotUid: {nextAvailableSlotUid}, total required: {totalRequired}, created: {slotsToCreate.IndexOf(requiredSlot) + 1}, isLast: {isLast}");

                        AppendEmptySlot(sb, requiredSlot, nextAvailableSlotUid, isFirstSlotAppended, isPreviousSlotAppended, isLast);

                        existingSlotUids.Add(nextAvailableSlotUid); // Update the list with the newly used UID
                        isFirstSlotAppended = false; // Adjusted after the first slot creation
                        isPreviousSlotAppended = true; // Ensures correct formatting for JSON
                    }
                }
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            // Write to .model file
            File.WriteAllText(modelOutputPath, sb.ToString());
        }

        private void AppendSlotPair(StringBuilder sb, ModelData.SlotDataPair slotPair, bool isLast)
        {
            sb.AppendLine("    {");
            sb.AppendLine($"      \"slotUid\": {slotPair.slotData.slotUid},");
            sb.AppendLine($"      \"name\": \"{slotPair.slotData.name}\",");
            sb.AppendLine($"      \"filterText\": \"{GetFilterText(slotPair.slotData.name)}\",");
            sb.AppendLine("      \"tagsBits\": 0,");
            sb.AppendLine("      \"shadowMaps\": 15,");
            sb.AppendLine("      \"meshResources\": {");
            if (slotPair.slotData.models.Any())
            {
                sb.AppendLine("        \"resources\": [");

                foreach (var model in slotPair.slotData.models)
                {
                    // Assume GenerateUserData is a method that returns long[] based on model info
                    long[] userData = GenerateUserData(model);

                    sb.AppendLine("          {");
                    sb.AppendLine($"            \"name\": \"{model.name}\",");
                    sb.AppendLine("            \"selected\": true,");
                    sb.AppendLine("            \"layoutId\": 4,");
                    AppendUserData(sb, userData);
                    //sb.AppendLine("            \"variantType\": \"standard\", ");
                    AppendMaterialsData(sb, model.materialsData);
                    AppendMaterialsResources(sb, model.materialsResources);
                    sb.AppendLine("          }" + (!model.Equals(slotPair.slotData.models.Last()) ? "," : ""));
                }
                sb.AppendLine("        ]");
            }
            else
            {
                sb.AppendLine("        \"resources\": []");
            }
            sb.AppendLine("      }");
            sb.AppendLine("    },");
        }
        private long[] GenerateUserData(ModelData.ModelInfo model)
        {
            return new long[] { 4286643968, 128, 4294901760, 0 };
        }


        private string GetFilterText(string name)
        {
            // Implement logic to determine filterText based on name
            // For example:
            return name.Split('_')[0].ToLower(); // Simplified example
        }

        private void AppendUserData(StringBuilder sb, long[] userData)
        {
            sb.AppendLine("            \"userData\": [");
            for (int i = 0; i < userData.Length; i++)
            {
                sb.Append($"              {userData[i]}");
                if (i < userData.Length - 1)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    sb.AppendLine();
                }
            }
            sb.AppendLine("            ],");
        }

        private void AppendProperties(StringBuilder sb, ModelData modelData)
        {
            sb.AppendLine("    \"meshAttribute\": [");
            sb.AppendLine("      0,");
            sb.AppendLine("      0,");
            sb.AppendLine("      0,");
            sb.AppendLine("      0");
            sb.AppendLine("    ],");
            sb.AppendLine("    \"properties\": [");

            var properties = new List<(string name, string value)>
    {
        ("class", modelData.modelProperties.@class),
        ("production_state", "prototype"),
        ("race", modelData.modelProperties.race),
        ("sex", modelData.modelProperties.sex),
        ("size", "normal"),
        ("type", "character"),
        ("weight", "normal")
    };

            for (int i = 0; i < properties.Count; i++)
            {
                var (name, value) = properties[i];
                sb.Append($"      {{ \"name\": \"{name}\", \"value\": \"{value}\" }}");

                if (i < properties.Count - 1)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    sb.AppendLine();
                }
            }

            sb.AppendLine("    ]");
            sb.AppendLine("  },");
        }

        private void AppendMaterialsData(StringBuilder sb, List<ModelData.MaterialData> materialsData)
        {
            sb.AppendLine("            \"materialsData\": [");
            for (int i = 0; i < materialsData.Count; i++)
            {
                var materialData = materialsData[i];
                sb.AppendLine("              {");
                sb.AppendLine($"                \"number\": {materialData.number},");
                sb.AppendLine($"                \"name\": \"{materialData.name}\",");
                sb.AppendLine("                \"layoutId\": 4,");  // Assuming a default layoutId of 4
                sb.AppendLine("                \"loadFlags\": \"S\"");  // Assuming default loadFlags of "S"

                // Append a comma except for the last item
                if (i < materialsData.Count - 1)
                {
                    sb.AppendLine("              },");
                }
                else
                {
                    sb.AppendLine("              }");
                }
            }
            sb.AppendLine("            ],");
        }

        private void AppendMaterialsResources(StringBuilder sb, List<ModelData.MaterialResource> materialsResources)
        {
            sb.AppendLine("            \"materialsResources\": [");
            foreach (var materialResource in materialsResources)
            {
                sb.AppendLine("              {");
                sb.AppendLine($"                \"number\": {materialResource.number},");
                sb.AppendLine("                \"resources\": [");

                foreach (var resource in materialResource.resources)
                {
                    sb.AppendLine("                  {");
                    sb.AppendLine($"                    \"name\": \"{resource.name.Replace("(Instance)", "").Replace(" ", "")}\",");
                    sb.AppendLine($"                    \"selected\": true,");
                    sb.AppendLine($"                    \"layoutId\": 4,");
                    sb.AppendLine($"                    \"loadFlags\": \"S\",");
                    AppendRttiValues(sb, resource.rttiValues);
                    sb.AppendLine("                  },");
                }
                if (materialResource.resources.Count > 0) sb.Remove(sb.Length - 3, 1);
                sb.AppendLine("                ]");
                sb.AppendLine("              },");
            }
            if (materialsResources.Count > 0) sb.Remove(sb.Length - 3, 1);
            sb.AppendLine("            ]");
        }

        private void AppendRttiValues(StringBuilder sb, List<ModelData.RttiValue> rttiValues)
        {
            // If there are no RTTI values, close the array immediately.
            if (rttiValues.Count == 0)
            {
                sb.AppendLine("                    \"rttiValues\": []");
            }
            else
            {
                sb.AppendLine("                    \"rttiValues\": [");
                foreach (var rttiValue in rttiValues)
                {
                    sb.AppendLine("                      {");
                    sb.AppendLine($"                        \"name\": \"{rttiValue.name}\",");
                    sb.AppendLine($"                        \"type\": {rttiValue.type},");
                    sb.AppendLine($"                        \"val_str\": \"{rttiValue.val_str}\"");
                    sb.AppendLine("                      },");
                }
                // Remove the last comma and close the array
                sb.Remove(sb.Length - 3, 1); // Remove last comma
                sb.AppendLine("                    ]");
            }
        }

        private int DetermineNextAvailableSlotUid(string category, string slotName, HashSet<int> existingSlotUids)
        {
            Dictionary<string, int> specialSlotUids = new Dictionary<string, int>
    {
        {"HEAD", 100},
        {"HEADCOVER", 200},
        {"HEADCOVER_PART_1", 201},
        {"PARAGLIDER", 520},
        {"SHIELDHOLDER", 530},
        {"PLAYER_GLOVES", 810}
    };

            // If it's for 'Player' category and the slot is special, return predefined UID
            if (category.Equals("Player", StringComparison.OrdinalIgnoreCase) && specialSlotUids.ContainsKey(slotName))
            {
                int specialUid = specialSlotUids[slotName];
                if (!existingSlotUids.Contains(specialUid))
                {
                    //Debug.Log($"Using predefined UID '{specialUid}' for slot '{slotName}' for category 'Player'.");
                    return specialUid;
                }
            }

            if (slotUidLookup.TryGetValue(slotName, out Queue<int> availableUids))
            {
                while (availableUids.Count > 0)
                {
                    int uid = availableUids.Dequeue(); // Try the next available UID
                    if (!existingSlotUids.Contains(uid))
                    {
                        //Debug.Log($"Found available UID '{uid}' for slot '{slotName}'.");
                        return uid; // Found an available UID
                    }
                }
            }
            else
            {
                Debug.LogWarning($"No available UIDs found for slot '{slotName}'.");
            }
            int startUid = 100;
            int endUid = 999;
            int newUid = startUid;

            while (existingSlotUids.Contains(newUid))
            {
                newUid++;
                // If it exceeds the range, wrap around or extend your range logic here
                if (newUid > endUid)
                {
                    Debug.LogError($"Exceeded UID range for slot '{slotName}'. Consider expanding UID range or checking for errors.");
                    // Handle error condition, perhaps by extending the range or other logic
                    break; // Or return -1 to indicate failure, based on your error handling policy
                }
            }

            //Debug.Log($"Generated new UID '{newUid}' for slot '{slotName}'.");
            return newUid;
        }

        private void AppendEmptySlot(StringBuilder sb, string slotName, int slotUid, bool isFirstSlotAppended, bool isPreviousSlotAppended, bool isLast)
        {
            sb.AppendLine("    {");
            sb.AppendLine($"      \"slotUid\": {slotUid},");
            sb.AppendLine($"      \"name\": \"{slotName}\",");
            sb.AppendLine($"      \"filterText\": \"{Regex.Replace(slotName.ToLower(), @"[\d]|_part_", string.Empty)}\",");
            sb.AppendLine("      \"tagsBits\": 0,");
            sb.AppendLine("      \"shadowMaps\": 15,");
            sb.AppendLine("      \"meshResources\": {");
            sb.AppendLine("        \"resources\": []");
            sb.AppendLine("      }");
            if (isFirstSlotAppended || isPreviousSlotAppended && !isLast)
            {
                sb.AppendLine("    },");
            }
            else if (!isFirstSlotAppended || isPreviousSlotAppended && isLast)
            {
                sb.AppendLine("    }");
            }
        }

    }
}