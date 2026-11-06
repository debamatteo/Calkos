using CalkosManager.Application.DTO.Statistiche;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Domain.Entities;

namespace CalkosManager.Application.Services
{
    /// <summary>
    /// Statistiche Globali: aggrega il fatturato di TUTTI i mandatari.
    /// - legge i dati "lunghi" (mandatario/anno/mese) dal repository;
    /// - completa l'elenco con i mandatari senza movimenti (valori a zero);
    /// - fa il pivot per mese (Report 1) o per anno (Report 2).
    /// Non conosce Excel: produce DTO che l'export trasforma in file.
    /// </summary>
    public class StatisticheGlobaliService
    {
        public const int AnnoMinimo = 2000;
        public const int MassimoAnniConfronto = 10;
        public const string UnitaMisuraMista = "MISTO";

        private readonly IStatisticheGlobaliRepository _statisticheRepository;
        private readonly IMandatarioRepository _mandatarioRepository;

        public StatisticheGlobaliService(
            IStatisticheGlobaliRepository statisticheRepository,
            IMandatarioRepository mandatarioRepository)
        {
            _statisticheRepository = statisticheRepository;
            _mandatarioRepository = mandatarioRepository;
        }

        // =====================================================================
        //  VALIDAZIONE (unica fonte di verità: usata da ViewModel e Service)
        // =====================================================================
        /// <summary>
        /// Restituisce l'elenco degli errori della richiesta (vuoto = richiesta valida).
        /// <paramref name="annoMassimo"/> è passato dal chiamante per rendere il metodo testabile.
        /// </summary>
        public static IReadOnlyList<string> ValidaRichiesta(
            TipoReportStatistiche? tipo, int? anno, int? annoDa, int? annoA, int annoMassimo)
        {
            var errori = new List<string>();

            if (tipo is null || !Enum.IsDefined(tipo.Value))
            {
                errori.Add("Selezionare il tipo di report.");
                return errori;
            }

            bool AnnoValido(int a) => a >= AnnoMinimo && a <= annoMassimo;
            string MessaggioRange(string etichetta) =>
                $"{etichetta} deve essere compreso tra {AnnoMinimo} e {annoMassimo}.";

            if (tipo == TipoReportStatistiche.FatturatoAnnuale)
            {
                if (anno is null) errori.Add("Selezionare l'anno.");
                else if (!AnnoValido(anno.Value)) errori.Add(MessaggioRange("L'anno"));
                return errori;
            }

            // TipoReportStatistiche.ConfrontoAnni
            if (annoDa is null || annoA is null)
            {
                errori.Add("Selezionare l'anno iniziale e l'anno finale.");
                return errori;
            }
            if (!AnnoValido(annoDa.Value)) errori.Add(MessaggioRange("L'anno iniziale"));
            if (!AnnoValido(annoA.Value)) errori.Add(MessaggioRange("L'anno finale"));
            if (annoDa > annoA) errori.Add("L'anno iniziale non può essere successivo all'anno finale.");
            else if (annoA - annoDa + 1 > MassimoAnniConfronto)
                errori.Add($"Il confronto può coprire al massimo {MassimoAnniConfronto} anni.");

            return errori;
        }

        public static int AnnoMassimoConsentito() => DateTime.Today.Year + 1;

        // =====================================================================
        //  ANNI PER LE DROPDOWN
        // =====================================================================
        public IReadOnlyList<int> GetAnniDisponibili()
        {
            //Estrazione Anni Disponibili (GetAnniDisponibili): Recupera gli anni presenti nel database
            //per popolare le tendine/dropdown dei filtri nella vista.
            var anni = _statisticheRepository.GetAnniDisponibili();
            return anni.Count > 0 ? anni : new[] { DateTime.Today.Year };
        }

