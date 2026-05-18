CREATE NONCLUSTERED INDEX [IDX_EW_Operativo_Filtri]
ON [dbo].[ProspettoElektraWire] (IsDeleted, Anno, Mese, Fatturata, IdMandatario)
INCLUDE (IdCliente, IdAgente, NumeroFattura, TotaleProvvigioneDaFatturare, TotaleSommaProvvigioni, Quantita, PrezzoVendita)
ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IDX_EW_Importazione]
ON [dbo].[ProspettoElektraWire] (IdFileImportato)
INCLUDE (IdProspettoElektraWire, IdCliente, NumeroOrdine, NumeroFattura)
ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IDX_EW_Cliente_Mandatario]
ON [dbo].[ProspettoElektraWire] (IdCliente, IdMandatario)
INCLUDE (NumeroFattura, DataConsegna, TotaleProvvigioneDaFatturare, IsDeleted)
ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IDX_EW_DataModifica_Sort]
ON [dbo].[ProspettoElektraWire] (DataModifica DESC)
INCLUDE (IdProspettoElektraWire, Utente, Anno, Mese)
ON [PRIMARY];
GO