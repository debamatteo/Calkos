CREATE NONCLUSTERED INDEX [IDX_Guerzoni_Operativo_Filtri]
ON [dbo].[ProspettoGuerzoni] (IsDeleted, Anno, Mese, Fatturata, IdMandatario)
INCLUDE (IdCliente, IdAgente, NumeroFattura, Imponibile, TotaleProvvigione, ProvvigioneAgente, Perc_Delta_Provvigione)
ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IDX_Guerzoni_Importazione]
ON [dbo].[ProspettoGuerzoni] (IdFileImportato)
INCLUDE (IdProspettoGuerzoni, IdCliente, NumeroFattura, Imponibile)
ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IDX_Guerzoni_Cliente_Join]
ON [dbo].[ProspettoGuerzoni] (IdCliente, IdMandatario, IsDeleted)
INCLUDE (NumeroFattura, Imponibile, TotaleProvvigione, DataConsegna)
ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IDX_Guerzoni_Ordinamento]
ON [dbo].[ProspettoGuerzoni] (DataModifica DESC)
INCLUDE (IdProspettoGuerzoni, Utente, Anno, Mese, IdCliente)
ON [PRIMARY];
GO