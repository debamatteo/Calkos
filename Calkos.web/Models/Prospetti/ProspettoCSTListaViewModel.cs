using System.Collections.Generic;
using CalkosManager.Domain.Entities;
using Calkos.web.Models.Prospetti;

namespace Calkos.web.Models.ViewModels.Prospetti
{
    /// <summary>
    /// ViewModel per la pagina ListaOrdini CST.
    /// Contiene:
    /// - Le righe del prospetto (dati dal database)
    /// - La configurazione delle colonne (dati dal file JSON)
    /// 
    /// Questo permette alla view di essere completamente dinamica:
    /// nessuna colonna hardcoded.
    /// </summary>
    public class ProspettoCSTListaViewModel
    {
        /// <summary>
        /// Righe del prospetto CST (dati letti dal repository).
        /// </summary>
        public IEnumerable<ProspettoCST> Righe { get; set; }

        /// <summary>
        /// Colonne configurate nel file JSON (Config/Prospetti/CST.json).
        /// </summary>
        public List<ProspettoColumnConfig> Colonne { get; set; }
    }
}
