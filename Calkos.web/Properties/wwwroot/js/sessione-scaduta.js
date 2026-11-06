// ============================================================
// 15/10/2026 GESTIONE SESSIONE SCADUTA (cookie di login scaduto)
// ------------------------------------------------------------
// Il server risponde 401 alle chiamate da script quando il cookie è scaduto.
// Questo file intercetta il 401 (jQuery e fetch), avvisa l'utente e lo porta al login.
// Caricato in _AdminLayout.cshtml e _Layout.cshtml dopo SweetAlert2.
// 16/10/2026: la parte jQuery non si aggancia più al caricamento del file ma tramite
// CalkosSessione.collegaJQuery(), chiamata a fine layout (vedi _AdminLayout.cshtml).
// ============================================================
(function () {
    "use strict";

    // URL del login iniettato dal layout (rispetta eventuali cartelle virtuali IIS)
    const loginUrl = window.CALKOS_LOGIN_URL || "/Identity/Account/Login";
    let gestioneInCorso = false;

    function vaiAlLogin() {
        const ritorno = encodeURIComponent(location.pathname + location.search);
        location.href = loginUrl + "?ReturnUrl=" + ritorno;
    }

    function sessioneScaduta() {
        // Già nella pagina di login, oppure già gestito: evita messaggi doppi e cicli
        if (gestioneInCorso || location.pathname.toLowerCase().includes("/identity/account/login")) return;
        gestioneInCorso = true;

        if (window.Swal) {
            Swal.fire({
                icon: "warning",
                title: "Sessione scaduta",
                text: "La sessione è scaduta per inattività. Effettua di nuovo l'accesso.",
                confirmButtonText: "Vai al login",
                allowOutsideClick: false,
                allowEscapeKey: false
            }).then(vaiAlLogin);
        } else {
            vaiAlLogin();
        }
    }

    // --- jQuery ($.ajax, $.getJSON, DataTables) ---
    // Va chiamata con l'istanza jQuery DEFINITIVA della pagina: alcune view ricaricano jQuery,
    // e gli eventi di un'istanza non sono visibili all'altra.
    function collegaJQuery($) {
        if (!$ || $.__calkosSessioneCollegata) return;
        $.__calkosSessioneCollegata = true;

        // DataTables mostra di default un alert nativo bloccante su ogni errore Ajax.
        // Per il 401 lo silenziamo (lo gestiamo con SweetAlert); per gli altri errori
        // manteniamo l'alert originale, così i bug reali restano visibili.
        function configuraErroriDataTables() {
            if (!$.fn.dataTable || $.fn.dataTable.__calkosErrMode) return;
            $.fn.dataTable.__calkosErrMode = true;
            $.fn.dataTable.ext.errMode = function (settings, techNote, message) {
                if (settings && settings.jqXHR && settings.jqXHR.status === 401) return;
                window.alert(message);
            };
        }
        configuraErroriDataTables();
        $(document).on("preXhr.dt", configuraErroriDataTables); // se DataTables arriva dopo

        $(document).ajaxError(function (evento, jqXHR) {
            if (jqXHR.status === 401) sessioneScaduta();
        });
    }

    window.CalkosSessione = { collegaJQuery: collegaJQuery };

    // --- fetch(): intercettazione centralizzata ---
    if (window.fetch) {
        const fetchOriginale = window.fetch.bind(window);

        window.fetch = async function (...args) {
            const risposta = await fetchOriginale(...args);

            // Solo chiamate verso questo sito (non CDN o servizi esterni)
            let stessoSito = false;
            try { stessoSito = new URL(risposta.url, location.href).origin === location.origin; } catch { }

            if (stessoSito) {
                // 401 dal server, oppure redirect al login seguito dal browser
                const redirectAlLogin = risposta.redirected &&
                    risposta.url.toLowerCase().includes("/identity/account/login");

                if (risposta.status === 401 || redirectAlLogin) {
                    sessioneScaduta();
                    return new Promise(function () { });
                }
            }
            return risposta;
        };
    }
})();