CREATE NONCLUSTERED INDEX [IDX_Hitech_Operativo_Filtri]
ON [dbo].[ProspettoHitech] (IsDeleted, Anno, Mese, Fatturata, IdMandatario)
INCLUDE (IdCliente, IdAgente, NumeroFattura, TotaleProvvigione, ProvvigioneAgente, PrezzoPraticato, DataDoc)
ON [PRIMARY];
GO
CREATE NONCLUSTERED INDEX [IDX_Hitech_FileImportato]
ON [dbo].[ProspettoHitech] (IdFileImportato)
INCLUDE (IdCliente, NumeroFattura, TotaleProvvigione)
ON [PRIMARY];
GO
CREATE NONCLUSTERED INDEX [IDX_Hitech_Join_ClientiCompleti]
ON [dbo].[ProspettoHitech] (IdCliente, IdMandatario, IsDeleted)
INCLUDE (NumeroFattura, TotaleProvvigione, DataConsegna, DataDoc)
ON [PRIMARY];
GO
CREATE NONCLUSTERED INDEX [IDX_Hitech_Ordinamento_DataModifica]
ON [dbo].[ProspettoHitech] (DataModifica DESC)
INCLUDE (IdProspettoHitech, Anno, Mese, IdCliente)
ON [PRIMARY];
GO