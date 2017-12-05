using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psd_importer
{
    class ByteArray
    {
        private byte[] _bytes;
        private byte[] _reverseBytes;
        private MemoryStream _stream;

        public byte[] bytes
        {
            get
            {
                return _bytes;
            }
        }

        public MemoryStream stream
        {
            get
            {
                return _stream;
            }
        }

        public long Position
        {
            get
            {
                return stream.Position;
            }
            set
            {
                stream.Position = value;
            }
        }

        public uint Length
        {
            get
            {
                return (uint)bytes.Length;
            }
        }

        public ByteArray(byte[] data)
        {
            _bytes = data;

            _reverseBytes = new byte[_bytes.Length];
            Array.Copy(bytes, 0, _reverseBytes, 0, _bytes.Length);
            Array.Reverse(_reverseBytes, 0, bytes.Length);
            
            _stream = new MemoryStream(_bytes);
        }

        public string getKey()
        {
            //the key length stuff is confusing, if its length is 0, its length is 4, 
            uint keyLength = getUI32();
            if (keyLength == 0)
            {
                keyLength = 4;
            }

            if(keyLength > 16)
            {
                keyLength = 4;
            }

            return getString((int)keyLength);
        }

        public void skipBytes(uint value)
        {
            stream.Position += value;
        }

        public byte getByte()
        {
            byte b = bytes[Position];
            Position++;
            return b;
        }

        public int getI8()
        {
            byte b = bytes[Position];
            Position++;
            return (int)(sbyte)b;
        }
        
        public ushort getUI16()
        {
            ushort value = BitConverter.ToUInt16(_reverseBytes, (int)(bytes.Length - stream.Position - 2));

            //have to increment the stream position manually here because we are 
            //dealing directory with the byte array
            stream.Position += 2;

            return value;
        }

        public short getI16()
        {
            short value = BitConverter.ToInt16(_reverseBytes, (int)(bytes.Length - stream.Position - 2));

            //have to increment the stream position manually here because we are 
            //dealing directory with the byte array
            stream.Position += 2;

            return value;
        }

        public uint getUI24()
        {
            uint value = BitConverter.ToUInt32(bytes, (int)stream.Position);

            //have to increment the stream position manually here because we are 
            //dealing directory with the byte array
            stream.Position += 3;

            return value & 0xffffff;
        }

        public uint getUI32()
        {
            uint value = BitConverter.ToUInt32(_reverseBytes, (int)(bytes.Length - stream.Position - 4));

            //have to increment the stream position manually here because we are 
            //dealing directory with the byte array
            stream.Position += 4;

            return value;
        }

        public int getI32()
        {
            int value = BitConverter.ToInt32(_reverseBytes, (int)(bytes.Length - stream.Position - 4));

            //have to increment the stream position manually here because we are 
            //dealing directory with the byte array
            stream.Position += 4;

            return value;
        }

        public string getString(int length)
        {
            byte[] text = new byte[length];

            for (int i = 0; i < length; i++)
            {
                text[i] = (byte)stream.ReadByte();
            }

            Encoding iso = Encoding.GetEncoding("us-ascii");
            return iso.GetString(text);
        }

        public string getPascalString()
        {
            uint length = getByte();
			if(length == 0)
			{
				skipBytes(1);
				return "";
			}
			
			String s = getString((int)length);
			skipBytes(1 - (length % 2));
			
			return s;
        }

        public string getPascalString(uint padding)
        {
            uint length = getByte();
			if(length == 0)
			{
				skipBytes(padding - 1);
				return "";
			}
			
			String s = getString((int)length);
			skipBytes((padding - 1) - length % padding);
			
			return s;
        }

        public byte[] getBytesAsArray(uint count)
        {
            byte[] data = new byte[count];
            Array.Copy(bytes, stream.Position, data, 0, count);
            skipBytes(count);

            return data;
        }

        public ByteArray getBytes(uint count)
        {
            if (count == 0)
            {
                return new ByteArray(new byte[0]);
            }
            else
            {
                byte[] data = new byte[count];
                Array.Copy(bytes, stream.Position, data, 0, count);

                skipBytes(count);

                return new ByteArray(data);
            }
        }
        
        //takes run length encoded data and decodes it
        public static byte[] unRLE(ByteArray packed)
        {
            int i;
			int n;
			byte b;
            MemoryStream unpacked = new MemoryStream();
			int count;
			
			while (packed.Position < packed.Length) 
			{
				n = packed.getI8();
				
				if ( n >= 0 ) 
				{
					count = n + 1;
					for ( i = 0; i < count; ++i ) 
					{
						unpacked.WriteByte( packed.getByte() );
					}
				} 
				else 
				{
					b = packed.getByte();
					
					count = 1 - n;
					for ( i = 0; i < count; ++i ) 
					{
                        unpacked.WriteByte(b);
					}
				}
			}
			
			return unpacked.ToArray();
        }

        public string readComplexString(uint length, string encoding)
        {
            byte[] data = getBytesAsArray(length * 2);

            Encoding iso = Encoding.GetEncoding(encoding);
            return iso.GetString(data);
        }

        public string getUnicodeString()
        {
            uint length = getUI32();

            length--; //for the null bytes at the end

            byte[] data = getBytesAsArray(length * 2);

            skipBytes(2); //for the null bytes at the end

            Encoding iso = Encoding.GetEncoding("unicodeFFFE");
            return iso.GetString(data);
        }
    }
}
