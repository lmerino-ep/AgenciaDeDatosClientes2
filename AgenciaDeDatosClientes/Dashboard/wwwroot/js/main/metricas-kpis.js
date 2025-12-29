// Datos de KPIs (se inicializan desde la página)
let kpisData = [];

// Inicializar datos desde el atributo data
function inicializarKpisData() {
    const dataElement = document.getElementById('kpis-data');
    if (dataElement) {
        kpisData = JSON.parse(dataElement.textContent);
    }
}

function actualizarFormularioParametros() {
    const select = document.getElementById('kpiSeleccionado');
    const selectedOption = select.options[select.selectedIndex];
    const kpiId = selectedOption.value;

    if (!kpiId) {
        document.getElementById('contenedorParametros').innerHTML = '';
        document.getElementById('descripcionKpi').textContent = '';
        return;
    }

    // Buscar KPI en los datos
    const kpi = kpisData.find(k => k.id === kpiId);
    if (!kpi) return;

    // Mostrar descripción
    document.getElementById('descripcionKpi').textContent = kpi.descripcion;

    // Generar campos de parámetros
    let html = '';
    if (kpi.parametros && kpi.parametros.length > 0) {
        html = '<h3>Parámetros:</h3>';
        kpi.parametros.forEach(param => {
            // Validar que el parámetro tenga nombre
            if (!param.nombre) {
                console.warn('Parámetro sin nombre encontrado:', param);
                return;
            }

            // Obtener el texto del label: descripción, nombre, o un valor por defecto
            const labelText = (param.descripcion && param.descripcion.trim()) 
                ? param.descripcion 
                : (param.nombre && param.nombre.trim() 
                    ? param.nombre 
                    : 'Parámetro');

            html += '<div class="form-group">';
            html += `<label for="param_${param.nombre}">${labelText}${param.requerido ? ' *' : ''}:</label>`;

            const paramName = `Parametros[${param.nombre}]`;
            const paramId = `param_${param.nombre}`;
            const defaultValue = (param.valorPorDefecto && param.valorPorDefecto.trim()) ? param.valorPorDefecto : '';
            const requiredAttr = param.requerido ? 'required' : '';

            if (param.tipo === 'fecha') {
                html += `<input type="date" class="form-control" name="${paramName}" id="${paramId}" ${requiredAttr}>`;
            } else if (param.tipo === 'numero') {
                html += `<input type="number" class="form-control" name="${paramName}" id="${paramId}" value="${defaultValue}" ${requiredAttr}>`;
            } else if (param.tipo === 'booleano') {
                html += `<input type="checkbox" name="${paramName}" id="${paramId}" value="true">`;
            } else {
                html += `<input type="text" class="form-control" name="${paramName}" id="${paramId}" value="${defaultValue}" ${requiredAttr}>`;
            }

            // Mostrar descripción adicional solo si es diferente del nombre y no está vacía
            if (param.descripcion && param.descripcion.trim() && param.descripcion !== param.nombre) {
                html += `<small class="form-text text-muted">${param.descripcion}</small>`;
            }

            html += '</div>';
        });
    }

    document.getElementById('contenedorParametros').innerHTML = html;
}

function limpiarFormulario() {
    document.getElementById('kpiSeleccionado').value = '';
    document.getElementById('contenedorParametros').innerHTML = '';
    document.getElementById('descripcionKpi').textContent = '';
    const forzarRecalculo = document.querySelector('input[name="forzarRecalculo"]');
    if (forzarRecalculo) {
        forzarRecalculo.checked = false;
    }
}

