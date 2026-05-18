USE [Calkos]
GO

/****** Object:  StoredProcedure [dbo].[spGeneraProspettoAnnualeExcel_Fatture]    Script Date: 15/05/2026 12:47:52 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE or alter PROCEDURE [dbo].[spGeneraProspettoAnnualeExcel_Fatture] 
    @Anno INT,
    @IdMandatario INT 
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NomeVista NVARCHAR(128);
    DECLARE @ColonnaImporto NVARCHAR(128);
    DECLARE @SQL NVARCHAR(MAX);

    -- 1. SELEZIONE DELLA VISTA IN BASE AL MANDATARIO
    SET @NomeVista = CASE @IdMandatario
        WHEN 1 THEN 'vProspettoDeAngeli'
        WHEN 2 THEN 'vProspettoCobral'
        WHEN 3 THEN 'vProspettoElektraWire'
        WHEN 4 THEN 'vProspettoCST'
        WHEN 5 THEN 'vProspettoSystemCore'
        WHEN 6 THEN 'vProspettoSystemP'
        WHEN 7 THEN 'vProspettoGuerzoni'
        WHEN 8 THEN 'vProspettoTradingAndConsulting'
        WHEN 9 THEN 'vProspettoHitech'
    END;

    -- 2. GESTIONE ECCEZIONI NOMI COLONNE 
    SET @ColonnaImporto = CASE @IdMandatario
        WHEN 1 THEN 'TotaleProvvigioneDaFatturare'
        WHEN 2 THEN 'ValoreCommissioni'
        WHEN 3 THEN 'TotaleProvvigioneDaFatturare'
        WHEN 4 THEN 'TotaleProvvigioneFattura'
        WHEN 5 THEN 'TotaleProvvigioneDaFatturare'
        WHEN 6 THEN 'TotaleProvvigione'
        WHEN 7 THEN 'TotaleProvvigione'
        WHEN 8 THEN 'TotaleProvvigioneFattura'
        WHEN 9 THEN 'TotaleProvvigione'
    END;

    -- 🔴 CONTROLLO SICUREZZA
    IF @NomeVista IS NULL OR @ColonnaImporto IS NULL 
    BEGIN
        RAISERROR('Vista o colonna non configurata per IdMandatario=%d',16,1,@IdMandatario);
        RETURN;
    END;

    -- 3. COSTRUZIONE QUERY DINAMICA
    SET @SQL = N'
    WITH DatiMensili AS (
        SELECT 
            Mese,
            RagioneSociale AS Cliente,
            -- NrFatt: numero di righe fatturate per cliente (esplicito)
            SUM(CASE WHEN Fatturata = 1 THEN 1 ELSE 0 END) AS NrFatt, 
            SUM(Quantita) AS Kg,
            SUM(' + QUOTENAME(@ColonnaImporto) + N') AS Importo,
            -- Posizione: ordinamento all''interno del mese per RagioneSociale (ordine alfabetico)
            ROW_NUMBER() OVER(
                PARTITION BY Mese 
                ORDER BY RagioneSociale ASC
            ) AS Posizione
        FROM [dbo].' + QUOTENAME(@NomeVista) + N'
        WHERE Anno = @Anno 
          AND (IdMandatario = @IdMandatario OR IdMandatario = 0)
          AND Fatturata = 1
        GROUP BY Mese, RagioneSociale
        HAVING SUM(' + QUOTENAME(@ColonnaImporto) + N') > 0 
    )
    
    SELECT 
        MAX(CASE WHEN Mese = 1 THEN NrFatt END) AS Gen_NrFatt,
        MAX(CASE WHEN Mese = 1 THEN Cliente END) AS Gen_Cliente,
        MAX(CASE WHEN Mese = 1 THEN Kg END) AS Gen_Kg,
        MAX(CASE WHEN Mese = 1 THEN Importo END) AS Gen_Importo,
        
        MAX(CASE WHEN Mese = 2 THEN NrFatt END) AS Feb_NrFatt,
        MAX(CASE WHEN Mese = 2 THEN Cliente END) AS Feb_Cliente,
        MAX(CASE WHEN Mese = 2 THEN Kg END) AS Feb_Kg,
        MAX(CASE WHEN Mese = 2 THEN Importo END) AS Feb_Importo,

        MAX(CASE WHEN Mese = 3 THEN NrFatt END) AS Mar_NrFatt,
        MAX(CASE WHEN Mese = 3 THEN Cliente END) AS Mar_Cliente,
        MAX(CASE WHEN Mese = 3 THEN Kg END) AS Mar_Kg,
        MAX(CASE WHEN Mese = 3 THEN Importo END) AS Mar_Importo,

        MAX(CASE WHEN Mese = 4 THEN NrFatt END) AS Apr_NrFatt,
        MAX(CASE WHEN Mese = 4 THEN Cliente END) AS Apr_Cliente,
        MAX(CASE WHEN Mese = 4 THEN Kg END) AS Apr_Kg,
        MAX(CASE WHEN Mese = 4 THEN Importo END) AS Apr_Importo,

        MAX(CASE WHEN Mese = 5 THEN NrFatt END) AS Mag_NrFatt,
        MAX(CASE WHEN Mese = 5 THEN Cliente END) AS Mag_Cliente,
        MAX(CASE WHEN Mese = 5 THEN Kg END) AS Mag_Kg,
        MAX(CASE WHEN Mese = 5 THEN Importo END) AS Mag_Importo,

        MAX(CASE WHEN Mese = 6 THEN NrFatt END) AS Giu_NrFatt,
        MAX(CASE WHEN Mese = 6 THEN Cliente END) AS Giu_Cliente,
        MAX(CASE WHEN Mese = 6 THEN Kg END) AS Giu_Kg,
        MAX(CASE WHEN Mese = 6 THEN Importo END) AS Giu_Importo,

        MAX(CASE WHEN Mese = 7 THEN NrFatt END) AS Lug_NrFatt,
        MAX(CASE WHEN Mese = 7 THEN Cliente END) AS Lug_Cliente,
        MAX(CASE WHEN Mese = 7 THEN Kg END) AS Lug_Kg,
        MAX(CASE WHEN Mese = 7 THEN Importo END) AS Lug_Importo,

        MAX(CASE WHEN Mese = 8 THEN NrFatt END) AS Ago_NrFatt,
        MAX(CASE WHEN Mese = 8 THEN Cliente END) AS Ago_Cliente,
        MAX(CASE WHEN Mese = 8 THEN Kg END) AS Ago_Kg,
        MAX(CASE WHEN Mese = 8 THEN Importo END) AS Ago_Importo,

        MAX(CASE WHEN Mese = 9 THEN NrFatt END) AS Set_NrFatt,
        MAX(CASE WHEN Mese = 9 THEN Cliente END) AS Set_Cliente,
        MAX(CASE WHEN Mese = 9 THEN Kg END) AS Set_Kg,
        MAX(CASE WHEN Mese = 9 THEN Importo END) AS Set_Importo,

        MAX(CASE WHEN Mese = 10 THEN NrFatt END) AS Ott_NrFatt,
        MAX(CASE WHEN Mese = 10 THEN Cliente END) AS Ott_Cliente,
        MAX(CASE WHEN Mese = 10 THEN Kg END) AS Ott_Kg,
        MAX(CASE WHEN Mese = 10 THEN Importo END) AS Ott_Importo,

        MAX(CASE WHEN Mese = 11 THEN NrFatt END) AS Nov_NrFatt,
        MAX(CASE WHEN Mese = 11 THEN Cliente END) AS Nov_Cliente,
        MAX(CASE WHEN Mese = 11 THEN Kg END) AS Nov_Kg,
        MAX(CASE WHEN Mese = 11 THEN Importo END) AS Nov_Importo,

        MAX(CASE WHEN Mese = 12 THEN NrFatt END) AS Dic_NrFatt,
        MAX(CASE WHEN Mese = 12 THEN Cliente END) AS Dic_Cliente,
        MAX(CASE WHEN Mese = 12 THEN Kg END) AS Dic_Kg,
        MAX(CASE WHEN Mese = 12 THEN Importo END) AS Dic_Importo

    FROM DatiMensili
    GROUP BY Posizione
    ORDER BY Posizione;';

    -- 4. DEBUG SICURO (NON ROMPE IL READER)
    DECLARE @SQL_LOG NVARCHAR(MAX);
    SET @SQL_LOG = REPLACE(@SQL, '@Anno', CAST(@Anno AS NVARCHAR(10)));
    SET @SQL_LOG = REPLACE(@SQL_LOG, '@IdMandatario', CAST(@IdMandatario AS NVARCHAR(10)));

    PRINT '--- DEBUG: QUERY CON VALORI REALI ---';
    PRINT @SQL_LOG;
    PRINT '--- FINE DEBUG ---';

    -- 5. ESECUZIONE
    EXEC sp_executesql 
        @SQL, 
        N'@Anno INT, @IdMandatario INT', 
        @Anno, 
        @IdMandatario;
END
GO


USE [Calkos]
GO

/****** Object:  StoredProcedure [dbo].[spGeneraProspettoAnnualeExcel_Fatture_originale_14_05_2026]    Script Date: 15/05/2026 12:47:58 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE or alter PROCEDURE [dbo].[spGeneraProspettoAnnualeExcel_Fatture_originale_14_05_2026] 
    @Anno INT,
    @IdMandatario INT 
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NomeVista NVARCHAR(128);
    DECLARE @ColonnaImporto NVARCHAR(128);
    DECLARE @SQL NVARCHAR(MAX);

    -- 1. SELEZIONE DELLA VISTA IN BASE AL MANDATARIO
    SET @NomeVista = CASE @IdMandatario
        WHEN 1 THEN 'vProspettoDeAngeli'
        WHEN 2 THEN 'vProspettoCobral'
        WHEN 3 THEN 'vProspettoElektraWire'
        WHEN 4 THEN 'vProspettoCST'
        WHEN 5 THEN 'vProspettoSystemCore'
        WHEN 6 THEN 'vProspettoSystemP'
        WHEN 7 THEN 'vProspettoGuerzoni'
        WHEN 8 THEN 'vProspettoTradingAndConsulting'
        WHEN 9 THEN 'vProspettoHitech'
    END;

    -- 2. GESTIONE ECCEZIONI NOMI COLONNE 
    SET @ColonnaImporto = CASE @IdMandatario
        WHEN 1 THEN 'TotaleProvvigioneDaFatturare'
        WHEN 2 THEN 'ValoreCommissioni'
        WHEN 3 THEN 'TotaleProvvigioneDaFatturare'
        WHEN 4 THEN 'TotaleProvvigioneFattura'
        WHEN 5 THEN 'TotaleProvvigioneDaFatturare'
        WHEN 6 THEN 'TotaleProvvigione'
        WHEN 7 THEN 'TotaleProvvigione'
        WHEN 8 THEN 'TotaleProvvigioneFattura'
        WHEN 9 THEN 'TotaleProvvigione'
    END;

    -- 🔴 CONTROLLO SICUREZZA
    IF @NomeVista IS NULL OR @ColonnaImporto IS NULL 
    BEGIN
        RAISERROR('Vista o colonna non configurata per IdMandatario=%d',16,1,@IdMandatario);
        RETURN;
    END;

    -- 3. COSTRUZIONE QUERY DINAMICA
    SET @SQL = N'
    WITH DatiMensili AS (
        SELECT 
            Mese,
            RagioneSociale AS Cliente,
            COUNT(DISTINCT NumeroFattura) AS NrFatt, 
            SUM(Quantita) AS Kg,
            SUM(' + QUOTENAME(@ColonnaImporto) + N') AS Importo,
            ROW_NUMBER() OVER(
                PARTITION BY Mese 
                ORDER BY SUM(' + QUOTENAME(@ColonnaImporto) + N') DESC
            ) AS Posizione
        FROM [dbo].' + QUOTENAME(@NomeVista) + N'
        WHERE Anno = @Anno 
          AND (IdMandatario = @IdMandatario OR IdMandatario = 0)
          AND Fatturata = 1
        GROUP BY Mese, RagioneSociale
        HAVING SUM(' + QUOTENAME(@ColonnaImporto) + N') > 0 
    )
    
    SELECT 
        MAX(CASE WHEN Mese = 1 THEN NrFatt END) AS Gen_NrFatt,
        MAX(CASE WHEN Mese = 1 THEN Cliente END) AS Gen_Cliente,
        MAX(CASE WHEN Mese = 1 THEN Kg END) AS Gen_Kg,
        MAX(CASE WHEN Mese = 1 THEN Importo END) AS Gen_Importo,
        
        MAX(CASE WHEN Mese = 2 THEN NrFatt END) AS Feb_NrFatt,
        MAX(CASE WHEN Mese = 2 THEN Cliente END) AS Feb_Cliente,
        MAX(CASE WHEN Mese = 2 THEN Kg END) AS Feb_Kg,
        MAX(CASE WHEN Mese = 2 THEN Importo END) AS Feb_Importo,

        MAX(CASE WHEN Mese = 3 THEN NrFatt END) AS Mar_NrFatt,
        MAX(CASE WHEN Mese = 3 THEN Cliente END) AS Mar_Cliente,
        MAX(CASE WHEN Mese = 3 THEN Kg END) AS Mar_Kg,
        MAX(CASE WHEN Mese = 3 THEN Importo END) AS Mar_Importo,

        MAX(CASE WHEN Mese = 4 THEN NrFatt END) AS Apr_NrFatt,
        MAX(CASE WHEN Mese = 4 THEN Cliente END) AS Apr_Cliente,
        MAX(CASE WHEN Mese = 4 THEN Kg END) AS Apr_Kg,
        MAX(CASE WHEN Mese = 4 THEN Importo END) AS Apr_Importo,

        MAX(CASE WHEN Mese = 5 THEN NrFatt END) AS Mag_NrFatt,
        MAX(CASE WHEN Mese = 5 THEN Cliente END) AS Mag_Cliente,
        MAX(CASE WHEN Mese = 5 THEN Kg END) AS Mag_Kg,
        MAX(CASE WHEN Mese = 5 THEN Importo END) AS Mag_Importo,

        MAX(CASE WHEN Mese = 6 THEN NrFatt END) AS Giu_NrFatt,
        MAX(CASE WHEN Mese = 6 THEN Cliente END) AS Giu_Cliente,
        MAX(CASE WHEN Mese = 6 THEN Kg END) AS Giu_Kg,
        MAX(CASE WHEN Mese = 6 THEN Importo END) AS Giu_Importo,

        MAX(CASE WHEN Mese = 7 THEN NrFatt END) AS Lug_NrFatt,
        MAX(CASE WHEN Mese = 7 THEN Cliente END) AS Lug_Cliente,
        MAX(CASE WHEN Mese = 7 THEN Kg END) AS Lug_Kg,
        MAX(CASE WHEN Mese = 7 THEN Importo END) AS Lug_Importo,

        MAX(CASE WHEN Mese = 8 THEN NrFatt END) AS Ago_NrFatt,
        MAX(CASE WHEN Mese = 8 THEN Cliente END) AS Ago_Cliente,
        MAX(CASE WHEN Mese = 8 THEN Kg END) AS Ago_Kg,
        MAX(CASE WHEN Mese = 8 THEN Importo END) AS Ago_Importo,

        MAX(CASE WHEN Mese = 9 THEN NrFatt END) AS Set_NrFatt,
        MAX(CASE WHEN Mese = 9 THEN Cliente END) AS Set_Cliente,
        MAX(CASE WHEN Mese = 9 THEN Kg END) AS Set_Kg,
        MAX(CASE WHEN Mese = 9 THEN Importo END) AS Set_Importo,

        MAX(CASE WHEN Mese = 10 THEN NrFatt END) AS Ott_NrFatt,
        MAX(CASE WHEN Mese = 10 THEN Cliente END) AS Ott_Cliente,
        MAX(CASE WHEN Mese = 10 THEN Kg END) AS Ott_Kg,
        MAX(CASE WHEN Mese = 10 THEN Importo END) AS Ott_Importo,

        MAX(CASE WHEN Mese = 11 THEN NrFatt END) AS Nov_NrFatt,
        MAX(CASE WHEN Mese = 11 THEN Cliente END) AS Nov_Cliente,
        MAX(CASE WHEN Mese = 11 THEN Kg END) AS Nov_Kg,
        MAX(CASE WHEN Mese = 11 THEN Importo END) AS Nov_Importo,

        MAX(CASE WHEN Mese = 12 THEN NrFatt END) AS Dic_NrFatt,
        MAX(CASE WHEN Mese = 12 THEN Cliente END) AS Dic_Cliente,
        MAX(CASE WHEN Mese = 12 THEN Kg END) AS Dic_Kg,
        MAX(CASE WHEN Mese = 12 THEN Importo END) AS Dic_Importo

    FROM DatiMensili
    GROUP BY Posizione
    ORDER BY Posizione;';

    -- 4. DEBUG SICURO (NON ROMPE IL READER)
    DECLARE @SQL_LOG NVARCHAR(MAX);
    SET @SQL_LOG = REPLACE(@SQL, '@Anno', CAST(@Anno AS NVARCHAR(10)));
    SET @SQL_LOG = REPLACE(@SQL_LOG, '@IdMandatario', CAST(@IdMandatario AS NVARCHAR(10)));

    PRINT '--- DEBUG: QUERY CON VALORI REALI ---';
    PRINT @SQL_LOG;
    PRINT '--- FINE DEBUG ---';

    -- ❌ NIENTE SELECT QUI

    -- 5. ESECUZIONE
    EXEC sp_executesql 
        @SQL, 
        N'@Anno INT, @IdMandatario INT', 
        @Anno, 
        @IdMandatario;
END
GO


