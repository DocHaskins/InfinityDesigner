using System.IO;
using UnityEngine;

public class ThumbnailGenerator : MonoBehaviour
{
    public string texturesPath = "Assets/Resources/Textures";
    public string materialsPath = "Assets/Resources/Materials";
    private string texturesThumbnailPath = "Assets/Resources/Thumbnails";
    private string materialsThumbnailPath = "Assets/Resources/Thumbnails";
    public bool skipExisting = true;
    public bool generateTextures = true;
    public bool generateMaterials = true;

    void Start()
    {
        GenerateAllThumbnails();
    }

    private void GenerateAllThumbnails()
    {
        if (generateTextures)
        {
            // Generate thumbnails for textures
            DirectoryInfo texturesDir = new DirectoryInfo(texturesPath);
            FileInfo[] textureFiles = texturesDir.GetFiles("*.png"); // Add other file formats as needed
            foreach (FileInfo fileInfo in textureFiles)
            {
                string thumbnailPath = Path.Combine(texturesThumbnailPath, fileInfo.Name.Replace(fileInfo.Extension, "") + "_tmb.png");
                if (skipExisting && File.Exists(thumbnailPath))
                    continue;

                Texture2D texture = LoadTextureFromFile(fileInfo.FullName);
                if (texture != null)
                {
                    GenerateThumbnail(texture, fileInfo.Name.Replace(fileInfo.Extension, ""), isTexture: true);
                }
            }
        }

        if (generateMaterials)
        {
            DirectoryInfo materialsDir = new DirectoryInfo(materialsPath);
            FileInfo[] materialFiles = materialsDir.GetFiles("*.mat");
            foreach (FileInfo fileInfo in materialFiles)
            {
                string thumbnailPath = Path.Combine(materialsThumbnailPath, fileInfo.Name.Replace(fileInfo.Extension, "") + "_tmb.png");
                if (skipExisting && File.Exists(thumbnailPath))
                    continue;

                Material material = Resources.Load<Material>(Path.Combine(materialsPath.Replace("Assets/Resources/", ""), fileInfo.Name.Replace(fileInfo.Extension, "")));
                if (material != null)
                {
                    Texture2D mainTexture = GetMainTextureFromMaterial(material);
                    if (mainTexture != null)
                    {
                        GenerateThumbnail(mainTexture, fileInfo.Name.Replace(fileInfo.Extension, ""), isTexture: false);
                    }
                }
            }
        }
    }

    private Texture2D LoadTextureFromFile(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(fileData))
            return tex;
        return null;
    }

    private void GenerateThumbnail(Texture2D sourceTexture, string originalName, bool isTexture)
    {
        RenderTexture rt = RenderTexture.GetTemporary(48, 48);
        RenderTexture.active = rt;
        Graphics.Blit(sourceTexture, rt);
        Texture2D thumbnail = new Texture2D(48, 48, TextureFormat.RGB24, false);
        thumbnail.ReadPixels(new Rect(0, 0, 48, 48), 0, 0);
        thumbnail.Apply();

        SaveThumbnail(thumbnail, originalName, isTexture);

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
    }

    private Texture2D GetMainTextureFromMaterial(Material material)
    {
        if (material.HasProperty("_MainTex"))
            return material.GetTexture("_MainTex") as Texture2D;
        if (material.HasProperty("_BaseColorMap"))
            return material.GetTexture("_BaseColorMap") as Texture2D;
        if (material.HasProperty("_dif"))
            return material.GetTexture("_dif") as Texture2D;

        // Add more properties as needed based on your materials
        return null;
    }

    private void SaveThumbnail(Texture2D thumbnail, string originalName, bool isTexture)
    {
        byte[] bytes = thumbnail.EncodeToPNG();
        string directory = isTexture ? texturesThumbnailPath : materialsThumbnailPath;
        Directory.CreateDirectory(directory); // Ensure the directory exists
        string path = Path.Combine(directory, originalName + "_tmb.png");
        File.WriteAllBytes(path, bytes);
    }
}