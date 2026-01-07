/*
Uso:
    var oCargandoContenedor = new ModalCargando($("#hola"));
    oCargandoContenedor.Mostrar();
    oCargandoContenedor.Ocultar();

    var oCargandoPagina = new ModalCargando();
    oCargandoPagina.Mostrar();
    oCargandoPagina.Ocultar();
*/

var ModalCargando = (function () {
    ModalCargando.prototype.domCargando = null;
    var iAcumulados = 0;

    function GenerarDomCargando(bPaginaCompleta) {
        let sHtml = '<div class="modulo-cargando ' + (bPaginaCompleta ? "pagina-completa" : "") + '">' +
            '    <div class="cargando">' +
            '        <div class="cssload-contain">' +
            '            <div class="cssload-dot"></div>' +
            '            <div class="cssload-dot"></div>' +
            '            <div class="cssload-dot"></div>' +
            '            <div class="cssload-dot"></div>' +
            '            <div class="cssload-dot"></div>' +
            '        </div>' +
            '    </div>' +
            '   </div>';
        const domTemplate = document.createElement('template');
        domTemplate.innerHTML = sHtml;
        domContenedor = domTemplate.content.children;
        return domContenedor[0];
    };   

    function ModalCargando(domContenedor) {
        var Me = this;

        Me.domCargando = null;
        Inicializa(Me, domContenedor);
    }

    ModalCargando.prototype.Mostrar = function () {
        var Me = this;
        Me.domCargando.classList.add("cargando-mostrar");
        iAcumulados = 0;
    }

    ModalCargando.prototype.Ocultar = function () {
        var Me = this;
        Me.domCargando.classList.remove("cargando-mostrar");
        iAcumulados = 0;
    }

    ModalCargando.prototype.MostrarAcumulativo = function () {
        var Me = this;
        iAcumulados += 1;
        if (iAcumulados > 0) { Me.domCargando.classList.add("cargando-mostrar"); }
    }

    ModalCargando.prototype.OcultarAcumulativo = function () {
        var Me = this;
        if (iAcumulados > 0) { iAcumulados -= 1; }
        if (iAcumulados === 0) { Me.domCargando.classList.remove("cargando-mostrar"); }
    }

    function Inicializa(Me, domContenedor) {
        let domContenedorEncontrado;
        if (domContenedor !== undefined && domContenedor !== null) {
            if (domContenedor.length !== undefined && domContenedor.length !== null && domContenedor.length > 0) {
                domContenedor = domContenedor[0];
            }
            domContenedorEncontrado = domContenedor.querySelectorAll(".cargando-contenedor");
            if (domContenedorEncontrado.length === 0) {
                Me.domCargando = GenerarDomCargando(false);
                domContenedor.prepend(Me.domCargando);
            } else {
                Me.domCargando = domContenedorEncontrado;
            }
        } else {            
            var domBody = document.getElementsByClassName("layout-body");
            if (domBody.length > 0) {
                Me.domCargando = GenerarDomCargando(true);
                domBody[0].prepend(Me.domCargando);
            }            
        }
    }

    return ModalCargando;
})();

