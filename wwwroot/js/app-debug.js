// ============================================================
// DEBUG SWITCH (ON/OFF) — abilita/disabilita TUTTI i log
// i log vanno messi cosi :
// log("Spessori DB:", listaSpessori);
// e non cosi 
// console.log("Spessori DB:", listaSpessori);)
// altrimenti non si possono disabilitare in produzione



// ============================================================
const DEBUG = true;   // true in sviluppo, false in produzione

function log() {
    if (DEBUG) console.log.apply(console, arguments);
}
