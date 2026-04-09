using Calkos.web.Models.Dashboard;
using CalkosManager.Infrastructure.Helpers; // Estensioni SqlDataReader
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Calkos.web.Services
{
    /// <summary>
    /// Service dedicato alla Dashboard Provvigioni.
    /// Contiene la logica di lettura aggregata per Mandatari e Agenti.
    /// NON è un repository perché non rappresenta un'entità del dominio.
    /// </summary>
    public class DashboardProvvigioniService
    {
        private readonly string _connectionString;

        /// <summary>
        /// Il service lavora su stored procedure aggregate,
        /// quindi utilizza direttamente la connection string.
        /// </summary>
        public DashboardProvvigioniService(string connectionString)
        {
            _connectionString = connectionString;
        }
        // =====================================================================
        //  LETTURA DATI COBRAL (MANDATARIO / AGENTE) - AGGIORNATO CON FILTRO FATTURA
        // GetDatiCobral serve a recuperare i dati AGGREGATI del prospetto provvigioni Cobral.
        // È pensato per costruire la parte alta della dashboard, cioè la sezione che mostra:  Totali per tipo pagamento ; Totali per agente ; Totali generali
        // GetDatiCobral restituisce righe tipo: TipoPagamento | Agente | QuantitàTotale | CommissioniTotali | Anno | Mese
        // =====================================================================
        /// <summary>
        /// Legge i dati del prospetto Cobral per anno, mese e stato fatturazione,
        /// tramite stored procedure, e li normalizza per la dashboard.
        /// </summary>
        /// <param name="anno">Anno di riferimento</param>
        /// <param name="mese">Mese di riferimento</param>
        /// <param name="fatturata">Filtro stato: 0=Non Fatturate, 1=Fatturate, null=Tutte</param>
        public List<DashboardRigaDTO> GetDatiCobral(int anno, int mese, int? fatturata = null)
        {
            var risultati = new List<DashboardRigaDTO>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("spDashboardProvvigioni_Cobral", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Parametri tipizzati
                    cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = anno;
                    cmd.Parameters.Add("@Mese", SqlDbType.Int).Value = mese;

                    // -------------------------------------------------------------
                    // Nuovo Parametro: Stato Fatturazione
                    // -------------------------------------------------------------
                    cmd.Parameters.Add("@Fatturata", SqlDbType.Int).Value = (object)fatturata ?? DBNull.Value;

                    conn.Open();

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var riga = new DashboardRigaDTO();

                            // 1. Quantità (KG o pezzi)
                            riga.Quantita = rdr.GetDecimalSafe("Quantita");

                            // 2. Importo totale delle commissioni
                            riga.CommissioniTotali = rdr.GetDecimalSafe("CommissioniTotali");

                            // 3. Info Pagamento
                            riga.IdTipoPagamento = rdr.GetIntSafe("IdTipoPagamento");
                            riga.TipoPagamento = rdr.GetStringSafe("TipoPagamento");

                            // 4. Info Agente
                            riga.IdAgente = rdr.GetIntSafe("IdAgente");
                            riga.AgenteDescrizione = rdr.GetStringSafe("AgenteDescrizione");

                            // 5. Periodo
                            riga.Anno = rdr.GetIntSafe("Anno");
                            riga.Mese = rdr.GetIntSafe("Mese");

                            risultati.Add(riga);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Errore durante la lettura dal database (GetDatiCobral).", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Errore imprevisto nel recupero dati dashboard (GetDatiCobral).", ex);
            }

            return risultati;
        }

        // =====================================================================
        //  LETTURA CLIENTI (30/60/90) PER DASHBOARD - AGGIORNATO CON FILTRO FATTURA
        // 1- GetClienti → popola la tabella Riepilogo Totali Clienti->Foreach (var gruppo in Model.ClientiPerPagamento)
        // 2- GetClienti → popola la tabella Dettaglio Provvigioni Agenti ->var gruppiPerAgente = Model.ProvvigioniPerPagamento
        // GetRigheAgentiPerStampa GetClienti restituisce righe tipo: Cliente | TipoPagamento | Agente | Quantità | Importo | Provvigione
        // =====================================================================
        /// <summary>
        /// Legge i dati cliente (30/60/90) per anno/mese, agente e stato fatturazione.
        /// Restituisce una lista di RigaCliente, già pronta per essere raggruppata.
        /// </summary>
        /// <param name="anno">Anno di riferimento</param>
        /// <param name="mese">Mese di riferimento</param>
        /// <param name="idAgente">ID Agente opzionale</param>
        /// <param name="fatturata">Filtro stato: 0=Non Fatturate, 1=Fatturate, null=Tutte</param>
        public List<RigaCliente> GetClienti(int anno, int mese, int? idAgente = null, int? fatturata = null)
        {
            var risultati = new List<RigaCliente>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("spDashboardProvvigioni_Cobral", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // -------------------------------------------------------------
                    // Parametri tipizzati
                    // -------------------------------------------------------------
                    cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = anno;
                    cmd.Parameters.Add("@Mese", SqlDbType.Int).Value = mese;

                    if (idAgente.HasValue)
                        cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = idAgente.Value;
                    else
                        cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = DBNull.Value;

                    // -------------------------------------------------------------
                    // Nuovo Parametro: Stato Fatturazione
                    // -------------------------------------------------------------
                    cmd.Parameters.Add("@Fatturata", SqlDbType.Int).Value = (object)fatturata ?? DBNull.Value;

                    conn.Open();

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            // ---------------------------------------------------------
                            // Mapping campo-per-campo (professionale e leggibile)
                            // ---------------------------------------------------------
                            var riga = new RigaCliente();

                            // 1. Cliente
                            riga.IdCliente = rdr.GetIntSafe("IdCliente");
                            riga.NomeCliente = rdr.GetStringSafe("NomeCliente");

                            // 2. Quantità e Importi
                            riga.Quantita = rdr.GetDecimalSafe("Quantita");
                            riga.Importo = rdr.GetDecimalSafe("Importo");

                            // 3. Commissioni
                            riga.CommissioniTotali = rdr.GetDecimalSafe("CommissioniTotali");
                            riga.ValoreProvvigioneAgente = rdr.GetDecimalSafe("ValoreProvvigioneAgente");

                            // 4. Tipo Pagamento
                            riga.IdTipoPagamento = rdr.GetIntSafe("IdTipoPagamento");
                            riga.TipoPagamento = rdr.GetStringSafe("TipoPagamento");

                            // 5. Agente
                            riga.IdAgente = rdr.GetIntSafe("IdAgente");
                            riga.AgenteDescrizione = rdr.GetStringSafe("AgenteDescrizione");

                            risultati.Add(riga);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // -------------------------------------------------------------
                // Errori SQL (timeout, SP mancante, problemi di connessione)
                // -------------------------------------------------------------
                throw new Exception("Errore SQL durante la lettura dei clienti per la dashboard.", ex);
            }
            catch (Exception ex)
            {
                // -------------------------------------------------------------
                // Errori generici (mapping, cast, null reference)
                // -------------------------------------------------------------
                throw new Exception("Errore imprevisto nel recupero dei dati clienti.", ex);
            }

            return risultati;
        }



        // ============================================================================
        //  CLIENTI → Restituisce SOLO le righe selezionate per la stampa Excel/PDF
        // ============================================================================
        //  - anno/mese  : periodo filtrato nella dashboard
        //  - tableId    : tipo pagamento (30 / 60 / 90)
        //  - ids        : lista di IdCliente selezionati nella dashboard
        //  - fatturata  : NUOVO - stato fatturazione (0=Non Fatturate, 1=Fatturate, null=Tutte)
        //
        //  Flusso:
        //  1. Legge TUTTE le righe clienti del mese/anno tramite GetClienti()
        //  2. Filtra SOLO quelle del tipo pagamento richiesto (tableId)
        //  3. Filtra SOLO gli ID selezionati (ids)
        //  4. Restituisce la lista pronta per Excel/PDF
        // ============================================================================
        public List<RigaCliente> GetRigheClientiPerStampa(int anno, int mese, string tableId, List<int> ids, int? fatturata = null)
        {
            // ---------------------------------------------------------
            // 1. Recupera TUTTE le righe clienti del periodo
            //    Usa il tuo metodo GetClienti(anno, mese, null, fatturata)
            //    Passiamo il parametro fatturata per coerenza con i filtri a video
            // ---------------------------------------------------------
            var tutti = GetClienti(anno, mese, null, fatturata);

            // ---------------------------------------------------------
            // 2. Filtra SOLO le righe del tipo pagamento richiesto
            //    (es: "30", "60", "90")
            // ---------------------------------------------------------
            var filtratiPerPagamento = tutti
                .Where(r => r.TipoPagamento == tableId)
                .ToList();

            // ---------------------------------------------------------
            // 3. Filtra SOLO gli ID selezionati nella dashboard
            // ---------------------------------------------------------
            var selezionati = filtratiPerPagamento
                .Where(r => ids.Contains(r.IdCliente))
                .ToList();

            // ---------------------------------------------------------
            // 4. Restituisce la lista finale
            // ---------------------------------------------------------
            return selezionati;
        }

        // ============================================================================
        //  AGENTI → Restituisce TUTTE le righe dell'agente per la stampa Excel/PDF
        // ============================================================================
        //  - anno/mese  : periodo filtrato nella dashboard
        //  - tableId    : tipo pagamento (30 / 60 / 90)
        //  - fatturata  : NUOVO - stato fatturazione (0=Non Fatturate, 1=Fatturate, null=Tutte)
        //
        //  Flusso:
        //  1. Legge TUTTE le righe clienti del mese/anno tramite GetClienti()
        //  2. Filtra SOLO quelle del tipo pagamento richiesto (tableId)
        //  3. Raggruppa per agente (IdAgente)
        //  4. Restituisce tutte le righe dell'agente
        // ============================================================================
        public List<RigaCliente> GetRigheAgentiPerStampa(int anno, int mese, string tableId, int? fatturata = null)
        {
            // ---------------------------------------------------------
            // 1. Recupera TUTTE le righe clienti del periodo
            //    Includiamo il filtro fatturazione passato dal controller
            // ---------------------------------------------------------
            var tutti = GetClienti(anno, mese, null, fatturata);

            // 2. Filtra per tipo pagamento (30/60/90)
            var filtratiPerPagamento = tutti
                .Where(r => r.TipoPagamento == tableId)
                .ToList();

            // 3. Restituisce tutte le righe dell'agente
            return filtratiPerPagamento;
        }


    }
}
