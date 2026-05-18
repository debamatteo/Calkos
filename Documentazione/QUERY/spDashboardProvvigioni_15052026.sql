ALTER PROCEDURE [dbo].[spDashboardProvvigioni_CST]  
(  
    @Anno INT,  
    @Mese INT,  
    @IdAgente INT = NULL,
    @Fatturata INT = NULL  -- NULL=Tutti, 0=Non Fatturati, 1=Fatturati
)  
AS  
BEGIN  
    SET NOCOUNT ON;  

    SELECT   
        p.IdCliente,  
        c.RagioneSociale AS NomeCliente,  
        ISNULL(p.IdAgente,0) AS IdAgente,
        ISNULL(a.AgenteDescrizione,'') AS AgenteDescrizione,  
        p.IdTipoPagamento,  
        tp.TipoPagamento,
        -- Percentuale calcolata tramite funzione (calcolata una sola volta per riga tramite CROSS APPLY)
        ca.ProvvigioneAgente AS ProvvigioneAgente,
        SUM(p.TotaleProvvigioneFattura * ca.ProvvigioneAgente / 100.0) AS ValoreProvvigioneAgente,
        p.Anno,  
        p.Mese,  
        tp.OrdineVisualizzazione,
        SUM(p.Quantita) AS Quantita,  
        --SUM(p.Quantita * p.Prezzo) AS Importo,  
        SUM(p.TotaleProvvigioneFattura) AS CommissioniTotali  
    FROM ProspettoCST p  
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente  
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento  
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente  
    -- Ottimizzazione: calcolo della percentuale una sola volta per riga
    CROSS APPLY (
        SELECT dbo.fn_GetPercentualeProvvigione(ISNULL(p.IdAgente,0), p.IdCliente, ISNULL(p.IdMandatario,0)) AS ProvvigioneAgente
    ) ca
    WHERE p.Anno = @Anno  
      AND p.Mese = @Mese  
      AND tp.VisualizzaInDashboard = 1  
      AND (@IdAgente IS NULL OR p.IdAgente = @IdAgente)  
      AND (@Fatturata IS NULL OR ISNULL(p.Fatturata, 0) = @Fatturata)

    GROUP BY  
        p.IdCliente, 
        c.RagioneSociale, 
        ISNULL(p.IdAgente,0), 
        ISNULL(a.AgenteDescrizione,''),  
        p.IdTipoPagamento, 
        tp.TipoPagamento, 
        ca.ProvvigioneAgente,  
        p.Anno, 
        p.Mese, 
        tp.OrdineVisualizzazione  
    ORDER BY   
        tp.OrdineVisualizzazione, 
        c.RagioneSociale;
END
GO

ALTER PROCEDURE [dbo].[spDashboardProvvigioni_DeAngeli]  
(  
    @Anno INT,  
    @Mese INT,  
    @IdAgente INT = NULL,
    @Fatturata INT = NULL  -- NULL=Tutti, 0=Non Fatturati, 1=Fatturati
)  
AS  
BEGIN  
    SET NOCOUNT ON;  

    SELECT   
        p.IdCliente,  
        c.RagioneSociale AS NomeCliente,  
        ISNULL(p.IdAgente,0) AS IdAgente, 
        ISNULL(a.AgenteDescrizione,'') AS AgenteDescrizione,  
        p.IdTipoPagamento,  
        tp.TipoPagamento,
        -- Percentuale calcolata tramite funzione (calcolata una sola volta per riga tramite CROSS APPLY)
        ca.ProvvigioneAgente AS ProvvigioneAgente,
        SUM(p.TotaleProvvigioneDaFatturare * ca.ProvvigioneAgente / 100.0) AS ValoreProvvigioneAgente,
        p.Anno,  
        p.Mese,  
        tp.OrdineVisualizzazione,
        SUM(p.Quantita) AS Quantita,  
        --SUM(p.Quantita * p.Prezzo) AS Importo,  
        SUM(p.TotaleProvvigioneDaFatturare) AS CommissioniTotali  
    FROM ProspettoDeAngeli p  
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente  
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento  
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente  
    -- Ottimizzazione: calcolo della percentuale una sola volta per riga
    CROSS APPLY (
        SELECT dbo.fn_GetPercentualeProvvigione(ISNULL(p.IdAgente,0), p.IdCliente, ISNULL(p.IdMandatario,0)) AS ProvvigioneAgente
    ) ca
    WHERE p.Anno = @Anno  
      AND p.Mese = @Mese  
      AND tp.VisualizzaInDashboard = 1  
      AND (@IdAgente IS NULL OR p.IdAgente = @IdAgente)  
      AND (@Fatturata IS NULL OR ISNULL(p.Fatturata, 0) = @Fatturata)

    GROUP BY  
        p.IdCliente, 
        c.RagioneSociale, 
        ISNULL(p.IdAgente,0), 
        ISNULL(a.AgenteDescrizione,''),  
        p.IdTipoPagamento, 
        tp.TipoPagamento, 
        ca.ProvvigioneAgente,  
        p.Anno, 
        p.Mese, 
        tp.OrdineVisualizzazione  
    ORDER BY   
        tp.OrdineVisualizzazione, 
        c.RagioneSociale;
