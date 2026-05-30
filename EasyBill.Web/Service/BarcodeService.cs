using System.Drawing.Imaging;
using ZXing; 
using System.Drawing; 
using System.IO;
using SkiaSharp;
using ZXing.SkiaSharp;
namespace EasyBill.UI.Service
{
    public class BarcodeService
    {
        public byte[] GenerateBarcode(string text)
        {
            var writer = new BarcodeWriter
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 100,
                    Width = 300,
                    Margin = 5
                }
            };

            using var image = writer.Write(text);
            using var ms = new MemoryStream();
            image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(ms);
            return ms.ToArray();
        }
    }
}
