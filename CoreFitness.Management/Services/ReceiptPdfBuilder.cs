using System.Globalization;
using System.Text;
using CoreFitness.Management.Models;

namespace CoreFitness.Management.Services;

public static class ReceiptPdfBuilder
{
    private const int PageWidth = 460;
    private const int PageHeight = 545;

    public static byte[] Build(Sale sale, string? logoPath = null)
    {
        var logo = TryReadPngImage(logoPath);
        var stream = BuildContentStream(sale, logo is not null);
        var streamLength = Encoding.ASCII.GetByteCount(stream);
        var logoObjectNumber = logo is null ? 0 : 6;
        var contentObjectNumber = logo is null ? 6 : 7;
        var imageResource = logo is null ? string.Empty : $" /XObject << /Logo {logoObjectNumber} 0 R >>";
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] /Resources << /Font << /F1 4 0 R /F2 5 0 R >>{imageResource} >> /Contents {contentObjectNumber} 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
        };

        if (logo is not null)
        {
            objects.Add(BuildImageObject(logo));
        }

        objects.Add($"<< /Length {streamLength} >>\nstream\n{stream}\nendstream");

        var builder = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };

        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(index + 1).Append(" 0 obj\n")
                .Append(objects[index]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append('\n');
        builder.Append("0000000000 65535 f \n");

        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset)
            .Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string BuildContentStream(Sale sale, bool hasLogo)
    {
        var id = sale.Id.ToString()[..8].ToUpperInvariant();
        var builder = new StringBuilder();

        builder.AppendLine($"0.169 0.169 0.169 rg 0 0 {PageWidth} {PageHeight} re f");
        builder.AppendLine("0.227 0.227 0.216 RG 1 w 0.5 0.5 459 544 re S");

        if (hasLogo)
        {
            builder.AppendLine("q 74 0 0 56 28 455 cm /Logo Do Q");
        }
        else
        {
            builder.AppendLine("0.141 0.141 0.141 rg 28 455 74 56 re f");
            builder.AppendLine("0.608 0.824 0.165 RG 1 w 28 455 74 56 re S");
        }

        Text(builder, "CORE FITNESS", 116, 482, 29, "F2", "0.957 0.945 0.91");
        Text(builder, $"{sale.Outlet.ToUpperInvariant()} RECEIPT", 116, 456, 14, "F2", "0.608 0.824 0.165");
        Line(builder, 28, 430, 432, 430, "0.227 0.227 0.216", 1);

        Text(builder, "RECEIPT", 28, 396, 13, "F2", "0.608 0.824 0.165");
        Text(builder, id, 432, 396, 13, "F2", "0.957 0.945 0.91", alignRight: true);

        Row(builder, "Date", sale.SoldAt.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture), 342);
        Row(builder, "Item", sale.ProductName, 308);
        Row(builder, "Quantity", sale.Quantity.ToString(CultureInfo.InvariantCulture), 274);
        Row(builder, "Unit Price", sale.UnitPrice.ToString("N0", CultureInfo.InvariantCulture), 240);
        Row(builder, "Payment", sale.PaymentMethod, 206);
        Row(builder, "Sold By", sale.SoldBy, 172);

        Text(builder, "TOTAL", 28, 116, 13, "F2", "0.608 0.824 0.165");
        Text(builder, sale.Total.ToString("N0", CultureInfo.InvariantCulture), 432, 100, 30, "F2", "0.608 0.824 0.165", alignRight: true);

        Line(builder, 28, 78, 432, 78, "0.227 0.227 0.216", 1);
        Text(builder, "Thank you for choosing CORE FITNESS.", 28, 50, 12, "F1", "0.957 0.945 0.91");

        return builder.ToString();
    }

    private static void Row(StringBuilder builder, string label, string value, int y)
    {
        Text(builder, label, 28, y, 12, "F2", "0.749 0.718 0.655");
        Text(builder, TrimForReceipt(value), 432, y, 12, "F1", "0.957 0.945 0.91", alignRight: true);
    }

    private static void Line(StringBuilder builder, int x1, int y1, int x2, int y2, string color = "0.227 0.227 0.216", double width = 1)
    {
        builder.Append(color).Append(" RG ")
            .Append(width.ToString("0.##", CultureInfo.InvariantCulture)).Append(" w ")
            .Append(x1).Append(' ').Append(y1).Append(" m ")
            .Append(x2).Append(' ').Append(y2).AppendLine(" l S");
    }

    private static void Text(StringBuilder builder, string text, int x, int y, int size, string font, string color, bool alignRight = false)
    {
        var safeText = Escape(text);
        var offset = alignRight ? EstimateWidth(text, size) : 0;
        builder.Append(color).Append(" rg BT /")
            .Append(font).Append(' ').Append(size).Append(" Tf ")
            .Append(x - offset).Append(' ').Append(y).Append(" Td (")
            .Append(safeText).AppendLine(") Tj ET");
    }

    private static int EstimateWidth(string text, int size)
    {
        return (int)Math.Round(text.Length * size * 0.54);
    }

    private static string TrimForReceipt(string value)
    {
        return value.Length <= 34 ? value : string.Concat(value.AsSpan(0, 31), "...");
    }

    private static string BuildImageObject(PngPdfImage image)
    {
        var imageStream = string.Concat(image.HexData, ">");

        return $"""
            << /Type /XObject /Subtype /Image /Width {image.Width} /Height {image.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter [/ASCIIHexDecode /FlateDecode] /DecodeParms [null << /Predictor 15 /Colors 3 /BitsPerComponent 8 /Columns {image.Width} >>] /Length {imageStream.Length} >>
            stream
            {imageStream}
            endstream
            """;
    }

    private static PngPdfImage? TryReadPngImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length < 33 || !bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            {
                return null;
            }

            var offset = 8;
            var width = 0;
            var height = 0;
            var bitDepth = 0;
            var colorType = 0;
            var interlace = 0;
            var data = new List<byte>();

            while (offset + 12 <= bytes.Length)
            {
                var length = ReadBigEndianInt(bytes, offset);
                var type = Encoding.ASCII.GetString(bytes, offset + 4, 4);
                var dataOffset = offset + 8;

                if (dataOffset + length > bytes.Length)
                {
                    return null;
                }

                if (type == "IHDR")
                {
                    width = ReadBigEndianInt(bytes, dataOffset);
                    height = ReadBigEndianInt(bytes, dataOffset + 4);
                    bitDepth = bytes[dataOffset + 8];
                    colorType = bytes[dataOffset + 9];
                    interlace = bytes[dataOffset + 12];
                }
                else if (type == "IDAT")
                {
                    data.AddRange(bytes.AsSpan(dataOffset, length).ToArray());
                }
                else if (type == "IEND")
                {
                    break;
                }

                offset += length + 12;
            }

            if (width <= 0 || height <= 0 || bitDepth != 8 || colorType != 2 || interlace != 0 || data.Count == 0)
            {
                return null;
            }

            return new PngPdfImage(width, height, Convert.ToHexString(data.ToArray()));
        }
        catch
        {
            return null;
        }
    }

    private static int ReadBigEndianInt(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24)
            | (bytes[offset + 1] << 16)
            | (bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    private static string Escape(string value)
    {
        var ascii = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(value));
        return ascii.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private sealed record PngPdfImage(int Width, int Height, string HexData);
}