function inicializarGrafico(resultadoDatos, nombreKpi) {
    if (!resultadoDatos || resultadoDatos.length === 0) return;

    // Detectar tipo de gráfico según las columnas
    const columnas = Object.keys(resultadoDatos[0]);
    let tipoGrafico = 'bar'; // Por defecto

    // Si hay columna "Fecha" o "fecha", usar gráfico de líneas
    if (columnas.some(c => c.toLowerCase().includes('fecha'))) {
        tipoGrafico = 'line';
    }

    // Si solo hay 2 columnas y una contiene "Total" o "Cantidad", puede ser pastel
    if (columnas.length === 2 && resultadoDatos.length <= 10) {
        const tieneTotal = columnas.some(c => c.toLowerCase().includes('total') || c.toLowerCase().includes('cantidad'));
        if (tieneTotal) {
            tipoGrafico = 'pie';
        }
    }

    // Preparar datos para Chart.js
    let labels = [];
    let datasets = [];

    if (tipoGrafico === 'pie') {
        // Para pastel: labels son la primera columna, data es la segunda
        labels = resultadoDatos.map(r => r[columnas[0]]);
        datasets = [{
            label: columnas[1],
            data: resultadoDatos.map(r => r[columnas[1]]),
            backgroundColor: [
                'rgba(255, 99, 132, 0.8)',
                'rgba(54, 162, 235, 0.8)',
                'rgba(255, 206, 86, 0.8)',
                'rgba(75, 192, 192, 0.8)',
                'rgba(153, 102, 255, 0.8)',
                'rgba(255, 159, 64, 0.8)',
                'rgba(199, 199, 199, 0.8)',
                'rgba(83, 102, 255, 0.8)',
                'rgba(255, 99, 255, 0.8)',
                'rgba(99, 255, 132, 0.8)'
            ]
        }];
    } else {
        // Para barras/líneas: labels son la primera columna, datasets son las demás
        labels = resultadoDatos.map(r => r[columnas[0]]);
        for (let i = 1; i < columnas.length; i++) {
            datasets.push({
                label: columnas[i],
                data: resultadoDatos.map(r => r[columnas[i]]),
                backgroundColor: `rgba(${Math.random() * 255}, ${Math.random() * 255}, ${Math.random() * 255}, 0.5)`,
                borderColor: `rgba(${Math.random() * 255}, ${Math.random() * 255}, ${Math.random() * 255}, 1)`,
                borderWidth: 2
            });
        }
    }

    // Crear gráfico
    const ctx = document.getElementById('chartKpi');
    if (ctx) {
        new Chart(ctx.getContext('2d'), {
            type: tipoGrafico,
            data: {
                labels: labels,
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: true,
                        position: 'top',
                    },
                    title: {
                        display: true,
                        text: nombreKpi
                    }
                },
                scales: tipoGrafico !== 'pie' ? {
                    y: {
                        beginAtZero: true
                    }
                } : {}
            }
        });
    }
}

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function() {
    inicializarKpisData();
    
    const kpiSelect = document.getElementById('kpiSeleccionado');
    if (kpiSelect) {
        kpiSelect.addEventListener('change', actualizarFormularioParametros);
        if (kpiSelect.value) {
            actualizarFormularioParametros();
        }
    }

    const btnLimpiar = document.getElementById('btnLimpiar');
    if (btnLimpiar) {
        btnLimpiar.addEventListener('click', limpiarFormulario);
    }

    // Inicializar gráfico si hay datos
    const resultadoDataElement = document.getElementById('resultado-datos');
    const nombreKpiElement = document.getElementById('nombre-kpi');
    if (resultadoDataElement && nombreKpiElement) {
        const resultadoDatos = JSON.parse(resultadoDataElement.textContent);
        const nombreKpi = nombreKpiElement.textContent;
        if (resultadoDatos && resultadoDatos.length > 0) {
            // Esperar a que Chart.js esté cargado
            if (typeof Chart !== 'undefined') {
                inicializarGrafico(resultadoDatos, nombreKpi);
            } else {
                // Si Chart.js se carga de forma asíncrona, esperar un poco
                setTimeout(() => {
                    if (typeof Chart !== 'undefined') {
                        inicializarGrafico(resultadoDatos, nombreKpi);
                    }
                }, 100);
            }
        }
    }
});

