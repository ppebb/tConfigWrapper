using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tConfigWrapper.DataTemplates {
	public struct TileInfo {
		public ushort? type;
		public byte? liquid;
		public ushort? wall;
		public ushort? sTileHeader;
		public byte? bTileHeader;
		public byte? bTileHeader2;
		public byte? bTileHeader3;
		public short? frameX;
		public short? frameY;
	}
}
