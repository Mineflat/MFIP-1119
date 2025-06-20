using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace MFIP_1119
{
    /// <summary>
    /// Детектор типа файла по magic-записям.
    /// </summary>
    public class Detector
    {
        private readonly List<MagicRecord> _records = new List<MagicRecord>();

        public Detector(string magicFilePath)
        {
            foreach (var line in File.ReadAllLines(magicFilePath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                var parts = trimmed.Split(new[] { '\t', ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4)
                    continue;

                int offset = int.Parse(parts[0]);
                PatternType type = parts[1].Equals("hex", StringComparison.OrdinalIgnoreCase)
                    ? PatternType.Hex
                    : PatternType.String;

                string rawPattern = parts[2];
                string description = parts[3];

                byte[] patternBytes = type == PatternType.Hex
                    ? Enumerable.Range(0, rawPattern.Length / 2)
                                .Select(i => Convert.ToByte(rawPattern.Substring(i * 2, 2), 16))
                                .ToArray()
                    : Encoding.ASCII.GetBytes(rawPattern);

                _records.Add(new MagicRecord
                {
                    Offset = offset,
                    Type = type,
                    Pattern = patternBytes,
                    Description = description
                });
            }
        }

        public string Detect(string filePath)
        {
            int maxRead = _records.Max(r => r.Offset + r.Pattern.Length);
            byte[] buffer;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int toRead = (int)Math.Min(maxRead, fs.Length);
                buffer = new byte[toRead];
                fs.Read(buffer, 0, toRead);
            }

            foreach (var record in _records)
            {
                if (buffer.Length < record.Offset + record.Pattern.Length)
                    continue;

                var segment = new Span<byte>(buffer, record.Offset, record.Pattern.Length);
                if (segment.SequenceEqual(record.Pattern))
                    return record.Description;
            }

            return "Unknown file type";
        }
    }
}
