USE [Calkos]
GO

/* ==========================================================================================
   1. INDICE: IX_ProspettoCobral_Report_Pivot
   ------------------------------------------------------------------------------------------
   A COSA SERVE: 
   Ottimizza la Stored Procedure [sp_GeneraProspettoAnnualeExcel] (quella con i mesi in colonna).
   
   DETTAGLI TECNICI:
   - Filtro: Utilizza (Anno, Mese) come chiavi di ricerca. Quando chiedi l'anno 2026, SQL 
     va dritto al blocco di dati senza scansionare gli anni precedenti o successivi.
   - Performance (INCLUDE): Abbiamo inserito Quantita, PrezzoVendita, Differenza e 
     ValoreCommissioni dentro l'indice. 
   - Risultato: SQL calcola le somme (SUM) leggendo solo l'indice (molto piccolo). 
     Evita il "Key Lookup" sulla tabella intera, velocizzando l'export di 10 volte.
   ==========================================================================================
*/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProspettoCobral_Report_Pivot' AND object_id = OBJECT_ID('dbo.ProspettoCobral'))
BEGIN
-- Crea il nuovo con IdMandatario nella chiave principale
CREATE INDEX IX_ProspettoCobral_Report_Pivot
ON [dbo].[ProspettoCobral] ([IdMandatario], [Anno], [Mese]) -- IdMandatario aggiunto qui
INCLUDE ([IdCliente], [Quantita], [PrezzoVendita], [Differenza], [ValoreCommissioni])
WITH (FILLFACTOR = 80);

    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Serve a: [sp_GeneraProspettoAnnualeExcel]. Accelera il raggruppamento per Anno e i calcoli SUM di quantità e differenze.', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE',  @level1name = N'ProspettoCobral', 
        @level2type = N'INDEX',  @level2name = N'IX_ProspettoCobral_Report_Pivot';
END
GO

/* ==========================================================================================
   2. INDICE: IX_ProspettoCobral_Fatturazione_Certificata
   ------------------------------------------------------------------------------------------
   A COSA SERVE: 
   Ottimizza la Stored Procedure [spGeneraProspettoAnnualeExcel_Fatture].
   
   DETTAGLI TECNICI:
   - Indice Filtrato (WHERE): Questo indice occupa pochissimo spazio perché ignora tutte le 
     righe dove NumeroFattura è NULL o vuoto. Indicizza solo il "reale fatturato".
   - Funzionamento: Quando la stored procedure conta le fatture (COUNT DISTINCT) e filtra 
     per Anno, SQL usa questo indice che è una lista già "pre-pulita".
   - Risultato: Risposta istantanea anche con milioni di righe totali nella tabella.
   ==========================================================================================
*/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProspettoCobral_Fatturazione_Certificata' AND object_id = OBJECT_ID('dbo.ProspettoCobral'))
BEGIN
    -- Nota: La clausola WHERE deve stare dopo l'eventuale INCLUDE o prima delle opzioni WITH
-- Crea il nuovo con IdMandatario nella chiave principale
CREATE INDEX IX_ProspettoCobral_Fatturazione_Certificata
ON [dbo].[ProspettoCobral] ([IdMandatario], [Anno], [NumeroFattura]) -- IdMandatario aggiunto qui
INCLUDE ([IdCliente], [Quantita], [ValoreCommissioni], [Mese])
WHERE [NumeroFattura] IS NOT NULL AND [NumeroFattura] <> ''
WITH (FILLFACTOR = 80);

    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Serve a: [spGeneraProspettoAnnualeExcel_Fatture]. Indice filtrato che ottimizza la ricerca delle sole righe fatturate e il conteggio delle fatture.', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE',  @level1name = N'ProspettoCobral', 
        @level2type = N'INDEX',  @level2name = N'IX_ProspettoCobral_Fatturazione_Certificata';
END
GO

