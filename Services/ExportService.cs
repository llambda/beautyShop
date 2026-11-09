using BeautyStudio.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace BeautyStudio.Services;

public sealed class ExportService
{
    public string ExportCustomerCsv(Customer customer, IEnumerable<Intervention> interventions)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"BeautyStudio_Cliente_{customer.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        var rows = new List<string> { "Data;Cliente;Tipo;Luogo;Prezzo;Pagamento;Note" };
        foreach (var item in interventions.OrderBy(x => x.Date))
            rows.Add(string.Join(";", Csv(item.Date.ToString("dd/MM/yyyy")), Csv(customer.FirstName + " " + customer.LastName),
                Csv(item.Type?.Name), Csv(item.Location?.Name), Csv(item.Price.ToString("0.00", CultureInfo.GetCultureInfo("it-IT"))),
                Csv(item.IsPaid ? "Pagato" : "Da saldare"), Csv(item.Notes)));
        File.WriteAllText(path, "\uFEFF" + string.Join(Environment.NewLine, rows), Encoding.UTF8);
        return path;
    }

    public string ExportCustomerPdf(Customer customer, IEnumerable<Intervention> interventions)
    {
        var lines = new List<string>
        {
            "BEAUTY STUDIO - STORICO CLIENTE",
            $"Cliente: {customer.FirstName} {customer.LastName}",
            $"Telefono: {customer.Phone ?? "-"}",
            $"Email: {customer.Email ?? "-"}",
            $"Data esportazione: {DateTime.Now:dd/MM/yyyy HH:mm}",
            "",
            "STORICO INTERVENTI"
        };
        foreach (var item in interventions.OrderBy(x => x.Date))
            lines.Add($"{item.Date:dd/MM/yyyy} | {item.Type?.Name ?? "Trattamento"} | {item.Location?.Name ?? "-"} | {item.Price:0.00} EUR | {(item.IsPaid ? "Pagato" : "Da saldare")}");
        lines.Add("");
        lines.Add($"Totale interventi: {interventions.Count()}");
        lines.Add($"Totale: {interventions.Sum(x => x.Price):0.00} EUR");
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"BeautyStudio_Cliente_{customer.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        MinimalPdf.Write(path, lines);
        return path;
    }

    private static string Csv(string? value) => "\"" + (value ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
}

internal static class MinimalPdf
{
    public static void Write(string path, IEnumerable<string> lines)
    {
        var content = new StringBuilder("BT /F1 10 Tf 48 790 Td 14 TL\n");
        foreach (var line in lines.Take(48))
        {
            var safe = line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("€", "EUR");
            content.Append('(').Append(safe).Append(") Tj T*\n");
        }
        content.Append("ET");
        var stream = Encoding.Latin1.GetBytes(content.ToString());
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {stream.Length} >>\nstream\n{content}\nendstream"
        };
        using var output = new FileStream(path, FileMode.Create, FileAccess.Write);
        void W(string s) { var b = Encoding.Latin1.GetBytes(s); output.Write(b); }
        W("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (int i = 0; i < objects.Length; i++)
        {
            offsets.Add(output.Position);
            W($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = output.Position;
        W($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) W($"{offset:0000000000} 00000 n \n");
        W($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
    }
}