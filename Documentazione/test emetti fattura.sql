
		
	select C.RagioneSociale, P.IdTipoPagamento, tp.VisualizzaInDashboard,anno,mese,DataFatturazione,fatturata ,NumeroFattura ,NumeroDDT,NumeroOrdine,P.ValoreCommissioni
    FROM ProspettoCobral p  
    LEFT JOIN Clienti c ON p.IdCliente = c.IdCliente  
    LEFT JOIN TipoPagamento tp ON tp.IdTipoPagamento = p.IdTipoPagamento  
    LEFT JOIN Agenti a ON a.IdAgente = p.IdAgente  
	WHERE NumeroOrdine = 'O0253031'



---SELEZIONA

SELECT  IdTipoPagamento, anno,mese,DataFatturazione,fatturata ,NumeroFattura ,NumeroDDT,NumeroOrdine,ValoreCommissioni
FROM ProspettoCobral
WHERE     IdMandatario = 2   
AND Anno = 2026    AND Mese = 2
AND IdCliente = 31    AND IdTipoPagamento = 3   
AND (Fatturata = 0 OR Fatturata IS NULL)

---fattura
UPDATE ProspettoCobral
SET     Fatturata = 1,   
DataFatturazione = GETDATE()
WHERE     IdMandatario = 2   
AND Anno = 2026    AND Mese = 2    
AND IdCliente = 31    AND IdTipoPagamento = 3   
AND (Fatturata = 0 OR Fatturata IS NULL)

--ELIMINA FATTURA
UPDATE ProspettoCobral
SET     Fatturata = 0, --1  
DataFatturazione =NULL-- GETDATE()
WHERE     IdMandatario = 2   
AND Anno = 2026    AND Mese = 2    
AND IdCliente = 31    AND IdTipoPagamento = 3   
--AND (Fatturata = 0 OR Fatturata IS NULL)

