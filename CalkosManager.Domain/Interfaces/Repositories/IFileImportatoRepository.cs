using CalkosManager.Domain.Entities;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    public interface IFileImportatoRepository
    {
        int Insert(FileImportato file);

        // Versione transazionale per l'inserimento
        int Insert(FileImportato file, SqlConnection conn, SqlTransaction tran);

        // 12/04/2026 Aggiunto per supportare la ri-importazione nello Scenario C
        void Update(FileImportato file, SqlConnection conn, SqlTransaction tran);

        FileImportato? GetById(int id);// Ritorna un file importato tramite ID

        IEnumerable<FileImportato> GetByMandatario(int idMandatario);// Ritorna tutti i file importati per un Mandatario
    }
}


