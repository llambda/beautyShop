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
            $"Indirizzo: {customer.Address ?? "-"}",
            $"Data di nascita: {customer.BirthDate:dd/MM/yyyy}",
            $"Allergie: {customer.Allergies ?? "-"}",
            $"Sensibilità: {customer.Sensitivities ?? "-"}",
            $"Note: {customer.Notes ?? "-"}",
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

    public string ExportCustomersCsv(IEnumerable<Customer> customers)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"BeautyStudio_Clienti_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        var rows = new List<string> { "Nome;Cognome;Telefono;Email;Indirizzo;Data di nascita" };
        foreach (var customer in customers.OrderBy(x => x.LastName).ThenBy(x => x.FirstName))
            rows.Add(string.Join(";", Csv(customer.FirstName), Csv(customer.LastName), Csv(customer.Phone), Csv(customer.Email), Csv(customer.Address), Csv(customer.BirthDate?.ToString("dd/MM/yyyy"))));
        File.WriteAllText(path, "\uFEFF" + string.Join(Environment.NewLine, rows), Encoding.UTF8);
        return path;
    }

    public string ExportAppointmentsPdf(IEnumerable<Appointment> appointments, DateTime month)
    {
        var items = appointments.Where(x => x.Date.Year == month.Year && x.Date.Month == month.Month).OrderBy(x => x.Date).ToList();
        var lines = new List<string> { "BEAUTY STUDIO - CALENDARIO APPUNTAMENTI", month.ToString("MMMM yyyy"), "" };
        foreach (var item in items)
            lines.Add($"{item.Date:dd/MM/yyyy HH:mm} | {item.Customer?.FirstName} {item.Customer?.LastName} | {item.InterventionType?.Name ?? "Trattamento"} | {item.Notes}");
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"BeautyStudio_Calendario_{month:yyyyMM}.pdf");
        MinimalPdf.Write(path, lines);
        return path;
    }

    public string ExportAppointmentsCsv(IEnumerable<Appointment> appointments, DateTime month)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"BeautyStudio_Calendario_{month:yyyyMM}.csv");
        var rows = new List<string> { "Data;Ora;Cliente;Tipo;Note" };
        foreach (var item in appointments.Where(x => x.Date.Year == month.Year && x.Date.Month == month.Month).OrderBy(x => x.Date))
            rows.Add(string.Join(";", Csv(item.Date.ToString("dd/MM/yyyy")), Csv(item.Date.ToString("HH:mm")), Csv($"{item.Customer?.FirstName} {item.Customer?.LastName}"), Csv(item.InterventionType?.Name), Csv(item.Notes)));
        File.WriteAllText(path, "\uFEFF" + string.Join(Environment.NewLine, rows), Encoding.UTF8);
        return path;
    }

    private static string Csv(string? value) => "\"" + (value ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
}

internal static class MinimalPdf
{
    public static void Write(string path, IEnumerable<string> sourceLines)
    {
        var lines = new List<string>();
        foreach (var source in sourceLines)
        {
            var line = source ?? string.Empty;
            while (line.Length > 100)
            {
                lines.Add(line.Substring(0, 100));
                line = line.Substring(100);
            }
            lines.Add(line);
        }

        var pages = lines.Chunk(48).Select(x => x.ToArray()).ToList();
        if (pages.Count == 0) pages.Add(Array.Empty<string>());

        var objects = new List<string>();
        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        var pageIds = Enumerable.Range(0, pages.Count).Select(i => 4 + i * 2).ToArray();
        objects.Add("<< /Type /Pages /Kids [" + string.Join(" ", pageIds.Select(id => id + " 0 R")) + "] /Count " + pages.Count + " >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");

        foreach (var page in pages)
        {
            var content = new StringBuilder("BT /F1 10 Tf 48 790 Td 14 TL\n");
            foreach (var line in page)
            {
                var safe = line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("€", "EUR");
                content.Append('(').Append(safe).Append(") Tj T*\n");
            }
            content.Append("ET\n");
            var bytes = Encoding.Latin1.GetBytes(content.ToString());
            var pageId = 4 + objects.Count(x => x.StartsWith("<< /Type /Page ", StringComparison.Ordinal)) * 2;
            var contentId = pageId + 1;
            objects.Add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents " + contentId + " 0 R >>");
            objects.Add("<< /Length " + bytes.Length + " >>\nstream\n" + content + "endstream");
        }

        using var output = new FileStream(path, FileMode.Create, FileAccess.Write);
        void WriteText(string value)
        {
            var bytes = Encoding.Latin1.GetBytes(value);
            output.Write(bytes, 0, bytes.Length);
        }

        WriteText("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            WriteText((i + 1) + " 0 obj\n" + objects[i] + "\nendobj\n");
        }
        var xref = output.Position;
        WriteText("xref\n0 " + (objects.Count + 1) + "\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            WriteText(offset.ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
        WriteText("trailer\n<< /Size " + (objects.Count + 1) + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF");
    }
}