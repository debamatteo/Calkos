CREATE NONCLUSTERED INDEX [IDX_SystemP_Filtri_Search]
ON [dbo].[ProspettoSystemP] (IsDeleted, IdMandatario, Anno, Mese, Fatturata)
INCLUDE (
    IdCliente, NumeroOrdine, NumeroFattura, Quantita, 
    TotaleProvvigione, ProvvigioneAgente, DataModifica, IdAgente
)
ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IDX_SystemP_Relationship_Clienti]
ON [dbo].[ProspettoSystemP] (IdCliente, IdMandatario, IsDeleted)
INCLUDE (IdMateriale, Quantita, PrezzoVendita, TotaleProvvigione)
ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IDX_SystemP_Sort_DataModifica]
ON [dbo].[ProspettoSystemP] (DataModifica DESC)
ON [PRIMARY];
GO

