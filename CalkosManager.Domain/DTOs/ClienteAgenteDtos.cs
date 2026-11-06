namespace CalkosManager.Application.DTOs
{
    // ==== MODIFICA 30/10/2026 - INIZIO ====
    // Nuovo file: tipi di ritorno della creazione cliente da parte di un Agente e dell'avviso "nomi simili".

    /// <summary>
    /// 30/10/2026 Esito di spClienti_InsertDaAgente (colonna "Esito" della stored procedure).
    /// </summary>
    public enum EsitoCreazioneCliente
    {
        Ok,
        GiaTuo,              // esiste già ed è tra i clienti dell'agente (si può aprire)
        EsisteNonVisibile,   // esiste già ma non è dell'agente (nessun dato rivelato)
        Archiviato,          // esiste già ed è nel Cestino (nessun dato rivelato)
        Duplicato,           // violazione indice unico (due salvataggi contemporanei)
        NoPercentuale,       // l'agente non ha Agenti.PercentualeDefault
        AgenteNonValido,
        MandatarioNonValido,
        NomeNonValido
    }

    public class EsitoCreazioneClienteDto
    {
        public EsitoCreazioneCliente Esito { get; set; }
        public int? IdCliente { get; set; }
    }

    /// <summary>
    /// 30/10/2026 Riga dell'avviso "nomi simili". Se Visibile = false, IdCliente e RagioneSociale sono null
    /// (l'agente non deve ricevere dati di clienti che non gli appartengono).
    /// </summary>
    public class ClienteSimileDto
    {
        public int? IdCliente { get; set; }
        public string? RagioneSociale { get; set; }
        public bool Visibile { get; set; }
        public bool Archiviato { get; set; }
    }
    // ==== MODIFICA 30/10/2026 - FINE ====
}