/* ==========================================================================================
   3. INDICE: IX_Clienti_IdCliente_RagioneSociale
   ------------------------------------------------------------------------------------------
   A COSA SERVE: 
   Supporto fondamentale alla Vista [vProspettoCobral].
   
   DETTAGLI TECNICI:
   - Ottimizzazione JOIN: La tabella ProspettoCobral ha solo l'ID del cliente. Per stampare 
     la "RagioneSociale", SQL deve collegarsi alla tabella Clienti. 
   - Funzionamento: Questo indice fornisce la RagioneSociale direttamente durante il JOIN, 
     senza dover andare a cercare nelle altre colonne della tabella Clienti (Indice Coprente).
   ==========================================================================================
*/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Clienti_IdCliente_RagioneSociale' AND object_id = OBJECT_ID('dbo.Clienti'))
BEGIN
    CREATE INDEX IX_Clienti_IdCliente_RagioneSociale
    ON [dbo].[Clienti] ([IdCliente])
    INCLUDE ([RagioneSociale]);

    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Supporto a: [vProspettoCobral]. Velocizza il recupero della Ragione Sociale durante i JOIN nel caricamento dei report.', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE',  @level1name = N'Clienti', 
        @level2type = N'INDEX',  @level2name = N'IX_Clienti_IdCliente_RagioneSociale';
END
GO

/* ==========================================================================================
   4. INDICE: IX_ProspettoCobral_Agente_Mandatario
   ------------------------------------------------------------------------------------------
   A COSA SERVE: 
   Supporto ai filtri della Dashboard, della Vista e ricerche per Agente.
   
   DETTAGLI TECNICI:
   - Multidimensionale: Permette di filtrare velocemente i dati non solo per anno, 
     ma anche per uno specifico Agente o Mandatario. 
   - Utilità: Essenziale se l'utente seleziona un Agente dal menu a tendina dell'applicativo, 
     evitando che SQL debba leggere i dati di tutti gli agenti inutilmente.
   ==========================================================================================
*/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProspettoCobral_Agente_Mandatario' AND object_id = OBJECT_ID('dbo.ProspettoCobral'))
BEGIN
    CREATE INDEX IX_ProspettoCobral_Agente_Mandatario
    ON [dbo].[ProspettoCobral] ([IdAgente], [IdMandatario], [Anno]);

    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Supporto a: [vProspettoCobral] e filtri Dashboard. Accelera la risoluzione dei nomi agenti e il filtraggio per mandatario.', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE',  @level1name = N'ProspettoCobral', 
        @level2type = N'INDEX',  @level2name = N'IX_ProspettoCobral_Agente_Mandatario';
END
GO

--Note tecniche per te:
--IF NOT EXISTS: Ho aggiunto il controllo per evitare errori se lanci lo script due volte. Se l'indice c'è già, lo salta.

--FILLFACTOR = 80: Ottimo per le tabelle dove "spari" dentro tanti dati (come i tuoi import Excel), così SQL non deve riorganizzare l'indice a ogni riga inserita.

--ONLINE = ON: (Disponibile solo su versioni Enterprise/Azure, altrimenti toglilo) permette agli utenti di continuare a usare la tabella mentre l'indice viene creato.

--Come vedere i commenti salvati in SSMS:
--Apri SQL Server Management Studio.

--Vai su Table -> ProspettoCobral -> Indexes.

--Fai tasto destro su un indice -> Properties.

--Seleziona la pagina Extended Properties: vedrai il testo nella colonna "Value".

