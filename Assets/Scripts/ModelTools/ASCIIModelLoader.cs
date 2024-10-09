using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class ASCIIModelLoader : MonoBehaviour
{
    public string filePath;
    public float uvDisplacementX = 0f;
    public float uvDisplacementY = 0f;

    private Dictionary<int, List<int>> mergedVertices = new Dictionary<int, List<int>>();
    private HashSet<Vector2Int> seamEdges = new HashSet<Vector2Int>();

    [Serializable]
    public class XPSMesh
    {
        public string name;
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<List<Vector2>> uvLayers = new List<List<Vector2>>();
        public List<Color> colors = new List<Color>();
        public List<int> triangles = new List<int>();
        public List<BoneWeight> boneWeights = new List<BoneWeight>();
        public List<XPSTexture> textures = new List<XPSTexture>();
    }

    [Serializable]
    public class XPSTexture
    {
        public int id;
        public string file;
        public int uvLayer;
    }

    public void LoadASCIIModel()
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Debug.LogError("Invalid file path");
            return;
        }

        string[] lines = File.ReadAllLines(filePath);
        int currentLine = 0;

        try
        {
            // Read bone data
            if (!int.TryParse(lines[currentLine], out int boneCount))
            {
                Debug.LogError($"Invalid bone count at line {currentLine}: {lines[currentLine]}");
                return;
            }
            currentLine++;
            Debug.Log($"Bone count: {boneCount}");

            // Create bones using the new CreateArmature method
            Transform[] bones = CreateArmature(boneCount);
            Matrix4x4[] bindPoses = new Matrix4x4[boneCount];
            string[] boneNames = new string[boneCount];
            Vector3[] bonePositions = new Vector3[boneCount];
            int[] parentIndices = new int[boneCount];

            for (int i = 0; i < boneCount; i++)
            {
                if (currentLine >= lines.Length)
                {
                    Debug.LogError($"Unexpected end of file while reading bones at line {currentLine}");
                    return;
                }

                boneNames[i] = lines[currentLine++];
                //Debug.Log($"Reading bone {i}: {boneNames[i]}");

                if (!int.TryParse(lines[currentLine], out int parentBoneIndex))
                {
                    Debug.LogError($"Invalid parent bone index at line {currentLine}: {lines[currentLine]}");
                    return;
                }
                parentIndices[i] = parentBoneIndex;
                currentLine++;

                bonePositions[i] = ReadVector3(lines[currentLine++]);
                //Debug.Log($"Bone {boneNames[i]} position: {bonePositions[i]}");
            }

            // Import bones using the new ImportBones method
            ImportBones(bones, boneNames, bonePositions, parentIndices);

            // Calculate bone tails after importing bones
            CalculateBoneTails(bones);

            // Set minimum bone lengths
            foreach (var bone in bones)
            {
                SetMinimumLength(bone);
            }

            ConnectBones(bones, true);

            for (int i = 0; i < bones.Length; i++)
            {
                bindPoses[i] = bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
            }

            // Read mesh count
            if (!int.TryParse(lines[currentLine], out int meshCount))
            {
                Debug.LogError($"Invalid mesh count at line {currentLine}: {lines[currentLine]}");
                return;
            }
            currentLine++;
            Debug.Log($"Mesh count: {meshCount}");

            List<XPSMesh> xpsMeshes = new List<XPSMesh>();

            for (int meshIndex = 0; meshIndex < meshCount; meshIndex++)
            {
                if (currentLine >= lines.Length)
                {
                    Debug.LogError($"Unexpected end of file while reading meshes at line {currentLine}");
                    return;
                }

                XPSMesh xpsMesh = new XPSMesh();
                xpsMesh.name = lines[currentLine++];
                Debug.Log($"Reading mesh {meshIndex}: {xpsMesh.name}");

                if (!int.TryParse(lines[currentLine], out int uvLayerCount))
                {
                    Debug.LogError($"Invalid UV layer count at line {currentLine}: {lines[currentLine]}");
                    return;
                }
                currentLine++;
                Debug.Log($"UV layer count: {uvLayerCount}");

                for (int i = 0; i < uvLayerCount; i++)
                {
                    xpsMesh.uvLayers.Add(new List<Vector2>());
                }

                if (!int.TryParse(lines[currentLine], out int textureCount))
                {
                    Debug.LogError($"Invalid texture count at line {currentLine}: {lines[currentLine]}");
                    return;
                }
                currentLine++;
                Debug.Log($"Texture count: {textureCount}");

                // Read texture data
                for (int i = 0; i < textureCount; i++)
                {
                    if (currentLine + 1 >= lines.Length)
                    {
                        Debug.LogError($"Unexpected end of file while reading textures at line {currentLine}");
                        return;
                    }

                    XPSTexture texture = new XPSTexture();
                    texture.file = lines[currentLine++];
                    if (!int.TryParse(lines[currentLine], out int uvLayer))
                    {
                        Debug.LogError($"Invalid UV layer index at line {currentLine}: {lines[currentLine]}");
                        return;
                    }
                    currentLine++;
                    texture.uvLayer = uvLayer;
                    texture.id = i;
                    xpsMesh.textures.Add(texture);
                    Debug.Log($"Added texture: {texture.file}, UV layer: {texture.uvLayer}");
                }

                // Read vertices
                if (!int.TryParse(lines[currentLine], out int vertexCount))
                {
                    Debug.LogError($"Invalid vertex count at line {currentLine}: {lines[currentLine]}");
                    return;
                }
                currentLine++;
                Debug.Log($"Vertex count: {vertexCount}");

                for (int i = 0; i < vertexCount; i++)
                {
                    if (currentLine + 2 + uvLayerCount >= lines.Length)
                    {
                        Debug.LogError($"Unexpected end of file while reading vertices at line {currentLine}");
                        return;
                    }

                    xpsMesh.vertices.Add(ReadVector3(lines[currentLine++]));
                    xpsMesh.normals.Add(ReadVector3(lines[currentLine++]));
                    xpsMesh.colors.Add(ReadColor(lines[currentLine++]));

                    for (int j = 0; j < uvLayerCount; j++)
                    {
                        Vector2 uv = ReadVector2(lines[currentLine++]);
                        xpsMesh.uvLayers[j].Add(TransformUV(uv));
                    }

                    // Read bone weights
                    int[] boneIndices = ReadIntArray(lines[currentLine++]);
                    float[] weights = ReadFloatArray(lines[currentLine++]);

                    if (boneIndices.Length != weights.Length)
                    {
                        Debug.LogWarning($"Mismatch between bone indices and weights count for vertex {i}. Indices: {boneIndices.Length}, Weights: {weights.Length}");
                    }

                    BoneWeight bw = new BoneWeight();
                    for (int j = 0; j < 4; j++)
                    {
                        if (j < boneIndices.Length && j < weights.Length)
                        {
                            switch (j)
                            {
                                case 0: bw.boneIndex0 = boneIndices[j]; bw.weight0 = weights[j]; break;
                                case 1: bw.boneIndex1 = boneIndices[j]; bw.weight1 = weights[j]; break;
                                case 2: bw.boneIndex2 = boneIndices[j]; bw.weight2 = weights[j]; break;
                                case 3: bw.boneIndex3 = boneIndices[j]; bw.weight3 = weights[j]; break;
                            }
                        }
                    }
                    xpsMesh.boneWeights.Add(bw);

                    int vertexIndex = GetMergedVertexIndex(xpsMesh.vertices[i], xpsMesh.normals[i]);
                    if (vertexIndex != i)
                    {
                        FindMergedVertices(mergedVertices, vertexIndex, new List<int> { i });
                    }

                    if (i % 1000 == 0)
                    {
                        Debug.Log($"Processed {i} vertices");
                    }
                }

                // Read triangles
                if (!int.TryParse(lines[currentLine], out int triangleCount))
                {
                    Debug.LogError($"Invalid triangle count at line {currentLine}: {lines[currentLine]}");
                    return;
                }
                currentLine++;
                Debug.Log($"Triangle count: {triangleCount}");

                for (int i = 0; i < triangleCount; i++)
                {
                    if (currentLine >= lines.Length)
                    {
                        Debug.LogError($"Unexpected end of file while reading triangles at line {currentLine}");
                        return;
                    }

                    int[] triIndices = ReadTriangle(lines[currentLine++]);
                    if (triIndices.Length == 3)
                    {
                        xpsMesh.triangles.Add(triIndices[0]);
                        xpsMesh.triangles.Add(triIndices[2]); // Swap these two to fix winding order
                        xpsMesh.triangles.Add(triIndices[1]);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid triangle data at line {currentLine - 1}: {lines[currentLine - 1]}");
                    }

                    if (i % 1000 == 0)
                    {
                        Debug.Log($"Processed {i} triangles");
                    }
                }

                xpsMeshes.Add(xpsMesh);
                Debug.Log($"Finished reading mesh {meshIndex}: {xpsMesh.name}");
            }

            Debug.Log("Starting to create game objects for each mesh");

            // Create game objects for each mesh
            foreach (XPSMesh xpsMesh in xpsMeshes)
            {
                GameObject meshObject = new GameObject(xpsMesh.name);
                meshObject.transform.SetParent(transform);
                //meshObject.transform.localPosition = Vector3.zero;  // Ensure the local position is reset
                //meshObject.transform.localRotation = Quaternion.identity;  // Reset rotation if necessary

                Mesh mesh = CreateMesh(
                    xpsMesh.name,
                    xpsMesh.vertices.ToArray(),
                    xpsMesh.normals.ToArray(),
                    xpsMesh.colors.ToArray(),
                    xpsMesh.triangles.ToArray(),
                    xpsMesh.boneWeights.ToArray(),
                    bindPoses,
                    xpsMesh.uvLayers.ToArray()
                );

                SkinnedMeshRenderer smr = meshObject.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = mesh;
                smr.bones = bones;
                smr.rootBone = bones[0];

                // Create and assign material
                Material material = CreateMaterial(xpsMesh);
                smr.material = material;

                Debug.Log($"Created game object for mesh: {xpsMesh.name}");
            }

            Debug.Log($"Loaded XPS ASCII model with {xpsMeshes.Count} meshes and {bones.Length} bones.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing XPS ASCII file at line {currentLine + 1}: {e.Message}");
            if (currentLine < lines.Length)
                Debug.LogError($"Line content: {lines[currentLine]}");
            Debug.LogException(e);
        }
    }

    private Mesh CreateMesh(string meshName, Vector3[] vertices, Vector3[] normals, Color[] colors, int[] triangles, BoneWeight[] boneWeights, Matrix4x4[] bindPoses, List<Vector2>[] uvs)
    {
        // Transform vertices to match Blender's coordinate system transformation.
        //for (int i = 0; i < vertices.Length; i++)
        //{
        //    // Swap Y and Z coordinates and invert Z to match Blender's coordTransform logic.
        //    vertices[i] = new Vector3(vertices[i].x, vertices[i].z, -vertices[i].y);
        //    normals[i] = new Vector3(normals[i].x, normals[i].z, -normals[i].y); // Adjust normal if needed
        //}

        Mesh mesh = new Mesh();
        mesh.name = meshName;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.boneWeights = boneWeights;
        mesh.bindposes = bindPoses;

        ApplySeams(mesh);

        if (uvs != null && uvs.Length > 0)
        {
            mesh.uv = uvs[0].ToArray();
            if (uvs.Length > 1)
                mesh.uv2 = uvs[1].ToArray();
            if (uvs.Length > 2)
                mesh.uv3 = uvs[2].ToArray();
            if (uvs.Length > 3)
                mesh.uv4 = uvs[3].ToArray();
        }

        return mesh;
    }

    private string GenerateVertexKey(Vector3 position, Vector3 normal)
    {
        return $"{position.x},{position.y},{position.z},{normal.x},{normal.y},{normal.z}";
    }

    private void FindMergedVertices(Dictionary<int, List<int>> mergedVertices, int vertexIndex, List<int> faceIndices)
    {
        if (!mergedVertices.ContainsKey(vertexIndex))
        {
            mergedVertices[vertexIndex] = new List<int>();
        }
        mergedVertices[vertexIndex].AddRange(faceIndices);
    }

    private Transform[] CreateArmature(int boneCount)
    {
        Transform[] bones = new Transform[boneCount];
        for (int i = 0; i < boneCount; i++)
        {
            GameObject bone = new GameObject($"Bone_{i}");
            bones[i] = bone.transform;
            bones[i].SetParent(transform, false);
        }
        return bones;
    }

    private void ImportBones(Transform[] bones, string[] boneNames, Vector3[] bonePositions, int[] parentIndices)
    {
        for (int i = 0; i < bones.Length; i++)
        {
            bones[i].name = boneNames[i];
            bones[i].localPosition = bonePositions[i];

            // Assuming you have rotation data (e.g., Quaternion[]) in your file, use that to set the local rotation.
            // If rotation data is not available, ensure bones are correctly oriented.
            Quaternion rotation = Quaternion.identity;  // Replace with the actual rotation data if available
            bones[i].localRotation = rotation;

            if (parentIndices[i] >= 0)
            {
                bones[i].SetParent(bones[parentIndices[i]], false);
            }
        }
    }

    private void ApplySeams(Mesh mesh)
    {
        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            MarkSeam(triangles[i], triangles[i + 1]);
            MarkSeam(triangles[i + 1], triangles[i + 2]);
            MarkSeam(triangles[i + 2], triangles[i]);
        }
    }

    private Material CreateMaterial(XPSMesh xpsMesh)
    {
        // Use a default shader if the specific one is not found
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogWarning("URP Lit shader not found. Falling back to Standard shader.");
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            Debug.LogError("No suitable shader found. Please ensure you have either URP or Standard shaders available.");
            return new Material(Shader.Find("Diffuse")); // Fallback to a very basic shader
        }

        Material material = new Material(shader);
        material.name = xpsMesh.name + "_Material";

        for (int i = 0; i < xpsMesh.textures.Count; i++)
        {
            string texturePath = Path.Combine(Path.GetDirectoryName(filePath), xpsMesh.textures[i].file);
            if (File.Exists(texturePath))
            {
                Texture2D texture = LoadTexture(texturePath);
                string textureName = GetTextureNameForIndex(i);
                material.SetTexture(textureName, texture);

                // Set UV scale and offset for this texture
                int uvLayer = xpsMesh.textures[i].uvLayer;
                if (uvLayer < xpsMesh.uvLayers.Count)
                {
                    material.SetTextureScale(textureName, Vector2.one);
                    material.SetTextureOffset(textureName, new Vector2(uvDisplacementX, uvDisplacementY));
                }

                // Enable relevant shader features based on texture type
                EnableShaderFeatures(material, i);
            }
        }

        return material;
    }

    private string GetTextureNameForIndex(int index)
    {
        switch (index)
        {
            case 0: return "_BaseMap"; // Diffuse
            case 1: return "_BumpMap"; // Normal map
            case 2: return "_MetallicGlossMap"; // Metallic/Specular
            case 3: return "_EmissionMap"; // Emission
            case 4: return "_OcclusionMap"; // Occlusion
                                            // Add more cases as needed
            default: return "_BaseMap";
        }
    }

    private void EnableShaderFeatures(Material material, int textureIndex)
    {
        switch (textureIndex)
        {
            case 1:
                material.EnableKeyword("_NORMALMAP");
                break;
            case 2:
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                break;
            case 3:
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                break;
            case 4:
                material.EnableKeyword("_OCCLUSIONMAP");
                break;
                // Add more cases as needed
        }
    }

    private void CalculateBoneTails(Transform[] bones)
    {
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i].childCount > 0)
            {
                Vector3 childrenCenter = Vector3.zero;
                for (int j = 0; j < bones[i].childCount; j++)
                {
                    childrenCenter += bones[i].GetChild(j).localPosition;
                }
                childrenCenter /= bones[i].childCount;

                bones[i].localScale = new Vector3(0.1f, (childrenCenter - bones[i].localPosition).magnitude, 0.1f);
                bones[i].LookAt(bones[i].parent.TransformPoint(childrenCenter));
            }
            else
            {
                bones[i].localScale = new Vector3(0.1f, 0.1f, 0.1f);
            }
        }
    }

    private void SetMinimumLength(Transform bone, float minLength = 0.005f)
    {
        if (bone.localScale.y < minLength)
        {
            bone.localScale = new Vector3(bone.localScale.x, minLength, bone.localScale.z);
        }
    }

    private void ConnectBones(Transform[] bones, bool connect)
    {
        for (int i = 1; i < bones.Length; i++)
        {
            if (connect && bones[i].parent != null)
            {
                bones[i].localPosition = bones[i].parent.localPosition + Vector3.up * bones[i].localScale.y;
            }
        }
    }

    private void SmoothNormals(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = new Vector3[vertices.Length];
        int[] triangles = mesh.triangles;

        Dictionary<Vector3, List<Vector3>> vertexToNormalMap = new Dictionary<Vector3, List<Vector3>>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int index0 = triangles[i];
            int index1 = triangles[i + 1];
            int index2 = triangles[i + 2];

            Vector3 vertex0 = vertices[index0];
            Vector3 vertex1 = vertices[index1];
            Vector3 vertex2 = vertices[index2];

            Vector3 normal = Vector3.Cross(vertex1 - vertex0, vertex2 - vertex0).normalized;

            if (!vertexToNormalMap.ContainsKey(vertex0))
                vertexToNormalMap[vertex0] = new List<Vector3>();
            if (!vertexToNormalMap.ContainsKey(vertex1))
                vertexToNormalMap[vertex1] = new List<Vector3>();
            if (!vertexToNormalMap.ContainsKey(vertex2))
                vertexToNormalMap[vertex2] = new List<Vector3>();

            vertexToNormalMap[vertex0].Add(normal);
            vertexToNormalMap[vertex1].Add(normal);
            vertexToNormalMap[vertex2].Add(normal);
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            if (vertexToNormalMap.ContainsKey(vertex))
            {
                Vector3 smoothNormal = Vector3.zero;
                foreach (Vector3 normal in vertexToNormalMap[vertex])
                {
                    smoothNormal += normal;
                }
                normals[i] = smoothNormal.normalized;
            }
        }

        mesh.normals = normals;
    }

    private Texture2D LoadTexture(string path)
    {
        Texture2D texture = new Texture2D(2, 2);
        byte[] fileData = File.ReadAllBytes(path);
        texture.LoadImage(fileData);
        return texture;
    }

    private Vector2 TransformUV(Vector2 uv)
    {
        return new Vector2(uv.x + uvDisplacementX, 1 - uv.y - uvDisplacementY);
    }

    private Vector3 ReadVector3(string line)
    {
        try
        {
            string[] values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Check if there are more than three values
            if (values.Length < 3)
            {
                Debug.LogError($"Invalid Vector3 data: {line}");
                return Vector3.zero;
            }

            // Assuming you only need the first three for position
            return new Vector3(
                float.Parse(values[0], CultureInfo.InvariantCulture),
                float.Parse(values[1], CultureInfo.InvariantCulture),
                float.Parse(values[2], CultureInfo.InvariantCulture)
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing Vector3: {line}");
            Debug.LogException(e);
            return Vector3.zero;
        }
    }

    private Vector2 ReadVector2(string line)
    {
        string[] values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return new Vector2(
            ParseFloat(values[0]),
            ParseFloat(values[1])
        );
    }
    private Quaternion ReadQuaternion(string line)
    {
        string[] values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return new Quaternion(
            ParseFloat(values[0]),
            ParseFloat(values[1]),
            ParseFloat(values[2]),
            ParseFloat(values[3])
        );
    }

    private void MarkSeam(int v1, int v2)
    {
        seamEdges.Add(new Vector2Int(Mathf.Min(v1, v2), Mathf.Max(v1, v2)));
    }

    private int GenerateVertexIndexKey(Vector3 position, Vector3 normal)
    {
        return position.GetHashCode() ^ normal.GetHashCode();
    }

    private int GetMergedVertexIndex(Vector3 position, Vector3 normal)
    {
        int key = GenerateVertexIndexKey(position, normal);
        if (mergedVertices.TryGetValue(key, out List<int> existingIndices))
        {
            return existingIndices.First();
        }

        int newIndex = mergedVertices.Count;
        mergedVertices[key] = new List<int> { newIndex };
        return newIndex;
    }

    private Vector2 ApplyUVDisplacement(Vector2 uv)
    {
        return new Vector2(uv.x + uvDisplacementX, 1 - uvDisplacementY - uv.y);
    }

    private Color ReadColor(string line)
    {
        string[] values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return new Color(
            int.Parse(values[0]) / 255f,
            int.Parse(values[1]) / 255f,
            int.Parse(values[2]) / 255f,
            int.Parse(values[3]) / 255f
        );
    }

    private int[] ReadTriangle(string line)
    {
        return ReadIntArray(line);
    }

    private int[] ReadIntArray(string line)
    {
        string[] values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return Array.ConvertAll(values, int.Parse);
    }

    private float[] ReadFloatArray(string line)
    {
        string[] values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return Array.ConvertAll(values, ParseFloat);
    }

    private float ParseFloat(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }
        throw new FormatException($"Unable to parse float value: {value}");
    }
}