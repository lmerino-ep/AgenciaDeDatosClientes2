/**
 * Convierte instantes UTC al huso local en la tabla de historial de descargas.
 */
(function () {
    function textoHoraLocal(iso) {
        if (!iso) return '';
        var d = new Date(iso);
        return isNaN(d.getTime()) ? '' : d.toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' });
    }
    document.querySelectorAll('[data-fecha-utc]').forEach(function (el) {
        var t = textoHoraLocal(el.getAttribute('data-fecha-utc'));
        if (!t) return;
        if (el.classList.contains('historial-fecha-en-titulo')) {
            el.setAttribute('title', 'Última descarga: ' + t);
        } else if (el.classList.contains('historial-fecha-en-celda')) {
            el.textContent = t;
        }
    });
})();