END
GO



ALTER PROCEDURE [dbo].[spDashboardProvvigioni_ElektraWire]  
(  
    @Anno INT,  
    @Mese INT,  
    @IdAgente INT = NULL,
    @Fatturata INT = NULL  -- NULL=Tutti, 0=Non Fatturati, 1=Fatturati
)  
AS  
BEGIN  
    SET NOCOUNT ON;  

    SELECT   
        p.IdCliente,  
        c.RagioneSociale AS NomeCliente,  
        ISNULL(p.IdAgente,0) AS IdAgente,  
        ISNULL(a.AgenteDescrizione,'') AS AgenteDescrizione,   
        p.IdTipoPagamento,  
        tp.TipoPagamento,
        -- Percentuale calcolata tramite funzione (calcolata una sola volta per riga tramite CROSS APPLY)
        ca.ProvvigioneAgente AS ProvvigioneAgente,
        SUM(p.TotaleProvvigioneDaFatturare * ca.ProvvigioneAgente / 100.0) AS ValoreProvvigioneAgente,
        p.Anno,  
        p.Mese,  
        tp.OrdineVisualizzazione,
        SUM(p.Quantita) AS Quantita,  
        --SUM(p.Quantita * p.Prezzo) AS Importo,  
        SUM(p.TotaleProvvigioneDaFatturare) AS CommissioniTotali  
    FROM ProspettoElektraWire p  
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente  
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento  
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente  
    -- Ottimizzazione: calcolo della percentuale una sola volta per riga
    CROSS APPLY (
        SELECT dbo.fn_GetPercentualeProvvigione(ISNULL(p.IdAgente,0), p.IdCliente, ISNULL(p.IdMandatario,0)) AS ProvvigioneAgente
    ) ca
    WHERE p.Anno = @Anno  
      AND p.Mese = @Mese  
      AND tp.VisualizzaInDashboard = 1  
      AND (@IdAgente IS NULL OR p.IdAgente = @IdAgente)  
      AND (@Fatturata IS NULL OR ISNULL(p.Fatturata, 0) = @Fatturata)

    GROUP BY  
        p.IdCliente, 
        c.RagioneSociale, 
        ISNULL(p.IdAgente,0), 
        ISNULL(a.AgenteDescrizione,''),  
        p.IdTipoPagamento, 
        tp.TipoPagamento, 
        ca.ProvvigioneAgente,  
        p.Anno, 
        p.Mese, 
        tp.OrdineVisualizzazione  
    ORDER BY   
        tp.OrdineVisualizzazione, 
        c.RagioneSociale;
END
GO




ALTER PROCEDURE [dbo].[spDashboardProvvigioni_Cobral]  
(  
    @Anno INT,  
    @Mese INT,  
    @IdAgente INT = NULL,
    @Fatturata INT = NULL  -- NULL=Tutti, 0=Non Fatturati, 1=Fatturati
)  
AS  
BEGIN  
    SET NOCOUNT ON;  

    SELECT   
        p.IdCliente,  
        c.RagioneSociale AS NomeCliente,  
        ISNULL(p.IdAgente, 0) AS IdAgente, 
        ISNULL(a.AgenteDescrizione, '') AS AgenteDescrizione,   
        p.IdTipoPagamento,  
        tp.TipoPagamento,

        -- Percentuale calcolata tramite funzione (calcolata una sola volta per riga tramite CROSS APPLY)
        ca.ProvvigioneAgente AS ProvvigioneAgente,
        SUM(p.ValoreCommissioni * ca.ProvvigioneAgente / 100.0) AS ValoreProvvigioneAgente, 
		
		p.Anno,  
        p.Mese,  
        tp.OrdineVisualizzazione,
        SUM(p.Quantita) AS Quantita,  
        SUM(p.ValoreCommissioni) AS CommissioniTotali  
    FROM ProspettoCobral p  
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente  
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento  
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente  
	    -- Ottimizzazione: calcolo della percentuale una sola volta per riga
    CROSS APPLY (
        SELECT dbo.fn_GetPercentualeProvvigione(ISNULL(p.IdAgente,0), p.IdCliente, ISNULL(p.IdMandatario,0)) AS ProvvigioneAgente
    ) ca
    WHERE p.Anno = @Anno  
      AND p.Mese = @Mese  
      AND tp.VisualizzaInDashboard = 1  
      AND (@IdAgente IS NULL OR p.IdAgente = @IdAgente)  
      AND (@Fatturata IS NULL OR ISNULL(p.Fatturata, 0) = @Fatturata)

    GROUP BY  
        p.IdCliente, 
        c.RagioneSociale, 
        ISNULL(p.IdAgente,0), 
        ISNULL(a.AgenteDescrizione,''), 
        p.IdTipoPagamento, 
        tp.TipoPagamento, 
        -- occorre raggruppare anche l'espressione della funzione perché è nella SELECT non aggregata
         ca.ProvvigioneAgente ,
        p.Anno, 
        p.Mese, 
        tp.OrdineVisualizzazione  
    ORDER BY   
        tp.OrdineVisualizzazione, 
        c.RagioneSociale;
