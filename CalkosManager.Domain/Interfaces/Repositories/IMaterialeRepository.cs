using System.Data;
using CalkosManager.Domain.Entities;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repository per la gestione dei materiali.
    /// Basato sulla tabella SQL [Materiali].
    /// </summary>
    public interface IMaterialeRepository
    {
        /// <summary>
        /// Restituisce un materiale tramite il codice univoco.
        /// </summary>
        Materiale? GetByCodice(string codiceMateriale);

        /// <summary>
        /// Restituisce un materiale tramite la descrizione esatta.
        /// </summary>
        Materiale? GetByDescrizione(string descrizioneMateriale);

        /// <summary>
        /// Inserisce un nuovo materiale e restituisce l'entità con Id valorizzato.
        /// </summary>
        Materiale Insert(Materiale materiale);

        /// <summary>
        /// Inserisce un nuovo materiale all'interno di una transazione esistente.
        /// Usato durante importazioni o operazioni batch.
        /// </summary>
        Materiale Insert(Materiale materiale, IDbConnection connection, IDbTransaction transaction);

        /// <summary>
        /// Aggiorna i dati di un materiale esistente.
        /// </summary>
        void Update(Materiale materiale);

        /// <summary>
        /// Elimina un materiale tramite ID.
        /// </summary>
        void Delete(int id);

        /// <summary>
        /// Restituisce tutti i materiali presenti in tabella.
        /// </summary>
        IEnumerable<Materiale> GetAll();

        /// <summary>
        /// Restituisce un materiale tramite ID.
        /// </summary>
        Materiale? GetById(int id);

    }
}




//namespace CalkosManager.Domain.Interfaces.Repositories
//{
//    /// <summary>
//    /// Repository per la gestione dei materiali.
//    /// Basato sui campi della tabella:
//    /// - IdMateriale
//    /// - CodiceMateriale (univoco)
//    /// - DescrizioneMateriale
//    /// </summary>
//    public interface IMaterialeRepository
//    {
//        /// <summary>
//        /// Restituisce un materiale tramite il codice univoco.
//        /// </summary>
//        Materiale? GetByCodice(string codiceMateriale);

//        /// <summary>
//        /// Restituisce un materiale tramite la descrizione esatta.
//        /// </summary>
//        Materiale? GetByDescrizione(string descrizioneMateriale);

//        /// <summary>
//        /// Inserisce un nuovo materiale e restituisce l'entità con Id valorizzato.
//        /// </summary>
//        Materiale Insert(Materiale materiale);

//        /// <summary>
//        /// Inserisce un nuovo materiale all'interno di una transazione esistente.
//        /// Serve solo se durante l'importazione devi creare materiali mancanti.
//        /// </summary>
//        Materiale Insert(Materiale materiale, IDbConnection connection, IDbTransaction transaction);


//        /// <summary>
//        /// Seleziona tutti i Materiali presenti in tabella.
//        /// </summary>
//        IEnumerable<Materiale> GetAll();

//    }
//}