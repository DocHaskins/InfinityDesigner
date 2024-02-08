using UnityEngine;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;

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
        public void ConvertJsonToModelFormat(string jsonInputPath, string modelOutputPath)
        {
            // Read the JSON input
            string jsonInput = File.ReadAllText(jsonInputPath);
            ModelData modelData = JsonUtility.FromJson<ModelData>(jsonInput);

            // Use StringBuilder to create the formatted string
            StringBuilder sb = new StringBuilder();

            var requiredSlots = new List<string>
        {
            "ARMS", "ARMS_PART_1", "ARMS_PART_2", "ARMS_PART_3", "ARMS_PART_4", "ARMS_PART_5", "ARMS_PART_6",
            "HANDS", "HANDS_PART_1", "HANDS_PART_2", "HANDS_PART_3", "HANDS_PART_4", "HANDS_PART_5", "HANDS_PART_6",
            "HAT", "HEAD", "HEADCOVER", "HEADCOVER_PART_1", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6",
            "LEGS", "LEGS_PART_1", "LEGS_PART_2", "LEGS_PART_3", "LEGS_PART_5", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5",
            "PANTS", "PANTS_PART_1", "PLAYER_GLOVES", "TORSO", "TORSO_PART_1", "TORSO_PART_2", "TORSO_PART_3", "TORSO_PART_4", "TORSO_PART_5", "TORSO_PART_6", "TORSO_PART_7", "TORSO_PART_8", "TORSO_PART_9", "SHIELDHOLDER", "PARAGLIDER",
        };

            int nextSlotUid = modelData.slotPairs.Any()
                          ? modelData.slotPairs.Max(sp => sp.slotData.slotUid) + 1
                          : 101;

            var emptySlots = requiredSlots.Except(modelData.slotPairs.Select(sp => sp.slotData.name)).ToList();

            sb.AppendLine("{");
            sb.AppendLine("  \"version\": 6");
            sb.AppendLine("  \"preset\": {");
            sb.AppendLine("    \"skeletonName\": \"" + modelData.skeletonName + "\"");
            sb.AppendLine("  },");
            sb.AppendLine("  \"data\": {");

            AppendProperties(sb, modelData);

            sb.AppendLine("  \"slots\": [");
            // Append slot pairs
            foreach (var slotPair in modelData.slotPairs)
            {
                sb.AppendLine("    {");
                sb.AppendLine($"      \"slotUid\": {slotPair.slotData.slotUid},");
                sb.AppendLine($"      \"name\": \"{slotPair.slotData.name}\",");
                sb.AppendLine("      \"filterText\": \"" + GetFilterText(slotPair.slotData.name) + "\",");
                sb.AppendLine("      \"tagsBits\": 0,");
                sb.AppendLine("      \"shadowMaps\": 15,");
                sb.AppendLine("      \"meshResources\": {");
                sb.AppendLine("        \"resources\": [");

                foreach (var model in slotPair.slotData.models)
                {
                    sb.AppendLine("          {");
                    sb.AppendLine($"            \"name\": \"{model.name}\",");
                    sb.AppendLine("            \"selected\": true,");
                    sb.AppendLine("            \"layoutId\": 4,");

                    // Define the userData array
                    long[] userData = new long[] { 4286643968, 128, 4294901760, 0 };
                    AppendUserData(sb, userData);

                    // Append materialsData
                    AppendMaterialsData(sb, model.materialsData);
                    // Append materialsResources
                    AppendMaterialsResources(sb, model.materialsResources);

                    sb.AppendLine("          },");
                }
                if (slotPair.slotData.models.Count > 0) sb.Remove(sb.Length - 3, 1); // Remove last comma
                sb.AppendLine("        ]");
                sb.AppendLine("      }");
                sb.AppendLine("    },");
            }
            if (modelData.slotPairs.Count > 0)
            {
                // Remove the last comma if there are no empty slots to append
                if (!emptySlots.Any())
                {
                    sb.Remove(sb.Length - 3, 1);
                }
            }

            // Append empty slots
            for (int i = 0; i < emptySlots.Count; i++)
            {
                bool isLastSlot = (i == emptySlots.Count - 1);
                AppendEmptySlot(sb, nextSlotUid++, emptySlots[i], isLastSlot);
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            // Write the formatted string to a .model file
            File.WriteAllText(modelOutputPath, sb.ToString());
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
        ("race", modelData.modelProperties.race),
        ("sex", modelData.modelProperties.sex),
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
                    sb.AppendLine($"                    \"name\": \"{resource.name}\",");
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

        private void AppendEmptySlot(StringBuilder sb, int slotUid, string slotName, bool isLastSlot)
        {
            sb.AppendLine("    {");
            sb.AppendLine($"      \"slotUid\": {slotUid},");
            sb.AppendLine($"      \"name\": \"{slotName}\",");
            sb.AppendLine($"      \"filterText\": \"{GetFilterText(slotName)}\",");
            sb.AppendLine("      \"tagsBits\": 0,");
            sb.AppendLine("      \"shadowMaps\": 15,");
            sb.AppendLine("      \"meshResources\": {");
            sb.AppendLine("        \"resources\": []");
            sb.AppendLine("      }");
            if (!isLastSlot)
            {
                sb.AppendLine("    },");
            }
            else
            {
                sb.AppendLine("    }");
            }
        }
    }
}