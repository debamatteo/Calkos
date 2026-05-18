
UPDATE MaterialiDimensioni 
SET Valore = 'mm ' + REPLACE(Valore, ' mm', '') 
WHERE Tipo = 'Larghezza' AND Valore NOT LIKE 'mm%';

-- Correzione specifica per il Bronzo (che ha range tipo 7,0 - 14,9)
UPDATE MaterialiDimensioni 
SET Valore = 'mm7,0-14,9' WHERE IdDimensione = 67;
UPDATE MaterialiDimensioni SET Valore = 'mm15-29,9' WHERE IdDimensione = 68;
UPDATE MaterialiDimensioni SET Valore = 'mm30-59,9' WHERE IdDimensione = 69;
UPDATE MaterialiDimensioni SET Valore = 'mm60-500' WHERE IdDimensione = 70;


CREATE TABLE MaterialiListinoProvvigioni (
    IdListino         INT PRIMARY KEY IDENTITY(1,1),
    IdMateriale       INT NOT NULL,
    IdDimensione      INT NOT NULL,
    PercentualeProvv  DECIMAL(5, 2) NOT NULL, -- Esempio: 10.50 (per 10,5%)
    ValoreEuro        DECIMAL(18, 4) NOT NULL DEFAULT 0, -- Il risultato della tua "Battaglia Navale"
    DataInserimento   DATETIME DEFAULT GETDATE(),
    Utente            VARCHAR(100),

    -- Relazione con la tabella Materiali
    CONSTRAINT FK_Materiali FOREIGN KEY (IdMateriale) 
        REFERENCES Materiali(IdMateriale),

    -- Relazione con la tabella Dimensioni
    CONSTRAINT FK_Dimensioni FOREIGN KEY (IdDimensione) 
        REFERENCES MaterialiDimensioni(IdDimensione),

    -- Impedisce di avere due volte lo stesso incrocio (Es: Alluminio + 0.20mm + 10%)
    CONSTRAINT UQ_IncrocioUnico UNIQUE (IdMateriale, IdDimensione, PercentualeProvv)
);


INSERT INTO MaterialiListinoProvvigioni (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro)
SELECT 
    IdMateriale, 
    IdDimensione, 
    10.00, -- La percentuale che vuoi pre-popolare
    0.00   -- Il valore in Euro (che aggiornerai dopo)
FROM MaterialiDimensioni
WHERE IdMateriale = 4;


-- Esempio inserimento per Alluminio (IdMateriale 4)
-- Assumo che gli IdDimensione corrispondano all'ordine degli spessori (1=0.20, 2=0.25, ecc.)

INSERT INTO MaterialiListinoProvvigioni (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente)
VALUES 
-- Spessore 0,20 mm (IdDim 1)
(4, 1, 4.00, 0.00, 'admin'), (4, 1, 7.00, 1.60, 'admin'), (4, 1, 10.00, 1.75, 'admin'), (4, 1, 20.00, 0.00, 'admin'),

-- Spessore 0,25 mm (IdDim 2)
(4, 2, 4.00, 0.00, 'admin'), (4, 2, 7.00, 1.50, 'admin'), (4, 2, 10.00, 1.65, 'admin'), (4, 2, 20.00, 0.00, 'admin'),

-- Spessore 0,30 mm (IdDim 3)
(4, 3, 4.00, 0.00, 'admin'), (4, 3, 7.00, 1.40, 'admin'), (4, 3, 10.00, 1.60, 'admin'), (4, 3, 20.00, 0.00, 'admin'),

-- Spessore 0,50 mm (IdDim 7)
(4, 7, 4.00, 0.00, 'admin'), (4, 7, 7.00, 1.35, 'admin'), (4, 7, 10.00, 1.60, 'admin'), (4, 7, 20.00, 0.00, 'admin');

-- ... e così via per tutte le righe della tua tabella.



-------------------ALLUMINIO SPESSORE
-- POPOLAMENTO LISTINO PROVVIGIONI: SOLO SPESSORI ALLUMINIO (ID 4)
-- Logica: (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente)

