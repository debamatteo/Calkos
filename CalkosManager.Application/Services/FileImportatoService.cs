using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;

namespace CalkosManager.Application.Services
{
    /// <summary>Gestisce la logica applicativa relativa ai file Excel importati.</summary>
    public class FileImportatoService
    {
        private readonly IFileImportatoRepository _fileRepo;

        public FileImportatoService(IFileImportatoRepository fileRepo)
        {
            _fileRepo = fileRepo;
        }

        /// <summary>Registra un nuovo file importato e ritorna l'ID generato.</summary>
        public int RegistraFile(FileImportato file)
        {
            return _fileRepo.Insert(file);
        }

        /// <summary>Ritorna un file importato tramite ID.</summary>
        public FileImportato? GetById(int id)
        {
            return _fileRepo.GetById(id);
        }

        /// <summary>Ritorna tutti i file importati per un mandatario.</summary>
        public IEnumerable<FileImportato> GetByMandatario(int idMandatario)
        {
            return _fileRepo.GetByMandatario(idMandatario);
        }
    }
}