END
GO




ALTER PROCEDURE [dbo].[spDashboardProvvigioni_Guerzoni]  
(  
    @Anno INT,  
    @Mese INT,  
    @IdAgente INT = NULL,
    @Fatturata INT = NULL  -- NULL=Tutti, 0=Non Fatturati, 1=Fatturati
)  
AS  
BEGIN  
    SET NOCOUNT ON;  

    SELECT   
        p.IdCliente,  
        c.RagioneSociale AS NomeCliente,  
        ISNULL(p.IdAgente,0) AS IdAgente,
        ISNULL(a.AgenteDescrizione,'') AS AgenteDescrizione,   
        p.IdTipoPagamento,  
        tp.TipoPagamento,
        -- Percentuale calcolata tramite funzione (calcolata una sola volta per riga tramite CROSS APPLY)
        ca.ProvvigioneAgente AS ProvvigioneAgente,
        SUM(p.TotaleProvvigione * ca.ProvvigioneAgente / 100.0) AS ValoreProvvigioneAgente,
        p.Anno,  
        p.Mese,  
        tp.OrdineVisualizzazione,
        SUM(p.Quantita) AS Quantita,  
        --SUM(p.Quantita * p.Prezzo) AS Importo,  
        SUM(p.TotaleProvvigione) AS CommissioniTotali  
    FROM ProspettoGuerzoni p  
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente  
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento  
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente  
    -- Ottimizzazione: calcolo della percentuale una sola volta per riga
    CROSS APPLY (
        SELECT dbo.fn_GetPercentualeProvvigione(ISNULL(p.IdAgente,0), p.IdCliente, ISNULL(p.IdMandatario,0)) AS ProvvigioneAgente
    ) ca
    WHERE p.Anno = @Anno  
      AND p.Mese = @Mese  
      AND tp.VisualizzaInDashboard = 1  
      AND (@IdAgente IS NULL OR p.IdAgente = @IdAgente)  
      AND (@Fatturata IS NULL OR ISNULL(p.Fatturata, 0) = @Fatturata)

    GROUP BY  
        p.IdCliente, 
        c.RagioneSociale, 
        ISNULL(p.IdAgente,0), 
        ISNULL(a.AgenteDescrizione,''),  
        p.IdTipoPagamento, 
        tp.TipoPagamento, 
        ca.ProvvigioneAgente,  
        p.Anno, 
        p.Mese, 
        tp.OrdineVisualizzazione  
    ORDER BY   
        tp.OrdineVisualizzazione, 
        c.RagioneSociale;
END
GO

