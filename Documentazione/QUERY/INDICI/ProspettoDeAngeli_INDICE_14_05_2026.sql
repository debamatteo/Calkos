USE [Calkos]
GO

-- 1. INDICE OPERATIVO FILTRATO (Il "motore" della spProspettoDeAngeli_GetAllFiltri)
-- Questo indice copre i filtri più comuni e include i campi calcolati per evitare Lookup sulla tabella.
CREATE NONCLUSTERED INDEX [IDX_DeAngeli_Operativo_Active]
ON [dbo].[ProspettoDeAngeli] (IdMandatario, Anno, Mese, Fatturata)
INCLUDE (IdCliente, IdAgente, IdFileImportato, Quantita, TotaleProvvigioneDaFatturare, Differenza, ProvvigioneAgente)
WHERE IsDeleted = 0;
GO

-- 2. INDICE DI ORDINAMENTO (Per spProspettoDeAngeli_GetAll)
-- Ottimizza la visualizzazione cronologica invertita eliminando il carico sulla CPU per l'ordinamento.
CREATE NONCLUSTERED INDEX [IDX_DeAngeli_DataModifica_Sort]
ON [dbo].[ProspettoDeAngeli] (DataModifica DESC)
INCLUDE (IdProspettoDeAngeli, IdCliente, NumeroFattura, Utente);
GO

-- 3. INDICE DI INTEGRAZIONE ANAGRAFICA (Per vProspettoDeAngeliClientiCompleti)
-- Ottimizza il JOIN basato su Cliente e Mandatario usato nelle griglie gestionali.
CREATE NONCLUSTERED INDEX [IDX_DeAngeli_RelazioneCliente]
ON [dbo].[ProspettoDeAngeli] (IdCliente, IdMandatario, IsDeleted)
INCLUDE (NumeroOrdine, NumeroFattura, TotaleProvvigioneDaFatturare);
GO

-- 4. INDICE PER IMPORTAZIONE E RICERCA (Per spProspettoDeAngeli_GetByFile)
-- Accelera il recupero dei dati relativi a un singolo caricamento file.
CREATE NONCLUSTERED INDEX [IDX_DeAngeli_FileImport]
ON [dbo].[ProspettoDeAngeli] (IdFileImportato)
INCLUDE (IdProspettoDeAngeli, IdCliente, NumeroFattura);
GO