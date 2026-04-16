/**

 * Landing de suscripciones: acordeón FAQ, selector anual/mensual Profesional,

 * comparativa móvil (una columna + tabs) y actualización de precio en tabla escritorio.

 */

document.addEventListener('DOMContentLoaded', function () {

    var acordeonItems = document.querySelectorAll('#suscripciones-pagina .acordeon-item');

    acordeonItems.forEach(function (item) {

        var titulo = item.querySelector('.acordeon-titulo');

        var contenido = item.querySelector('.acordeon-contenido');

        var icono = item.querySelector('.acordeon-icono');

        if (!titulo || !contenido) return;

        titulo.addEventListener('click', function () {

            var isExpanded = titulo.getAttribute('aria-expanded') === 'true';

            acordeonItems.forEach(function (otherItem) {

                if (otherItem !== item) {

                    var ot = otherItem.querySelector('.acordeon-titulo');

                    var oc = otherItem.querySelector('.acordeon-contenido');

                    var oi = otherItem.querySelector('.acordeon-icono');

                    if (ot && oc) {

                        ot.setAttribute('aria-expanded', 'false');

                        oc.style.maxHeight = '';

                        if (oi) oi.textContent = '+';

                    }

                }

            });

            if (isExpanded) {

                titulo.setAttribute('aria-expanded', 'false');

                contenido.style.maxHeight = '';

                if (icono) icono.textContent = '+';

            } else {

                titulo.setAttribute('aria-expanded', 'true');

                contenido.style.maxHeight = contenido.scrollHeight + 'px';

                if (icono) icono.textContent = '−';

            }

        });

    });



    var intervaloFacturacionProfesional = 'anual';



    function actualizarPrecioComparativaProfesional() {

        var esMensual = intervaloFacturacionProfesional === 'mensual';

        document.querySelectorAll('[data-js-comparativa-celda-precio-profesional]').forEach(function (celda) {

            var texto = esMensual

                ? (celda.getAttribute('data-texto-precio-mensual') || '')

                : (celda.getAttribute('data-texto-precio-anual') || '');

            celda.textContent = texto;

        });

    }



    var planPro = document.getElementById('suscripcion-plan-profesional');

    var formPro = document.getElementById('suscripcion-plan-profesional-form');

    var pistaIntervalo = document.getElementById('suscripciones-intervalo-pista');

    var tabAnual = document.getElementById('suscripciones-intervalo-tab-anual');

    var tabMensual = document.getElementById('suscripciones-intervalo-tab-mensual');

    var precioEl = document.getElementById('suscripcion-plan-profesional-precio');

    var periodoEl = document.getElementById('suscripcion-plan-profesional-periodo');



    if (planPro && formPro && pistaIntervalo && tabAnual && tabMensual && precioEl && periodoEl) {

        function idCheckoutPara(intervalo) {

            var idA = planPro.getAttribute('data-checkout-anual-id');

            var idM = planPro.getAttribute('data-checkout-mensual-id');

            if (intervalo === 'mensual') {

                return (idM && idM.length) ? idM : idA;

            }

            return (idA && idA.length) ? idA : idM;

        }



        function aplicarIntervalo(intervalo) {

            intervaloFacturacionProfesional = intervalo;

            var esMensual = intervalo === 'mensual';

            pistaIntervalo.setAttribute('data-intervalo-seleccionado', esMensual ? 'mensual' : 'anual');

            if (esMensual) {

                pistaIntervalo.classList.add('suscripciones-intervalo-pista--mensual');

            } else {

                pistaIntervalo.classList.remove('suscripciones-intervalo-pista--mensual');

            }

            tabAnual.classList.toggle('is-active', !esMensual);

            tabMensual.classList.toggle('is-active', esMensual);

            tabAnual.setAttribute('aria-selected', (!esMensual).toString());

            tabMensual.setAttribute('aria-selected', esMensual.toString());



            precioEl.textContent = esMensual

                ? (planPro.getAttribute('data-texto-precio-mensual') || '')

                : (planPro.getAttribute('data-texto-precio-anual') || '');

            periodoEl.textContent = esMensual

                ? (planPro.getAttribute('data-texto-periodo-mensual') || '')

                : (planPro.getAttribute('data-texto-periodo-anual') || '');



            var id = idCheckoutPara(intervalo);

            if (id && id.length) {

                formPro.setAttribute('action', '/stripe/checkout/' + id);

            }



            actualizarPrecioComparativaProfesional();

        }



        tabAnual.addEventListener('click', function () { aplicarIntervalo('anual'); });

        tabMensual.addEventListener('click', function () { aplicarIntervalo('mensual'); });

        aplicarIntervalo('anual');

    } else {

        actualizarPrecioComparativaProfesional();

    }



    var mqMovilComparativa = window.matchMedia('(max-width: 767px)');

    var comparativaBloque = document.getElementById('suscripciones-comparativa-bloque');

    var comparativaPista = document.getElementById('suscripciones-comparativa-pista');

    var comparativaCabecera = document.getElementById('suscripciones-comparativa-cabecera-plan');



    if (comparativaBloque && comparativaPista && comparativaCabecera) {

        var botonesComparativa = comparativaPista.querySelectorAll('[data-js-comparativa-plan]');



        function clasePistaPorIndice(i) {

            comparativaPista.classList.remove('suscripciones-comparativa-pista--plan-0', 'suscripciones-comparativa-pista--plan-1', 'suscripciones-comparativa-pista--plan-2');

            comparativaPista.classList.add('suscripciones-comparativa-pista--plan-' + i);

        }



        function aplicarPlanComparativa(slug, etiquetaCabecera, indice) {

            clasePistaPorIndice(indice);

            comparativaCabecera.textContent = etiquetaCabecera;



            botonesComparativa.forEach(function (btn, j) {

                var activo = j === indice;

                btn.classList.toggle('is-active', activo);

                btn.setAttribute('aria-selected', activo.toString());

            });



            var celdas = comparativaBloque.querySelectorAll('td[data-js-comparativa-columna]');

            celdas.forEach(function (td) {

                var col = td.getAttribute('data-js-comparativa-columna');

                if (col === slug) {

                    td.removeAttribute('hidden');

                } else {

                    td.setAttribute('hidden', 'hidden');

                }

            });



            if (slug === 'profesional') {

                actualizarPrecioComparativaProfesional();

            }

        }



        function enlazarComparativaMovil() {

            botonesComparativa.forEach(function (btn, indice) {

                btn.addEventListener('click', function () {

                    var slug = btn.getAttribute('data-js-comparativa-plan');

                    var etiqueta = btn.getAttribute('data-js-comparativa-etiqueta-cabecera') || slug || '';

                    aplicarPlanComparativa(slug, etiqueta, indice);

                });

            });

        }



        function sincronizarComparativaMovilSiAplica() {

            if (!mqMovilComparativa.matches) return;

            var btnExplorador = document.getElementById('suscripciones-comparativa-tab-explorador');

            if (btnExplorador) {

                aplicarPlanComparativa(

                    'explorador',

                    btnExplorador.getAttribute('data-js-comparativa-etiqueta-cabecera') || 'Explorador',

                    0

                );

            }

        }



        enlazarComparativaMovil();

        sincronizarComparativaMovilSiAplica();



        if (typeof mqMovilComparativa.addEventListener === 'function') {

            mqMovilComparativa.addEventListener('change', sincronizarComparativaMovilSiAplica);

        } else if (typeof mqMovilComparativa.addListener === 'function') {

            mqMovilComparativa.addListener(sincronizarComparativaMovilSiAplica);

        }

    }

});