ALTER PROCEDURE [dbo].[spDashboardProvvigioni_Hitech]  
(  
    @Anno INT,  
    @Mese INT,  
    @IdAgente INT = NULL,
    @Fatturata INT = NULL  -- NULL=Tutti, 0=Non Fatturati, 1=Fatturati
)  
AS  
BEGIN  
    SET NOCOUNT ON;  

    SELECT   
        p.IdCliente,  
        c.RagioneSociale AS NomeCliente,  
        ISNULL(p.IdAgente,0) AS IdAgente, 
        ISNULL(a.AgenteDescrizione,'') AS AgenteDescrizione,   
        p.IdTipoPagamento,  
        tp.TipoPagamento,
        -- Percentuale calcolata tramite funzione (calcolata una sola volta per riga tramite CROSS APPLY)
        ca.ProvvigioneAgente AS ProvvigioneAgente,
        SUM(p.TotaleProvvigione * ca.ProvvigioneAgente / 100.0) AS ValoreProvvigioneAgente,
        p.Anno,  
        p.Mese,  
        tp.OrdineVisualizzazione,
        SUM(p.Quantita) AS Quantita,  
        --SUM(p.Quantita * p.Prezzo) AS Importo,  
        SUM(p.TotaleProvvigione) AS CommissioniTotali  
    FROM ProspettoHitech p  
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente  
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento  
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente  
    -- Ottimizzazione: calcolo della percentuale una sola volta per riga
    CROSS APPLY (
        SELECT dbo.fn_GetPercentualeProvvigione(ISNULL(p.IdAgente,0), p.IdCliente, ISNULL(p.IdMandatario,0)) AS ProvvigioneAgente
    ) ca
    WHERE p.Anno = @Anno  
      AND p.Mese = @Mese  
      AND tp.VisualizzaInDashboard = 1  
      AND (@IdAgente IS NULL OR p.IdAgente = @IdAgente)  
      AND (@Fatturata IS NULL OR ISNULL(p.Fatturata, 0) = @Fatturata)

    GROUP BY  
        p.IdCliente, 
        c.RagioneSociale, 
        ISNULL(p.IdAgente,0), 
        ISNULL(a.AgenteDescrizione,''),  
        p.IdTipoPagamento, 
        tp.TipoPagamento, 
        ca.ProvvigioneAgente,  
        p.Anno, 
        p.Mese, 
        tp.OrdineVisualizzazione  
    ORDER BY   
        tp.OrdineVisualizzazione, 
        c.RagioneSociale;
END
GO


ALTER PROCEDURE [dbo].[spDashboardProvvigioni_SystemCore]  
(  
    @Anno INT,  
    @Mese INT,  
    @IdAgente INT = NULL,
    @Fatturata INT = NULL  -- NULL=Tutti, 0=Non Fatturati, 1=Fatturati
)  
AS  
BEGIN  
    SET NOCOUNT ON;  

    SELECT   
        p.IdCliente,  
        c.RagioneSociale AS NomeCliente,  
        ISNULL(p.IdAgente,0) AS IdAgente,  
        ISNULL(a.AgenteDescrizione,'') AS AgenteDescrizione,  
        p.IdTipoPagamento,  
        tp.TipoPagamento,
        -- Percentuale calcolata tramite funzione (calcolata una sola volta per riga tramite CROSS APPLY)
        ca.ProvvigioneAgente AS ProvvigioneAgente,
        SUM(p.TotaleProvvigioneDaFatturare * ca.ProvvigioneAgente / 100.0) AS ValoreProvvigioneAgente,
        p.Anno,  
        p.Mese,  
        tp.OrdineVisualizzazione,
        SUM(p.Quantita) AS Quantita,  
        --SUM(p.Quantita * p.Prezzo) AS Importo,  
        SUM(p.TotaleProvvigioneDaFatturare) AS CommissioniTotali  
    FROM ProspettoSystemCore p  
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente  
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento  
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente  
    -- Ottimizzazione: calcolo della percentuale una sola volta per riga
    CROSS APPLY (
        SELECT dbo.fn_GetPercentualeProvvigione(ISNULL(p.IdAgente,0), p.IdCliente, ISNULL(p.IdMandatario,0)) AS ProvvigioneAgente
    ) ca
    WHERE p.Anno = @Anno  
      AND p.Mese = @Mese  
      AND tp.VisualizzaInDashboard = 1  
      AND (@IdAgente IS NULL OR p.IdAgente = @IdAgente)  
      AND (@Fatturata IS NULL OR ISNULL(p.Fatturata, 0) = @Fatturata)

    GROUP BY  
        p.IdCliente, 
        c.RagioneSociale, 
        ISNULL(p.IdAgente,0), 
        ISNULL(a.AgenteDescrizione,''),  
        p.IdTipoPagamento, 
        tp.TipoPagamento, 
        ca.ProvvigioneAgente, 
        p.Anno, 
        p.Mese,
        tp.OrdineVisualizzazione  
    ORDER BY   
        tp.OrdineVisualizzazione, 
        c.RagioneSociale;
END
GO


