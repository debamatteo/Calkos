using System.Collections.Generic;
using CalkosManager.Domain.Entities;
using Calkos.web.Models.Prospetti;

namespace Calkos.web.Models.ViewModels.Prospetti
{
    /// <summary>
    /// ViewModel per la pagina ListaOrdini SystemCore.
    /// Contiene:
    /// - Le righe del prospetto (dati dal database)
    /// - La configurazione delle colonne (dati dal file JSON)
    /// 
    /// Questo permette alla view di essere completamente dinamica:
    /// nessuna colonna hardcoded.
    /// </summary>
    public class ProspettoSystemCoreListaViewModel
    {
        /// <summary>
        /// Righe del prospetto SystemCore (dati letti dal repository).
        /// </summary>
        public IEnumerable<ProspettoSystemCore> Righe { get; set; }

        /// <summary>
        /// Colonne configurate nel file JSON (Config/Prospetti/SystemCore.json).
        /// </summary>
        public List<ProspettoColumnConfig> Colonne { get; set; }
    }
}
