using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CalkosManager.Domain.Entities
{
    public class Cliente
    {
        [Key]
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "La ragione sociale è obbligatoria")]
        [StringLength(150, ErrorMessage = "La Ragione Sociale non può superare i 150 caratteri")]
        public string RagioneSociale { get; set; } = string.Empty;

        [StringLength(16, ErrorMessage = "Il Codice Fiscale deve essere di 16 caratteri")]
        public string? CodiceFiscale { get; set; }

        [StringLength(11, ErrorMessage = "La Partita IVA deve essere di 11 caratteri")]
        public string? PartitaIva { get; set; }

        #region SEDE LEGALE (Campi Storici + Nazione)

        [Display(Name = "Sede Legale - Indirizzo")]
        [StringLength(255, ErrorMessage = "L'indirizzo della sede legale non può superare i 255 caratteri")]
        public string? Indirizzo { get; set; }

        [Display(Name = "Sede Legale - Città")]
        [StringLength(100, ErrorMessage = "La città della sede legale non può superare i 100 caratteri")]
        public string? Citta { get; set; }

        [Display(Name = "Sede Legale - Provincia")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "La provincia della sede legale deve essere di 2 caratteri")]
        public string? Provincia { get; set; }

        [Display(Name = "Sede Legale - CAP")]
        [StringLength(5, MinimumLength = 5, ErrorMessage = "Il CAP della sede legale deve essere di 5 caratteri")]
        public string? CAP { get; set; }

        [Display(Name = "Sede Legale - Nazione")]
        [StringLength(100, ErrorMessage = "La nazione della sede legale non può superare i 100 caratteri")]
        public string? Nazione { get; set; } = "Italia";

        #endregion

        #region SEDE OPERATIVA (Nuovi Campi)

        [Display(Name = "Sede Operativa - Indirizzo")]
        [StringLength(255, ErrorMessage = "L'indirizzo della sede operativa non può superare i 255 caratteri")]
        public string? SO_Indirizzo { get; set; }

        [Display(Name = "Sede Operativa - Città")]
        [StringLength(100, ErrorMessage = "La città della sede operativa non può superare i 100 caratteri")]
        public string? SO_Citta { get; set; }

        [Display(Name = "Sede Operativa - Provincia")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "La provincia della sede operativa deve essere di 2 caratteri")]
        public string? SO_Provincia { get; set; }

        [Display(Name = "Sede Operativa - CAP")]
        [StringLength(5, MinimumLength = 5, ErrorMessage = "Il CAP della sede operativa deve essere di 5 caratteri")]
        public string? SO_CAP { get; set; }

        [Display(Name = "Sede Operativa - Nazione")]
        [StringLength(100, ErrorMessage = "La nazione della sede operativa non può superare i 100 caratteri")]
        public string? SO_Nazione { get; set; } = "Italia";

        // =========================================================================
        // PROPRIETÀ CALCOLATE PER FALLBACK AUTOMATICO (Ideali per DDT e Spedizioni)
        // =========================================================================
        public string? IndirizzoOperativoEffettivo => string.IsNullOrWhiteSpace(SO_Indirizzo) ? Indirizzo : SO_Indirizzo;
        public string? CittaOperativaEffettiva => string.IsNullOrWhiteSpace(SO_Citta) ? Citta : SO_Citta;
        public string? ProvinciaOperativaEffettiva => string.IsNullOrWhiteSpace(SO_Provincia) ? Provincia : SO_Provincia;
        public string? CAPOperativoEffettivo => string.IsNullOrWhiteSpace(SO_CAP) ? CAP : SO_CAP;
        public string? NazioneOperativaEffettiva => string.IsNullOrWhiteSpace(SO_Nazione) ? Nazione : SO_Nazione;


        #endregion

        #region RECAPITI E NOTE

        [EmailAddress(ErrorMessage = "Formato Email non valido")]
        [StringLength(100, ErrorMessage = "L'Email non può superare i 100 caratteri")]
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "Il telefono non può superare i 20 caratteri")]
        public string? Telefono { get; set; }

        // Note è nvarchar(max), non necessita di StringLength
        public string? Note { get; set; }

        #endregion





        #region REFERENTE
        //21/10/2026 - Referente
        [Display(Name = "Referente")]
        [StringLength(150, ErrorMessage = "Il Referente non può superare i 150 caratteri")]
        public string? Referente { get; set; }

        [Display(Name = "Telefono Referente")]
        [StringLength(20, ErrorMessage = "Il telefono del referente non può superare i 20 caratteri")]
        public string? TelefonoReferente { get; set; }

        [Display(Name = "Email Referente")]
        [EmailAddress(ErrorMessage = "Formato Email non valido")]
        [StringLength(100, ErrorMessage = "L'Email del referente non può superare i 100 caratteri")]
        public string? EmailReferente { get; set; }

        [Display(Name = "Note Referente")]
        [StringLength(500, ErrorMessage = "Le note del referente non possono superare i 500 caratteri")]
        public string? NoteReferente { get; set; }
        #endregion









        #region METADATI E CONFIGURAZIONI

        [Display(Name = "Agente")]
        public int? IdAgente { get; set; }

        [Required]
        public DateTime DataInserimento { get; set; }

        public DateTime? DataModifica { get; set; }

        [StringLength(100)]
        public string? Utente { get; set; }

        // AGGIUNTA PER SOFT DELETE
        public bool IsDeleted { get; set; }

        [Display(Name = "Tipo Pagamento Predefinito")]
        public int? IdTipoPagamentoDefault { get; set; } // 26/08/2026

        // 14/10/2026 Campo SOLO FORM: NON è una colonna di Clienti.
        // L'associazione Cliente <-> Mandatario vive nella tabella MandatariClienti
        // (letta/scritta da ClienteRepository.GetIdMandatarioByCliente / SetMandatarioCliente).
        [Display(Name = "Mandatario")]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int? IdMandatario { get; set; }


        // 16/10/2026 Campi SOLO LETTURA per la lista clienti: descrizioni risolte con LEFT JOIN
        // da spClienti_GetAll / spClienti_GetDeleted. NON sono colonne di Clienti e non vengono mai scritti.
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? AgenteDescrizione { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? TipoPagamentoDescrizione { get; set; }


        #endregion
    }
}