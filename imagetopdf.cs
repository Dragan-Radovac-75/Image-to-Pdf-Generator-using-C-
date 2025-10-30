using System.IO.Compression;
using System.Text;

namespace Application;

public partial class ImageToPdf
{
    static void Main()
    {
        List<Bitmap>? bitmaps = [new("1.jpeg"), new("1.tiff"), new("1.png"), new("1.bmp"), new("1.gif")];
        FileStream? pdf = File.Open("document.pdf", FileMode.Create);

        try
        {
            var pages = new List<string>();
            var positions = new List<long>();
            var position = 1;

            Write(pdf, "%PDF-1.4\n");

            bitmaps?.ForEach(bitmap =>
            {
                var image = position++;
                var content = position++;
                var page = position++;

                var rgb = new MemoryStream();
                Enumerable.Range(0, bitmap.Height).ToList().ForEach(height =>
                    Enumerable.Range(0, bitmap.Width).Select(width =>
                        bitmap.GetPixel(width, height)).ToList().ForEach(color =>
                            rgb.Write([color.R, color.G, color.B], 0, 3)));

                var flatedecode = new MemoryStream();
                flatedecode.Write([120, 156], 0, 2);

                var deflatestream = new DeflateStream(flatedecode, CompressionMode.Compress, true);
                var pixels = rgb.ToArray();

                deflatestream.Write(pixels, 0, pixels.Length);
                deflatestream.Close();

                uint a = 1, b = 0;
                pixels.ToList().ForEach(pixel => { a = a + pixel; b = b + a; });

                var endofdata = BitConverter.GetBytes((b << 16) | a);
                flatedecode.Write(endofdata, 0, endofdata.Length);

                positions.Add(pdf.Position);
                Write(pdf, $"{image} 0 obj << /Type /XObject /Subtype /Image /Width {bitmap.Width} /Height {bitmap.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length {flatedecode.Length} >>");

                Write(pdf, "\nstream\n");
                pdf.Write(flatedecode.ToArray(), 0, (int)flatedecode.Length);
                Write(pdf, "\nendstream\nendobj\n");

                string stream = $"q {bitmap.Width / 2} 0 0 {bitmap.Height / 2} 0 0 cm /Im{image} Do Q\n";
                positions.Add(pdf.Position);
                Write(pdf, $"{content} 0 obj << /Length {stream.Length} >>\nstream\n{stream}endstream\nendobj\n");

                positions.Add(pdf.Position);
                Write(pdf, $"{page} 0 obj << /Type /Page /Parent {position} 0 R /MediaBox [0 0 {bitmap.Width / 2} {bitmap.Height / 2}] /Resources << /XObject << /Im{image} {image} 0 R >> >> /Contents {content} 0 R >> endobj\n");
                pages.Add($"{page} 0 R");
            });

            var identifier = position++;
            positions.Add(pdf.Position);
            Write(pdf, $"{identifier} 0 obj << /Type /Pages /Kids [ {string.Join(" ", pages.ToArray())} ] /Count {pages.Count} >> endobj\n");

            var catalog = position++;
            positions.Add(pdf.Position);
            Write(pdf, $"{catalog} 0 obj << /Type /Catalog /Pages {identifier} 0 R >> endobj\n");

            var startxref = pdf.Position;
            Write(pdf, $"xref 0 {position}\n0000000000 65535 f \n");

            positions.ForEach(position => Write(pdf, $"{position.ToString("D10")} 00000 n \n"));
            Write(pdf, $"trailer << /Size {position} /Root {catalog} 0 R >>\nstartxref\n{startxref}\n%%EOF");
        }
        catch (Exception exception)
        {
            throw new Exception(exception.Message);
        }
        finally
        {
            pdf?.Close();
            pdf?.Dispose();
        }
    }

    public static void Write(FileStream pdf, string section)
    {
        var bytes = Encoding.ASCII.GetBytes(section);
        pdf.Write(bytes, 0, bytes.Length);
    }
}