        // =====================================================================
        //  REPORT 1 - SINGOLO ANNO (pivot per mese)
        //  Generazione Report 1 (GetReportFatturatoAnnuale): Invocato per un singolo anno, estrae
        //  i dati dal repository, raggruppa e trasforma
        //  i dati mensili nei DTO per ciascun mandatario (con un array di 12 posizioni per i mesi).
        // =====================================================================
        public ReportFatturatoAnnualeDto GetReportFatturatoAnnuale(int anno)
        {
            LanciaSeNonValida(TipoReportStatistiche.FatturatoAnnuale, anno, null, null);

            var dati = _statisticheRepository.GetFatturatoMensile(anno, anno);
            var perMandatario = RaggruppaPerMandatario(dati);

            var righe = ElencoMandatari(perMandatario.Keys)
                .Select(m =>
                {
                    var righeMandatario = perMandatario.GetValueOrDefault(m.Id) ?? new List<FatturatoMensileMandatario>();
                    var quantita = new decimal[12];
                    var euro = new decimal[12];

                    foreach (var r in righeMandatario.Where(r => r.Mese is >= 1 and <= 12))
                    {
                        quantita[r.Mese - 1] += r.Quantita;
                        euro[r.Mese - 1] += r.EuroProvvigione;
                    }

                    return new RigaFatturatoAnnualeDto
                    {
                        IdMandatario = m.Id,
                        NomeMandatario = m.Nome,
                        UnitaMisura = UnitaMisuraDi(righeMandatario),
                        QuantitaMensile = quantita,
                        EuroMensile = euro
                    };
                })
                .ToList();

            // ==== MODIFICA 29/09/2026 - INIZIO ====
            var (unitaTotali, nonTotalizzati) = AnalizzaUnitaMisura(
                righe.Select(r => (r.NomeMandatario, r.UnitaMisura, HaQuantita: r.TotaleQuantita != 0)));

            return new ReportFatturatoAnnualeDto
            {
                Anno = anno,
                Righe = righe,
                UnitaMisuraTotali = unitaTotali,
                MandatariQuantitaNonTotalizzate = nonTotalizzati
            };
            // ==== MODIFICA 29/09/2026 - FINE ====
        }

        // =====================================================================
        //  REPORT 2 - CONFRONTO ANNI (pivot per anno)
        //  Generazione Report 2 (GetReportConfrontoAnni): Invocato per un intervallo di anni, estrae
        //  i dati dal repository, raggruppa e trasforma
        //  i dati annuali nei DTO per ciascun mandatario (con un array di N posizioni per gli anni).
        // =====================================================================
        public ReportConfrontoAnniDto GetReportConfrontoAnni(int annoDa, int annoA)
        {
            LanciaSeNonValida(TipoReportStatistiche.ConfrontoAnni, null, annoDa, annoA);

            var anni = Enumerable.Range(annoDa, annoA - annoDa + 1).ToList();
            var dati = _statisticheRepository.GetFatturatoMensile(annoDa, annoA);
            var perMandatario = RaggruppaPerMandatario(dati);

            var righe = ElencoMandatari(perMandatario.Keys)
                .Select(m =>
                {
                    var righeMandatario = perMandatario.GetValueOrDefault(m.Id) ?? new List<FatturatoMensileMandatario>();
                    var quantita = new decimal[anni.Count];
                    var euro = new decimal[anni.Count];

                    foreach (var r in righeMandatario)
                    {
                        int i = r.Anno - annoDa;
                        if (i < 0 || i >= anni.Count) continue; // difesa: fuori intervallo
                        quantita[i] += r.Quantita;
                        euro[i] += r.EuroProvvigione;
                    }

                    return new RigaConfrontoAnniDto
                    {
                        IdMandatario = m.Id,
                        NomeMandatario = m.Nome,
                        UnitaMisura = UnitaMisuraDi(righeMandatario),
                        QuantitaPerAnno = quantita,
                        EuroPerAnno = euro
                    };
                })
                .ToList();

            // ==== MODIFICA 29/09/2026 - INIZIO ====
            var (unitaTotali, nonTotalizzati) = AnalizzaUnitaMisura(
                righe.Select(r => (r.NomeMandatario, r.UnitaMisura, r.HaQuantita)));

            return new ReportConfrontoAnniDto
            {
                AnnoDa = annoDa,
                AnnoA = annoA,
                Anni = anni,
                Righe = righe,
                UnitaMisuraTotali = unitaTotali,
                MandatariQuantitaNonTotalizzate = nonTotalizzati
            };
            // ==== MODIFICA 29/09/2026 - FINE ====
        }

        // =====================================================================
        //  METODI DI SUPPORTO
        //  (ElencoMandatari, UnitaMisuraDi): Garantisce che anche i mandatari
        //  senza vendite/movimenti compaiano nei report con valori azzerati e
        //  gestisce l'omogeneità dell'Unità di Misura
        //  (o segnala "MISTO" se un mandatario gestisce diverse unità di misura).
        // =====================================================================
        private static void LanciaSeNonValida(TipoReportStatistiche tipo, int? anno, int? annoDa, int? annoA)
        {
            var errori = ValidaRichiesta(tipo, anno, annoDa, annoA, AnnoMassimoConsentito());
            if (errori.Count > 0)
                throw new ArgumentException(string.Join(" ", errori));
        }

        private static Dictionary<int, List<FatturatoMensileMandatario>> RaggruppaPerMandatario(
            IEnumerable<FatturatoMensileMandatario> dati) =>
            dati.GroupBy(d => d.IdMandatario).ToDictionary(g => g.Key, g => g.ToList());

        /// <summary>
        /// Tutti i mandatari censiti (anche senza movimenti) + eventuali Id presenti
        /// nei dati ma non più in anagrafica, ordinati per nome.
        /// </summary>
        //private List<(int Id, string Nome)> ElencoMandatari(IEnumerable<int> idConDati)
        //{
        //    var elenco = _mandatarioRepository.GetAll()
        //        .Select(m => (Id: m.IdMandatario, Nome: m.NomeMandatario))
        //        .ToList();