INSERT INTO MaterialiListinoProvvigioni (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente)
VALUES 
(4, 1, 4.0, 0.00, 'admin'), (4, 1, 7.0, 1.60, 'admin'), (4, 1, 10.0, 1.75, 'admin'), (4, 1, 20.0, 0.00, 'admin'),
(4, 2, 4.0, 0.00, 'admin'), (4, 2, 7.0, 1.50, 'admin'), (4, 2, 10.0, 1.65, 'admin'), (4, 2, 20.0, 0.00, 'admin'),
(4, 3, 4.0, 0.00, 'admin'), (4, 3, 7.0, 1.40, 'admin'), (4, 3, 10.0, 1.60, 'admin'), (4, 3, 20.0, 0.00, 'admin'),
(4, 4, 4.0, 0.00, 'admin'), (4, 4, 7.0, 1.40, 'admin'), (4, 4, 10.0, 1.60, 'admin'), (4, 4, 20.0, 0.00, 'admin'),
(4, 5, 4.0, 0.00, 'admin'), (4, 5, 7.0, 1.40, 'admin'), (4, 5, 10.0, 1.60, 'admin'), (4, 5, 20.0, 0.00, 'admin'),
(4, 6, 4.0, 0.00, 'admin'), (4, 6, 7.0, 1.40, 'admin'), (4, 6, 10.0, 1.60, 'admin'), (4, 6, 20.0, 0.00, 'admin'),
(4, 7, 4.0, 0.00, 'admin'), (4, 7, 7.0, 1.35, 'admin'), (4, 7, 10.0, 1.60, 'admin'), (4, 7, 20.0, 0.00, 'admin'),
(4, 8, 4.0, 0.00, 'admin'), (4, 8, 7.0, 1.35, 'admin'), (4, 8, 10.0, 1.45, 'admin'), (4, 8, 20.0, 0.00, 'admin'),
(4, 9, 4.0, 0.00, 'admin'), (4, 9, 7.0, 1.25, 'admin'), (4, 9, 10.0, 1.40, 'admin'), (4, 9, 20.0, 0.00, 'admin'),
(4, 10, 4.0, 0.00, 'admin'), (4, 10, 7.0, 1.25, 'admin'), (4, 10, 10.0, 1.40, 'admin'), (4, 10, 20.0, 0.00, 'admin'),
(4, 11, 4.0, 0.00, 'admin'), (4, 11, 7.0, 1.25, 'admin'), (4, 11, 10.0, 1.40, 'admin'), (4, 11, 20.0, 0.00, 'admin'),
(4, 12, 4.0, 0.00, 'admin'), (4, 12, 7.0, 1.25, 'admin'), (4, 12, 10.0, 1.40, 'admin'), (4, 12, 20.0, 0.00, 'admin'),
(4, 13, 4.0, 0.00, 'admin'), (4, 13, 7.0, 1.25, 'admin'), (4, 13, 10.0, 1.40, 'admin'), (4, 13, 20.0, 0.00, 'admin'),
(4, 14, 4.0, 0.00, 'admin'), (4, 14, 7.0, 1.25, 'admin'), (4, 14, 10.0, 1.40, 'admin'), (4, 14, 20.0, 0.00, 'admin'),
(4, 15, 4.0, 0.00, 'admin'), (4, 15, 7.0, 1.25, 'admin'), (4, 15, 10.0, 1.40, 'admin'), (4, 15, 20.0, 0.00, 'admin'),
(4, 16, 4.0, 0.00, 'admin'), (4, 16, 7.0, 1.25, 'admin'), (4, 16, 10.0, 1.40, 'admin'), (4, 16, 20.0, 0.00, 'admin'),
(4, 17, 4.0, 0.00, 'admin'), (4, 17, 7.0, 1.25, 'admin'), (4, 17, 10.0, 1.40, 'admin'), (4, 17, 20.0, 0.00, 'admin'),
(4, 18, 4.0, 0.00, 'admin'), (4, 18, 7.0, 1.25, 'admin'), (4, 18, 10.0, 1.40, 'admin'), (4, 18, 20.0, 0.00, 'admin'),
(4, 19, 4.0, 0.00, 'admin'), (4, 19, 7.0, 1.25, 'admin'), (4, 19, 10.0, 1.40, 'admin'), (4, 19, 20.0, 0.00, 'admin'),
(4, 20, 4.0, 0.00, 'admin'), (4, 20, 7.0, 1.25, 'admin'), (4, 20, 10.0, 1.40, 'admin'), (4, 20, 20.0, 0.00, 'admin'),
(4, 21, 4.0, 0.00, 'admin'), (4, 21, 7.0, 1.25, 'admin'), (4, 21, 10.0, 1.40, 'admin'), (4, 21, 20.0, 0.00, 'admin'),
(4, 22, 4.0, 0.00, 'admin'), (4, 22, 7.0, 1.25, 'admin'), (4, 22, 10.0, 1.40, 'admin'), (4, 22, 20.0, 0.00, 'admin'),
(4, 23, 4.0, 0.00, 'admin'), (4, 23, 7.0, 1.25, 'admin'), (4, 23, 10.0, 1.40, 'admin'), (4, 23, 20.0, 0.00, 'admin'),
(4, 24, 4.0, 0.00, 'admin'), (4, 24, 7.0, 1.25, 'admin'), (4, 24, 10.0, 1.40, 'admin'), (4, 24, 20.0, 0.00, 'admin'),
(4, 25, 4.0, 0.00, 'admin'), (4, 25, 7.0, 1.25, 'admin'), (4, 25, 10.0, 1.40, 'admin'), (4, 25, 20.0, 0.00, 'admin'),
(4, 26, 4.0, 0.00, 'admin'), (4, 26, 7.0, 1.25, 'admin'), (4, 26, 10.0, 1.40, 'admin'), (4, 26, 20.0, 0.00, 'admin'),
(4, 27, 4.0, 0.00, 'admin'), (4, 27, 7.0, 1.25, 'admin'), (4, 27, 10.0, 1.40, 'admin'), (4, 27, 20.0, 0.00, 'admin'),
(4, 28, 4.0, 0.00, 'admin'), (4, 28, 7.0, 1.25, 'admin'), (4, 28, 10.0, 1.40, 'admin'), (4, 28, 20.0, 0.00, 'admin'),
(4, 29, 4.0, 0.00, 'admin'), (4, 29, 7.0, 1.25, 'admin'), (4, 29, 10.0, 1.40, 'admin'), (4, 29, 20.0, 0.00, 'admin'),
(4, 30, 4.0, 0.00, 'admin'), (4, 30, 7.0, 1.25, 'admin'), (4, 30, 10.0, 1.40, 'admin'), (4, 30, 20.0, 0.00, 'admin'),
(4, 31, 4.0, 0.00, 'admin'), (4, 31, 7.0, 1.25, 'admin'), (4, 31, 10.0, 1.40, 'admin'), (4, 31, 20.0, 0.00, 'admin'),
(4, 32, 4.0, 0.00, 'admin'), (4, 32, 7.0, 1.25, 'admin'), (4, 32, 10.0, 1.40, 'admin'), (4, 32, 20.0, 0.00, 'admin'),
(4, 33, 4.0, 0.00, 'admin'), (4, 33, 7.0, 1.25, 'admin'), (4, 33, 10.0, 1.40, 'admin'), (4, 33, 20.0, 0.00, 'admin'),
(4, 34, 4.0, 0.00, 'admin'), (4, 34, 7.0, 1.25, 'admin'), (4, 34, 10.0, 1.40, 'admin'), (4, 34, 20.0, 0.00, 'admin'),
(4, 35, 4.0, 0.00, 'admin'), (4, 35, 7.0, 1.25, 'admin'), (4, 35, 10.0, 1.40, 'admin'), (4, 35, 20.0, 0.00, 'admin'),
(4, 36, 4.0, 0.00, 'admin'), (4, 36, 7.0, 1.25, 'admin'), (4, 36, 10.0, 1.40, 'admin'), (4, 36, 20.0, 0.00, 'admin'),
(4, 37, 4.0, 0.00, 'admin'), (4, 37, 7.0, 1.25, 'admin'), (4, 37, 10.0, 1.40, 'admin'), (4, 37, 20.0, 0.00, 'admin'),
(4, 38, 4.0, 0.00, 'admin'), (4, 38, 7.0, 1.30, 'admin'), (4, 38, 10.0, 1.50, 'admin'), (4, 38, 20.0, 0.00, 'admin'),
(4, 39, 4.0, 0.00, 'admin'), (4, 39, 7.0, 1.30, 'admin'), (4, 39, 10.0, 1.50, 'admin'), (4, 39, 20.0, 0.00, 'admin');




