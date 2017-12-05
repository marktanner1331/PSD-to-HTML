using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace psd_importer
{
    class Program
    { 
        private static ByteArray bytes;

        private static string signiture;
        private static ushort version;
        private static ushort numberOfChannels;
        private static uint height;
        private static uint width;
        private static ushort bitDepth;
        private static ushort colorMode;
        private static uint numberOfLayers;
        private static List<Layer> layers = new List<Layer>();
        private static List<Layer> topLevelLayers = new List<Layer>();

        static void Main(string[] args)
        {
            string fileName;

            //load the psd into memory
            if (args.Length == 1)
            {
                fileName = args[0];
            }
            else
            {
                fileName = Directory.GetCurrentDirectory() + "/somepsd.psd";
            }

            byte[] data = GetBytesFromFile(fileName);

            bytes = new ByteArray(data);

           // try
           // {
                parseHeader();
                parseColorModeData();
                parseImageResources();
                parseLayersAndMasks();

                //now we have the layers, we can build the layer tree
                buildLayerTree();

                foreach (Layer layer in topLevelLayers)
                {
                    printLayer(layer, "");
                }
                
                createHTML(Path.GetFileNameWithoutExtension(fileName));
                //createPercentHTML(Path.GetFileNameWithoutExtension(fileName));

                Console.Out.WriteLine("finished parsing psd");
         //   }
         //   catch (Exception e)
         //   {
              //  Console.Out.WriteLine(bytes.Position + ": " + e);
         //   }
        }

        //converts the bounds of the layers to percentages
        private static void convertToPercentages()
        {
            foreach (Layer layer in layers)
            {
                layer.top = (int)((float)layer.top * 100 / (float)height);
                layer.left = (int)((float)layer.left * 100 / (float)width);
                layer.bottom = (int)((float)layer.bottom * 100 / (float)height);
                layer.right = (int)((float)layer.right * 100 / (float)width);
            }
        }

        //outputs the psd as html in percentages
        private static void createPercentHTML(string filename)
        {
            Console.Out.WriteLine("creating percent html");

            convertToPercentages();

            string folderName = Directory.GetCurrentDirectory() + "/" + filename + "/";

            //create the folder based on the psd name
            Directory.CreateDirectory(folderName);

            //save the non text layers as jpegs
            foreach (Layer layer in layers)
            {
                if (layer.isTextLayer == false)
                {
                    if (layer.image != null)
                    {
                        string tempName = layer.name;

                        if (File.Exists(folderName + layer.name + ".jpg"))
                        {
                            ushort i = 1;

                            while (File.Exists(folderName + tempName + ".jpg"))
                            {
                                tempName = layer.name + "_" + i;
                                i++;
                            }
                        }

                        layer.name = tempName;

                        //Console.Out.WriteLine("creating jpg: " + layer.name + ".jpg");

                        layer.image.Save(folderName + layer.name + ".jpg");
                    }
                }
            }

            Console.Out.WriteLine("generating html file");

            //now build the html
            string html = "<html><body>";

            foreach (Layer layer in layers)
            {
                if (layer.isTextLayer == false)
                {
                    if (layer.image != null)
                    {
                        html += "<image src=\"./" + layer.name + ".jpg\" style=\"position:absolute;top:" +
                            layer.top + "%;left:" + layer.left + "%;width:" + (layer.right - layer.left) +
                            "%;height:" + (layer.bottom - layer.top) + "%;";

                        if (layer.visible == false)
                        {
                            html += "display:none;";
                        }
                        else if (layer.opacity != 255)
                        {
                            double opacity = layer.opacity;
                            opacity /= 255;
                            html += "opacity:" + opacity + ";";
                        }

                        html += "\" />";
                    }
                }
                else
                {
                    html += "<div style=\"position:absolute;overflow:hidden;font-family:'" + layer.fontName +
                        "';font-size:" + layer.fontSize + "px;top:" +
                            layer.top + "%;left:" + layer.left + "%;width:" + (layer.right - layer.left + 10) +
                            "%;height:" + (layer.bottom - layer.top + 10) + "%;\">" + layer.text + "</div>";
                }
            }

            html += "</body></html>";

            html = PrintXML(html);

            File.WriteAllText(folderName + "template.html", html);
        }

        //outputs the psd as html
        private static void createHTML(string filename)
        {
            Console.Out.WriteLine("creating html");

            string folderName = Directory.GetCurrentDirectory() + "/" + filename + "/";

            //create the folder based on the psd name
            Directory.CreateDirectory(folderName);

            //save the non text layers as jpegs
            foreach (Layer layer in layers)
            {
                if (layer.isTextLayer == false)
                {
                    if (layer.image != null)
                    {
                        string tempName = layer.name;

                        if (File.Exists(folderName + layer.name + ".jpg"))
                        {
                            ushort i = 1;

                            while (File.Exists(folderName + tempName + ".jpg"))
                            {
                                tempName = layer.name + "_" + i;
                                i++;
                            }
                        }

                        layer.name = tempName;

                        //Console.Out.WriteLine("creating jpg: " + layer.name + ".jpg");

                        layer.image.Save(folderName + layer.name + ".jpg");
                    }
                }
            }

            Console.Out.WriteLine("generating html file");

            //now build the html
            string html = "<html><body>";

            foreach (Layer layer in layers)
            {
                if (layer.isTextLayer == false)
                {
                    if (layer.image != null)
                    {
                        html += "<image src=\"./" + layer.name + ".jpg\" style=\"position:absolute;top:" +
                            layer.top + "px;left:" + layer.left + "px;width:" + (layer.right - layer.left) +
                            "px;height:" + (layer.bottom - layer.top) + "px;";

                        if (layer.visible == false)
                        {
                            html += "display:none;";
                        }
                        else if (layer.opacity != 255)
                        {
                            double opacity = layer.opacity;
                            opacity /= 255;
                            html += "opacity:" + opacity + ";";
                        }

                        html += "\" />";
                    }
                }
                else
                {
                    html += "<div style=\"position:absolute;overflow:hidden;font-family:'" + layer.fontName + 
                        "';font-size:" + layer.fontSize + "px;top:" +
                            layer.top + "px;left:" + layer.left + "px;width:" + (layer.right - layer.left + 10) +
                            "px;height:" + (layer.bottom - layer.top + 10) + "px;\">" + layer.text + "</div>"; 
                }
            }

            html += "</body></html>";

            html = PrintXML(html);

            File.WriteAllText(folderName + "output.html", html);
        }

        public static String PrintXML(String XML)
        {
            String Result = "";

            MemoryStream mStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode);
            XmlDocument document = new XmlDocument();

            try
            {
                // Load the XmlDocument with the XML.
                document.LoadXml(XML);

                writer.Formatting = Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new StreamReader(mStream);

                // Extract the text from the StreamReader.
                String FormattedXML = sReader.ReadToEnd();

                Result = FormattedXML;
            }
            catch (XmlException)
            {
                return XML;
            }

            mStream.Close();
            writer.Close();

            return Result;
        }

        //arranging the layers into a tree by filling in layer.parentLayer and layer.children
        private static void buildLayerTree()
        {
            Console.Out.WriteLine("building layer tree");

			//ifs its top level, we need to add it to the topLevelLayers vector rather than a layer.children
			Boolean isTopLevel = true;
			
			//as the folders are recursive. we need a reference the current folder we are filling in
			Layer currentFolder = null;
			
			//running through the array backwards due to how folders are ordered in a psd
			for(int i = layers.Count - 1;i > -1;i--)
			{
				//we already have all the layers in the 'layers' vector.
				Layer layer = layers[i];
				
				//if its a top level, add it to toplevelLayers, else add it to the current folder
				if(isTopLevel)
				{
					topLevelLayers.Add(layer);
				}
				else
				{
					currentFolder.children.Add(layer);
					//layers know about their parent, which breaks a few rules, but it never really
					//uses its parent
					layer.parentLayer = currentFolder;
				}
				
				//depending on the type of layer, we may need to update parentFolder and isTopLevel
				switch(layer.type)
				{
					case Layer.TYPE_NORMAL:
						//dont need to do anything
						break;
					case Layer.TYPE_FOLDER_CLOSED:
					case Layer.TYPE_FOLDER_OPEN:
					
						if(layer.left != 0)
						{
							throw new Exception("found layer with some offset");
						}

						//there is a new parent folder
						currentFolder = layer;
						isTopLevel = false;
						break;
					case Layer.TYPE_HIDDEN:
						if(layer.left != 0)
						{
							throw new Exception("found layer with some offset");
						}
						//because we have done everything backwards, we need to reverse the array to keep the depths right
                        if (currentFolder != null)
                        {
                            currentFolder.children.Reverse();
                        }
                        else
                        {
                            throw new Exception("adding layer to null folder");
                        }
						
						//the offset information for the folder is stored in the layer group
						//so move it to the folder
						//currentFolder.left = layer.left;
						//currentFolder.right = layer.right;
						//currentFolder.top = layer.top;
						//currentFolder.bottom = layer.bottom;
						
						//the current folder has closed, so we switch to its parent folder
						currentFolder = currentFolder.parentLayer;
						
						//we have hit the top level, (top level layers dont have parents)
						if(currentFolder == null)
						{
							isTopLevel = true;
						}
						break;
				}
			}
			
			//and finally reverse the top level layer, could have used unshift here. but i reckon this is more efficient
            topLevelLayers.Reverse();
        }

        //a simple function that traces the relevant information about a layer, and then recurs
        //to trace its children
        private static void printLayer(Layer layer, string indent)
		{
			Console.Out.WriteLine(indent + "layer name: " + layer.name + " textLayer: " + layer.isTextLayer);
			
			//and now cycle the through the children and trace them
			foreach(Layer childLayer in layer.children)
			{
				printLayer(childLayer, indent + "    ");
			}
		}

        //the header contains all the basic information for the psd, such as its size
        private static void parseHeader()
        {
            Console.Out.WriteLine("parsing header");

            signiture = bytes.getString(4);
            if (signiture != "8BPS")
            {
                throw new Exception("Incorrect PSD signiture: " + signiture);
            }
            traceProperty("signiture", signiture);

            //read the version
            version = bytes.getUI16();
            if (version != 1)
            {
                if (version == 2)
                {
                    throw new Exception("PSB is not supported.");
                }
                else
                {
                    throw new Exception("Unknown version: " + version);
                }
            }
            traceProperty("version", version);

            //there are 6 reserved bytes that dont do anything
            bytes.skipBytes(6);

            //the number of color channels
            numberOfChannels = bytes.getUI16();
            traceProperty("number of channels", numberOfChannels);

            //the width and height of the canvas in pixels
            height = bytes.getUI32();
            width = bytes.getUI32();
            traceProperty("width", width);
            traceProperty("height", height);

            //the number of bits per channel
            bitDepth = bytes.getUI16();
            traceProperty("bit depth", bitDepth);

            //the color mode, there is an enum for this called colorModeEnum
            colorMode = bytes.getUI16();
            if (ColorModeEnum.getName(colorMode) == null)
            {
                throw new Exception("Unknown color mode: " + colorMode);
            }
            traceProperty("color mode", ColorModeEnum.getName(colorMode));
        }

        //parses the color mode data, e.g. whether it is rgb or cymk, 
        //i can not parse indexed or duotone colors
        private static void parseColorModeData()
        {
            Console.Out.WriteLine("parsing color mode data");

            uint length = bytes.getUI32();

            switch (colorMode)
            {
                case ColorModeEnum.INDEXED:
                    throw new Exception("Indexed color mode not supported");
                case ColorModeEnum.DUOTONE:
                    throw new Exception("Duotone color mode not supported");
                default:
                    if (length != 0)
                    {
                        throw new Exception("Color mode data found for: " + ColorModeEnum.getName(colorMode));
                    }
                    break;
            }
        }

        //the image resources are a set of resource blocks, 
        private static void parseImageResources()
        {
            Console.Out.WriteLine("parsing image resources");

            //get the length of this section and the position of the next section
			uint length = bytes.getUI32();
			long nextSection = bytes.Position + length;

            //generate and parse the image resource blocks from the byte array
            while (true)
            {
                //bit of a weird way of doing while loops, but it means we can catch overflows
                if (bytes.Position == nextSection)
                {
                    break;
                }
                else if (bytes.Position > nextSection)
                {
                    throw new Exception("Image Resources section has overflowed");
                }
                else
                {
                    //check the signiture
			        String sig  = bytes.getString(4);
			        if(sig != "8BIM")
			        {
				        throw new Exception("Encountered Image Resource Block with signiture: " + sig);
			        }

                    //get the id
			        ushort imageResourceBlockID = bytes.getUI16();
                    //traceProperty("image resource block", imageResourceBlockID);

                    //get its name
                    string resourceName = bytes.getPascalString();

                    //and its length
                    uint resourceDataLength = bytes.getUI32();
                    //pad the data if its odd
                    if (resourceDataLength % 2 == 1)
                    {
                        resourceDataLength++;
                    }
                    
                    switch (imageResourceBlockID)
                    {
                        case 1036:
                            byte[] thumb = parseThumbnail(bytes.getBytes(resourceDataLength));
                            ByteArrayToFile(Directory.GetCurrentDirectory() + "/thumb.jpg", thumb);
                            Console.Out.WriteLine("writing thumbnail to file");
                            break;
                        default: //if the bytes aren't being read, they need to be skipped
                            bytes.skipBytes(resourceDataLength);
                            break;
                    }
                }
            }
        }

        //parses the layers and mask section, for efficiency, we only care about the layer info
        private static void parseLayersAndMasks()
		{
            Console.Out.WriteLine("parsing layers and masks");

            //get the length of this section and the position of the next section
			uint length = bytes.getUI32();
			uint nextSection = (uint)bytes.Position + length - 4;

            //parse the layer info
            parseLayerInfo();

            parseImageChannelData();

            bytes.Position = nextSection;
        }

        private static void parseImageChannelData()
        {
            Console.Out.WriteLine("parsing image channel data");

            uint channelDataOffset = (uint)bytes.Position;
			
            foreach(Layer layer in layers)
			{
				bytes.Position = channelDataOffset;
				
				//parse the data
				layer.parseImageChannelData(bytes);
				
				//attempt to reset byte stream position
				foreach(LayerChannelData channel in layer.channels)
				{
					channelDataOffset += channel.channelDataLength;
				}
			}
        }

        //this is where all the main layer information is parsed
        private static void parseLayerInfo()
        {
            Console.Out.WriteLine("parsing layer info");

            //get the length of the layers data
			uint lengthOfLayersData = bytes.getUI32();
			
            //and the number of layers
			short layerCount = bytes.getI16();
            traceProperty("number of layers", layerCount);

            //if the layers count is negative, it maens there is something special
            //about the alpha channel
            if (layerCount < 0)
            {
                layerCount *= -1;
                //throw new Exception("negative layers not implemented yet");
            }

            //numberOfLayers is a uint
            numberOfLayers = (uint)layerCount;

            //cycle through each layer and parse it
			for(uint i = 0;i < layerCount;i++)
			{
                //using the builder pattern here to avoid a massive constructor
				Layer layer = new Layer();

                //doing the parsing here rather than delegating it to the layer class for readability
                //think these are ints
                layer.top = bytes.getI32();
                layer.left = bytes.getI32();
                layer.bottom = bytes.getI32();
                layer.right = bytes.getI32();

                //although the channels are stored in a different section, the number
                //of channels is stored here
                layer.numberOfChannels = bytes.getUI16();
                layer.hasTransparency = layer.numberOfChannels > 3;

                //parse the channels info, (not actually the channels yet/0
				for(uint a = 0;a < layer.numberOfChannels;a++)
				{
					LayerChannelData channelData = new LayerChannelData();
					channelData.ID = bytes.getI16();
					channelData.channelDataLength = bytes.getUI32();
					layer.channels.Add(channelData);
				}

                layer.signiture = bytes.getString(4);

                if (layer.signiture != "8BIM")
                {
                    throw new Exception("Encountered layer with signiture: " + layer.signiture);
                }

                //options like the blendModeKey arent used, but still parsed
                bytes.skipBytes(4);

                //between 0-255
                layer.opacity = bytes.getByte();
				
                //the base
                bytes.skipBytes(1);

                //the flags are booleans which are stored as single bits in one byte
				byte flags = bytes.getByte();
				//checked in libpsd, its a bit cryptic, but looks like 0 is true, 1 is false
				layer.visible = (flags & 2) == 0;

                //padding
                bytes.skipBytes(1);

                //the extra data fields are tags found at the end of the layer
				//and are very similar to image resource blocks
				uint extraDataFieldLength = bytes.getUI32();
				
                //end of the whole layer, i think
				uint possibleEnd = (uint)bytes.Position + extraDataFieldLength;

                //parse the layer mask, dont think this is implemented yet
                parseLayerMask(layer);

                //parse the blending ranges, dont think this is implemented yet
                parseLayerBlendingRanges(layer);

                //read the name of the layer,  the name also exists as a unicode name
                //in an extra tag block
                layer.name = bytes.getPascalString(4);
				
                //cycle through the extra tags
                while (bytes.Position != possibleEnd)
                {
                    if (bytes.Position > possibleEnd)
                    {
                        throw new Exception("overflow when parsing extra layer tags");
                    }

                    String sig = bytes.getString(4);
					
					if(sig != "8BIM")
					{
						//really screwed up here
                        throw new Exception("unknown sig for layer tag: " + sig);
					}

                    //get the tag name and the tag length
					String tagName = bytes.getString(4);
					int tagLength = bytes.getI32();
					
                    //the tag length is padded to an even number
					if(tagLength % 2 == 1)
                    {
                        tagLength++;
                    }
                    
					uint tagEnd = (uint)(bytes.Position + tagLength);
					
					//i dont like giving 'bytes' to the layer, but im resetting the position afterwards
					//so it should be fine. 
					//parse the tag
					layer.parseAdditionalLayerInfo(tagName, tagLength, bytes);

                    //make sure that we are at the start of the next tag, 
					bytes.Position = tagEnd;
                }

                //make sure we are at the end of the section
				bytes.Position = possibleEnd;
				
				layers.Add(layer);
            }
        }

        private static void parseLayerBlendingRanges(Layer layer)
        {
            uint length = bytes.getUI32();
			
			//just get to the next section
			bytes.skipBytes(length);
        }

        private static void parseLayerMask(Layer layer)
        {
            //get the length of this section
			uint length = bytes.getUI32();
			
			//no layer mask, i think.
			if(length == 0)
			{
				return;
			}

            //create a new layer mask object
			LayerMask layerMask = new LayerMask();

            //fill in its size
            layerMask.top = bytes.getI32();
            layerMask.left = bytes.getI32();
            layerMask.bottom = bytes.getI32();
            layerMask.right = bytes.getI32();

            //get the default color, either white or black, 0 or 255
            bytes.skipBytes(1);

            //all the flags are stored as single bits of one byte
            bytes.skipBytes(1);

            //depending on the length of the layer, differnt things are expected
            if (length == 20)
            {
                //mask padding 
                bytes.skipBytes(2);
            }
            else
            {
                bytes.skipBytes(1); //real flags, same as flags, so why read them?

                //real user mask background
                bytes.skipBytes(1);
                
                bytes.skipBytes(16); //i think this is just the same rect as the earlier bounding rectangle

                //and finally give the layer its mask
                layer.mask = layerMask;
            }
        }

        private static byte[] parseThumbnail(ByteArray byteArray)
        {
            Console.Out.WriteLine("parsing thumbnail");

            uint format = byteArray.getUI32();

            uint width = byteArray.getUI32();
            uint height = byteArray.getUI32();

            uint paddedRowBytes = byteArray.getUI32();
            
            uint totalSize = byteArray.getUI32();
            
            uint compressedSize = byteArray.getUI32();
            
            ushort bitsPerPixel = byteArray.getUI16();
            
            ushort numberOfPlanes = byteArray.getUI16();

            byte[] jpegData = byteArray.getBytesAsArray((uint)(byteArray.Length - byteArray.Position));

            return jpegData;
        }

        private static void traceProperty(string name, string p)
        {
            Console.Out.WriteLine(name + ": " + p);
        }

        private static void traceProperty(string name, object p)
        {
            Console.Out.WriteLine(name + ": " + p);
        }

        private static byte[] GetBytesFromFile(string fullFilePath)
        {
            // this method is limited to 2^32 byte files (4.2 GB)

            FileStream fs = null;
            try
            {
                fs = File.OpenRead(fullFilePath);
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                return bytes;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }

        private static void ByteArrayToFile(string _FileName, byte[] _ByteArray)
        {
            try
            {
                // Open file for reading
                FileStream _FileStream = new FileStream(_FileName, FileMode.Create, FileAccess.Write);
                // Writes a block of bytes to this stream using data from
                // a byte array.
                _FileStream.Write(_ByteArray, 0, _ByteArray.Length);

                // close file stream
                _FileStream.Close();
            }
            catch (Exception _Exception)
            {
                // Error
                Console.WriteLine("Exception caught in process: {0}", _Exception.ToString());
            }
        }
    }
}
