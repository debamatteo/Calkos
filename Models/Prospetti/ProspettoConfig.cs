using System.Collections.Generic;

namespace Calkos.web.Models.Prospetti
{
    /// <summary>
    /// Rappresenta la configurazione di una singola colonna di un prospetto.
    /// I dati arrivano dal file JSON (es. cobral.json).
    /// </summary>
    public class ProspettoColumnConfig
    {
        /// <summary>
        /// Nome della proprietà nel modello (es. "NumeroFattura", "Quantita").
        /// Deve corrispondere al nome della proprietà C# (RigaCobral, ecc.).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Etichetta da mostrare in tabella, Excel, PDF.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Indica se la colonna è visibile oppure no.
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Formato logico del valore (es. "text", "int", "currency", "date:dd/MM/yyyy", "decimal:0.00").
        /// Sarà usato da Excel e PDF per formattare correttamente.
        /// </summary>
        public string Format { get; set; } = "text";

        /// <summary>
        /// Larghezza relativa della colonna (usata soprattutto per PDF).
        /// </summary>
        public int Width { get; set; } = 1;


        public bool Export { get; set; } = true;// determina se esportare una colonna del prospetto nel file pdf\excel
    }

    /// <summary>
    /// Rappresenta la configurazione completa di un prospetto (insieme di colonne).
    /// </summary>
    public class ProspettoConfig
    {
        /// <summary>
        /// Elenco delle colonne configurate per il prospetto.
        /// </summary>
        public List<ProspettoColumnConfig> Columns { get; set; } = new();
    }
}
