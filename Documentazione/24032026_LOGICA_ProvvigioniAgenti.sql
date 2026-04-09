--Devo gestire questa situazione.

--Un agente può avere una provvigione o più provvigioni, in base al mandatario e al cliente.

--Ad esempio, mandatario Cobral.

--Agente “FABIO FABIO”:

--ha 3 percentuali diverse:

--per il cliente GBE: 25%;
--per il cliente BTicino: 35%;
--per tutti gli altri clienti: 50%.









/*1. Nuova Tabella: ProvvigioniAgenti
Questa tabella memorizza le percentuali specifiche. 
Se IdCliente è NULL, 
la percentuale si applica a tutti i clienti di quel mandatario per quell'agente (il tuo caso del 50%).
*/
CREATE TABLE [dbo].[ProvvigioniAgenti](
	[IdProvvigione] [int] IDENTITY(1,1) NOT NULL,
	[IdAgente] [int] NOT NULL,
	[IdMandatario] [int] NOT NULL,
	[IdCliente] [int] NULL, -- Se NULL, vale per tutti i clienti di questo mandatario
	[Percentuale] [decimal](5, 2) NOT NULL,
	[DataInserimento] [datetime] NOT NULL DEFAULT (getdate()),
	[DataModifica] [datetime] NULL,
	[Utente] [nvarchar](100) NULL,
 CONSTRAINT [PK_ProvvigioniAgenti] PRIMARY KEY CLUSTERED ([IdProvvigione] ASC),
 CONSTRAINT [FK_Provvigioni_Agente] FOREIGN KEY([IdAgente]) REFERENCES [dbo].[Agenti] ([IdAgente]),
 CONSTRAINT [FK_Provvigioni_Mandatario] FOREIGN KEY([IdMandatario]) REFERENCES [dbo].[Mandatari] ([IdMandatario]),
 CONSTRAINT [FK_Provvigioni_Cliente] FOREIGN KEY([IdCliente]) REFERENCES [dbo].[Clienti] ([IdCliente])
) ON [PRIMARY]
GO

-- Indice per velocizzare la ricerca della provvigione corretta
CREATE UNIQUE INDEX UIX_Provvigione_Agente_Mandatario_Cliente 
ON [dbo].[ProvvigioniAgenti] ([IdAgente], [IdMandatario], [IdCliente]);



/*

2. Esempio di popolamento (Il tuo caso Fabio Fabio)
Supponendo che l'agente Fabio abbia IdAgente = 10, Cobral IdMandatario = 5, Gbe IdCliente = 100 e Bticino IdCliente = 101:
*/

-- 1. Eccezione per Gbe (25%)
INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale)
VALUES (10, 5, 100, 25.00);

-- 2. Eccezione per Bticino (35%)
INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale)
VALUES (10, 5, 101, 35.00);

-- 3. Regola per "Tutti gli altri clienti Cobral" (50%)
-- Usiamo IdCliente NULL per indicare "Tutti gli altri"
INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale)
VALUES (10, 5, NULL, 50.00);



/*

3. Come recuperare la provvigione (Logica SQL)
Per ottenere la provvigione corretta in una query di calcolo, useresti una logica di COALESCE o ISNULL cercando prima il caso più specifico.
*/

SELECT 
    A.Agente,
    M.NomeMandatario,
    C.RagioneSociale,
    -- Logica a cascata:
    COALESCE(
        P_Spec.Percentuale,   -- 1. Cerca Agente+Mandatario+Cliente
        P_Mand.Percentuale,   -- 2. Se non c'è, cerca Agente+Mandatario (Cliente è NULL)
        A.PercentualeDefault  -- 3. Se non c'è, usa il default dell'agente
    ) AS PercentualeApplicata
FROM Agenti A
JOIN Clienti C ON C.IdAgente = A.IdAgente
JOIN MandatariClienti MC ON MC.IdCliente = C.IdCliente
JOIN Mandatari M ON M.IdMandatario = MC.IdMandatario
-- Join per l'eccezione specifica del cliente
LEFT JOIN ProvvigioniAgenti P_Spec 
    ON P_Spec.IdAgente = A.IdAgente 
    AND P_Spec.IdMandatario = M.IdMandatario 
    AND P_Spec.IdCliente = C.IdCliente
-- Join per la regola generale del mandatario
LEFT JOIN ProvvigioniAgenti P_Mand 
    ON P_Mand.IdAgente = A.IdAgente 
    AND P_Mand.IdMandatario = M.IdMandatario 
    AND P_Mand.IdCliente IS NULL