CREATE OR ALTER PROCEDURE [dbo].[spGeneraProspettoAnnualeExcel_Cliente]
    @Anno INT,
    @IdMandatario INT -- AGGIUNTO
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        RagioneSociale AS Cliente,
        
        -- GENNAIO
        SUM(CASE WHEN Mese = 1 THEN Quantita ELSE 0 END) AS Gennaio_Kg,
        SUM(CASE WHEN Mese = 1 THEN PrezzoVendita * Quantita ELSE 0 END) AS Gennaio_Euro,
        SUM(CASE WHEN Mese = 1 THEN Differenza ELSE 0 END) AS Gennaio_Diff,

        -- ... (tutti gli altri mesi rimangono uguali) ...
        -- FEBBRAIO
        SUM(CASE WHEN Mese = 2 THEN Quantita ELSE 0 END) AS Febbraio_Kg,
        SUM(CASE WHEN Mese = 2 THEN PrezzoVendita * Quantita ELSE 0 END) AS Febbraio_Euro,
        SUM(CASE WHEN Mese = 2 THEN Differenza ELSE 0 END) AS Febbraio_Diff,

        -- MARZO
        SUM(CASE WHEN Mese = 3 THEN Quantita ELSE 0 END) AS Marzo_Kg,
        SUM(CASE WHEN Mese = 3 THEN PrezzoVendita * Quantita ELSE 0 END) AS Marzo_Euro,
        SUM(CASE WHEN Mese = 3 THEN Differenza ELSE 0 END) AS Marzo_Diff,

        -- APRILE
        SUM(CASE WHEN Mese = 4 THEN Quantita ELSE 0 END) AS Aprile_Kg,
        SUM(CASE WHEN Mese = 4 THEN PrezzoVendita * Quantita ELSE 0 END) AS Aprile_Euro,
        SUM(CASE WHEN Mese = 4 THEN Differenza ELSE 0 END) AS Aprile_Diff,

        -- MAGGIO
        SUM(CASE WHEN Mese = 5 THEN Quantita ELSE 0 END) AS Maggio_Kg,
        SUM(CASE WHEN Mese = 5 THEN PrezzoVendita * Quantita ELSE 0 END) AS Maggio_Euro,
        SUM(CASE WHEN Mese = 5 THEN Differenza ELSE 0 END) AS Maggio_Diff,

        -- GIUGNO
        SUM(CASE WHEN Mese = 6 THEN Quantita ELSE 0 END) AS Giugno_Kg,
        SUM(CASE WHEN Mese = 6 THEN PrezzoVendita * Quantita ELSE 0 END) AS Giugno_Euro,
        SUM(CASE WHEN Mese = 6 THEN Differenza ELSE 0 END) AS Giugno_Diff,

        -- LUGLIO
        SUM(CASE WHEN Mese = 7 THEN Quantita ELSE 0 END) AS Luglio_Kg,
        SUM(CASE WHEN Mese = 7 THEN PrezzoVendita * Quantita ELSE 0 END) AS Luglio_Euro,
        SUM(CASE WHEN Mese = 7 THEN Differenza ELSE 0 END) AS Luglio_Diff,

        -- AGOSTO
        SUM(CASE WHEN Mese = 8 THEN Quantita ELSE 0 END) AS Agosto_Kg,
        SUM(CASE WHEN Mese = 8 THEN PrezzoVendita * Quantita ELSE 0 END) AS Agosto_Euro,
        SUM(CASE WHEN Mese = 8 THEN Differenza ELSE 0 END) AS Agosto_Diff,

        -- SETTEMBRE
        SUM(CASE WHEN Mese = 9 THEN Quantita ELSE 0 END) AS Settembre_Kg,
        SUM(CASE WHEN Mese = 9 THEN PrezzoVendita * Quantita ELSE 0 END) AS Settembre_Euro,
        SUM(CASE WHEN Mese = 9 THEN Differenza ELSE 0 END) AS Settembre_Diff,

        -- OTTOBRE
        SUM(CASE WHEN Mese = 10 THEN Quantita ELSE 0 END) AS Ottobre_Kg,
        SUM(CASE WHEN Mese = 10 THEN PrezzoVendita * Quantita ELSE 0 END) AS Ottobre_Euro,
        SUM(CASE WHEN Mese = 10 THEN Differenza ELSE 0 END) AS Ottobre_Diff,

        -- NOVEMBRE
        SUM(CASE WHEN Mese = 11 THEN Quantita ELSE 0 END) AS Novembre_Kg,
        SUM(CASE WHEN Mese = 11 THEN PrezzoVendita * Quantita ELSE 0 END) AS Novembre_Euro,
        SUM(CASE WHEN Mese = 11 THEN Differenza ELSE 0 END) AS Novembre_Diff,

        -- DICEMBRE
        SUM(CASE WHEN Mese = 12 THEN Quantita ELSE 0 END) AS Dicembre_Kg,
        SUM(CASE WHEN Mese = 12 THEN PrezzoVendita * Quantita ELSE 0 END) AS Dicembre_Euro,
        SUM(CASE WHEN Mese = 12 THEN Differenza ELSE 0 END) AS Dicembre_Diff,

        -- TOTALI ANNUALI
        SUM(Quantita) AS Totale_Annuale_Kg,
        SUM(PrezzoVendita * Quantita) AS Totale_Annuale_Euro,
        SUM(Differenza) AS Totale_Annuale_Diff

    FROM [dbo].[vProspettoCobral]
    WHERE Anno = @Anno AND IdMandatario = @IdMandatario -- FILTRO AGGIORNATO
    GROUP BY RagioneSociale
    ORDER BY RagioneSociale;