-- Inserimento valori per il campo [AlluminioLarghezza]
-- Usiamo PercentualeProvv per indicare a quale spessore si riferisce il sovrapprezzo

INSERT INTO MaterialiListinoProvvigioni (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente)
VALUES 
-- Larghezza: mm 8 - 15 (ID 41, Tipo 'Larghezza')
(4, 41, 0.20, 0.40, 'admin'), 
(4, 41, 0.25, 0.30, 'admin'), 
(4, 41, 0.30, 0.30, 'admin'), 
(4, 41, 0.35, 0.30, 'admin'), 
(4, 41, 0.40, 0.25, 'admin'), 
(4, 41, 0.50, 0.20, 'admin'), 
(4, 41, 0.60, 0.20, 'admin'), 
(4, 41, 0.70, 0.20, 'admin'), 
(4, 41, 0.80, 0.20, 'admin'), 
(4, 41, 0.90, 0.20, 'admin'), 
(4, 41, 1.00, 0.20, 'admin'), 
(4, 41, 1.20, 0.20, 'admin'), 
(4, 41, 1.50, 0.30, 'admin'), 
(4, 41, 2.00, 0.40, 'admin'), 
(4, 41, 2.50, 0.40, 'admin'), 
(4, 41, 3.00, 0.40, 'admin'),

