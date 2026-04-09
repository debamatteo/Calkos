Update ProspettoCobral set [IsDeleted]=0

Update ProspettoCobral set [Fatturata]=0,[DataFatturazione] = null where mese


select distinct anno,mese from  ProspettoCobral

Update ProspettoCobral set Anno= year(DataInserimento) , mese= month (DataInserimento)


DELETE ProspettoCobral WHERE ANNO=2024 AND MESE=3


--IdTipoPagamento	CodiceTipoPagamento	TipoPagamento
--2	60	60GG
--3	90	90GG
update ProspettoCobral set IdTipoPagamento=2 where  anno=2026 and mese=2


select * from [ImportCobral]


TRUNCATE TABLE [dbo].[ImportCobral];
TRUNCATE TABLE [dbo].[FileImportato];
TRUNCATE TABLE [dbo].[MandatariClienti];
TRUNCATE TABLE [dbo].[Clienti];
TRUNCATE TABLE [dbo].[ProspettoCobral]



		
SELECT count(*) [FileImportato] FROM [dbo].[FileImportato];

SELECT count(*) [Clienti] FROM [dbo].[Clienti];
SELECT count(*)  [MandatariClienti] FROM[dbo].[MandatariClienti];

SELECT count(*) [ImportCobral]  FROM[dbo].[ImportCobral];
SELECT count(*) [ProspettoCobral]  FROM [dbo].[ProspettoCobral]

TRUNCATE TABLE [ServizioQueryUtili]

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'UPDATE ProspettoCobral SET [IsDeleted] = 0');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'UPDATE ProspettoCobral SET [Fatturata] = 0, [DataFatturazione] = NULL');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'UPDATE ProspettoCobral SET Anno = YEAR(DataInserimento), Mese = MONTH(DataInserimento)');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'DELETE ProspettoCobral WHERE Anno = 2024 AND Mese = 3');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'TRUNCATE TABLE [dbo].[ImportCobral]');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'TRUNCATE TABLE [dbo].[FileImportato]');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'TRUNCATE TABLE [dbo].[MandatariClienti]');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'TRUNCATE TABLE [dbo].[Clienti]');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'TRUNCATE TABLE [dbo].[ProspettoCobral]');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'SELECT COUNT(*) AS [FileImportato] FROM [dbo].[FileImportato]');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'SELECT COUNT(*) AS [Clienti] FROM [dbo].[Clienti]');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'SELECT COUNT(*) AS [MandatariClienti] FROM [dbo].[MandatariClienti]');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'SELECT COUNT(*) AS [ImportCobral] FROM [dbo].[ImportCobral]');

INSERT INTO ServizioQueryUtili (Comando) VALUES (N'SELECT COUNT(*) AS [ProspettoCobral] FROM [dbo].[ProspettoCobral]');


select * from ServizioQueryUtili