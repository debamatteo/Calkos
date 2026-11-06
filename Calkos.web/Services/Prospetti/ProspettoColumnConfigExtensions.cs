using Calkos.web.Models.Prospetti;

namespace Calkos.web.Services.Prospetti
{
    /// <summary>
    /// 07/08/2026 Applica la visibilità per ruolo alla configurazione colonne,
    /// riusabile da griglia, export Excel e export PDF.
    /// </summary>
    public static class ProspettoColumnConfigExtensions
    {
        public static List<ProspettoColumnConfig> FiltraPerRuolo(
            this List<ProspettoColumnConfig> colonne, bool isAgente)
        {
            if (!isAgente)
                return colonne; // Admin/Operatore: nessuna restrizione

            // Per l'Agente: le colonne con VisibileAgente = false
            // vengono nascoste sia in griglia (Visible) che negli export (Export).
            return colonne.Select(c => new ProspettoColumnConfig
            {
                Name = c.Name,
                Label = c.Label,
                Visible = c.Visible && c.VisibileAgente,
                Format = c.Format,
                Width = c.Width,
                Export = c.Export && c.VisibileAgente,
                SortOrder = c.SortOrder,
                SortDirection = c.SortDirection,
                VisibileAgente = c.VisibileAgente
            }).ToList();
        }
    }
}