-- Larghezza: mm 15,1 - 30 (ID 42, Tipo 'Larghezza')
(4, 42, 0.20, 0.30, 'admin'), 
(4, 42, 0.25, 0.20, 'admin'), 
(4, 42, 0.30, 0.15, 'admin'), 
(4, 42, 0.35, 0.15, 'admin'), 
(4, 42, 0.40, 0.13, 'admin'), 
(4, 42, 0.50, 0.10, 'admin'), 
(4, 42, 0.60, 0.10, 'admin'), 
(4, 42, 0.70, 0.10, 'admin'), 
(4, 42, 0.80, 0.10, 'admin'), 
(4, 42, 0.90, 0.10, 'admin'), 
(4, 42, 1.00, 0.10, 'admin'), 
(4, 42, 1.20, 0.10, 'admin'), 
(4, 42, 1.50, 0.15, 'admin'), 
(4, 42, 2.00, 0.20, 'admin'), 
(4, 42, 2.50, 0.20, 'admin'), 
(4, 42, 3.00, 0.25, 'admin');



--SELECT 
--    -- Valore per AlluminioSpessore
--    (SELECT ValoreEuro FROM MaterialiListinoProvvigioni 
--     WHERE IdMateriale = 4 AND IdDimensione = @IdSpessore AND PercentualeProvv = @ProvvigioneAgente) AS ValoreSpessore,

--    -- Valore per AlluminioLarghezza
--    (SELECT ValoreEuro FROM MaterialiListinoProvvigioni 
--     WHERE IdMateriale = 4 AND IdDimensione = @IdLarghezza AND PercentualeProvv = @ValoreSpessoreScelto) AS ValoreLarghezza
	 
	 
	 
	 --[dbo].[ProspettoCobral]
	 --[RameLarghezza]
	 --[RameSpessore]

	 -- POPOLAMENTO LISTINO PROVVIGIONI: SPESSORE RAME (ID 2)
-- Logica: (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente)

INSERT INTO MaterialiListinoProvvigioni (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente)
VALUES 
-- 0,10 - 0,19 mm (ID 43)
(2, 43, 10.0, 1.40, 'admin'), 
(2, 43, 15.0, 1.43, 'admin'),

-- 0,20 - 0,29 mm (ID 44)
(2, 44, 10.0, 1.10, 'admin'), 
(2, 44, 15.0, 1.25, 'admin'),

-- 0,30 - 0,49 mm (ID 45)
(2, 45, 10.0, 0.95, 'admin'), 
(2, 45, 15.0, 1.00, 'admin'),

-- 0,50 - 0,79 mm (ID 46)
(2, 46, 10.0, 0.82, 'admin'), 
(2, 46, 15.0, 0.85, 'admin'),

-- 0,80 - 1,5 mm (ID 47)
(2, 47, 10.0, 0.78, 'admin'), 
(2, 47, 15.0, 0.82, 'admin'),

-- > 1,6 mm (ID 48)
(2, 48, 10.0, 0.95, 'admin'), 
(2, 48, 15.0, 1.00, 'admin');


-- POPOLAMENTO LISTINO PROVVIGIONI: LARGHEZZA RAME (ID 2)
-- Logica: (IdMateriale, IdDimensioneLarg, IdDimensioneSpess_As_Provv, ValoreEuro, Utente)

