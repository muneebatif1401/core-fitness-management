using System.Globalization;
using System.Text;
using CoreFitness.Management.Models;

namespace CoreFitness.Management.Services;

public static class ReceiptPdfBuilder
{
    public static byte[] Build(Sale sale)
    {
        var stream = BuildContentStream(sale);
        var streamLength = Encoding.ASCII.GetByteCount(stream);
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
            $"<< /Length {streamLength} >>\nstream\n{stream}\nendstream"
        };

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

    private static string BuildContentStream(Sale sale)
    {
        var id = sale.Id.ToString()[..8].ToUpperInvariant();
        var builder = new StringBuilder();

        builder.AppendLine("1 1 1 rg 0 0 612 792 re f");
        builder.AppendLine("0 0 0 RG 1 w 96 76 420 640 re S");
        builder.AppendLine("0 0 0 RG 1.5 w 126 674 m 486 674 l S");

        Text(builder, "CORE FITNESS", 126, 692, 24, "F2", "0 0 0");
        Text(builder, $"{sale.Outlet.ToUpperInvariant()} RECEIPT", 126, 652, 13, "F2", "0 0 0");
        Text(builder, $"#{id}", 486, 652, 13, "F2", "0 0 0", alignRight: true);

        Line(builder, 126, 626, 486, 626);
        Row(builder, "Date", sale.SoldAt.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture), 596);
        Row(builder, "Item", sale.ProductName, 560);
        Row(builder, "Quantity", sale.Quantity.ToString(CultureInfo.InvariantCulture), 524);
        Row(builder, "Unit Price", sale.UnitPrice.ToString("N0", CultureInfo.InvariantCulture), 488);
        Row(builder, "Payment", sale.PaymentMethod, 452);
        Row(builder, "Sold By", sale.SoldBy, 416);

        Line(builder, 126, 384, 486, 384);
        Text(builder, "TOTAL", 126, 344, 14, "F2", "0 0 0");
        Text(builder, sale.Total.ToString("N0", CultureInfo.InvariantCulture), 486, 344, 28, "F2", "0 0 0", alignRight: true);

        Line(builder, 126, 292, 486, 292);
        Text(builder, "THANK YOU FOR CHOOSING CORE FITNESS", 126, 264, 11, "F2", "0 0 0");
        Text(builder, "Ink-friendly bill copy for printing or saving.", 126, 246, 10, "F1", "0.28 0.28 0.28");

        return builder.ToString();
    }

    private static void Row(StringBuilder builder, string label, string value, int y)
    {
        Text(builder, label.ToUpperInvariant(), 126, y, 10, "F2", "0.22 0.22 0.22");
        Text(builder, value, 486, y, 12, "F2", "0 0 0", alignRight: true);
    }

    private static void Line(StringBuilder builder, int x1, int y1, int x2, int y2)
    {
        builder.Append("0 0 0 RG 0.75 w ")
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

    private static string Escape(string value)
    {
        var ascii = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(value));
        return ascii.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