ALTER PROCEDURE [dbo].[spDashboardProvvigioni_SystemP]  
(  
    @Anno INT,  
    @Mese INT,  
    @IdAgente INT = NULL,
    @Fatturata INT = NULL  -- NULL=Tutti, 0=Non Fatturati, 1=Fatturati
)  
AS  
BEGIN  
    SET NOCOUNT ON;  

    SELECT   
        p.IdCliente,  
        c.RagioneSociale AS NomeCliente,  
        ISNULL(p.IdAgente,0) AS IdAgente, 
        ISNULL(a.AgenteDescrizione,'') AS AgenteDescrizione,   
        p.IdTipoPagamento,  
        tp.TipoPagamento,
        -- Percentuale calcolata tramite funzione (calcolata una sola volta per riga tramite CROSS APPLY)
        ca.ProvvigioneAgente AS ProvvigioneAgente,
        SUM(p.TotaleProvvigione * ca.ProvvigioneAgente / 100.0) AS ValoreProvvigioneAgente,
        p.Anno,  
        p.Mese,  
        tp.OrdineVisualizzazione,
        SUM(p.Quantita) AS Quantita,  
        --SUM(p.Quantita * p.Prezzo) AS Importo,  
        SUM(p.TotaleProvvigione) AS CommissioniTotali  
    FROM ProspettoSystemP p  
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente  
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento  
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente  
    -- Ottimizzazione: calcolo della percentuale una sola volta per riga
    CROSS APPLY (
        SELECT dbo.fn_GetPercentualeProvvigione(ISNULL(p.IdAgente,0), p.IdCliente, ISNULL(p.IdMandatario,0)) AS ProvvigioneAgente
    ) ca
    WHERE p.Anno = @Anno  
      AND p.Mese = @Mese  
      AND tp.VisualizzaInDashboard = 1  
      AND (@IdAgente IS NULL OR p.IdAgente = @IdAgente)  
      AND (@Fatturata IS NULL OR ISNULL(p.Fatturata, 0) = @Fatturata)

    GROUP BY  
        p.IdCliente, 
        c.RagioneSociale, 
        ISNULL(p.IdAgente,0), 
        ISNULL(a.AgenteDescrizione,''),  
        p.IdTipoPagamento, 
        tp.TipoPagamento, 
        ca.ProvvigioneAgente,  
        p.Anno, 
        p.Mese, 
        tp.OrdineVisualizzazione  
    ORDER BY   
        tp.OrdineVisualizzazione, 
        c.RagioneSociale;
END
GO



ALTER PROCEDURE [dbo].[spDashboardProvvigioni_TradingAndConsulting]  
(  
    @Anno INT,  
    @Mese INT,  
    @IdAgente INT = NULL,
    @Fatturata INT = NULL  -- NULL=Tutti, 0=Non Fatturati, 1=Fatturati
)  
AS  
BEGIN  
    SET NOCOUNT ON;  

    SELECT   
        p.IdCliente,  
        c.RagioneSociale AS NomeCliente,  
        p.IdAgente,  
        a.AgenteDescrizione,  
        p.IdTipoPagamento,  
        tp.TipoPagamento,
        -- Percentuale calcolata tramite funzione (calcolata una sola volta per riga tramite CROSS APPLY)
        ca.ProvvigioneAgente AS ProvvigioneAgente,
        SUM(p.TotaleProvvigioneFattura * ca.ProvvigioneAgente / 100.0) AS ValoreProvvigioneAgente,
        p.Anno,  
        p.Mese,  
        tp.OrdineVisualizzazione,
        SUM(p.Quantita) AS Quantita,  
        --SUM(p.Quantita * p.Prezzo) AS Importo,  
        SUM(p.TotaleProvvigioneFattura) AS CommissioniTotali  
    FROM ProspettoTradingAndConsulting p  
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente  
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento  
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente  
    -- Ottimizzazione: calcolo della percentuale una sola volta per riga
    CROSS APPLY (
        SELECT dbo.fn_GetPercentualeProvvigione(ISNULL(p.IdAgente,0), p.IdCliente, ISNULL(p.IdMandatario,0)) AS ProvvigioneAgente
    ) ca
    WHERE p.Anno = @Anno  
      AND p.Mese = @Mese  
      AND tp.VisualizzaInDashboard = 1  
      AND (@IdAgente IS NULL OR p.IdAgente = @IdAgente)  
      AND (@Fatturata IS NULL OR ISNULL(p.Fatturata, 0) = @Fatturata)

    GROUP BY  
        p.IdCliente, 
        c.RagioneSociale, 
        p.IdAgente, 
        a.AgenteDescrizione,  
        p.IdTipoPagamento, 
        tp.TipoPagamento, 
        ca.ProvvigioneAgente,  
        p.Anno, 
        p.Mese, 
        tp.OrdineVisualizzazione  
    ORDER BY   
        tp.OrdineVisualizzazione, 
        c.RagioneSociale;
END
GO
