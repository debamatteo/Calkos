using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalkosManager.Domain.Entities
{
    /// <summary>
    /// Rappresenta un file Excel importato.
    /// Tabella: dbo.FileImportato
    /// </summary>
    public class FileImportato
    {
        public int IdFileImportato { get; set; }

        /// <summary>
        /// FK verso Mandatario.
        /// </summary>
        public int IdMandatario { get; set; }

        /// <summary>
        /// Nome del file Excel importato.
        /// </summary>
        public string NomeFile { get; set; } = string.Empty;

        /// <summary>
        /// Percorso del file di backup fisico.
        /// </summary>
        public string PercorsoBackup { get; set; } = string.Empty;

        /// <summary>
        /// Data di importazione del file.
        /// </summary>
        public DateTime DataImportazione { get; set; }


        public DateTime DataInserimento { get; set; }
        public DateTime? DataModifica { get; set; }
        public string Utente { get; set; }
        public int Anno { get; set; }
        public int Mese { get; set; }


    }
}
