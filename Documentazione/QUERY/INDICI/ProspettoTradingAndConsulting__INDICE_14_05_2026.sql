CREATE NONCLUSTERED INDEX [IDX_TradingAndConsulting_Operativo_Filtri]
ON [dbo].[ProspettoTradingAndConsulting] (IdMandatario, IsDeleted, Anno, Mese, Fatturata)
INCLUDE (IdCliente, IdAgente, TotaleProvvigioneFattura, Quantita, PrezzoVendita, Delta, CIT)
ON [PRIMARY];
GO
CREATE NONCLUSTERED INDEX [IDX_TradingAndConsulting_Cliente_Mandatario]
ON [dbo].[ProspettoTradingAndConsulting] (IdCliente, IdMandatario, IsDeleted)
INCLUDE (NumeroOrdine, NumeroFattura, TotaleProvvigioneFattura, DataConsegna)
ON [PRIMARY];
GO
CREATE NONCLUSTERED INDEX [IDX_TradingAndConsulting_DataModifica_Sort]
ON [dbo].[ProspettoTradingAndConsulting] (DataModifica DESC)
INCLUDE (IdProspettoTradingAndConsulting, Utente, NumeroFattura)
ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IDX_TradingAndConsulting_Importazione]
ON [dbo].[ProspettoTradingAndConsulting] (IdFileImportato)
INCLUDE (IdProspettoTradingAndConsulting, IdImportTradingAndConsulting, IdCliente)
ON [PRIMARY];
GO