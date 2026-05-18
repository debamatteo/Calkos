
--CLIENTI 
INSERT INTO Clienti (RagioneSociale) VALUES ('ALSTOM');
INSERT INTO Clienti (RagioneSociale) VALUES ('ANSALDO');
INSERT INTO Clienti (RagioneSociale) VALUES ('ARTECHE');
INSERT INTO Clienti (RagioneSociale) VALUES ('ALTRAFO');
INSERT INTO Clienti (RagioneSociale) VALUES ('AURIL');
INSERT INTO Clienti (RagioneSociale) VALUES ('AVELTO');
INSERT INTO Clienti (RagioneSociale) VALUES ('BANCHI');
INSERT INTO Clienti (RagioneSociale) VALUES ('BCV');
INSERT INTO Clienti (RagioneSociale) VALUES ('BELOTTI');
INSERT INTO Clienti (RagioneSociale) VALUES ('BSTRANSFO');
INSERT INTO Clienti (RagioneSociale) VALUES ('CABUR');
INSERT INTO Clienti (RagioneSociale) VALUES ('CEMBRE');
INSERT INTO Clienti (RagioneSociale) VALUES ('COLOMBO');
INSERT INTO Clienti (RagioneSociale) VALUES ('DANESE');
INSERT INTO Clienti (RagioneSociale) VALUES ('DSR');
INSERT INTO Clienti (RagioneSociale) VALUES ('EFACEC');
INSERT INTO Clienti (RagioneSociale) VALUES ('ELECTROMOTOR');
INSERT INTO Clienti (RagioneSociale) VALUES ('ELEKTRA WIRE');
INSERT INTO Clienti (RagioneSociale) VALUES ('ELEKTROREMONT');
INSERT INTO Clienti (RagioneSociale) VALUES ('ELETTROM. CAMPANA');
INSERT INTO Clienti (RagioneSociale) VALUES ('ELETTROMECCANICA CAMPANA');
INSERT INTO Clienti (RagioneSociale) VALUES ('ELETTROMECCANICA COLOMBO');
INSERT INTO Clienti (RagioneSociale) VALUES ('ELETTROMIL');
INSERT INTO Clienti (RagioneSociale) VALUES ('ELETTROMIL ALBANIA');
INSERT INTO Clienti (RagioneSociale) VALUES ('ELETTROMIL SLOVACCHIA');
INSERT INTO Clienti (RagioneSociale) VALUES ('ERGOLINES');
INSERT INTO Clienti (RagioneSociale) VALUES ('ESERVICE');
INSERT INTO Clienti (RagioneSociale) VALUES ('FDUEG');
INSERT INTO Clienti (RagioneSociale) VALUES ('FMT');
INSERT INTO Clienti (RagioneSociale) VALUES ('FTM');
INSERT INTO Clienti (RagioneSociale) VALUES ('GEDELSA');
INSERT INTO Clienti (RagioneSociale) VALUES ('GETRA');
INSERT INTO Clienti (RagioneSociale) VALUES ('HEUSH');
INSERT INTO Clienti (RagioneSociale) VALUES ('HTT');
INSERT INTO Clienti (RagioneSociale) VALUES ('IEE');
INSERT INTO Clienti (RagioneSociale) VALUES ('IMEFY');
INSERT INTO Clienti (RagioneSociale) VALUES ('JARA');
INSERT INTO Clienti (RagioneSociale) VALUES ('LEAM');
INSERT INTO Clienti (RagioneSociale) VALUES ('MALINVERNO');
INSERT INTO Clienti (RagioneSociale) VALUES ('MANGOLDT');
INSERT INTO Clienti (RagioneSociale) VALUES ('MEC DELACHAUX');
INSERT INTO Clienti (RagioneSociale) VALUES ('MEVIS');
INSERT INTO Clienti (RagioneSociale) VALUES ('METAL PROFILI');
INSERT INTO Clienti (RagioneSociale) VALUES ('MF');
INSERT INTO Clienti (RagioneSociale) VALUES ('MGS');
INSERT INTO Clienti (RagioneSociale) VALUES ('MIME');
INSERT INTO Clienti (RagioneSociale) VALUES ('NCC');
INSERT INTO Clienti (RagioneSociale) VALUES ('NECOM');
INSERT INTO Clienti (RagioneSociale) VALUES ('OMIS');
INSERT INTO Clienti (RagioneSociale) VALUES ('PERSICO');
INSERT INTO Clienti (RagioneSociale) VALUES ('PIZZAMIGLIO');
INSERT INTO Clienti (RagioneSociale) VALUES ('RASERA');
INSERT INTO Clienti (RagioneSociale) VALUES ('REMA');
INSERT INTO Clienti (RagioneSociale) VALUES ('SCAME PARRE');
INSERT INTO Clienti (RagioneSociale) VALUES ('SEA');
INSERT INTO Clienti (RagioneSociale) VALUES ('SEMAR');
INSERT INTO Clienti (RagioneSociale) VALUES ('SIDER');
INSERT INTO Clienti (RagioneSociale) VALUES ('TAMINI');
INSERT INTO Clienti (RagioneSociale) VALUES ('TAMURA');
INSERT INTO Clienti (RagioneSociale) VALUES ('TECNO ELECTRIC');
INSERT INTO Clienti (RagioneSociale) VALUES ('TIRONI');
INSERT INTO Clienti (RagioneSociale) VALUES ('VEZZARO');


