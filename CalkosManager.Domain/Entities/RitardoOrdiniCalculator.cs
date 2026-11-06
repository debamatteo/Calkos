/*
    ============================================================
    LOGICA GENERALE DEL MODULO RITARDO ORDINI
    ============================================================

    • Questa classe contiene la REGOLA UNICA di calcolo del ritardo,
      valida per tutti i 9 mandatari del sistema.

    • La logica è completamente indipendente dal database:
      lavora solo su un'interfaccia (IRigaConDate) che espone
      le date necessarie al calcolo.

    • La classe calcola due informazioni fondamentali:
        - InRitardo: indica se l’ordine deve essere considerato in ritardo.
        - GiorniRitardo: indica quanti giorni di ritardo ha accumulato.

    • Motivo della scelta architetturale:
        - Centralizzare la logica in una classe statica evita duplicazioni
          e garantisce che tutti i mandatari usino la stessa regola.
        - La logica è riutilizzabile, testabile e non dipende da SQL.
        - Applica la "Opzione A": se l’ordine è ancora aperto (senza DataConsegna),
          il ritardo viene calcolato rispetto alla data odierna.

    • Regole principali:
        1. Se l’ordine è escluso dal controllo → mai in ritardo.
        2. Se l’ordine è stato inserito prima dell’attivazione del modulo → ignorato.
        3. Se l’ordine è già consegnato → non è in ritardo.
        4. Se manca la DataConsegnaIpotetica → non valutabile.
        5. Se la DataConsegnaIpotetica è minore di oggi → ordine in ritardo.

    ============================================================
*/

using CalkosManager.Domain.Interfaces;
using Humanizer;
using System;
using System.Runtime.ConstrainedExecution;

namespace CalkosManager.Domain.Entities
{
    /// <summary>
    /// Regola unica di calcolo del ritardo, condivisa da tutti i 9 mandatari.
    /// Riceve una riga che implementa IRigaConDate e la data di attivazione modulo,
    /// valorizzando InRitardo e GiorniRitardo.
    /// </summary>
    public static class RitardoOrdiniCalculator
    {
        /// <summary>
        /// Metodo principale: calcola sia la flag InRitardo che i GiorniRitardo.
        /// </summary>
        public static void Calcola(IRigaConDate riga, DateTime dataAttivazioneModulo, DateTime? oggi = null)
        {

            //dataAttivazioneModulo= È la data in cui è stato attivato il controllo ritardi per quel mandatario.
            //In pratica:Prima di quella data NON devi calcolare ritardi. Dopo quella data sì.
            //Perché se hai attivato il modulo ritardi il 01/09/2026,
            //un ordine inserito il 20/08/2026 NON deve essere considerato in ritardo.
            //Non esisteva ancora il controllo.


            /*  adesso=spiegazione
                • Il calcolo del ritardo deve basarsi su una "data di riferimento"=adesso. 
                Normalmente questa data è OGGI (la data corrente del sistema).
                • MA nei test automatici o simulazioni possiamo voler usare una data diversa (passata tramite parametro 'oggi').
                Quindi:  - Se mi passi una data → uso quella. - Se NON mi passi una data → uso la data di oggi del sistema.
                ------------------------------------------------------------
            */
            var adesso = (oggi ?? DateTime.Today).Date;

            /* • Determina se l’ordine deve essere considerato "in ritardo". • Usa la regola definita nel metodo EInRitardo.            */
            riga.InRitardo = EInRitardo(riga, dataAttivazioneModulo, adesso);

            /*
                • Calcola quanti giorni di ritardo ha accumulato l’ordine.
                • Se non è in ritardo → ritorna null.
                • Se è consegnato → calcola ritardo consuntivo.
                • Se è aperto → calcola ritardo rispetto ad oggi.
                ------------------------------------------------------------
            */
            //riga.GiorniRitardo = GiorniDiRitardo(riga, adesso);
            riga.GiorniRitardo = GiorniDiRitardo(riga, dataAttivazioneModulo, adesso);   // <-- 05/10/2026 aggiunto dataAttivazioneModulo
        }


        /// <summary>
        /// Regola che stabilisce se un ordine è in ritardo.
        /// </summary>
        private static bool EInRitardo(IRigaConDate riga, DateTime dataAttivazioneModulo, DateTime oggi)
        {
            // 1. L’utente ha escluso manualmente il controllo → mai in ritardo
            if (riga.EscludiControlloRitardo) return false;

            // 2. Ordine inserito prima dell’attivazione del modulo → ignorato
            if (riga.DataInserimento.Date < dataAttivazioneModulo.Date) return false;

            // 3. Ordine già consegnato → non è in ritardo
            if (riga.DataConsegna.HasValue) return false;

            // 4. Nessuna data ipotetica → impossibile valutare ritardo
            if (!riga.DataConsegnaIpotetica.HasValue) return false;

            // 5. Opzione A: ordine aperto con data ipotetica superata → in ritardo
            return riga.DataConsegnaIpotetica.Value.Date < oggi;
        }

        /// <summary>
        /// Calcola i giorni di ritardo dell’ordine.
        /// </summary>
        private static int? GiorniDiRitardo(IRigaConDate riga, DateTime dataAttivazioneModulo, DateTime oggi)
        {
            // 05/10/2026 Escluso dal controllo o precedente all'attivazione → nessun ritardo calcolabile
            if (riga.EscludiControlloRitardo) return null;
            if (riga.DataInserimento.Date < dataAttivazioneModulo.Date) return null;

            // Se manca la data ipotetica → nessun calcolo possibile
            if (!riga.DataConsegnaIpotetica.HasValue) return null;

            // Caso 1: ordine già consegnato → ritardo consuntivo
            if (riga.DataConsegna.HasValue)
                return (riga.DataConsegna.Value.Date - riga.DataConsegnaIpotetica.Value.Date).Days;

            // Caso 2: ordine aperto → ritardo calcolato rispetto ad oggi
            return riga.InRitardo
                ? (oggi - riga.DataConsegnaIpotetica.Value.Date).Days
                : null;
        }
    }
}
