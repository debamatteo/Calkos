
//cd C:\Lavoro\Calkos\Calkos.web

//npm test
import { ricalcolaTuttoPure } from "../deangelicalcoli.js";

test("Confronto con Excel — RIGA DI TEST", () => {

    // ================================
    // 1) VALORI DELLA RIGA EXCEL
    // ================================
    const input = {
        PercentualeProvvigioneVariabile: 5,
        PrezzoConLavorazione: 1000,
        QuantitaOrdine: "no",
        PrezzoVendita: 200,
        Prezzo: 10,
        Materiale: "rame",
        CostoLavorazione: 100,
        Quantita: 10,
        Perc_Delta_Provvigione: 20,
        FlagIndicatoreMaggiorazione: "false"
    };

    // ================================
    // 2) RISULTATI ATTESI DA EXCEL
    // (METTI QUI I NUMERI DI EXCEL)
    // ================================
    const expected = {
        ValoreProvvigioneVariabile: 50,
        ValoreProvvigioneFissa1: 15,
        ValoreProvvigioneFissa2: 0,
        ValoreProvvigioneFissa3: 0,
        Differenza: 0,
        TotaleFissoProvvigione: 150,
        TotaleVariabileProvvigione: 0,
        TotaleProvvigioneDaFatturare: 150
    };

    // ================================
    // 3) CALCOLO DEL TUO CODICE
    // ================================
    const r = ricalcolaTuttoPure(input);

    // ================================
    // 4) CONFRONTO AUTOMATICO
    // ================================
    for (const key of Object.keys(expected)) {
        expect(r[key]).toBeCloseTo(expected[key], 6);
    }

    console.log("\n✔ TUTTO COINCIDE CON EXCEL\n");
});

