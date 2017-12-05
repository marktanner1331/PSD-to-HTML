using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace psd_importer
{
    class Layer
    {
        //the bounds of the layer
		public int left = 0;
		public int right = 0;
		public int top = 0;
		public int bottom = 0;

		//the number of color channels, including alpha channels
		public uint numberOfChannels = 1;

		//a vector containing the channels
		public List<LayerChannelData> channels = new List<LayerChannelData>();

		public string signiture = "8BIM";
        
		public uint opacity = 255; //0 - 255
			
		public Boolean hasTransparency = false;

		//deciding whether a layer is visible takes more than just checking this
		//boolean, for example this can be set to true, but for the parent folder 
		//it could be set to false, making all its children invisible
		public Boolean visible = true;
		public string name = "";
			
		//depending on whether its a text layer, we decide whether to render the layer
		//as an image, or extract its text
		public Boolean isTextLayer = false;

		public LayerMask mask;
		public string text = "";

		public uint type = TYPE_NORMAL; //corresponds to the TYPE_ constants below
        
		//the children and parentLayer variables lets you navigate the layers like a tree
		//however only layers of type TYPE_FOLDER_OPEN and TYPE_FOLDER_CLOSED will have
		//children, while only layers that aren't top level will have a parent layer
		public List<Layer> children = new List<Layer>();
		public Layer parentLayer;
		
		//these correspond to the layer.type
		public const uint TYPE_NORMAL = 0;
		public const uint TYPE_FOLDER_OPEN = 1;
		public const uint TYPE_FOLDER_CLOSED = 2;
		public const uint TYPE_HIDDEN = 3;
			
		//obviously dont use this for a full implementation
        public string fontName = "";
		public string fontSize = "1";
        public Bitmap image;

        //parse stuff at the end of the layer, this is where the text is stored
        public void parseAdditionalLayerInfo(string tagName, int tagLength, ByteArray bytes)
        {
            //get the start of the next tag, so that if we screw up parsing this one, we can still parse the 
            //one after it
			long endOfTag = bytes.Position + tagLength;
			
			uint version;
			uint descriptorVersion;

            //these are just like the image resource blocks in the image resources section
			//cant be bothered to parse most of these at the moment
            switch (tagName)
            {
                case "TySh":
                    //type tool for photoshop 6.0 and later
                    //this is the most important tag as it contains the text and font
                    //properties that we need to extract

                    //tell everyone that it is a text layer so that we dont accidently
                    //render it as an image
                    isTextLayer = true;
                    
                    //get the version, although we dont care about it
                    version = bytes.getUI16();

                    bytes.skipBytes(48); //transform, not parsing it at the moment, but i think we will
                    //need to in order to get the proper bounds of the layer with respect to parent layers
                    //and masks

                    //get the text version, although we dont care about it
					uint textVersion = bytes.getUI16();

                    //get the descriptor version, although we dont care about it
                    descriptorVersion = bytes.getUI32();

                    parseDescriptor(bytes, endOfTag);
                    break;
                case "lsct":
                    //layer divider info
                    type = bytes.getUI32();
                    break;
            }
        }

        private void parseDescriptor(ByteArray bytes, long endOfTag)
        {
            //not too fussed about the classID, but we dont have much of a choice but
			//to parse it
            String classIDName = bytes.getUnicodeString();
            
            //class id is stored as a key
            String classID = bytes.getKey();
            
            //the number of items this descriptor has, not sure if this is completely
			//accurate
			uint numItems = bytes.getUI32();

            //the amount of items is never 'numItems' so just read to the end of the tag
			//this loop is a little dangerous as there is no way to find out where
			//one item ends and the next one starts, all we can do is make sure we 
			//dont overflow the whole tag
			for(uint i = 0;i < numItems;i++)
			{
				if(parseItem(bytes, endOfTag, true) == false)
				{
					return;
				}
			}
        }

        //parses an individual tag in the data, returns false if it finds something it doesnt understand
        private bool parseItem(ByteArray bytes, long endOfTag, Boolean hasKey)
        {
            //the key length stuff is confusing, if its length is 0, its length is 4,
			//and the key corresponds to a known type
			String key = "";
			if(hasKey)
			{
                key = bytes.getKey();
			}

            //the os type key is the type of data to follow
			String OSTypeKey = bytes.getString(4);

            switch (OSTypeKey)
            {
                case "TEXT": //straight forward enough
					this.text = bytes.getUnicodeString();
					break;
				case "enum":
                    String enumKey = bytes.getKey();

                    //the value is stored like a key
                    String enumValue = bytes.getKey();
					break;
				case "long":
					int value = bytes.getI32();
					break;
				case "ObAr":
                    //throw new Exception("i cant handle object arrays");
					return false;
				case "doub":
                    //throw new Exception("i cant handle doubles");
					break;
				case "tdta": //this is raw data, could be fun
					if(key == "EngineData")
					{
						parseEngineDataStructure(bytes, endOfTag);
					}
					break;
				case "UntF":
				    //throw new Exception("i cant handle unit floats");
					break;
                case "bool":
                    //bools are stored as a single byte,
                    bytes.skipBytes(1);
                    break;
				case "Objc": //found a descriptor within a descriptor
                    parseDescriptor(bytes, endOfTag);
					break;
				case "VlLs": //this is a list
					uint listLength = bytes.getUI32();
					
					for(uint i = 0;i < listLength;i++)
					{
                        //parse item returns false if it finds something it doesnt understand
						if(parseItem(bytes, endOfTag, false) == false)
						{
							return false;
						}
					}
					break;
				default:
					return false;
			}
			
			return true;
        }

        //the text engine data format is complicated, although godden told me its the 
		//same as how pdf's store it
		private void parseEngineDataStructure(ByteArray bytes, long endOfTag)
		{
			byte b;
			
			//find the start of the data by looking for the first <
			while((b = bytes.getByte()) != 0x3c && bytes.Position < endOfTag)
			{
				
			}
			
			//make sure we actually found one
			if(bytes.Position == endOfTag)
			{
				throw new Exception("could not find start of text engine data");
			}
			
			long startPos = bytes.Position;
            
			//we are at the start now, the end of the data will be 0x3e 0x3e 0x00
			byte firstByte = bytes.getByte();
			byte secondByte = bytes.getByte();
			byte thirdByte = bytes.getByte();
			
            //move to the start of the structure
			while(bytes.Position < endOfTag)
			{
                // 0x3e is a >
				if(firstByte == 0x3e && secondByte == 0x3e && thirdByte == 0x00)
				{
					break;
				}
				
				firstByte = secondByte;
				secondByte = thirdByte;
				thirdByte = bytes.getByte();
			}
			
			if(bytes.Position == endOfTag)
			{
				throw new Exception("could not find end of text engine data");
			}
			
			long endPos = bytes.Position;
			
			//we can use the startPos and endPos to get the data in the middle
			bytes.Position = startPos;
			ByteArray engineData = bytes.getBytes((uint)(endPos - startPos));
			
            //engineData now contains the engineData, this code works

			TextEngineNode dataObject = parseEngineData(engineData, 0);
			
            //now that we have parsed all of the engineData and stored it in 'dataObject', we retreive the
            //nodes that we need

			TextEngineNode tempNode;
			
			tempNode = dataObject.getNestedNodeByKey(new string[]{"ResourceDict", "FontSet", "Name"});
			if(tempNode != null)
			{
				fontName = tempNode.value;
			}

            tempNode = dataObject.getNestedNodeByKey(new string[] { "EngineDict", "StyleRun", "RunArray", 
                                                                    "StyleSheet", "StyleSheetData", "FontSize"});
			if(tempNode != null)
			{
				fontSize = tempNode.value;
			}

            tempNode = dataObject.getNestedNodeByKey(new string[] { "EngineDict", "Editor", "Text"});
			if(tempNode != null)
			{
				text = tempNode.value;
			}
		}

        private TextEngineNode parseEngineData(ByteArray bytes, uint depth)
		{
			//set up the root node that is the top of the tree
			TextEngineNode rootNode = new TextEngineNode();
			rootNode.type = TextEngineNode.TYPE_STRUCTURE;
			rootNode.key = "root";

            long tempPos = bytes.Position;

			while(bytes.Position < bytes.Length)
			{
				//find a key
				String key = findKeyOrEnd(bytes); //returns null if end of structure is found
				if(key == null)
				{
					return rootNode;
				}
                
                //if we have reached this point we have found a key of the form "/keyname "
                //what follows is either a value or a structure, the following code works out which it is

				Boolean isStructure = false;
				
				byte b = bytes.getByte();

				//depending on the value of the next byte, we know what is going to follow
				switch(b)
				{
					case 0x20: //if there is a space, then a value follows
						//little hack to get structures inside of arrays working
						if(bytes.bytes[bytes.Position] == 0x5b && bytes.bytes[bytes.Position + 1] == 0x0a)
						{
							Boolean stopBracketLoop = false;
							while(! stopBracketLoop)
							{
								TextEngineNode node2 = parseEngineData(bytes, depth + 1);
								node2.key = key;
								rootNode.structure.Add(node2);

                                if (bytes.Position == bytes.Length)
                                {
                                    return rootNode; //massive hack just to get it working, 
                                    //basically it just ends if we run out of data
                                }

								while(bytes.Position < bytes.Length)
								{
									b = bytes.getByte();
									if(b != 0x09 && b != 0x3e && b != 0x0a) //its a tab, so we can skip
									{
										if(b == 0x5d)
										{
											stopBracketLoop = true;
										}
										else if(b == 0x3c || b == 0x2f)
										{
											//we have a new structure or property
										}
										else
										{
                                            if (bytes.Position == bytes.Length)
                                            {
                                                return rootNode; //massive hack just to get it working, 
                                                //basically it just ends if we run out of data
                                            }
                                            Console.Out.WriteLine("unknown byte in middle of array at: " + bytes.Position);
										}
										
										break;
									}
								}
								
							}
							
							continue;
						}
						else
						{
							isStructure = false;
						}
						
						break;
					case 0x0a: //if its a new line, then we have found a structure
						isStructure = true;
						break;
					default:
                        Console.Out.WriteLine("unknown value after key: 0x" + b + ", at: " + bytes.Position);
						break;
				}
				
				//now we can build the node
				TextEngineNode node;
				if(isStructure)
				{
					//if its a structure, we use recursion to build the node
					node = parseEngineData(bytes, depth + 1);
					node.key = key;
				}
				else
				{
					//if its a value, we create a new node and fill in the value manually
					node = new TextEngineNode();
					node.key = key;
					node.value = findValue(bytes);
					node.type = TextEngineNode.TYPE_VALUE;
				}
				
				rootNode.structure.Add(node);
			}
			
			return rootNode;
		}

        private String findValue(ByteArray bytes)
		{
			String value = "";
			
			byte b;
			long startPos;
			long endPos;
			
			b = bytes.getByte();
			
			switch(b)
			{
				case 0x28: //if the first byte is a bracket "(" then the value is a string
					//the bytes here are encoded with unicodeFFFE
					//needs to be tested / modified for text that contains brackets
					
					//need to skip 2 bytes as the first 2 bytes in a bracket are weird and i 
					//dont understand them
					startPos = bytes.Position + 2;
					
					while(bytes.Position < bytes.Length)
					{
						b = bytes.getByte();
						if(b == 0x29)
						{
							break;
						}
					}
					
					endPos = bytes.Position;
			
					bytes.Position = startPos;
					
					value = bytes.readComplexString((uint)(endPos - startPos - 1) / 2, "unicodeFFFE");
					break;
				case 0x5b: //if the first byte is a square bracket "[" then the value is an array
					b = bytes.getByte();
					
					if(b != 0x20) //we have a structure inside an array
					{
						//trace("found structure inside array");
						//currentDataObject.structure.push(parseEngineData(bytes));
						throw new Exception("cant parse structures inside arrays");
					}
					else
					{
						//first we need to find where the array ends by looking for an end bracket "]"
						startPos = bytes.Position;
						
						while(bytes.Position < bytes.Length)
						{
							b = bytes.getByte();
							if(b == 0x5d)
							{
								break;
							}
						}
						
						endPos = bytes.Position;
						bytes.Position = startPos;
						value = bytes.getString((int)(endPos - startPos - 1));
						
						//need to skip a byte to get over the end bracket
						bytes.skipBytes(1);
					}
					break;
				case 0x74: //represents the letter 't', check to see if this means 'true'
					bytes.Position -= 1;
					
					value = bytes.getString(4);
					if(value != "true")
					{
						Console.Out.WriteLine("unknown value starting with t: " + value);
					}
					
					break;
				case 0x66: //represents the lettre 'f', check to see if this means 'false'
					bytes.Position -= 1;
					
					value = bytes.getString(5);
					if(value != "false")
					{
						Console.Out.WriteLine("unknown value starting with f: " + value);
					}
					
					break;
				//look for numbers
				case 46: //for numbers that start with a .
				case 45: //minus numbers
				case 48: //0
				case 49:
				case 50:
				case 51:
				case 52:
				case 53:
				case 54:
				case 55:
				case 56:
				case 57: //9
					startPos = bytes.Position + -1;
					
					while(bytes.Position < bytes.Length)
					{
						b = bytes.getByte();
						if(b == 0x0a)
						{
							break;
						}
					}
					
					endPos = bytes.Position;
			
					bytes.Position = startPos;
					value = bytes.getString((int)(endPos - startPos - 1));
					break;
				default:
					Console.Out.WriteLine("unknown value type: " + b + ", at: " + bytes.Position);
					break;
			}
			
			return value;
		}

        //looks for the next key, if it cant find one it returns null
        private String findKeyOrEnd(ByteArray bytes)
		{
			byte b;
			while(bytes.Position < bytes.Length)
			{
				b = bytes.getByte();
				if(b == 0x2f) //looks for a forward slash
				{
					break;
				}
				else if(b == 0x3e && bytes.bytes[bytes.Position] == 0x3e)
				{
                    //if we have hit here it means we have found the end of a structure
					return null;
				}
			}
			
            //if we have hit here we have found a key at bytes.position
			long startPos = bytes.Position;
			
            //look for the end of a key, denoted by a space, or new line
			while(bytes.Position < bytes.Length)
			{
				b = bytes.getByte();
				if(b == 0x20 || b == 0x0a)
				{
					break;
				}
			}
			
			long endPos = bytes.Position;
			
            //using the start and end positions of the key, we can get the length, and therefore the data
			bytes.Position = startPos;
			String key = bytes.getString((int)(endPos - startPos - 1));
			
			return key;
		}

        internal void parseImageChannelData(ByteArray bytes)
        {
            Console.Out.WriteLine("layer: " + name);

            //dont want to render folders or invalid bounds
            if (type != Layer.TYPE_NORMAL || bottom <= top || right <= left)
            {
                return;
            }
            
            image = new Bitmap(right - left, bottom - top, PixelFormat.Format32bppArgb);

            //cycle through the layers, parsing the bytes, and decoding if necessary
			foreach(LayerChannelData channel in channels)
			{
				//get the next channel pos, so if something goes wrong parsing
				//this channel, it wont screw up the next one
				uint nextChannelPos = (uint)(bytes.Position + channel.channelDataLength);

                if (channel.ID < -1)
                {
                    //skipping user supplied masks
                    bytes.Position = nextChannelPos;
                    continue;
                }
				
                //lets hope bytes.position is in the right place
				uint compressionMethod = bytes.getUI16();

                //if it needs decompressing
				switch(compressionMethod)
				{
					case 0:
						//not compressed
						//get the data and store it in the individual channel objects
						//-2 is for the 2 bytes taken up by the compression type
						channel.data = bytes.getBytes((uint)(image.Width * image.Height));
						channel.data.Position = 0;
						break;
					case 1:
						//rle
						channel.data = new ByteArray(new byte[0]);
						
						//each scan line is compressed seperately, so we need to get the 
						//length of each encoded line
						List<uint> lineLengths = new List<uint>(image.Height);
						
						//then we can cycle though each line and decode it
						for (int i = 0;i < image.Height;++i) 
						{
							lineLengths.Add(bytes.getUI16());
						}
						
						//now that the lines have been decoded, we can write them back to 
						//the channel
                        MemoryStream data = new MemoryStream();
						for (int i = 0;i < image.Height;++i) 
						{
                            ByteArray line = bytes.getBytes(lineLengths[i]);
                            byte[] unrle = ByteArray.unRLE(line);
                            data.Write(unrle, 0, unrle.Length);
						}

                        channel.data = new ByteArray(data.ToArray());

						break;
					case 2:
					case 3:
						//cant handle these compression methods, 
						//going to skip for now
						return;
				}
				
				//just in case the rle doesnt reset the position properly
				channel.data.Position = 0;
				
				bytes.Position = nextChannelPos;
            }

            //will be null if it doesnt exist
			LayerChannelData alphaChannel = getChannel(LayerChannelData.TRANSPARENCY_MASK);
			
			LayerChannelData redChannel = getChannel(LayerChannelData.RED);
			LayerChannelData greenChannel = getChannel(LayerChannelData.GREEN);
			LayerChannelData blueChannel = getChannel(LayerChannelData.BLUE);
			
            for(uint j = 0;j < image.Height;j++)
			{
				for(uint k = 0;k < image.Width;k++)
				{					
					//if there is alpha we have to use setPixel32 as there are 4 bytes 
					//to worry about
					if (alphaChannel != null)
					{
                        image.SetPixel((int)k, (int)j, Color.FromArgb(alphaChannel.data.getByte(),
                                                            redChannel.data.getByte(),
                                                            greenChannel.data.getByte(),
                                                            blueChannel.data.getByte()));
					}
					else
					{
                        image.SetPixel((int)k, (int)j, Color.FromArgb(redChannel.data.getByte(),
                                                            greenChannel.data.getByte(),
                                                            blueChannel.data.getByte()));
					}
				}
			}

            //now sort out out the mask, cant do masks very well
			if(mask != null && mask.right > mask.left && mask.top < mask.bottom)
			{
				//this represents the bounds of the image
				Rectangle realRect = new Rectangle(left, top, right - left, bottom - top);
				
				//this represents the bounds of the mask
				Rectangle maskRect = new Rectangle(mask.left, mask.top, mask.right - mask.left, mask.bottom - mask.top);
				
				//we combine them to get the new bounds
				Rectangle insideRect = new Rectangle();
				insideRect.X = Math.Max(realRect.X, maskRect.X);
				insideRect.Y = Math.Max(realRect.Y, maskRect.Y);
				insideRect.Width =  Math.Min(realRect.Right, maskRect.Right) - insideRect.X;
				insideRect.Height =  Math.Min(realRect.Bottom, maskRect.Bottom) - insideRect.Y;

                image = image.Clone(insideRect, PixelFormat.Format32bppArgb);
                left = insideRect.X;
                right = insideRect.Right;
                top = insideRect.Y;
                bottom = insideRect.Bottom;
			}
        }

        //given an id, corresponding to LayerChannelData.RED etc,
		//it will return the layer channel data object if it exists
		public LayerChannelData getChannel(int channelID)
		{
            return channels.FirstOrDefault(channel => channel.ID == channelID);
		}
    }
}
