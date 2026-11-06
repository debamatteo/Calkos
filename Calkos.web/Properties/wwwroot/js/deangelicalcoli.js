// ============================================================
// HELPER NUMERICI — PRECISIONE DEANGELI
// ============================================================
export function toNum(v) {
    if (v === null || v === undefined) return 0;
    if (typeof v === "string") {
        v = v.replace(',', '.').trim();
        if (v === "") return 0;
    }
    const n = parseFloat(v);
    return isNaN(n) ? 0 : n;
}

export function round6(v) { return Number(toNum(v).toFixed(6)); }
export function round3(v) { return Number(toNum(v).toFixed(3)); }

// ============================================================
// FUNZIONI PURE DI CALCOLO
// ============================================================

// 1 — Variabile
export function calcolaValoreProvvigioneVariabile({ PercentualeProvvigioneVariabile, PrezzoConLavorazione }) {
    const perc = toNum(PercentualeProvvigioneVariabile);
    const costo = toNum(PrezzoConLavorazione);
    return round6(perc === 0 ? 0 : costo * (perc / 100));
}

// 2 — Fissa 1
export function calcolaValoreProvvigioneFissa1({ QuantitaOrdine, PrezzoVendita, PercentualeProvvigioneVariabile, Prezzo, Materiale, PrezzoConLavorazione }) {
    const qtaOrdine = (QuantitaOrdine || "").toLowerCase();
    const prezzoVendita = toNum(PrezzoVendita);
    const aliquotaVar = toNum(PercentualeProvvigioneVariabile);
    const prezzo = toNum(Prezzo);
    const materiale = (Materiale || "").toLowerCase();
    const prezzoConLav = toNum(PrezzoConLavorazione);

    let totale = 0;

    if (qtaOrdine === "si") {
        totale = prezzoVendita * 0.015;
    } else if (aliquotaVar >= 5) {
        if (prezzo !== 0) {
            if (materiale === "rame") totale = prezzoConLav * 0.015;
            if (materiale === "rame smaltato") totale = prezzoVendita * 0.015;
        }
    }

    return round6(totale);
}

// 3 — Fissa 2
export function calcolaValoreProvvigioneFissa2({ Prezzo, Materiale, PrezzoConLavorazione, PrezzoVendita }) {
    const prezzo = toNum(Prezzo);
    const materiale = (Materiale || "").toLowerCase();
    const prezzoConLav = toNum(PrezzoConLavorazione);
    const prezzoVendita = toNum(PrezzoVendita);

    let totale = 0;

    if (prezzo !== 0) {
        if (materiale === "alluminio") totale += prezzoConLav * 0.03;
        if (materiale === "alluminio smaltato") totale += prezzoVendita * 0.03;
    }

    return round6(totale);
}

// 4 — Fissa 3
export function calcolaValoreProvvigioneFissa3({ Materiale, PercentualeProvvigioneVariabile, CostoLavorazione }) {
    const materiale = (Materiale || "").toLowerCase();
    const aliquotaVar = toNum(PercentualeProvvigioneVariabile);
    const costoLav = toNum(CostoLavorazione);

    const totale = (materiale !== "alluminio" && aliquotaVar < 5)
        ? costoLav * 0.08
        : 0;

    return round6(totale);
}

// 5 — Differenza
export function calcolaDifferenza({ PrezzoVendita, PrezzoConLavorazione, QuantitaOrdine, FlagIndicatoreMaggiorazione }) {
    const vendita = toNum(PrezzoVendita);
    const costo = toNum(PrezzoConLavorazione);
    const qta = toNum(QuantitaOrdine);
    const flag = (FlagIndicatoreMaggiorazione || "").toLowerCase();

    let delta = 0;

    if (flag === "true") delta = 0;
    else if (qta !== 0 && qta < 500) delta = 0;
    else if (costo > vendita) delta = 0;
    else delta = vendita - costo;

    return round6(delta);
}

// 6 — Totale Fisso
export function calcolaTotaleFissoProvvigione({ Quantita, F1, F2, F3 }) {
    const q = toNum(Quantita);
    return round6((toNum(F1) * q) + (toNum(F2) * q) + (toNum(F3) * q));
}

// 7 — Totale Variabile
export function calcolaTotaleVariabileProvvigione({ Differenza, Quantita, Perc_Delta_Provvigione }) {
    const delta = toNum(Differenza);
    const kg = toNum(Quantita);
    const perc = toNum(Perc_Delta_Provvigione);
    return round6(delta * kg * (perc / 100));
}

// 8 — Totale Fattura
export function calcolaTotaleProvvigioneDaFatturare({ TotaleFisso, TotaleVariabile }) {
    return round6(toNum(TotaleFisso) + toNum(TotaleVariabile));
}

// ============================================================
// FUNZIONE COMPLETA ricalcolaTutto (versione pura)
// ============================================================
export function ricalcolaTuttoPure(input) {
    const out = {};

    out.ValoreProvvigioneVariabile = calcolaValoreProvvigioneVariabile(input);
    out.ValoreProvvigioneFissa1 = calcolaValoreProvvigioneFissa1(input);
    out.ValoreProvvigioneFissa2 = calcolaValoreProvvigioneFissa2(input);
    out.ValoreProvvigioneFissa3 = calcolaValoreProvvigioneFissa3(input);
    out.Differenza = calcolaDifferenza(input);

    out.TotaleFissoProvvigione = calcolaTotaleFissoProvvigione({
        Quantita: input.Quantita,
        F1: out.ValoreProvvigioneFissa1,
        F2: out.ValoreProvvigioneFissa2,
        F3: out.ValoreProvvigioneFissa3
    });

    out.TotaleVariabileProvvigione = calcolaTotaleVariabileProvvigione({
        Differenza: out.Differenza,
        Quantita: input.Quantita,
        Perc_Delta_Provvigione: input.Perc_Delta_Provvigione
    });

    out.TotaleProvvigioneDaFatturare = calcolaTotaleProvvigioneDaFatturare({
        TotaleFisso: out.TotaleFissoProvvigione,
        TotaleVariabile: out.TotaleVariabileProvvigione
    });

    return out;
}
