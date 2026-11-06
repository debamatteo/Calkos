
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalkosManager.Application.Services
{
    public class CalcoloBaseMaterialeService
    {
        private readonly ICalcoloBaseMaterialeRepository _repo;

        public CalcoloBaseMaterialeService(ICalcoloBaseMaterialeRepository repo)
        {
            _repo = repo;
        }

        public List<CalcoloBaseDto> GetStorico(int idMateriale, int top = 4)
        {
            return _repo.GetStorico(idMateriale, top)
                .Select(ToDto)
                .ToList();
        }

        public CalcoloBaseDto? GetUltimoAttivo(int idMateriale)
        {
            var entity = _repo.GetUltimoAttivo(idMateriale);
            return entity == null ? null : ToDto(entity);
        }

        public CalcoloBaseDto Salva(CreateCalcoloBaseRequestDto dto, string utente)
        {
            var entity = new CalcoloBaseMateriale
            {
                IdMateriale = dto.IdMateriale,
                DataRiferimento = dto.DataRiferimento,
                Lme = dto.Lme,
                PremioMiniera = dto.PremioMiniera,
                Cambio = dto.Cambio,
                PercFinanziamento = dto.PercFinanziamento,
                EuroFinanziamento = dto.EuroFinanziamento,
                MaterialeEuroRiferimento = dto.MaterialeEuroRiferimento,
                Utente = utente
            };

            int id = _repo.Insert(entity);
            var salvato = _repo.GetById(id)
                ?? throw new Exception("Errore nel recupero del CalcoloBase appena salvato.");

            return ToDto(salvato);
        }

        private static CalcoloBaseDto ToDto(CalcoloBaseMateriale e) => new CalcoloBaseDto
        {
            Id = e.Id,
            IdMateriale = e.IdMateriale,
            DescrizioneMateriale = e.DescrizioneMateriale,
            DataRiferimento = e.DataRiferimento,
            Lme = e.Lme,
            PremioMiniera = e.PremioMiniera,
            Cambio = e.Cambio,
            PercFinanziamento = e.PercFinanziamento,
            EuroFinanziamento = e.EuroFinanziamento,
            MaterialeEuroRiferimento = e.MaterialeEuroRiferimento
        };


        public (bool Eliminato, string Messaggio) Elimina(int id)//28/08/2026
        {
            try
            {
                return _repo.Delete(id);
            }
            catch (Exception ex)
            {
                // Cattura eventuali errori SQL (es. deadlock timeout) e li traduce
                // in un messaggio gestibile invece di far esplodere il controller.
                return (false, "Errore durante la cancellazione: " + ex.Message);
            }
        }

    }
}