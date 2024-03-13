using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class RPackParser
{
    public string MagicID { get; set; }
    public int Version { get; set; }
    public int Flags { get; set; }
    public int TotalParts { get; set; }
    public int TotalSections { get; set; }
    public int TotalFiles { get; set; }
    public List<string> FileNames { get; set; } = new List<string>();
    public List<FileSection> FileSections { get; set; } = new List<FileSection>();
    public List<FilePart> FileParts { get; set; } = new List<FilePart>();

    public void ParseRPack(string filePath)
    {
        using (var reader = new BinaryReader(File.OpenRead(filePath)))
        {
            // Basic header data
            MagicID = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Version = reader.ReadInt32();
            Flags = reader.ReadInt32();
            TotalParts = reader.ReadInt32();
            TotalSections = reader.ReadInt32();
            TotalFiles = reader.ReadInt32();

            // Skipping placeholder bytes for file names sizes
            reader.BaseStream.Seek(4, SeekOrigin.Current);  // Skip file name size placeholder

            // Parsing sections
            FileSections = new List<FileSection>();
            for (int i = 0; i < TotalSections; i++)
            {
                var section = new FileSection
                {
                    Type = reader.ReadByte(),
                    Flag = reader.ReadByte(),
                    Version = reader.ReadInt16(),
                };
                FileSections.Add(section);
            }

            // Calculating the offset for file names; this will depend on your specific RPack structure
            long fileNameOffset = reader.BaseStream.Position + (TotalParts * 16); // This assumes each part entry is 16 bytes. Adjust if necessary.
            for (int i = 0; i < TotalParts; i++)
            {
                // Read each part's data based on your structure, this is a basic template
                var part = new FilePart
                {
                    Type = reader.ReadInt32(), // Adjust based on actual part structure
                    Version = reader.ReadBytes(2), // Assuming 2 bytes for version, adjust as needed
                    // Read more data as required by your structure
                };
                FileParts.Add(part);
            }

            // Move to the file name offset position
            reader.BaseStream.Seek(fileNameOffset, SeekOrigin.Begin);

            // Parse file names
            FileNames = new List<string>();
            for (int i = 0; i < TotalFiles; i++)
            {
                FileNames.Add(ReadNullTerminatedString(reader));
            }
        }
    }

    private string ReadNullTerminatedString(BinaryReader reader)
    {
        List<byte> byteList = new List<byte>();
        byte currentByte;
        while ((currentByte = reader.ReadByte()) != 0)
        {
            byteList.Add(currentByte);
        }
        return Encoding.UTF8.GetString(byteList.ToArray());
    }
}

public class FileSection
{
    public int Type { get; set; }
    public byte Flag { get; set; }
    public short Version { get; set; }
}

public class FilePart
{
    public int Type { get; set; }
    public byte[] Version { get; set; }
    public List<byte[]> PartData { get; set; }
    public string FileName { get; set; }
}