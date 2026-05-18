CREATE NONCLUSTERED INDEX [IDX_CST_Operativo_Filtri]
ON [dbo].[ProspettoCST] (IdMandatario, IsDeleted, Anno, Mese, Fatturata)
INCLUDE (IdCliente, IdAgente, TotaleProvvigioneFattura, Quantita, PrezzoVendita, Delta, CIT)
ON [PRIMARY];
GO
CREATE NONCLUSTERED INDEX [IDX_CST_Cliente_Mandatario]
ON [dbo].[ProspettoCST] (IdCliente, IdMandatario, IsDeleted)
INCLUDE (NumeroOrdine, NumeroFattura, TotaleProvvigioneFattura, DataConsegna)
ON [PRIMARY];
GO
CREATE NONCLUSTERED INDEX [IDX_CST_DataModifica_Sort]
ON [dbo].[ProspettoCST] (DataModifica DESC)
INCLUDE (IdProspettoCST, Utente, NumeroFattura)
ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IDX_CST_Importazione]
ON [dbo].[ProspettoCST] (IdFileImportato)
INCLUDE (IdProspettoCST, IdImportCST, IdCliente)
ON [PRIMARY];
GO


CREATE TABLE [dbo].[ProspettoCST](


La query qui sotto non restituisce dati
    SELECT 
        i.name AS IndexName,
        i.type_desc AS IndexType,
        c.name AS ColumnName,
        ic.key_ordinal AS Ordine,
        ic.is_included_column AS IsIncluded
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic 
        ON i.object_id = ic.object_id 
       AND i.index_id = ic.index_id
    INNER JOIN sys.columns c 
        ON ic.object_id = c.object_id 
       AND ic.column_id = c.column_id
    WHERE i.object_id = OBJECT_ID('dbo.ProspettoElektraWire')
      AND i.is_primary_key = 0  -- opzionale: esclude la PK se vuoi
    ORDER BY i.name, ic.key_ordinal;