--Ecco la soluzione completa. Creeremo una User-Defined Function (UDF) in SQL Server. Questa è la scelta migliore perché puoi riutilizzarla ovunque: nelle tabelle, nelle Vista (View) o nei report, passandogli semplicemente i tre ID.

--1. La Funzione Scalare: fn_GetPercentualeProvvigione
--Questa funzione applica la logica "a cascata": cerca prima il cliente specifico, poi il default del mandatario, e infine il default dell'agente.


CREATE OR ALTER FUNCTION [dbo].[fn_GetPercentualeProvvigione]
(
    @IdAgente INT,
    @IdMandatario INT,
    @IdCliente INT
)
RETURNS DECIMAL(5,2)
AS
BEGIN
    DECLARE @Result DECIMAL(5,2) = NULL;

    -- PASSO A: Cerco la combinazione esatta (es. Fabio + Cobral + Gbe)
    SELECT TOP 1 @Result = Percentuale 
    FROM ProvvigioniAgenti 
    WHERE IdAgente = @IdAgente 
      AND IdMandatario = @IdMandatario 
      AND IdCliente = @IdCliente;

    -- PASSO B: Se fallisce, cerco il default del Mandatario (es. Fabio + Cobral + Altri)
    IF @Result IS NULL
    BEGIN
        SELECT TOP 1 @Result = Percentuale 
        FROM ProvvigioniAgenti 
        WHERE IdAgente = @IdAgente 
          AND IdMandatario = @IdMandatario 
          AND IdCliente IS NULL;
    END

    -- PASSO C: Se fallisce tutto, prendo il default dell'Agente (es. Fabio Base 10%)
    IF @Result IS NULL
    BEGIN
        SELECT TOP 1 @Result = PercentualeDefault 
        FROM Agenti 
        WHERE IdAgente = @IdAgente;
    END

    RETURN ISNULL(@Result, 0);
END
GO

-- Esempio: Fabio (Id=1), Cobral (Id=2), Gbe (Id=1), Bticino (Id=2)
INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale) VALUES (1, 2, 1, 25.00); -- Gbe
INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale) VALUES (1, 2, 2, 35.00); -- Bticino
INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale) VALUES (1, 2, NULL, 50.00); -- Altri Cobral


select * from clienti
/*1	Gbe	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	2026-03-22 11:54:01.487	NULL	NULL
2	Bticino	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	2026-03-22 11:54:12.113	NULL	NULL*/


select * from Agenti--1	Fabio	Fabio	FABIO FABIO	AG001	01234567890	RSSMRA80A01L219O	30.00	NULL	NULL	NULL	2026-03-17 11:39:36.837	NULL	NULL
select * from Mandatari--2	Cobral	Cobral	2026-03-17 11:39:36.840	NULL	NULL




-- Pulizia test precedenti (opzionale)
truncate table  ProvvigioniAgenti
-- 1. Regola Specifica per Gbe (IdCliente 1) -> 25%
INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale) 
VALUES (1, 2, 1, 25.00);

-- 2. Regola Specifica per Bticino (IdCliente 2) -> 35%
INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale) 
VALUES (1, 2, 2, 35.00);

-- 3. Regola per TUTTI gli altri clienti Cobral (IdCliente NULL) -> 50%
INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale) 
VALUES (1, 2, NULL, 50.00);


--3. Test di verifica immediato
--Esegui queste tre query per vedere se il motore del database risponde come ti aspetti:


-- TEST 1: Fabio + Cobral + Gbe -> Deve restituire 25.00
SELECT [dbo].[fn_GetPercentualeProvvigione](1, 2, 1) as QuotaGbe;

-- TEST 2: Fabio + Cobral + Bticino -> Deve restituire 35.00
SELECT [dbo].[fn_GetPercentualeProvvigione](1, 2, 2) as QuotaBticino;

-- TEST 3: Fabio + Cobral + Cliente X (es. ID 99) -> Deve restituire 50.00
SELECT [dbo].[fn_GetPercentualeProvvigione](1, 2, 99) as QuotaStandardCobral;



--ProspettiCobralController.cs: Mi serve vedere l'azione (il metodo) DettaglioOrdine (GET) e quella che salva i dati (POST). Devo capire come recuperi l'ID Mandatario.

--DettaglioOrdine.cshtml: Mi serve il file completo (non solo il pezzetto dell'agente). Devo vedere dove sono gli altri campi (Cliente, Prezzo base, ecc.) per assicurarmi che gli ID degli input siano corretti per lo script.

--ProspettoCobral.cs (La Entity): Voglio vedere com'è definita la classe nel Domain, per essere sicuro dei nomi delle proprietà (es. IdAgente, ProvvigioneAgente, ecc.).


select * from ProspettoCobral