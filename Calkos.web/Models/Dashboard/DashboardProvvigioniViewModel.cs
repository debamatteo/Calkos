using CalkosManager.Domain.Entities;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;

//Creazione del ViewModel principale
//Il ViewModel contiene:
//Filtri(Anno, Mese, Mandatario, Agente)
//Liste dropdown
//Dati del Mandatario
//Dati dell’Agente
//Helper di formattazione


namespace Calkos.web.Models.Dashboard
{
    /// <summary>
    /// ViewModel principale della Dashboard Provvigioni.
    /// Contiene filtri, dati aggregati e metodi helper.
    /// Ora esteso per gestire anche i CLIENTI in modo dinamico per tipo pagamento.
    /// </summary>
    public class DashboardProvvigioniViewModel
    {
        // -----------------------------
        // FILTRI GLOBALI
        // -----------------------------

        public int Anno { get; set; }
        public int Mese { get; set; }
        public int IdMandatario { get; set; }
        public int? IdAgente { get; set; }
        public int? Fatturata { get; set; }
        // -----------------------------
        // LISTE PER DROPDOWN
        // -----------------------------

        public List<MandatarioSelectItem> Mandatari { get; set; } = new();
        public List<AgenteSelectItem> Agenti { get; set; } = new();

        // -----------------------------
        // DATI NORMALIZZATI (MANDATARIO / AGENTE)
        // -----------------------------

        /// <summary>
        /// Dati del Mandatario selezionato (tutte le righe del prospetto).
        /// </summary>
        public List<DashboardRigaDTO> DatiMandatario { get; set; } = new();

        /// <summary>
        /// Dati filtrati per Agente (se selezionato).
        /// </summary>
        public List<DashboardRigaDTO> DatiAgente { get; set; } = new();

        // -----------------------------
        // DATI CLIENTI (NUOVO BLOCCO)
        // -----------------------------

        /// <summary>
        /// Dati clienti per la sezione "Riepilogo Totali Clienti".
        /// </summary>
        public List<RigaCliente> DatiClienti { get; set; } = new();

        /// <summary>
        /// Dati clienti per la sezione "Calcolo Provvigione Agente".
        /// </summary>
        public List<RigaCliente> DatiProvvigioniAgente { get; set; } = new();

        /// <summary>
        /// Raggruppamento dinamico dei clienti per tipo pagamento (30/60/90 o qualsiasi altro).
        /// Key = TipoPagamento (es. "30GG"), Value = lista righe cliente.
        /// </summary>
        public Dictionary<string, List<RigaCliente>> ClientiPerPagamento { get; private set; } = new();

        /// <summary>
        /// Raggruppamento dinamico delle provvigioni agente per tipo pagamento.
        /// </summary>
        public Dictionary<string, List<RigaCliente>> ProvvigioniPerPagamento { get; private set; } = new();

        /// <summary>
        /// Costruisce i raggruppamenti dinamici per tipo pagamento
        /// sulla base dei dati popolati dal service.
        /// </summary>
        public void CostruisciRaggruppamentiClienti()
        {
            // Raggruppo i clienti per tipo pagamento (dashboard Riepilogo Clienti)
            ClientiPerPagamento = DatiClienti
                .GroupBy(x => x.TipoPagamento)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );

            // Raggruppo le provvigioni agente per tipo pagamento
            ProvvigioniPerPagamento = DatiProvvigioniAgente
                .GroupBy(x => x.TipoPagamento)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );
        }

        // -----------------------------
        // METODI HELPER FORMATTATI
        // -----------------------------

        /// <summary>
        /// Formatta un numero decimale in stile italiano (es. 1.234,56).
        /// </summary>
        public string FormatNumero(decimal value)
        {
            return value.ToString("#,0.00", new CultureInfo("it-IT"));
        }

        /// <summary>
        /// Formatta un importo in euro (es. € 1.234,56).
        /// </summary>
        public string FormatEuro(decimal value)
        {
            return "€ " + value.ToString("#,0.00", new CultureInfo("it-IT"));
        }

        // -----------------------------
        // KPI (espressioni calcolate)
        // -----------------------------

        // Totali basati sui dati del Mandatario (come prima)
        public decimal TotaleQuantita => DatiMandatario.Sum(x => x.Quantita);
        public decimal TotaleCommissioni => DatiMandatario.Sum(x => x.CommissioniTotali);
        public int NumeroAgentiCoinvolti => DatiMandatario.Select(x => x.IdAgente).Distinct().Count();
        public int NumeroTipiPagamento => DatiMandatario.Select(x => x.IdTipoPagamento).Distinct().Count();
    }

    /// <summary>
    /// Elemento per la dropdown Mandatari.
    /// </summary>
    public class MandatarioSelectItem
    {
        public int IdMandatario { get; set; }
        public string Nome { get; set; }
    }

    /// <summary>
    /// Elemento per la dropdown Agenti.
    /// </summary>
    public class AgenteSelectItem
    {
        public int IdAgente { get; set; }
  

        public string AgenteDescrizione { get; set; }
    }
}
