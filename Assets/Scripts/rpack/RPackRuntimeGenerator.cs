using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class RPackRuntimeGenerator
{
    public const int INVALID = 0;
    public const int MESH = 0x10;
    public const int SKIN = 0x12;
    public const int MODEL = 0x18;
    public const int TEXTURE = 0x20;
    public const int MATERIAL = 0x30;
    public const int ANIMATION = 0x40;
    public const int ANIMATION_ID = 0x41;
    public const int ANIMATION_SCR = 0x42;
    public const int ANIM_GRAPH_BANK = 0x47;
    public const int ANIM_CUSTOM_RESOURCE = 0x49;
    public const int FX = 0x50;
    public const int GPUFX = 0x51;
    public const int AREA = 0x5A;
    public const int PREFAB = 0x61;
    public const int SOUND = 0x65;
    public const int SOUND_MUSIC = 0x66;
    public const int SOUND_SPEECH = 0x67;
    public const int SOUND_STREAM = 0x68;
    public const int SOUND_LOCAL = 0x69;
    public const int TINY_OBJECTS = 0xF8;

    public class FilePart
    {
        public int Type { get; set; }
        public byte[] Version { get; set; } = new byte[] { 0x89, 0x02 };
        public List<byte[]> PartData { get; set; } = new List<byte[]>();
        public string FileName { get; set; }
        public int Offset { get; set; } = 0; // Adjust based on actual data
        public int Size { get; set; } = 0; // Adjust based on actual part size
    }

    public static void CreateRPack(string filePath, List<FilePart> fileParts, RPackParser template = null)
    {
        Debug.Log("Starting RPack generation...");

        // Use the template for predefined structure or create a new one based on fileParts
        var sectionTypes = fileParts.GroupBy(fp => fp.Type)
                                    .ToDictionary(g => g.Key, g => g.ToList());
        int totalSections = sectionTypes.Count;
        int totalParts = fileParts.Count;
        int totalFiles = fileParts.Select(fp => fp.FileName).Distinct().Count();

        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(fileStream, Encoding.ASCII))
        {
            Debug.Log("Writing RPack header...");
            // Header fields might be taken from the template if available
            writer.Write(Encoding.ASCII.GetBytes(template?.MagicID ?? "RP6L"));
            writer.Write(BitConverter.GetBytes(template?.Version ?? 4));
            writer.Write(BitConverter.GetBytes(template?.Flags ?? 0x1000));
            writer.Write(BitConverter.GetBytes(totalParts));
            writer.Write(BitConverter.GetBytes(totalSections));
            writer.Write(BitConverter.GetBytes(totalFiles));

            // Reserve space for file names sizes, will be updated later
            long fileNameSizeOffset = writer.BaseStream.Position;
            writer.Write(new byte[4]); // Placeholder

            // Sections could be templated or generated
            Debug.Log("Writing sections...");
            if (template != null)
            {
                // Use template to write sections
                foreach (var section in template.FileSections)
                {
                    writer.Write((byte)section.Type);
                    writer.Write(section.Flag);
                    writer.Write(section.Version);
                    // Write additional fields as required
                }
            }
            else
            {
                // Fallback to generic section writing based on fileParts
                foreach (var section in sectionTypes)
                {
                    writer.Write((byte)section.Key);
                    writer.Write(new byte[15]); // Placeholder for now, adjust as needed
                }
            }

            // Similar approach for parts
            Debug.Log("Writing parts...");
            foreach (var part in fileParts)
            {
                writer.Write((byte)part.Type);
                writer.Write(BitConverter.GetBytes(part.Offset));
                writer.Write(BitConverter.GetBytes(part.Size));
                writer.Write(new byte[6]); // Adjust as necessary
            }

            // File names handling remains similar
            Debug.Log("Writing file names...");
            //WriteFileNames(writer, fileParts);

            // Final steps for file names sizes and finalizing file
            writer.Seek((int)fileNameSizeOffset, SeekOrigin.Begin);
            writer.Write(BitConverter.GetBytes((int)(writer.BaseStream.Position - fileNameSizeOffset - 4)));

            Debug.Log($"RPack file successfully created: {filePath}");
        }
    }
}