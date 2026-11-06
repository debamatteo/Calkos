using System.IO.Compression;
using Microsoft.AspNetCore.Http;
//20/10/2026
namespace Calkos.web.Helpers;

/// <summary>
/// Copia un file caricato in una cartella temporanea FUORI da wwwroot, con nome casuale
/// ed estensione da allowlist. Il file si cancella da solo con Dispose().
/// </summary>
public sealed class UploadExcelTemp : IDisposable
{
    private static readonly string[] EstensioniAmmesse = [".xlsx", ".xlsm"];
    private const long MaxBytes = 20L * 1024 * 1024;               // file caricato
    private const long MaxBytesDecompressi = 200L * 1024 * 1024;   // contenuto dello zip

    public string Percorso { get; }

    private UploadExcelTemp(string percorso) => Percorso = percorso;

    public static bool TryCreate(IFormFile? file, string prefisso, out UploadExcelTemp? risultato, out string errore)
    {
        risultato = null;
        errore = string.Empty;

        if (file is null || file.Length == 0) { errore = "Seleziona un file Excel valido."; return false; }
        if (file.Length > MaxBytes) { errore = "Il file supera il limite di 20 MB."; return false; }

        var estensione = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!EstensioniAmmesse.Contains(estensione)) { errore = "Sono ammessi solo file .xlsx e .xlsm."; return false; }

        try
        {
            using var zip = new ZipArchive(file.OpenReadStream(), ZipArchiveMode.Read);
            long totale = 0;
            foreach (var voce in zip.Entries)
            {
                totale += voce.Length;
                if (totale > MaxBytesDecompressi) { errore = "Il contenuto del file è troppo grande."; return false; }
            }
            if (!zip.Entries.Any(e => e.FullName.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase)))
            { errore = "Il file non è un documento Excel valido."; return false; }
        }
        catch (InvalidDataException)
        {
            errore = "Il file non è un documento Excel valido.";
            return false;
        }

        var cartella = Path.Combine(Path.GetTempPath(), "CalkosImport");
        Directory.CreateDirectory(cartella);

        var percorso = Path.Combine(cartella, $"{prefisso}_{Guid.NewGuid():N}{estensione}");
        using (var fs = new FileStream(percorso, FileMode.CreateNew, FileAccess.Write))
            file.CopyTo(fs);

        risultato = new UploadExcelTemp(percorso);
        return true;
    }

    public void Dispose()
    {
        try { if (File.Exists(Percorso)) File.Delete(Percorso); } catch { /* non blocca l'importazione */ }
    }
}
