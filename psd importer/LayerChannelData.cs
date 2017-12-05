using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace psd_importer
{
    class LayerChannelData
    {
		public const int RED = 0;
		public const int GREEN = 1;
		public const int BLUE = 2;
		public const int TRANSPARENCY_MASK = -1;
		public const int USER_SUPPLIED_LAYER_MASK = -2;
		public const int REAL_USER_SUPPLIED_LAYER_MASK = -3;
			
		public int ID = 0;
        public uint channelDataLength = 0;
		public ByteArray data;
    }
}