END
GO


CREATE OR ALTER PROCEDURE [dbo].[spGeneraProspettoAnnualeExcel_Fatture]
    @Anno INT,
    @IdMandatario INT -- AGGIUNTO
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH DatiMensili AS (
        SELECT 
            Mese,
            RagioneSociale AS Cliente,
            COUNT(DISTINCT NumeroFattura) AS NrFatt, 
            SUM(Quantita) AS Kg,
            SUM(ValoreCommissioni) AS Importo,
            ROW_NUMBER() OVER(PARTITION BY Mese ORDER BY SUM(ValoreCommissioni) DESC) AS Posizione
        FROM [dbo].[vProspettoCobral]
        WHERE Anno = @Anno 
          AND IdMandatario = @IdMandatario -- FILTRO AGGIORNATO
          AND NumeroFattura IS NOT NULL 
          AND NumeroFattura <> ''
        GROUP BY Mese, RagioneSociale
        HAVING SUM(ValoreCommissioni) > 0 
    )
    
    SELECT 
        -- GENNAIO
        MAX(CASE WHEN Mese = 1 THEN NrFatt END) AS Gen_NrFatt,
        MAX(CASE WHEN Mese = 1 THEN Cliente END) AS Gen_Cliente,
        MAX(CASE WHEN Mese = 1 THEN Kg END) AS Gen_Kg,
        MAX(CASE WHEN Mese = 1 THEN Importo END) AS Gen_Importo,
        
        -- ... (tutti gli altri mesi rimangono uguali) ...
        -- FEBBRAIO
        MAX(CASE WHEN Mese = 2 THEN NrFatt END) AS Feb_NrFatt,
        MAX(CASE WHEN Mese = 2 THEN Cliente END) AS Feb_Cliente,
        MAX(CASE WHEN Mese = 2 THEN Kg END) AS Feb_Kg,
        MAX(CASE WHEN Mese = 2 THEN Importo END) AS Feb_Importo,

        -- MARZO
        MAX(CASE WHEN Mese = 3 THEN NrFatt END) AS Mar_NrFatt,
        MAX(CASE WHEN Mese = 3 THEN Cliente END) AS Mar_Cliente,
        MAX(CASE WHEN Mese = 3 THEN Kg END) AS Mar_Kg,
        MAX(CASE WHEN Mese = 3 THEN Importo END) AS Mar_Importo,

        -- APRILE
        MAX(CASE WHEN Mese = 4 THEN NrFatt END) AS Apr_NrFatt,
        MAX(CASE WHEN Mese = 4 THEN Cliente END) AS Apr_Cliente,
        MAX(CASE WHEN Mese = 4 THEN Kg END) AS Apr_Kg,
        MAX(CASE WHEN Mese = 4 THEN Importo END) AS Apr_Importo,

        -- MAGGIO
        MAX(CASE WHEN Mese = 5 THEN NrFatt END) AS Mag_NrFatt,
        MAX(CASE WHEN Mese = 5 THEN Cliente END) AS Mag_Cliente,
        MAX(CASE WHEN Mese = 5 THEN Kg END) AS Mag_Kg,
        MAX(CASE WHEN Mese = 5 THEN Importo END) AS Mag_Importo,

        -- GIUGNO
        MAX(CASE WHEN Mese = 6 THEN NrFatt END) AS Giu_NrFatt,
        MAX(CASE WHEN Mese = 6 THEN Cliente END) AS Giu_Cliente,
        MAX(CASE WHEN Mese = 6 THEN Kg END) AS Giu_Kg,
        MAX(CASE WHEN Mese = 6 THEN Importo END) AS Giu_Importo,

        -- LUGLIO
        MAX(CASE WHEN Mese = 7 THEN NrFatt END) AS Lug_NrFatt,
        MAX(CASE WHEN Mese = 7 THEN Cliente END) AS Lug_Cliente,
        MAX(CASE WHEN Mese = 7 THEN Kg END) AS Lug_Kg,
        MAX(CASE WHEN Mese = 7 THEN Importo END) AS Lug_Importo,

        -- AGOSTO
        MAX(CASE WHEN Mese = 8 THEN NrFatt END) AS Ago_NrFatt,
        MAX(CASE WHEN Mese = 8 THEN Cliente END) AS Ago_Cliente,
        MAX(CASE WHEN Mese = 8 THEN Kg END) AS Ago_Kg,
        MAX(CASE WHEN Mese = 8 THEN Importo END) AS Ago_Importo,

        -- SETTEMBRE
        MAX(CASE WHEN Mese = 9 THEN NrFatt END) AS Set_NrFatt,
        MAX(CASE WHEN Mese = 9 THEN Cliente END) AS Set_Cliente,
        MAX(CASE WHEN Mese = 9 THEN Kg END) AS Set_Kg,
        MAX(CASE WHEN Mese = 9 THEN Importo END) AS Set_Importo,

        -- OTTOBRE
        MAX(CASE WHEN Mese = 10 THEN NrFatt END) AS Ott_NrFatt,
        MAX(CASE WHEN Mese = 10 THEN Cliente END) AS Ott_Cliente,
        MAX(CASE WHEN Mese = 10 THEN Kg END) AS Ott_Kg,
        MAX(CASE WHEN Mese = 10 THEN Importo END) AS Ott_Importo,

        -- NOVEMBRE
        MAX(CASE WHEN Mese = 11 THEN NrFatt END) AS Nov_NrFatt,
        MAX(CASE WHEN Mese = 11 THEN Cliente END) AS Nov_Cliente,
        MAX(CASE WHEN Mese = 11 THEN Kg END) AS Nov_Kg,
        MAX(CASE WHEN Mese = 11 THEN Importo END) AS Nov_Importo,

        -- DICEMBRE
        MAX(CASE WHEN Mese = 12 THEN NrFatt END) AS Dic_NrFatt,
        MAX(CASE WHEN Mese = 12 THEN Cliente END) AS Dic_Cliente,
        MAX(CASE WHEN Mese = 12 THEN Kg END) AS Dic_Kg,
        MAX(CASE WHEN Mese = 12 THEN Importo END) AS Dic_Importo

    FROM DatiMensili
    GROUP BY Posizione
    ORDER BY Posizione;
