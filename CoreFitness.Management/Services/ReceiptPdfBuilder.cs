using System.Text;
using CoreFitness.Management.Models;

namespace CoreFitness.Management.Services;

public static class ReceiptPdfBuilder
{
    public static byte[] Build(Sale sale)
    {
        var lines = new[]
        {
            "CORE FITNESS",
            $"{sale.Outlet.ToUpperInvariant()} RECEIPT",
            $"Receipt: {sale.Id.ToString()[..8].ToUpperInvariant()}",
            $"Date: {sale.SoldAt:dd MMM yyyy hh:mm tt}",
            $"Sold by: {sale.SoldBy}",
            $"Payment: {sale.PaymentMethod}",
            "",
            $"Item: {sale.ProductName}",
            $"Quantity: {sale.Quantity}",
            $"Unit Price: {sale.UnitPrice:N0}",
            $"Total: {sale.Total:N0}",
            "",
            "Thank you for choosing CORE FITNESS."
        };

        var text = BuildContentStream(lines);
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(text)} >>\nstream\n{text}\nendstream"
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
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset)
            .Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string BuildContentStream(IEnumerable<string> lines)
    {
        var builder = new StringBuilder();
        builder.Append("BT\n/F1 20 Tf\n50 742 Td\n");

        foreach (var line in lines)
        {
            builder.Append('(').Append(Escape(line)).Append(") Tj\n0 -28 Td\n");
        }

        builder.Append("ET");
        return builder.ToString();
    }

    private static string Escape(string value)
    {
        var ascii = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(value));
        return ascii.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
