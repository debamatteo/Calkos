USE [Calkos]
GO

-- Rimuovo l'indice filtrato parziale
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'F_IX_ProspettoCobral_ActiveRecords' AND object_id = OBJECT_ID('dbo.ProspettoCobral'))
    DROP INDEX [F_IX_ProspettoCobral_ActiveRecords] ON [dbo].[ProspettoCobral];

-- Rimuovo l'indice Agente/Mandatario (sarà coperto dal nuovo indice Operativo)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProspettoCobral_Agente_Mandatario' AND object_id = OBJECT_ID('dbo.ProspettoCobral'))
    DROP INDEX [IX_ProspettoCobral_Agente_Mandatario] ON [dbo].[ProspettoCobral];

-- Rimuovo l'indice Anno/Mandatario/Fatturata
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProspettoCobral_Anno_Mandatario_Fatturata' AND object_id = OBJECT_ID('dbo.ProspettoCobral'))
    DROP INDEX [IX_ProspettoCobral_Anno_Mandatario_Fatturata] ON [dbo].[ProspettoCobral];

-- Rimuovo il vecchio indice DataModifica (lo rifaremo ottimizzato per l'ORDER BY DESC)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProspettoCobral_DataModifica_DESC' AND object_id = OBJECT_ID('dbo.ProspettoCobral'))
    DROP INDEX [IX_ProspettoCobral_DataModifica_DESC] ON [dbo].[ProspettoCobral];

-- Rimuovo l'indice Fatturazione Certificata
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProspettoCobral_Fatturazione_Certificata' AND object_id = OBJECT_ID('dbo.ProspettoCobral'))
    DROP INDEX [IX_ProspettoCobral_Fatturazione_Certificata] ON [dbo].[ProspettoCobral];

-- Rimuovo l'indice Report Pivot (sarà assorbito dal nuovo indice Operativo)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProspettoCobral_Report_Pivot' AND object_id = OBJECT_ID('dbo.ProspettoCobral'))
    DROP INDEX [IX_ProspettoCobral_Report_Pivot] ON [dbo].[ProspettoCobral];

-- Rimuovo l'indice IsDeleted se ancora presente
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProspettoCobral_IsDeleted' AND object_id = OBJECT_ID('dbo.ProspettoCobral'))
    DROP INDEX [IX_ProspettoCobral_IsDeleted] ON [dbo].[ProspettoCobral];
GO


USE [Calkos]
GO

-- 1. INDICE OPERATIVO FILTRATO (Il più importante per GetAllFiltri e Report)
-- Copre: IdMandatario, Anno, Mese, Fatturata. Esclude i cancellati.
CREATE NONCLUSTERED INDEX [IDX_Cobral_Operativo_Active]
ON [dbo].[ProspettoCobral] (IdMandatario, Anno, Mese, Fatturata)
INCLUDE (IdCliente, IdAgente, IdFileImportato, Quantita, ValoreCommissioni, PrezzoVendita, Differenza)
WHERE IsDeleted = 0;
GO

-- 2. INDICE DI AUDIT E ORDINAMENTO (Per velocizzare l'ORDER BY DataModifica DESC)
-- Evita l'operazione di "Sort" nelle griglie dell'applicazione.
CREATE NONCLUSTERED INDEX [IDX_Cobral_DataModifica_Sort]
ON [dbo].[ProspettoCobral] (DataModifica DESC)
INCLUDE (IdProspettoCobral, IdCliente, NumeroFattura, Utente);
GO

-- 3. INDICE DOCUMENTALE (Per ricerche veloci per Fattura o Ordine)
-- Utile per quando l'utente cerca un documento specifico.
CREATE NONCLUSTERED INDEX [IDX_Cobral_Documenti]
ON [dbo].[ProspettoCobral] (NumeroFattura, NumeroOrdine)
INCLUDE (IdCliente, DataInserimento);
GO

-- 4. INDICE DI RELAZIONE (Per velocizzare i JOIN nelle VIEW)
-- Ottimizza il collegamento tra Clienti e Prospetto Cobral.
CREATE NONCLUSTERED INDEX [IDX_Cobral_FK_Cliente]
ON [dbo].[ProspettoCobral] (IdCliente, IsDeleted)
INCLUDE (IdMandatario, Anno, Mese, ValoreCommissioni);
GO