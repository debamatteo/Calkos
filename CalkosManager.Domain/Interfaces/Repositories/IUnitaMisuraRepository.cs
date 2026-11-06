using CalkosManager.Domain.Entities;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repository per la gestione delle unità di misura.
    /// </summary>
    public interface IUnitaMisuraRepository
    {
        /// <summary>
        /// Recupera tutte le unità di misura.
        /// </summary>
        List<UnitaMisura> GetAll();

        /// <summary>
        /// Recupera l'unità di misura tramite codice.
        /// </summary>
        UnitaMisura? GetByCodice(string codiceUnitaMisura);

        /// <summary>
        /// Recupera l'unità di misura tramite descrizione.
        /// </summary>
        UnitaMisura? GetByDescrizione(string descrizione);

        /// <summary>
        /// Inserimento manuale (nuova istanza).
        /// </summary>
        void InsertManuale(UnitaMisura unitaMisura);

        /// <summary>
        /// Aggiornamento unità di misura esistente.
        /// </summary>
        void Update(UnitaMisura unitaMisura);

        /// <summary>
        /// Eliminazione tramite ID.
        /// </summary>
        void Delete(int id);

        /// <summary>
        /// Inserimento all'interno di una transazione esistente.
        /// </summary>
        UnitaMisura Insert(UnitaMisura item, SqlConnection conn, SqlTransaction tran);
    }
}