INSERT INTO MaterialiListinoProvvigioni (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente)
VALUES 
-- LARGHEZZA 8 - 15 mm (ID 49)
(2, 49, 43, 0.50, 'admin'), -- per spessore 0,10-0,19
(2, 49, 44, 0.40, 'admin'), -- per spessore 0,20-0,29
(2, 49, 45, 0.30, 'admin'), -- per spessore 0,30-0,49
(2, 49, 46, 0.20, 'admin'), -- per spessore 0,50-0,79
(2, 49, 47, 0.15, 'admin'), -- per spessore 0,80-1,5
(2, 49, 48, 0.30, 'admin'), -- per spessore > 1,6

-- LARGHEZZA 15,1 - 20 mm (ID 50)
(2, 50, 43, 0.40, 'admin'), 
(2, 50, 44, 0.30, 'admin'), 
(2, 50, 45, 0.20, 'admin'), 
(2, 50, 46, 0.15, 'admin'), 
(2, 50, 47, 0.10, 'admin'), 
(2, 50, 48, 0.20, 'admin'),

-- LARGHEZZA 20,1 - 25 mm (ID 51)
(2, 51, 43, 0.30, 'admin'), 
(2, 51, 44, 0.20, 'admin'), 
(2, 51, 45, 0.15, 'admin'), 
(2, 51, 46, 0.10, 'admin'), 
(2, 51, 47, 0.05, 'admin'), 
(2, 51, 48, 0.10, 'admin');



----------------ottone

-- Sistema gli spessori dell'Ottone (IdMateriale 1)
UPDATE MaterialiDimensioni 
SET Valore = '0,50 - 0,79 mm' 
WHERE IdDimensione = 55;

UPDATE MaterialiDimensioni 
SET Valore = '0,80 - 1,5 mm' 
WHERE IdDimensione = 56;

UPDATE MaterialiDimensioni 
SET Valore = '> 1,6 mm' 
WHERE IdDimensione = 57;


--Select * FROM MaterialiDimensioni WHERE IdMateriale = 1 AND IdDimensione BETWEEN 55 AND 57;

Popolamento [OttoneSpessore]
Incrociamo gli ID 52-57 con le colonne provvigione (10.0 e 15.0).

-- PREZZI SPESSORE OTTONE (ID 1)
-- Coordinata A: IdDimensione (52-57) | Coordinata B: PercentualeProvv (10.0, 15.0)

INSERT INTO MaterialiListinoProvvigioni (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente)
VALUES 
(1, 52, 10.0, 1.05, 'admin'), (1, 52, 15.0, 1.20, 'admin'), -- 0,10 - 0,19 mm
(1, 53, 10.0, 0.75, 'admin'), (1, 53, 15.0, 0.90, 'admin'), -- 0,20 - 0,29 mm
(1, 54, 10.0, 0.65, 'admin'), (1, 54, 15.0, 0.80, 'admin'), -- 0,30 - 0,49 mm
(1, 55, 10.0, 0.60, 'admin'), (1, 55, 15.0, 0.75, 'admin'), -- 0,50 - 0,79 mm
(1, 56, 10.0, 0.60, 'admin'), (1, 56, 15.0, 0.70, 'admin'), -- 0,80 - 1,5 mm
(1, 57, 10.0, 0.65, 'admin'), (1, 57, 15.0, 0.80, 'admin'); -- > 1,6 mm


-- POPOLAMENTO LISTINO PROVVIGIONI: LARGHEZZA OTTONE (ID 1)
-- Coordinata A (IdDimensione): La Larghezza scelta (58, 59 o 60)
-- Coordinata B (PercentualeProvv): L'ID dello Spessore scelto (52, 53, 54, 55, 56 o 57)

INSERT INTO MaterialiListinoProvvigioni (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente)
VALUES 
-- LARGHEZZA: mm 8 - 15 (ID 58)
(1, 58, 52, 0.50, 'admin'), -- rif. spessore 0,10 - 0,19
(1, 58, 53, 0.40, 'admin'), -- rif. spessore 0,20 - 0,29
(1, 58, 54, 0.30, 'admin'), -- rif. spessore 0,30 - 0,49
(1, 58, 55, 0.20, 'admin'), -- rif. spessore 0,50 - 0,79
(1, 58, 56, 0.15, 'admin'), -- rif. spessore 0,80 - 1,5
(1, 58, 57, 0.20, 'admin'), -- rif. spessore > 1,6

