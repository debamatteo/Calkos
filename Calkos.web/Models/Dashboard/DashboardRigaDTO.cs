using Calkos.web.Models.Dashboard;
using System;

//Creazione del DTO normalizzato: DashboardRigaDTO
//Questo è il formato standard che la dashboard userà per ogni riga, indipendentemente dal Mandatario.

namespace Calkos.web.Models.Dashboard
{
    /// <summary>
    /// DTO normalizzato per una riga della Dashboard Provvigioni.
    /// Questo formato è identico per TUTTI i Mandatari,
    /// indipendentemente dalla struttura delle tabelle ProspettoXXX.
    /// </summary>
    public class DashboardRigaDTO
    {
        /// <summary>
        /// Quantità espressa in KG o pezzi, a seconda del Mandatario.
        /// La dashboard non deve sapere l'unità di misura originale.
        /// </summary>
        public decimal Quantita { get; set; }

        /// <summary>
        /// Importo totale delle commissioni in euro.
        /// Ogni Mandatario lo calcola in modo diverso, ma qui è normalizzato.
        /// </summary>
        public decimal CommissioniTotali { get; set; }

        /// <summary>
        /// Identificativo del tipo pagamento (es. 30GG, 60GG, ecc.)
        /// Serve per raggruppare le card della dashboard.
        /// </summary>
        public int IdTipoPagamento { get; set; }

        /// <summary>
        /// Descrizione del tipo pagamento (es. "Bonifico 60GG").
        /// </summary>
        public string TipoPagamento { get; set; }

        /// <summary>
        /// Identificativo dell'agente associato alla riga.
        /// Serve per filtrare la sezione inferiore via AJAX.
        /// </summary>
        public int IdAgente { get; set; }

        /// <summary>
        /// Nome e cognome dell'agente.
        /// </summary>
        public string AgenteDescrizione { get; set; }

        /// <summary>
        /// Anno del prospetto (campo obbligatorio per filtrare).
        /// </summary>
        public int Anno { get; set; }

        /// <summary>
        /// Mese del prospetto (campo obbligatorio per filtrare).
        /// </summary>
        public int Mese { get; set; }
    }
}