-----------------MANDATARI CLIENTI 

truncate table MandatariClienti


INSERT INTO MandatariClienti (IdMandatario, IdCliente, Utente)
SELECT m.IdMandatario, c.IdCliente, 'Denis'
FROM Mandatari m
JOIN Clienti c ON c.RagioneSociale IN (
    'ALSTOM','ANSALDO','AURIL','BELOTTI','CABUR','CEMBRE','ELECTROMOTOR',
    'ELEKTRA WIRE','ELEKTROREMONT','ELETTROMECCANICA CAMPANA','ELETTROMIL',
    'ELETTROMIL ALBANIA','FTM','IEE','MALINVERNO','MEC DELACHAUX','MGS',
    'OMIS','PIZZAMIGLIO','REMA','SCAME PARRE','SIDER','VEZZARO'
)
WHERE m.CodiceMandatario = 'DeAngeli';


INSERT INTO MandatariClienti (IdMandatario, IdCliente, Utente)
SELECT m.IdMandatario, c.IdCliente, 'Denis'
FROM Mandatari m
JOIN Clienti c ON c.RagioneSociale IN (
    'DANESE','ELETTROM. CAMPANA','HEUSH','MANGOLDT','MEVIS','NCC',
    'PERSICO','SCAME PARRE','TAMURA'
)
WHERE m.CodiceMandatario = 'ElektraWire';


INSERT INTO MandatariClienti (IdMandatario, IdCliente, Utente)
SELECT m.IdMandatario, c.IdCliente, 'Denis'
FROM Mandatari m
JOIN Clienti c ON c.RagioneSociale IN (
    'ARTECHE','BCV','COLOMBO','EFACEC','ESERVICE','FMT','FTM','GEDELSA',
    'GETRA','LEAM','JARA','MANGOLDT','MEC DELACHAUX','METAL PROFILI',
    'MF','NECOM','RASERA','TAMURA'
)
WHERE m.CodiceMandatario = 'Cobral';



INSERT INTO MandatariClienti (IdMandatario, IdCliente, Utente)
SELECT m.IdMandatario, c.IdCliente, 'Denis'
FROM Mandatari m
JOIN Clienti c ON c.RagioneSociale IN (
    'ALTRAFO','AVELTO','BANCHI','DSR','ELETTROMIL','ELETTROMIL SLOVACCHIA',
    'FTM','MEC DELACHAUX','MGS','SCAME PARRE','SEA'
)
WHERE m.CodiceMandatario = 'SystemP';