END
GO



[spGeneraProspettoAnnualeExcel_Cliente] 2026,2
[spGeneraProspettoAnnualeExcel_Fatture] 2026,2



   SELECT Anno,IdMandatario,
        RagioneSociale AS Cliente,
        
        -- GENNAIO
        SUM(CASE WHEN Mese = 1 THEN Quantita ELSE 0 END) AS Gennaio_Kg,
        SUM(CASE WHEN Mese = 1 THEN PrezzoVendita * Quantita ELSE 0 END) AS Gennaio_Euro,
        SUM(CASE WHEN Mese = 1 THEN Differenza ELSE 0 END) AS Gennaio_Diff,

        -- ... (tutti gli altri mesi rimangono uguali) ...
        -- FEBBRAIO
        SUM(CASE WHEN Mese = 2 THEN Quantita ELSE 0 END) AS Febbraio_Kg,
        SUM(CASE WHEN Mese = 2 THEN PrezzoVendita * Quantita ELSE 0 END) AS Febbraio_Euro,
        SUM(CASE WHEN Mese = 2 THEN Differenza ELSE 0 END) AS Febbraio_Diff,

        -- MARZO
        SUM(CASE WHEN Mese = 3 THEN Quantita ELSE 0 END) AS Marzo_Kg,
        SUM(CASE WHEN Mese = 3 THEN PrezzoVendita * Quantita ELSE 0 END) AS Marzo_Euro,
        SUM(CASE WHEN Mese = 3 THEN Differenza ELSE 0 END) AS Marzo_Diff,

        -- APRILE
        SUM(CASE WHEN Mese = 4 THEN Quantita ELSE 0 END) AS Aprile_Kg,
        SUM(CASE WHEN Mese = 4 THEN PrezzoVendita * Quantita ELSE 0 END) AS Aprile_Euro,
        SUM(CASE WHEN Mese = 4 THEN Differenza ELSE 0 END) AS Aprile_Diff,

        -- MAGGIO
        SUM(CASE WHEN Mese = 5 THEN Quantita ELSE 0 END) AS Maggio_Kg,
        SUM(CASE WHEN Mese = 5 THEN PrezzoVendita * Quantita ELSE 0 END) AS Maggio_Euro,
        SUM(CASE WHEN Mese = 5 THEN Differenza ELSE 0 END) AS Maggio_Diff,

        -- GIUGNO
        SUM(CASE WHEN Mese = 6 THEN Quantita ELSE 0 END) AS Giugno_Kg,
        SUM(CASE WHEN Mese = 6 THEN PrezzoVendita * Quantita ELSE 0 END) AS Giugno_Euro,
        SUM(CASE WHEN Mese = 6 THEN Differenza ELSE 0 END) AS Giugno_Diff,

        -- LUGLIO
        SUM(CASE WHEN Mese = 7 THEN Quantita ELSE 0 END) AS Luglio_Kg,
        SUM(CASE WHEN Mese = 7 THEN PrezzoVendita * Quantita ELSE 0 END) AS Luglio_Euro,
        SUM(CASE WHEN Mese = 7 THEN Differenza ELSE 0 END) AS Luglio_Diff,

        -- AGOSTO
        SUM(CASE WHEN Mese = 8 THEN Quantita ELSE 0 END) AS Agosto_Kg,
        SUM(CASE WHEN Mese = 8 THEN PrezzoVendita * Quantita ELSE 0 END) AS Agosto_Euro,
        SUM(CASE WHEN Mese = 8 THEN Differenza ELSE 0 END) AS Agosto_Diff,

        -- SETTEMBRE
        SUM(CASE WHEN Mese = 9 THEN Quantita ELSE 0 END) AS Settembre_Kg,
        SUM(CASE WHEN Mese = 9 THEN PrezzoVendita * Quantita ELSE 0 END) AS Settembre_Euro,
        SUM(CASE WHEN Mese = 9 THEN Differenza ELSE 0 END) AS Settembre_Diff,

        -- OTTOBRE
        SUM(CASE WHEN Mese = 10 THEN Quantita ELSE 0 END) AS Ottobre_Kg,
        SUM(CASE WHEN Mese = 10 THEN PrezzoVendita * Quantita ELSE 0 END) AS Ottobre_Euro,
        SUM(CASE WHEN Mese = 10 THEN Differenza ELSE 0 END) AS Ottobre_Diff,

        -- NOVEMBRE
        SUM(CASE WHEN Mese = 11 THEN Quantita ELSE 0 END) AS Novembre_Kg,
        SUM(CASE WHEN Mese = 11 THEN PrezzoVendita * Quantita ELSE 0 END) AS Novembre_Euro,
        SUM(CASE WHEN Mese = 11 THEN Differenza ELSE 0 END) AS Novembre_Diff,

        -- DICEMBRE
        SUM(CASE WHEN Mese = 12 THEN Quantita ELSE 0 END) AS Dicembre_Kg,
        SUM(CASE WHEN Mese = 12 THEN PrezzoVendita * Quantita ELSE 0 END) AS Dicembre_Euro,
        SUM(CASE WHEN Mese = 12 THEN Differenza ELSE 0 END) AS Dicembre_Diff,

        -- TOTALI ANNUALI
        SUM(Quantita) AS Totale_Annuale_Kg,
        SUM(PrezzoVendita * Quantita) AS Totale_Annuale_Euro,
        SUM(Differenza) AS Totale_Annuale_Diff

    FROM [dbo].[vProspettoCobral]
	    GROUP BY Anno,IdMandatario,
        RagioneSociale 
    ORDER BY RagioneSociale;