-- LARGHEZZA: mm 15,1 - 20 (ID 59)
(1, 59, 52, 0.40, 'admin'), 
(1, 59, 53, 0.30, 'admin'), 
(1, 59, 54, 0.20, 'admin'), 
(1, 59, 55, 0.15, 'admin'), 
(1, 59, 56, 0.10, 'admin'), 
(1, 59, 57, 0.15, 'admin'),

-- LARGHEZZA: mm 20,1 - 25 (ID 60)
(1, 60, 52, 0.30, 'admin'), 
(1, 60, 53, 0.20, 'admin'), 
(1, 60, 54, 0.15, 'admin'), 
(1, 60, 55, 0.10, 'admin'), 
(1, 60, 56, 0.05, 'admin'), 
(1, 60, 57, 0.10, 'admin');



-------------------------BRONZO

-- Sistemazione Spessori (Coordinata Verticale)
UPDATE MaterialiDimensioni SET Valore = '0,41 - 0,60 mm' WHERE IdDimensione = 64;
UPDATE MaterialiDimensioni SET Valore = '0,61 - 1,50 mm' WHERE IdDimensione = 65;
UPDATE MaterialiDimensioni SET Valore = '> 1,51 mm'       WHERE IdDimensione = 66;

-- Sistemazione Larghezze (Coordinata Orizzontale)
UPDATE MaterialiDimensioni SET Valore = '7,0 - 14,9 mm'  WHERE IdDimensione = 67;
UPDATE MaterialiDimensioni SET Valore = '15 - 29,9 mm'   WHERE IdDimensione = 68;
UPDATE MaterialiDimensioni SET Valore = '30 - 59,9 mm'   WHERE IdDimensione = 69;
UPDATE MaterialiDimensioni SET Valore = '60 - 500 mm'    WHERE IdDimensione = 70;


-- LARGHEZZA 7,0 - 14,9 mm (ID 67)
INSERT INTO MaterialiListinoProvvigioni (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente) VALUES 
(3, 67, 62, 1.61, 'admin'), -- spessore 0,16-0,25
(3, 67, 63, 1.53, 'admin'), -- spessore 0,26-0,40
(3, 67, 64, 1.47, 'admin'), -- spessore 0,41-0,60
(3, 67, 65, 1.40, 'admin'); -- spessore 0,61-1,50

-- LARGHEZZA 15 - 29,9 mm (ID 68)
INSERT INTO MaterialiListinoProvvigioni (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente) VALUES 
(3, 68, 62, 1.55, 'admin'), 
(3, 68, 63, 1.48, 'admin'), 
(3, 68, 64, 1.42, 'admin'), 
(3, 68, 65, 1.34, 'admin'),
(3, 68, 66, 1.34, 'admin'); -- spessore > 1,51

-- LARGHEZZA 30 - 59,9 mm (ID 69)
INSERT INTO MaterialiListinoProvvigioni (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente) VALUES 
(3, 69, 62, 1.41, 'admin'), 
(3, 69, 63, 1.38, 'admin'), 
(3, 69, 64, 1.28, 'admin'), 
(3, 69, 65, 1.20, 'admin'),
(3, 69, 66, 1.20, 'admin');

-- LARGHEZZA 60 - 500 mm (ID 70)
INSERT INTO MaterialiListinoProvvigioni (IdMateriale, IdDimensione, PercentualeProvv, ValoreEuro, Utente) VALUES 
(3, 70, 62, 1.30, 'admin'), 
(3, 70, 63, 1.22, 'admin'), 
(3, 70, 64, 1.16, 'admin'), 
(3, 70, 65, 1.10, 'admin'),
(3, 70, 66, 1.19, 'admin');








 
select * from MaterialiDimensioni where Tipo = 'Spessore' and  idmateriale=2 and valore='0,50 - 0,79 mm'

IdDimensione	IdMateriale	Tipo	Valore	Ordine	DataInserimento	DataModifica	Utente
46	2	Spessore	0,50 - 0,79 mm	4	2026-04-21 16:02:03.113	NULL	NULL

select * from MaterialiListinoProvvigioni where idmateriale=2 and IdDimensione=46  