INSERT INTO MandatariClienti (IdMandatario, IdCliente, Utente)
SELECT m.IdMandatario, c.IdCliente, 'Denis'
FROM Mandatari m
JOIN Clienti c ON c.RagioneSociale IN (
    'BSTRANSFO','ERGOLINES','GEDELSA','HTT','MANGOLDT','MEC DELACHAUX',
    'MF','SEMAR'
)
WHERE m.CodiceMandatario = 'Guerzoni';


INSERT INTO MandatariClienti (IdMandatario, IdCliente, Utente)
SELECT m.IdMandatario, c.IdCliente, 'Denis'
FROM Mandatari m
JOIN Clienti c ON c.RagioneSociale IN (
    'ELETTROMECCANICA CAMPANA','ELETTROMECCANICA COLOMBO','ESERVICE',
    'FDUEG','IMEFY','TECNO ELECTRIC'
)
WHERE m.CodiceMandatario = 'CST';


INSERT INTO MandatariClienti (IdMandatario, IdCliente, Utente)
SELECT m.IdMandatario, c.IdCliente, 'Denis'
FROM Mandatari m
JOIN Clienti c ON c.RagioneSociale IN (
    'GETRA','LEAM','MIME'
)
WHERE m.CodiceMandatario = 'TradingAndConsulting';



INSERT INTO MandatariClienti (IdMandatario, IdCliente, Utente)
SELECT m.IdMandatario, c.IdCliente, 'Denis'
FROM Mandatari m
JOIN Clienti c ON c.RagioneSociale IN (
    'GETRA','SEA','TAMINI','TIRONI'
)
WHERE m.CodiceMandatario = 'Hitech';


exec [sp_Servizio_Inserisci_ProvvigioniAgenti] 1


