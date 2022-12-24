namespace BARSReaderGUI
{
    public partial class Form1 : Form
    {
        List<AudioAsset> audioAssets = new List<AudioAsset>();
        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stream fileStream;
            OpenFileDialog fileDialog = new OpenFileDialog();

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                audioAssets.Clear();
                BwavListBox.Items.Clear();
                string inputFile = fileDialog.FileName;

                // Check for compression first.
                using (NativeReader reader = new(new FileStream(inputFile, FileMode.Open)))
                {
                    if (reader.ReadUInt() == 0xFD2FB528)
                    {
                        ZstdUtils zstdUtils = new ZstdUtils();
                        reader.Position -= 4;
                        fileStream = zstdUtils.Decompress(reader.BaseStream); // Stores decompressed data into stream
                    }
                    else
                    {
                        reader.Position -= 4;
                        fileStream = new MemoryStream(reader.BaseStream.ToArray()); // If no compression is found, we store the original file data in the stream.
                    }
                }

                // Read the file stored in the stream.
                using (NativeReader reader = new NativeReader(fileStream))
                {
                    //KeyValuePair<uint, AssetOffsetPair>[] assets;

                    string magic = reader.ReadSizedString(4);
                    if (magic != "BARS")
                    {
                        MessageBox.Show("Not a BARS file.");
                        return;
                    }

                    uint size = reader.ReadUInt();

                    ushort endian = reader.ReadUShort();
                    if (endian != 0xFEFF)
                    {
                        MessageBox.Show("Unsupported endian!");
                        return;
                    }

                    ushort version = reader.ReadUShort();
                    if (version != 0x0102)
                    {
                        MessageBox.Show("BARS V1.1 Is unsupported at this time."); //we don't support anything but v102 atm
                        return;
                    }

                    uint assetcount = reader.ReadUInt();
                    //assets = new KeyValuePair<uint, AssetOffsetPair>[assetcount];

                    // Create audioAssets and tie crcHashes to them.
                    for (int i = 0; i < assetcount; i++)
                    {
                        audioAssets.Add(new AudioAsset());
                        audioAssets[i].crcHash = reader.ReadUInt();
                    }

                    // Pair ATMA/BWAV offsets with asset
                    for (int i = 0; i < assetcount; i++)
                    {
                        audioAssets[i].amtaOffset = reader.ReadUInt();
                        audioAssets[i].bwavOffset = reader.ReadUInt();
                    }

                    // Get names for audioAssets
                    for (int i = 0; i < assetcount; i++)
                    {
                        reader.Position = audioAssets[i].amtaOffset + 0x24;
                        uint unkOffset = reader.ReadUInt();
                        reader.Position = audioAssets[i].amtaOffset + unkOffset + 36;
                        audioAssets[i].assetName = reader.ReadNullTerminatedString();
                        BwavListBox.Items.Add(audioAssets[i].assetName);
                    }

                    // Read AMTA data
                    for (int i = 0; i < assetcount; i++)
                    {
                        audioAssets[i].amtaData = new AMTA();
                        reader.Position = audioAssets[i].amtaOffset + 8;
                        uint amtaSize = reader.ReadUInt();
                        audioAssets[i].amtaData.data = reader.ReadBytes(Convert.ToInt32(amtaSize));
                    }

                    this.Text = $"BARSReaderGUI - {fileDialog.SafeFileName} - {assetcount} Assets";
                    MessageBox.Show("Successfully read " + assetcount + " assets.");
                }
            }
        }

        public void ReadAMTA(uint startPosition, NativeReader reader)
        {

            //reader.Position = startPosition;
            //string magic = reader.ReadSizedString(4);
            //ushort endian = reader.ReadUShort();
            //ushort version = reader.ReadUShort();
            //uint size = reader.ReadUInt();
            //uint unk1 = reader.ReadUInt();
            //uint unk2 = reader.ReadUInt();
            //uint unk3 = reader.ReadUInt();
            //uint unk4 = reader.ReadUInt();
            //uint unk5 = reader.ReadUInt();
            //uint unk6 = reader.ReadUInt();

            //string fileName;

            //if (magic != "AMTA")
            //    return "";

            //if (endian != 0xFEFF)
            //    return "";

            //reader.Position += 0x1C;

            //uint nameOffset1 = reader.ReadUInt();
            //reader.Position = startPosition + nameOffset1 + 36;
            //fileName = reader.ReadNullTerminatedString();

            //reader.Position = startPosition;

            //return fileName;
        }

        private void BwavListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = BwavListBox.SelectedIndex;
            AudioAssetNameLabel.Text = audioAssets[index].assetName;
            AudioAssetCrc32HashLabel.Text = audioAssets[index].crcHash.ToString("X");
            AudioAssetAmtaOffsetLabel.Text = audioAssets[index].amtaOffset.ToString("X");
            AudioAssetBwavOffsetLabel.Text = audioAssets[index].bwavOffset.ToString("X");
        }
    }
    public class AssetOffsetPair
    {
        public uint amtaoffset;
        public uint bwavoffset;
    }
}