----------------------------------------TEST-----------------------------------
-- TEST SPESSORE (Ottone e Rame)
--DECLARE @Materiale NVARCHAR(50) = 'OTTONE'; -- Oppure 'RAME'
--DECLARE @SpessoreTesto NVARCHAR(100) = '0,10 - 0,19 mm'; -- La riga dell'Excel
--DECLARE @ProvvigioneAgente DECIMAL(18,2) = 10.0; -- La colonna dell'Excel (10.0 o 15.0)
DECLARE @Materiale NVARCHAR(50) = 'Rame'; -- Oppure 'RAME'
DECLARE @SpessoreTesto NVARCHAR(100) = '0,50 - 0,79 mm'; -- La riga dell'Excel
DECLARE @ProvvigioneAgente DECIMAL(18,2) =  15.0; -- La colonna dell'Excel (10.0 o 15.

SELECT 
    M.DescrizioneMateriale AS Materiale,
    D.Valore AS Spessore,
    L.PercentualeProvv AS Provvigione_Cercata,
    L.ValoreEuro AS Prezzo_Euro_Risultante
FROM Materiali M
JOIN MaterialiDimensioni D ON M.IdMateriale = D.IdMateriale
JOIN MaterialiListinoProvvigioni L ON D.IdDimensione = L.IdDimensione
WHERE M.DescrizioneMateriale = @Materiale
  AND D.Valore = @SpessoreTesto
  AND D.Tipo = 'Spessore'
  AND L.PercentualeProvv = @ProvvigioneAgente;


  -- TEST LARGHEZZA (Bronzo, Ottone, Rame)

 
select * from MaterialiDimensioni where Tipo = 'Larghezza' and  idmateriale=2 and valore='0,50 - 0,79 mm'

IdDimensione	IdMateriale	Tipo	Valore	Ordine	DataInserimento	DataModifica	Utente
46	2	Spessore	0,50 - 0,79 mm	4	2026-04-21 16:02:03.113	NULL	NULL

select * from MaterialiListinoProvvigioni where idmateriale=2 and IdDimensione=46  







--DECLARE @MaterialeLarg NVARCHAR(50) = 'BRONZO';
--DECLARE @LarghezzaTesto NVARCHAR(100) = '7,0 - 14,9 mm'; -- La riga dell'Excel
--DECLARE @SpessoreRiferimento NVARCHAR(100) = '0,16 - 0,25 mm'; -- La COLONNA dell'Excel
DECLARE @MaterialeLarg NVARCHAR(50) = 'Rame';
DECLARE @LarghezzaTesto NVARCHAR(100) = 'mm 8 - 15'; -- La riga dell'Excel
DECLARE @SpessoreRiferimento NVARCHAR(100) = '0,50 - 0,79 mm'; -- La COLONNA dell'Excel
SELECT 
    M.DescrizioneMateriale AS Materiale,
    @SpessoreRiferimento AS Spessore_di_Riferimento,
    D_Larg.Valore AS Larghezza_Cercata,
    L.ValoreEuro AS Prezzo_Euro_Risultante
FROM Materiali M
-- Trovo l'ID della larghezza
JOIN MaterialiDimensioni D_Larg ON M.IdMateriale = D_Larg.IdMateriale 
    AND D_Larg.Valore = @LarghezzaTesto AND D_Larg.Tipo = 'Larghezza'
-- Trovo l'ID dello spessore che fa da "colonna"
JOIN MaterialiDimensioni D_Spess ON M.IdMateriale = D_Spess.IdMateriale 
    AND D_Spess.Valore = @SpessoreRiferimento AND D_Spess.Tipo = 'Spessore'
-- Cerco il prezzo incrociando i due ID
JOIN MaterialiListinoProvvigioni L ON L.IdDimensione = D_Larg.IdDimensione 
    AND L.PercentualeProvv = D_Spess.IdDimensione
WHERE M.DescrizioneMateriale = @MaterialeLarg;




UPDATE MaterialiDimensioni 
SET Valore = 'mm ' + REPLACE(Valore, ' mm', '') 
WHERE Tipo = 'Larghezza' AND Valore NOT LIKE 'mm%';

-- Correzione specifica per il Bronzo (che ha range tipo 7,0 - 14,9)
UPDATE MaterialiDimensioni 
SET Valore = 'mm7,0-14,9' WHERE IdDimensione = 67;
UPDATE MaterialiDimensioni SET Valore = 'mm15-29,9' WHERE IdDimensione = 68;
UPDATE MaterialiDimensioni SET Valore = 'mm30-59,9' WHERE IdDimensione = 69;
UPDATE MaterialiDimensioni SET Valore = 'mm60-500' WHERE IdDimensione = 70;