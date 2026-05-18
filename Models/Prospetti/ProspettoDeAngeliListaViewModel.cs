using System.Collections.Generic;
using CalkosManager.Domain.Entities;
using Calkos.web.Models.Prospetti;

namespace Calkos.web.Models.ViewModels.Prospetti
{
    /// <summary>
    /// ViewModel per la pagina ListaOrdini DeAngeli.
    /// Contiene:
    /// - Le righe del prospetto (dati dal database)
    /// - La configurazione delle colonne (dati dal file JSON)
    /// 
    /// Questo permette alla view di essere completamente dinamica:
    /// nessuna colonna hardcoded.
    /// </summary>
    public class ProspettoDeAngeliListaViewModel
    {
        /// <summary>
        /// Righe del prospetto DeAngeli (dati letti dal repository).
        /// </summary>
        public IEnumerable<ProspettoDeAngeli> Righe { get; set; }

        /// <summary>
        /// Colonne configurate nel file JSON (Config/Prospetti/DeAngeli.json).
        /// </summary>
        public List<ProspettoColumnConfig> Colonne { get; set; }
    }
}
