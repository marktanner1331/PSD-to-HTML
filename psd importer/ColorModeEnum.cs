using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psd_importer
{
    class ColorModeEnum
    {
        public const ushort BITMAP = 0;
        public const ushort GRAYSCALE = 1;
        public const ushort INDEXED = 2;
        public const ushort RGB = 3;
        public const ushort CYMK = 4;
        public const ushort MULTI_CHANNEL = 5;
        public const ushort DUOTONE = 6;
        public const ushort LAB = 7;

        public static string getName(ushort value)
		{
			switch(value)
			{
				case BITMAP:
					return "Bitmap";
				case GRAYSCALE:
					return "Grayscale";
				case INDEXED:
					return "Indexed";
				case RGB:
					return "RGB";
				case CYMK:
					return "CYMK";
				case MULTI_CHANNEL:
					return "Multi Channel";
				case DUOTONE:
					return "Duotone";
				case LAB:
					return "Lab";
				default:
					return null;
			}
		}
    }
}
