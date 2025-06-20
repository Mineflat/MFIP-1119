using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFIP_1119
{
    /// <summary>
    /// Описание одной записи из magic-файла.
    /// </summary>
    internal class MagicRecord
    {
        public int Offset { get; set; }
        public PatternType Type { get; set; }
        public byte[] Pattern { get; set; }
        public string Description { get; set; }
    }
}
