using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace DMG
{
    public class Tile
    {
        const int size = 150;
        const int quality = 75;

        Color[] palette = new Color[4] { Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), Color.FromArgb(0xFF, 0xC0, 0xC0, 0xC0), Color.FromArgb(0xFF, 0x60, 0x60, 0x60), Color.FromArgb(0xFF, 0x00, 0x00, 0x00) };
        public byte[,] renderTile { get; private set; }

        public Tile()
        {
            renderTile  = new byte[8, 8];
            /*
            var image = new Bitmap(16, 16);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    image.SetPixel(x, y, palette[2]);
                }
            }

            image.Save("../../../../gb_out.JPG");

            
            string inputPath = "../../../../gb.JPG";
            string outputPath = "../../../../gb_out.JPG";

            using var image = new Bitmap(System.Drawing.Image.FromFile(inputPath));
            int width, height;
            if (image.Width > image.Height)
            {
                width = size;
                height = Convert.ToInt32(image.Height * size / (double)image.Width);
            }
            else
            {
                width = Convert.ToInt32(image.Width * size / (double)image.Height);
                height = size;
            }

            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(image, 0, 0, width, height);

                
                using (var output = File.Open(outputPath, FileMode.Create))
                {
                    var qualityParamId = Encoder.Quality;
                    var encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(qualityParamId, quality);
                    var codec = ImageCodecInfo.GetImageDecoders()
                        .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                    resized.Save(output, codec, encoderParameters);
                }
                
            }
            */
        }


        public void Parse(byte[] vramTile, int offset)
        {
            // Gameboy tiles are 8x8 pixels wide and 2 bits per pixel. This means 2 bytes per row
            // The first bit of the first pixel of each row is stored in the msb of the vram byte 1
            // The second bit of the first pixel of each row is stored in the msb of the vram byte 2
            // This function converts the format to something we can actually draw. The renderTile data becomes an 8x8 array of palette indices which can be 0-3 for the 4 gb colours            
            int y = 0;
            for (int i = 0; i < 16; i += 2)
            {
                for (int x = 0; x < 8; x++)
                {
                    int bitIndex = 1 << (7 - x);

                    byte pixelValue = 0;
                    if ((vramTile[offset + i] & bitIndex) != 0) pixelValue += 1;
                    if ((vramTile[offset + i + 1] & bitIndex) != 0) pixelValue += 2;

                    renderTile[x, y] = pixelValue;
                 }

                y++;
            }


            Console.WriteLine();
        }


        public void DumptToImageFile(string fn)
        {
            var image = new Bitmap(8, 8);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    image.SetPixel(x, y, palette[renderTile[x, y]]);
                }
            }

            image.Save(fn);
        }
    }
}