        //    var idCensiti = elenco.Select(m => m.Id).ToHashSet();
        //    elenco.AddRange(idConDati
        //        .Where(id => !idCensiti.Contains(id))
        //        .Select(id => (Id: id, Nome: $"Mandatario {id}")));

        //    return elenco
        //        .OrderBy(m => m.Nome, StringComparer.OrdinalIgnoreCase)
        //        .ToList();
        //}
        private List<(int Id, string Nome)> ElencoMandatari(IEnumerable<int> idConDati)//22/10/2026 - Modifica per gestire mandatari presenti solo nello storico
        {
            var elenco = _mandatarioRepository.GetAll()
                .Select(m => (Id: m.IdMandatario, Nome: m.NomeMandatario))
                .ToList();

            var idCensiti = elenco.Select(m => m.Id).ToHashSet();
            elenco.AddRange(idConDati
                .Where(id => !idCensiti.Contains(id))
                // ==== MODIFICA 22/10/2026 - INIZIO ====
                // Nome dal dizionario dei mandatari solo-storico; fallback generico per Id sconosciuti
                .Select(id => (Id: id, Nome: MandatariSoloStorico.GetValueOrDefault(id, $"Mandatario {id}"))));
            // ==== MODIFICA 22/10/2026 - FINE ====

            return elenco
                .OrderBy(m => m.Nome, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        /// <summary>Unica U.M. del mandatario nel periodo; "MISTO" se ce n'è più di una.</summary>
        private static string UnitaMisuraDi(IEnumerable<FatturatoMensileMandatario> righe)
        {
            var unita = righe
                .Select(r => r.UnitaMisura?.Trim() ?? "")
                .Where(u => u.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return unita.Count switch
            {
                0 => "",
                1 => unita[0],
                _ => UnitaMisuraMista
            };
        }

        // ==== MODIFICA 29/09/2026 - INIZIO ====
        // Sostituisce IsUnitaMisuraOmogenea: invece di decidere SE sommare le quantità,
        // stabilisce PER QUALI unità servono i totali separati (es. KG e PZ).
        /// <summary>
        /// Unità di misura da totalizzare (solo quelle dei mandatari con quantità, esclusi
        /// "" e "MISTO") e mandatari le cui quantità non possono entrare in alcun totale.
        /// </summary>
        private static (IReadOnlyList<string> Unita, IReadOnlyList<string> NonTotalizzati) AnalizzaUnitaMisura(
            IEnumerable<(string NomeMandatario, string UnitaMisura, bool HaQuantita)> righe)
        {
            var conQuantita = righe.Where(r => r.HaQuantita).ToList();

            bool Totalizzabile(string um) =>
                um.Length > 0 && !um.Equals(UnitaMisuraMista, StringComparison.OrdinalIgnoreCase);

            var unita = conQuantita
                .Select(r => r.UnitaMisura)
                .Where(Totalizzabile)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var nonTotalizzati = conQuantita
                .Where(r => !Totalizzabile(r.UnitaMisura))
                .Select(r => r.NomeMandatario)
                .ToList();

            return (unita, nonTotalizzati);
        }
        // ==== MODIFICA 29/09/2026 - FINE ====


        // ==== MODIFICA 22/10/2026 - INIZIO ====
        // Mandatari presenti SOLO nello storico (non esistono in dbo.Mandatari).
        // Gli Id >= 10000 sono riservati a questo uso: dbo.Mandatari usa IDENTITY e non li raggiungerà mai.
        // Se un mandatario diventa gestito, va riallineato l'Id nello storico (vedi punto 5).
        /*
                5. Se un domani diventano mandatari gestiti
                Dopo averli creati nel gestionale, riallinea gli Id storici. Il pivot li unisce poi da solo:
                sql
                DECLARE @NuovoId INT = (SELECT IdMandatario FROM dbo.Mandatari WHERE NomeMandatario = N'Green Metal');
                EXEC sp_set_session_context N'ConsentiModificaStorico', 1;
                UPDATE dbo.StoricoStatisticheFatturato SET IdMandatario = @NuovoId WHERE IdMandatario = 10000;
                EXEC sp_set_session_context N'ConsentiModificaStorico', 0;

                Stessa procedura per Altek con 11000. Non c’è conflitto sulla PK, perché il nuovo Id non ha righe per gli anni < 2026.
         */
        private static readonly IReadOnlyDictionary<int, string> MandatariSoloStorico =
            new Dictionary<int, string>
            {
                [10000] = "Green Metal",
                [11000] = "Altek"
            };
        // ==== MODIFICA 22/10/2026 - FINE ====


    }
}