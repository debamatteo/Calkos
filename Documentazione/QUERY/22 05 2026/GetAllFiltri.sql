--23/05/2026 Rivisto idtipopagamento
--C:\Lavoro\Calkos\Calkos.web\Documentazione\QUERY\22 05 2026\GetAllFiltri



USE [Calkos]
GO
/****** Object:  StoredProcedure [dbo].[spProspettoCobral_GetAllFiltri]    Script Date: 22/05/2026 16:38:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER  PROCEDURE [dbo].[spProspettoCobral_GetAllFiltri]
    @IdMandatario INT = NULL,
    @IdFileImportato INT = NULL,
    @Anno INT = NULL,
    @Mese INT = NULL,
    @Fatturata INT = NULL,
    @MostraEliminati BIT = 0  -- 0: Solo Attivi (Default), 1: Solo Eliminati (Cestino)
	, @IdTipoPagamento INT = NULL   -- 22/05/2026
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.IdProspettoCobral,
        p.IdFileImportato,
        p.IdMandatario,
        p.IdImportCobral,
        p.IdCliente,
        p.IdMateriale,
        p.IdUnitaMisura,
        p.NumeroOrdine,
        p.NumeroDDT,
        p.NumeroFattura,
        p.Quantita,
        p.Prezzo,
        p.DataRiferimentoPrezzo,
        p.Spessore,
        p.Larghezza,
        p.Provvigione,
        p.AlluminioSpessore,
        p.OttoneSpessore,
        p.RameSpessore,
        p.AltrePercentuali,
        p.PrLavSpess,
        p.AlluminioLarghezza,
        p.OttoneLarghezza,
        p.RameLarghezza,
        p.BronzoLarghezza,
        p.PrLavLarg,
        p.ExtraPrezzoKg,
        p.ExtraPrezzoStagnato,
        p.PrLavTotale,
        p.ValoreCommissioni,
        p.PrezzoVendita,
        p.Differenza,
        p.ProvvigioneAgente,
        p.DataConsegnaIpotetica,
        p.Scadenza,
        p.IdAgente,
        p.IdTipoPagamento,
        p.DataInserimento,
        p.DataModifica,
        p.Utente,
        p.Anno, 
        p.Mese,
        p.Fatturata,
		p.DataFatturazione, --25/04/2026
        p.IsDeleted, -- Aggiunto per il mapping lato C#
        c.RagioneSociale,
        m.DescrizioneMateriale,
        u.UnitaMisura,
        a.AgenteDescrizione,
        a.PercentualeDefault,
        tp.TipoPagamento
    FROM ProspettoCobral p
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente
    LEFT JOIN Materiali m ON p.IdMateriale = m.IdMateriale
    LEFT JOIN UnitaMisura u ON p.IdUnitaMisura = u.IdUnitaMisura
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente
    WHERE 
        (@IdMandatario IS NULL OR p.IdMandatario = @IdMandatario) AND
        (@IdFileImportato IS NULL OR p.IdFileImportato = @IdFileImportato) AND
        (@Anno IS NULL OR @Anno = 0 OR p.Anno = @Anno) AND
        (@Mese IS NULL OR @Mese = 0 OR p.Mese = @Mese) AND
        (@Fatturata IS NULL OR @Fatturata = -1 OR p.Fatturata = @Fatturata) AND

        (@IdTipoPagamento IS NULL OR  @IdTipoPagamento = 0 OR p.IdTipoPagamento = @IdTipoPagamento) AND --22/05/2026
        -- LOGICA CESTINO (ESCLUSIVA)
        -- Se @MostraEliminati = 0: restituisce solo IsDeleted = 0
        -- Se @MostraEliminati = 1: restituisce solo IsDeleted = 1
        p.IsDeleted = @MostraEliminati

    ORDER BY p.DataModifica DESC;
END
GO
/****** Object:  StoredProcedure [dbo].[spProspettoCST_GetAllFiltri]    Script Date: 22/05/2026 16:38:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



ALTER     PROCEDURE [dbo].[spProspettoCST_GetAllFiltri]
    @IdMandatario INT = NULL,
    @IdFileImportato INT = NULL,
    @Anno INT = NULL,
    @Mese INT = NULL,
    @Fatturata INT = NULL,
    @MostraEliminati BIT = 0  -- 0: Solo Attivi (Default), 1: Solo Eliminati (Cestino)
	,@IdTipoPagamento INT = NULL   -- 22/05/2026
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.IdProspettoCST,
        p.IdFileImportato,
        p.IdMandatario,
        p.IdImportCST,
        p.IdCliente,
        p.IdMateriale,
        p.IdUnitaMisura,
        p.NumeroOrdine,
        p.NumeroDDT,
        p.NumeroFattura,
        p.Quantita,
        p.CostoLavorazione,
        p.PrezzoBase,
        p.PrezzoVendita,
        p.PrezzoBaseConLavorazione,
        p.PrezzoEuroRiferimento,
        p.ProvvigioneFissa01_05,
        p.ProvvigioneFissa03,
        p.TotaleProvvigioneFissa,
        p.CIT,
        p.Delta,
        p.TotaleProvvigioneVariabile,
        p.TotaleProvvigioneFattura,
        p.DataConsegna,
        --p.Agente,
        p.Fatturare,
        p.IdAgente,
        p.IdTipoPagamento,
        p.ProvvigioneAgente,
        p.Scadenza,
        p.DataInserimento,
        p.DataModifica,
        p.Utente,
        p.Anno,
        p.Mese,
        p.Fatturata,
        p.DataFatturazione,
        p.IsDeleted,
		p.Perc_Delta_Provvigione,
        -- JOIN STANDARD IDENTICHE AL MODELLO CST
        c.RagioneSociale,
        m.DescrizioneMateriale,
        u.UnitaMisura,
        a.AgenteDescrizione,
        a.PercentualeDefault,
        tp.TipoPagamento
    FROM ProspettoCST p
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente
    LEFT JOIN Materiali m ON p.IdMateriale = m.IdMateriale
    LEFT JOIN UnitaMisura u ON p.IdUnitaMisura = u.IdUnitaMisura
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente
    WHERE 
        (@IdMandatario IS NULL OR p.IdMandatario = @IdMandatario) AND
        (@IdFileImportato IS NULL OR p.IdFileImportato = @IdFileImportato) AND
        (@Anno IS NULL OR @Anno = 0 OR p.Anno = @Anno) AND
        (@Mese IS NULL OR @Mese = 0 OR p.Mese = @Mese) AND
        (@Fatturata IS NULL OR @Fatturata = -1 OR p.Fatturata = @Fatturata) AND
        (@IdTipoPagamento IS NULL OR  @IdTipoPagamento = 0 OR p.IdTipoPagamento = @IdTipoPagamento) AND --22/05/2026
		-- LOGICA CESTINO (ESCLUSIVA)
        -- Se @MostraEliminati = 0: restituisce solo IsDeleted = 0
        -- Se @MostraEliminati = 1: restituisce solo IsDeleted = 1
        p.IsDeleted = @MostraEliminati

    ORDER BY p.DataModifica DESC;
END

GO
/****** Object:  StoredProcedure [dbo].[spProspettoDeAngeli_GetAllFiltri]    Script Date: 22/05/2026 16:38:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER  PROCEDURE  [dbo].[spProspettoDeAngeli_GetAllFiltri]
    @IdMandatario INT = NULL,
    @IdFileImportato INT = NULL,
    @Anno INT = NULL,
    @Mese INT = NULL,
    @Fatturata INT = NULL,
    @MostraEliminati BIT = 0
	,@IdTipoPagamento INT = NULL   -- 22/05/2026
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.IdProspettoDeAngeli,
        p.IdFileImportato,
        p.IdMandatario,
        p.IdImportDeAngeli,
        p.IdCliente,
        p.IdMateriale,
        p.IdUnitaMisura,

        -- Documenti
        p.NumeroOrdine,
        p.NumeroDDT,
        p.NumeroFattura,              -- AGGIUNTO

        -- Quantità e Prezzi
        p.Quantita,
        p.QuantitaOrdine,
        p.CostoLavorazione,
        p.Prezzo,
        p.PrezzoConLavorazione,
        p.PrezzoVendita,
        p.FlagIndicatoreMaggiorazione,
        -- Provvigioni
        p.PercentualeProvvigioneVariabile,
        p.ValoreProvvigioneVariabile,
        p.ValoreProvvigioneFissa1,
        p.ValoreProvvigioneFissa2,
        p.ValoreProvvigioneFissa3,
        p.TotaleFissoProvvigione,
        p.Differenza,
        p.TotaleVariabileProvvigione,
        p.TotaleProvvigioneDaFatturare,
        p.ProvvigioneAgente,          -- AGGIUNTO
		p.Perc_Delta_Provvigione,
        -- Date
        p.DataConsegnaIpotetica,
        p.Scadenza,

        -- Lookup
        p.IdAgente,
        p.IdTipoPagamento,

        -- Audit
        p.DataInserimento,
        p.DataModifica,
        p.Utente,
        p.Anno,
        p.Mese,
        p.Fatturata,
        p.DataFatturazione,
        p.IsDeleted,

        -- Join
        c.RagioneSociale,
        m.DescrizioneMateriale,
        u.UnitaMisura,
        a.AgenteDescrizione,
        a.PercentualeDefault,
        tp.TipoPagamento

    FROM ProspettoDeAngeli p
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente
    LEFT JOIN Materiali m ON p.IdMateriale = m.IdMateriale
    LEFT JOIN UnitaMisura u ON p.IdUnitaMisura = u.IdUnitaMisura
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente
    WHERE 
        (@IdMandatario IS NULL OR p.IdMandatario = @IdMandatario) AND
        (@IdFileImportato IS NULL OR p.IdFileImportato = @IdFileImportato) AND
        (@Anno IS NULL OR @Anno = 0 OR p.Anno = @Anno) AND
        (@Mese IS NULL OR @Mese = 0 OR p.Mese = @Mese) AND
        (@Fatturata IS NULL OR @Fatturata = -1 OR p.Fatturata = @Fatturata) AND
        (@IdTipoPagamento IS NULL OR  @IdTipoPagamento = 0 OR p.IdTipoPagamento = @IdTipoPagamento) AND --22/05/2026
        -- LOGICA CESTINO (identica a Cobral)
        p.IsDeleted = @MostraEliminati

    ORDER BY p.DataModifica DESC;
END
GO
/****** Object:  StoredProcedure [dbo].[spProspettoElektraWire_GetAllFiltri]    Script Date: 22/05/2026 16:38:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


ALTER  PROCEDURE [dbo].[spProspettoElektraWire_GetAllFiltri]
    @IdMandatario INT = NULL,
    @IdFileImportato INT = NULL,
    @Anno INT = NULL,
    @Mese INT = NULL,
    @Fatturata INT = NULL,
    @MostraEliminati BIT = 0
	,@IdTipoPagamento INT = NULL   -- 22/05/2026
AS
BEGIN
    SET NOCOUNT ON;

   SELECT 
        p.IdProspettoElektraWire,
        p.IdFileImportato,
        p.IdMandatario,
        p.IdImportElektraWire,
        p.IdCliente,
        p.IdMateriale,
        p.IdUnitaMisura,
		p.IdAgente,
		p.IdTipoPagamento,
        -- Documenti
        p.NumeroOrdine,
        p.NumeroDDT,
        p.NumeroFattura,

        -- Quantità e Prezzi
		p.Misure,
        p.Quantita,
        p.CostoLavorazione,
        p.PrezzoBase,
        p.PrezzoConLavorazione,
        p.PrezzoVendita,
        p.DeltaMargine,

        -- Provvigioni Fissa 1.5
        p.ValUnitProvvigioneFissa1_5,
        p.Delta1ProvvigioneFissa1_5,
        p.Delta2ProvvigioneFissa1_5,
        p.TotProvvigioneFissa1_5,

        -- Provvigioni Fissa 0.3
        p.ValUnitProvvigioneFissa_03,
        p.Delta1ProvvigioneFissa_03,
        p.Delta2ProvvigioneFissa_03,
        p.TotProvvigioneFissa_03,

        -- Quote e Totali
        p.ValQuotaFissa_1_5,
        p.ValQuotaFissa_03,
        p.TotaleFissoProvvigione,
        p.DeltaResiduoVariabile,
        p.TotaleVariabileProvvigione,
        p.TotaleSommaProvvigioni,
        p.TotaleProvvigioneDaFatturare,
        p.ProvvigioneAgente,
		p.Perc_Delta_Provvigione,
        -- Date e Audit
        p.DataInserimento,
        p.DataModifica,
        p.Utente,
        p.Anno,
        p.Mese,
        p.Fatturata,
        p.DataFatturazione,
        p.DataConsegna,
		p.Scadenza,
        -- Lookup
        c.RagioneSociale,
        m.DescrizioneMateriale,
        u.UnitaMisura,
        a.AgenteDescrizione,
        a.PercentualeDefault,
        tp.TipoPagamento

    FROM ProspettoElektraWire p
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente
    LEFT JOIN Materiali m ON p.IdMateriale = m.IdMateriale
    LEFT JOIN UnitaMisura u ON p.IdUnitaMisura = u.IdUnitaMisura
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente
    WHERE 
        (@IdMandatario IS NULL OR p.IdMandatario = @IdMandatario) AND
        (@IdFileImportato IS NULL OR p.IdFileImportato = @IdFileImportato) AND
        (@Anno IS NULL OR @Anno = 0 OR p.Anno = @Anno) AND
        (@Mese IS NULL OR @Mese = 0 OR p.Mese = @Mese) AND
        (@Fatturata IS NULL OR @Fatturata = -1 OR p.Fatturata = @Fatturata) AND
        (@IdTipoPagamento IS NULL OR  @IdTipoPagamento = 0 OR p.IdTipoPagamento = @IdTipoPagamento) AND --22/05/2026
        -- LOGICA CESTINO
        p.IsDeleted = @MostraEliminati

    ORDER BY p.DataModifica DESC;
END
GO
/****** Object:  StoredProcedure [dbo].[spProspettoGuerzoni_GetAllFiltri]    Script Date: 22/05/2026 16:38:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO




ALTER    PROCEDURE [dbo].[spProspettoGuerzoni_GetAllFiltri]
    @IdMandatario INT = NULL,
    @IdFileImportato INT = NULL,
    @Anno INT = NULL,
    @Mese INT = NULL,
    @Fatturata INT = NULL,
    @MostraEliminati BIT = 0  -- 0: Solo Attivi (Default), 1: Solo Eliminati (Cestino)
	,@IdTipoPagamento INT = NULL   -- 22/05/2026
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.IdProspettoGuerzoni,
        p.IdFileImportato,
        p.IdMandatario,
        p.IdImportGuerzoni,
        p.IdCliente,
        p.IdMateriale,
        p.IdUnitaMisura,
        p.NumeroOrdine,
        p.NumeroDDT,
        p.NumeroFattura,
        p.Quantita,
        p.PrezzoUnitario,
        p.Imponibile,
		Perc_Delta_Provvigione,--29/04/2026
        p.TotaleProvvigione,
        p.DataConsegna,
        p.Fatturare,
        p.IdAgente,
        p.IdTipoPagamento,
        p.ProvvigioneAgente,
        p.DataInserimento,
        p.DataModifica,
        p.Utente,
        p.Anno,
        p.Mese,
        p.Fatturata,
        p.DataFatturazione,
        p.IsDeleted,
        p.Scadenza,

        -- JOIN STANDARD DEL MODELLO Guerzoni
        c.RagioneSociale,
        m.DescrizioneMateriale,
        u.UnitaMisura,
        a.AgenteDescrizione,
        a.PercentualeDefault,
        tp.TipoPagamento
    FROM ProspettoGuerzoni p
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente
    LEFT JOIN Materiali m ON p.IdMateriale = m.IdMateriale
    LEFT JOIN UnitaMisura u ON p.IdUnitaMisura = u.IdUnitaMisura
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente
    WHERE 
        (@IdMandatario IS NULL OR p.IdMandatario = @IdMandatario) AND
        (@IdFileImportato IS NULL OR p.IdFileImportato = @IdFileImportato) AND
        (@Anno IS NULL OR @Anno = 0 OR p.Anno = @Anno) AND
        (@Mese IS NULL OR @Mese = 0 OR p.Mese = @Mese) AND
        (@Fatturata IS NULL OR @Fatturata = -1 OR p.Fatturata = @Fatturata) AND
         (@IdTipoPagamento IS NULL OR  @IdTipoPagamento = 0 OR p.IdTipoPagamento = @IdTipoPagamento) AND --22/05/2026       
        -- LOGICA CESTINO (ESCLUSIVA)
        -- Se @MostraEliminati = 0: restituisce solo IsDeleted = 0
        -- Se @MostraEliminati = 1: restituisce solo IsDeleted = 1
        p.IsDeleted = @MostraEliminati

    ORDER BY p.DataModifica DESC;
END

GO
/****** Object:  StoredProcedure [dbo].[spProspettoHitech_GetAllFiltri]    Script Date: 22/05/2026 16:38:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO




ALTER     PROCEDURE [dbo].[spProspettoHitech_GetAllFiltri]
    @IdMandatario INT = NULL,
    @IdFileImportato INT = NULL,
    @Anno INT = NULL,
    @Mese INT = NULL,
    @Fatturata INT = NULL,
    @MostraEliminati BIT = 0  -- 0: Solo Attivi (Default), 1: Solo Eliminati (Cestino)
	,@IdTipoPagamento INT = NULL   -- 22/05/2026
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
           p.IdProspettoHitech,
        p.IdFileImportato,
        p.IdMandatario,
        p.IdImportHitech,
        p.IdCliente,
        p.IdMateriale,
        p.IdUnitaMisura,
        p.NumeroOrdine,
        p.NumeroDDT,
        p.NumeroFattura,
        p.Quantita,
        p.PrezzoBase,
        p.PrezzoPraticato,
        p.ProvvigioneUnitaria,
        p.TotaleProvvigione,
		p.DataDoc,
        p.DataConsegna,
        --p.Agente,
        p.Fatturare,
        p.IdAgente,
        p.IdTipoPagamento,
        p.ProvvigioneAgente,
        p.Scadenza,
        p.DataInserimento,
        p.DataModifica,
        p.Utente,
        p.Anno,
        p.Mese,
        p.Fatturata,
        p.DataFatturazione,
        p.IsDeleted,

        -- JOIN identiche al modello Hitech
        c.RagioneSociale,
        m.DescrizioneMateriale,
        u.UnitaMisura,
        a.AgenteDescrizione,
        a.PercentualeDefault,
        tp.TipoPagamento

    FROM ProspettoHitech p
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente
    LEFT JOIN Materiali m ON p.IdMateriale = m.IdMateriale
    LEFT JOIN UnitaMisura u ON p.IdUnitaMisura = u.IdUnitaMisura
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente
    WHERE 
        (@IdMandatario IS NULL OR p.IdMandatario = @IdMandatario) AND
        (@IdFileImportato IS NULL OR p.IdFileImportato = @IdFileImportato) AND
        (@Anno IS NULL OR @Anno = 0 OR p.Anno = @Anno) AND
        (@Mese IS NULL OR @Mese = 0 OR p.Mese = @Mese) AND
        (@Fatturata IS NULL OR @Fatturata = -1 OR p.Fatturata = @Fatturata) AND
          (@IdTipoPagamento IS NULL OR  @IdTipoPagamento = 0 OR p.IdTipoPagamento = @IdTipoPagamento) AND --22/05/2026      
        -- LOGICA CESTINO (ESCLUSIVA)
        -- Se @MostraEliminati = 0: restituisce solo IsDeleted = 0
        -- Se @MostraEliminati = 1: restituisce solo IsDeleted = 1
        p.IsDeleted = @MostraEliminati

    ORDER BY p.DataModifica DESC;
END

GO
/****** Object:  StoredProcedure [dbo].[spProspettoSystemCore_GetAllFiltri]    Script Date: 22/05/2026 16:38:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


ALTER    PROCEDURE [dbo].[spProspettoSystemCore_GetAllFiltri]
    @IdMandatario INT = NULL,
    @IdFileImportato INT = NULL,
    @Anno INT = NULL,
    @Mese INT = NULL,
    @Fatturata INT = NULL,
    @MostraEliminati BIT = 0
	,@IdTipoPagamento INT = NULL   -- 22/05/2026
AS
BEGIN
    SET NOCOUNT ON;

         SELECT
          -- IDENTIFICATIVI PRINCIPALI        
        p.IdProspettoSystemCore,
        p.IdFileImportato,
        p.IdMandatario,
        p.IdImportSystemCore,        
        -- DATI ANAGRAFICI / RIFERIMENTI        
        p.IdCliente,
        p.IdMateriale,
        p.IdUnitaMisura,
        p.Misure,  -- campo descrittivo        
        -- DOCUMENTI        
        p.NumeroOrdine,
        p.NumeroDDT,
        p.NumeroFattura,        
        -- QUANTITÀ E VALORI DI BASE        
        p.Quantita,
        
        p.PrezzoVendita,
        p.CostoLavorazione,        
        -- PROVVIGIONI / QUOTE        
        p.PercentualeQuotaFissa,
        p.ValoreQuotaFissa,
        p.TotaleProvvigioneVariabile,
        p.TotaleProvvigione,
        p.TotaleProvvigioneDaFatturare,
        p.Delta,        
        -- AGENTE / PAGAMENTO        
        p.IdAgente,
        p.IdTipoPagamento,        
        -- DATE OPERATIVE        
        p.DataConsegna,
		p.Scadenza,
        p.DataFatturazione,        
        -- STATO / FLAG        
        p.Fatturata,
        p.IsDeleted,
		p.Perc_Delta_Provvigione,
		p.QuotaCentesimiPerKg ,
        p.Fatturare,
        p.IsProcessed,        
        -- AUDIT        
        p.DataInserimento,
        p.DataModifica,
        p.Utente,        
     	p.Anno,--21/04/2026
        p.Mese,--21/04/2026
        p.ProvvigioneAgente,--21/04/2026  
        -- JOIN ESTERNI         
        c.RagioneSociale,
        m.DescrizioneMateriale,
        u.UnitaMisura,
        a.AgenteDescrizione,
        a.PercentualeDefault,
        tp.TipoPagamento
    FROM ProspettoSystemCore p
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente
    LEFT JOIN Materiali m ON p.IdMateriale = m.IdMateriale
    LEFT JOIN UnitaMisura u ON p.IdUnitaMisura = u.IdUnitaMisura
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente
    WHERE 
        (@IdMandatario IS NULL OR p.IdMandatario = @IdMandatario) AND
        (@IdFileImportato IS NULL OR p.IdFileImportato = @IdFileImportato) AND
        (@Anno IS NULL OR @Anno = 0 OR p.Anno = @Anno) AND
        (@Mese IS NULL OR @Mese = 0 OR p.Mese = @Mese) AND
        (@Fatturata IS NULL OR @Fatturata = -1 OR p.Fatturata = @Fatturata) AND
        (@IdTipoPagamento IS NULL OR  @IdTipoPagamento = 0 OR p.IdTipoPagamento = @IdTipoPagamento) AND --22/05/2026
        -- LOGICA CESTINO (identica a Cobral)
        p.IsDeleted = @MostraEliminati
    ORDER BY p.DataModifica DESC;
END
GO
/****** Object:  StoredProcedure [dbo].[spProspettoSystemP_GetAllFiltri]    Script Date: 22/05/2026 16:38:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER  PROCEDURE [dbo].[spProspettoSystemP_GetAllFiltri]
    @IdMandatario INT = NULL,
    @IdFileImportato INT = NULL,
    @Anno INT = NULL,
    @Mese INT = NULL,
    @Fatturata INT = NULL,
    @MostraEliminati BIT = 0
	,@IdTipoPagamento INT = NULL   -- 22/05/2026
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.IdProspettoSystemP,
        p.IdFileImportato,
        p.IdMandatario,
        p.IdImportSystemP,
        p.IdCliente,
        p.IdMateriale,
        p.IdUnitaMisura,

        p.NumeroOrdine,
        p.NumeroDDT,
        p.NumeroFattura,

        p.Quantita,

        p.LavorazionePrezzoAcquisto,
        p.BaseAcquisto,
        p.CIT,
        p.PrezzoAcquisto,
        p.DataRiferimentoPrezzoAcquisto,
        p.CIT_EUR_KG,
        p.CITsuAcquisto,

        p.LavorazionePrezzoVendita,
        p.BaseVendita,
        p.CITsuVendita,
        p.PrezzoVendita,

        p.ProvvigioneUnitaria,
        p.ValoreProvvigioneFissa,
        p.Delta,
        p.PercentualeDelta,
        p.TotaleVariabileProvvigione,
        p.TotaleProvvigione,

        p.DataConsegnaIpotetica,
        p.Scadenza,
        p.DataFatturazione,

        p.IdAgente,
        p.ProvvigioneAgente,
        p.IdTipoPagamento,

        p.DataInserimento,
        p.DataModifica,
        p.Utente,

        p.Anno,
        p.Mese,
        p.Fatturata,
        p.IsDeleted,

        c.RagioneSociale,
        m.DescrizioneMateriale,
        u.UnitaMisura,
        a.AgenteDescrizione,
        a.PercentualeDefault,
        tp.TipoPagamento

    FROM ProspettoSystemP p
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente
    LEFT JOIN Materiali m ON p.IdMateriale = m.IdMateriale
    LEFT JOIN UnitaMisura u ON p.IdUnitaMisura = u.IdUnitaMisura
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente
    WHERE 
        (@IdMandatario IS NULL OR p.IdMandatario = @IdMandatario) AND
        (@IdFileImportato IS NULL OR p.IdFileImportato = @IdFileImportato) AND
        (@Anno IS NULL OR @Anno = 0 OR p.Anno = @Anno) AND
        (@Mese IS NULL OR @Mese = 0 OR p.Mese = @Mese) AND
        (@Fatturata IS NULL OR @Fatturata = -1 OR p.Fatturata = @Fatturata) AND
        (@IdTipoPagamento IS NULL OR  @IdTipoPagamento = 0 OR p.IdTipoPagamento = @IdTipoPagamento) AND --22/05/2026
        p.IsDeleted = @MostraEliminati
    ORDER BY p.DataModifica DESC;
END
GO
/****** Object:  StoredProcedure [dbo].[spProspettoTradingAndConsulting_GetAllFiltri]    Script Date: 22/05/2026 16:38:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



ALTER     PROCEDURE [dbo].[spProspettoTradingAndConsulting_GetAllFiltri]
    @IdMandatario INT = NULL,
    @IdFileImportato INT = NULL,
    @Anno INT = NULL,
    @Mese INT = NULL,
    @Fatturata INT = NULL,
    @MostraEliminati BIT = 0  -- 0: Solo Attivi (Default), 1: Solo Eliminati (Cestino)
	,@IdTipoPagamento INT = NULL   -- 22/05/2026
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.IdProspettoTradingAndConsulting,
        p.IdFileImportato,
        p.IdMandatario,
        p.IdImportTradingAndConsulting,
        p.IdCliente,
        p.IdMateriale,
        p.IdUnitaMisura,
        p.NumeroOrdine,
        p.NumeroDDT,
        p.NumeroFattura,
        p.Quantita,
        p.CostoLavorazione,
        p.PrezzoBase,
        p.PrezzoVendita,
        p.PrezzoBaseConLavorazione,
        p.PrezzoEuroRiferimento,
        p.ProvvigioneFissa01_05,
        p.ProvvigioneFissa03,
        p.TotaleProvvigioneFissa,
        p.CIT,
        p.Delta,
        p.TotaleProvvigioneVariabile,
        p.TotaleProvvigioneFattura,
        p.DataConsegna,
        --p.Agente,
        p.Fatturare,
        p.IdAgente,
        p.IdTipoPagamento,
        p.ProvvigioneAgente,
        p.Scadenza,
        p.DataInserimento,
        p.DataModifica,
        p.Utente,
        p.Anno,
        p.Mese,
        p.Fatturata,
        p.DataFatturazione,
        p.IsDeleted,
		p.Perc_Delta_Provvigione,

        -- JOIN STANDARD IDENTICHE AL MODELLO TradingAndConsulting
        c.RagioneSociale,
        m.DescrizioneMateriale,
        u.UnitaMisura,
        a.AgenteDescrizione,
        a.PercentualeDefault,
        tp.TipoPagamento
    FROM ProspettoTradingAndConsulting p
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente
    LEFT JOIN Materiali m ON p.IdMateriale = m.IdMateriale
    LEFT JOIN UnitaMisura u ON p.IdUnitaMisura = u.IdUnitaMisura
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente
    WHERE 
        (@IdMandatario IS NULL OR p.IdMandatario = @IdMandatario) AND
        (@IdFileImportato IS NULL OR p.IdFileImportato = @IdFileImportato) AND
        (@Anno IS NULL OR @Anno = 0 OR p.Anno = @Anno) AND
        (@Mese IS NULL OR @Mese = 0 OR p.Mese = @Mese) AND
        (@Fatturata IS NULL OR @Fatturata = -1 OR p.Fatturata = @Fatturata) AND
        (@IdTipoPagamento IS NULL OR  @IdTipoPagamento = 0 OR p.IdTipoPagamento = @IdTipoPagamento) AND --22/05/2026        
        -- LOGICA CESTINO (ESCLUSIVA)
        -- Se @MostraEliminati = 0: restituisce solo IsDeleted = 0
        -- Se @MostraEliminati = 1: restituisce solo IsDeleted = 1
        p.IsDeleted = @MostraEliminati

    ORDER BY p.DataModifica DESC;
END

GO
