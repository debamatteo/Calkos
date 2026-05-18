CREATE NONCLUSTERED INDEX [IDX_SystemCore_Grid_Performance]
ON [dbo].[ProspettoSystemCore] (IsDeleted, IdMandatario, Anno, Mese, Fatturata)
INCLUDE (
    IdCliente, IdAgente, NumeroFattura, DataModifica,
    TotaleProvvigione, TotaleProvvigioneDaFatturare, 
    Delta, ProvvigioneAgente, IsProcessed
)
ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IDX_SystemCore_View_Clienti]
ON [dbo].[ProspettoSystemCore] (IdCliente, IdMandatario, IsDeleted)
INCLUDE (NumeroFattura, DataConsegna, TotaleProvvigione, IdMateriale)
ON [PRIMARY];
GO


CREATE NONCLUSTERED INDEX [IDX_SystemCore_FileImport]
ON [dbo].[ProspettoSystemCore] (IdFileImportato)
INCLUDE (IdProspettoSystemCore, IdCliente, NumeroFattura, TotaleProvvigione)
ON [PRIMARY];
GO


CREATE NONCLUSTERED INDEX [IDX_SystemCore_OrderBy_DataModifica]
ON [dbo].[ProspettoSystemCore] (DataModifica DESC)
ON [PRIMARY];
GO