CREATE OR ALTER PROCEDURE [dbo].[sp_Servizio_Inserisci_ProvvigioniAgenti]
    @IdAgente INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        ------------------------------------------------------------
        -- 1) CANCELLA SOLO LE PROVVIGIONI DI QUESTO AGENTE
        ------------------------------------------------------------
        DELETE FROM ProvvigioniAgenti
        WHERE IdAgente = @IdAgente;

        ------------------------------------------------------------
        -- 2) REINSERIMENTO PROVVIGIONI
        ------------------------------------------------------------

        ------------------------------------------------------------
        -- DE ANGELI (50%)
        ------------------------------------------------------------
        INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale, Utente)
        SELECT @IdAgente, m.IdMandatario, c.IdCliente, 50, 'Denis'
        FROM Mandatari m 
        JOIN Clienti c ON c.RagioneSociale IN (
            'Alstom','Ansaldo','Auril','Belotti','Cabur','Electromotor',
            'Elektra Wire','Elektroremont','Elettromeccanica Campana',
            'Elettromil','Elettromil Albania','FTM','IEE','Malinverno',
            'Mec Delachaux','MGS','Omis','Pizzamiglio','Rema','Scame Parre',
            'Sider'
        )
        WHERE m.CodiceMandatario = 'DeAngeli';

        -- DE ANGELI (35%) VEZZARO
        INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale, Utente)
        SELECT @IdAgente, m.IdMandatario, c.IdCliente, 35, 'Denis'
        FROM Mandatari m JOIN Clienti c ON c.RagioneSociale = 'Vezzaro'
        WHERE m.CodiceMandatario = 'DeAngeli';

        -- DE ANGELI (25%) CEMBRE
        INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale, Utente)
        SELECT @IdAgente, m.IdMandatario, c.IdCliente, 25, 'Denis'
        FROM Mandatari m JOIN Clienti c ON c.RagioneSociale = 'Cembre'
        WHERE m.CodiceMandatario = 'DeAngeli';

        ------------------------------------------------------------
        -- ELEKTRA WIRE (40%)
        ------------------------------------------------------------
        INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale, Utente)
        SELECT @IdAgente, m.IdMandatario, c.IdCliente, 40, 'Denis'
        FROM Mandatari m 
        JOIN Clienti c ON c.RagioneSociale IN (
            'Danese','Elettrom. Campana','Heush','Mangoldt','Mevis','Ncc',
            'Persico','Scame Parre','Tamura'
        )
        WHERE m.CodiceMandatario = 'ElektraWire';

        ------------------------------------------------------------
        -- COBRAL (40%)
        ------------------------------------------------------------
        INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale, Utente)
        SELECT @IdAgente, m.IdMandatario, c.IdCliente, 40, 'Denis'
        FROM Mandatari m 
        JOIN Clienti c ON c.RagioneSociale IN (
            'Arteche','Bcv','Colombo','Efacec','Eservice','Fmt','Gedelsa',
            'Getra','Jara','Mangoldt','Mec Delachaux','Metal Profili','Mf',
            'Necom','Rasera','Tamura'
        )
        WHERE m.CodiceMandatario = 'Cobral';

        ------------------------------------------------------------
        -- SYSTEM P (50%)
        ------------------------------------------------------------
        INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale, Utente)
        SELECT @IdAgente, m.IdMandatario, c.IdCliente, 50, 'Denis'
        FROM Mandatari m 
        JOIN Clienti c ON c.RagioneSociale IN (
            'Altrafo','Avelto','Banchi','Dsr','Elettromil','Elettromil Slovacchia',
            'FTM','Mec Delachaux','MGS','Scame Parre','Sea'
        )
        WHERE m.CodiceMandatario = 'SystemP';

        ------------------------------------------------------------
        -- GUERZONI (50%)
        ------------------------------------------------------------
        INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale, Utente)
        SELECT @IdAgente, m.IdMandatario, c.IdCliente, 50, 'Denis'
        FROM Mandatari m 
        JOIN Clienti c ON c.RagioneSociale IN (
            'Bstransfo','Ergolines','Gedelsa','Htt','Mangoldt','Mec Delachaux',
            'Mf','Semar'
        )
        WHERE m.CodiceMandatario = 'Guerzoni';

        ------------------------------------------------------------
        -- CST (50%)
        ------------------------------------------------------------
        INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale, Utente)
        SELECT @IdAgente, m.IdMandatario, c.IdCliente, 50, 'Denis'
        FROM Mandatari m 
        JOIN Clienti c ON c.RagioneSociale IN (
            'Elettromeccanica Campana','Elettromeccanica Colombo','Eservice',
            'Fdueg','Imefy','Tecno Electric'
        )
        WHERE m.CodiceMandatario = 'CST';

        ------------------------------------------------------------
        -- TRADING AND CONSULTING (50%)
        ------------------------------------------------------------
        INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale, Utente)
        SELECT @IdAgente, m.IdMandatario, c.IdCliente, 50, 'Denis'
        FROM Mandatari m 
        JOIN Clienti c ON c.RagioneSociale IN (
            'Getra','Leam','Mime'
        )
        WHERE m.CodiceMandatario = 'TradingAndConsulting';

        ------------------------------------------------------------
        -- HI TECH (50%)
        ------------------------------------------------------------
        INSERT INTO ProvvigioniAgenti (IdAgente, IdMandatario, IdCliente, Percentuale, Utente)
        SELECT @IdAgente, m.IdMandatario, c.IdCliente, 50, 'Denis'
        FROM Mandatari m 
        JOIN Clienti c ON c.RagioneSociale IN (
            'Getra','Sea','Tamini','Tironi'
        )
        WHERE m.CodiceMandatario = 'Hitech';

        ------------------------------------------------------------
        -- FINE
        ------------------------------------------------------------
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO

