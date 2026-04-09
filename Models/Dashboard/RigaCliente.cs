using System;

namespace Calkos.web.Models.Dashboard
{
    /// <summary>
    /// DTO che rappresenta una singola riga cliente nella dashboard provvigioni.
    /// Contiene informazioni su cliente, quantità, importi e provvigioni.
    /// </summary>
    public class RigaCliente
    {
        // -----------------------------
        // 1. Informazioni Cliente
        // -----------------------------
        public int IdCliente { get; set; }
        public string NomeCliente { get; set; }

        // -----------------------------
        // 2. Quantità e Importi
        // -----------------------------
        public decimal Quantita { get; set; }
        public decimal Importo { get; set; }

        // -----------------------------
        // 3. Commissioni
        // -----------------------------
        public decimal CommissioniTotali { get; set; }
        public decimal ProvvigioneAgente { get; set; }//è la percentuale dell'agente

        public decimal ValoreProvvigioneAgente  { get; set; }// è il valore della percentuale  calcolata sul valore della commissione del mandatario

        // -----------------------------
        // 4. Tipo Pagamento (30/60/90)
        // -----------------------------
        public int IdTipoPagamento { get; set; }
        public string TipoPagamento { get; set; }

        // -----------------------------
        // 5. Informazioni Agente
        // -----------------------------
        public int IdAgente { get; set; }
        public string AgenteDescrizione { get; set; }
    }

    // Questa classe serve a "smontare" il JSON che arriva dal Javascript
    public class EmissioneFatturaRequest
    {
        public List<RigaCliente> RigheSelezionate { get; set; }
        public int IdMandatario { get; set; }
        public int Anno { get; set; }
        public int Mese { get; set; }
        public int IdTipoPagamento { get; set; }
    }
}
