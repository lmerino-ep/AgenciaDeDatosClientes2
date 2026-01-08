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

function inicializarGrafico(resultadoDatos, nombreKpi, kpiId) {
    if (!resultadoDatos || resultadoDatos.length === 0) return;

    // Función para determinar si una columna es numérica
    function esColumnaNumerica(nombreColumna, datos) {
        const nombreLower = nombreColumna.toLowerCase();
        // Columnas que siempre son numéricas
        if (nombreLower.includes('total') || 
            nombreLower.includes('cantidad') || 
            nombreLower.includes('count') ||
            nombreLower.includes('suma') ||
            nombreLower.includes('promedio') ||
            nombreLower.includes('media') ||
            nombreLower.includes('usuarios')) {
            return true;
        }
        // Verificar si los valores son numéricos
        const valores = datos.map(r => r[nombreColumna]).filter(v => v != null);
        if (valores.length === 0) return false;
        // Si al menos el 80% de los valores son numéricos, considerarla numérica
        const valoresNumericos = valores.filter(v => !isNaN(parseFloat(v)) && isFinite(v));
        return valoresNumericos.length / valores.length >= 0.8;
    }

    // Detectar si hay columnas numéricas en los datos
    const columnas = Object.keys(resultadoDatos[0]);
    const tieneColumnasNumericas = columnas.some(c => esColumnaNumerica(c, resultadoDatos));

    // Si no hay columnas numéricas, ocultar gráfico y mostrar tabla por defecto
    if (!tieneColumnasNumericas) {
        const contenedorGrafico = document.querySelector('.contenedor-grafico');
        if (contenedorGrafico) {
            contenedorGrafico.style.display = 'none';
        }
        
        // Mostrar tabla por defecto
        const contenidoTabla = document.getElementById('contenidoTabla');
        const textoToggleTabla = document.getElementById('textoToggleTabla');
        const btnToggleTabla = document.getElementById('btnToggleTabla');
        
        if (contenidoTabla) {
            contenidoTabla.classList.remove('tabla-contenido-oculto');
        }
        if (textoToggleTabla) {
            textoToggleTabla.textContent = 'Ocultar tabla';
        }
        if (btnToggleTabla) {
            btnToggleTabla.style.display = 'none'; // Ocultar el botón ya que la tabla está visible por defecto
        }
        
        return;
    }

    // KPIs que no deben generar gráfico, solo tabla (mantener para compatibilidad)
    const kpisSinGrafico = [
        'tokens-activos',
        'total-datos',
        'fuentes-totales'
    ];

    if (kpiId && kpisSinGrafico.includes(kpiId)) {
        // Ocultar contenedor del gráfico si existe
        const contenedorGrafico = document.querySelector('.contenedor-grafico');
        if (contenedorGrafico) {
            contenedorGrafico.style.display = 'none';
        }
        
        // Mostrar tabla por defecto
        const contenidoTabla = document.getElementById('contenidoTabla');
        const textoToggleTabla = document.getElementById('textoToggleTabla');
        const btnToggleTabla = document.getElementById('btnToggleTabla');
        
        if (contenidoTabla) {
            contenidoTabla.classList.remove('tabla-contenido-oculto');
        }
        if (textoToggleTabla) {
            textoToggleTabla.textContent = 'Ocultar tabla';
        }
        if (btnToggleTabla) {
            btnToggleTabla.style.display = 'none';
        }
        
        return;
    }

    // Detectar tipo de gráfico según las columnas
    // (columnas ya está declarada arriba, reutilizamos la misma variable)
    let tipoGrafico = 'bar'; // Por defecto


    // Función para determinar si una columna es descriptiva/metadatos (debe ignorarse para el gráfico)
    function esColumnaDescriptiva(nombreColumna) {
        const nombreLower = nombreColumna.toLowerCase();
        // Excluir columnas que contienen "total" o "cantidad" aunque tengan otras palabras
        if (nombreLower.includes('total') || nombreLower.includes('cantidad')) {
            return false;
        }
        // NO excluir "Actividad" - es una columna de categoría válida
        if (nombreLower === 'actividad') {
            return false;
        }
        // Patrones de columnas descriptivas que no deben usarse en gráficos
        const patronesDescriptivos = [
            'nombre', 'email', 'usuario', 'descripcion', 'descripción',
            'ultima', 'última', 'fechaalta', 'fechacreacion',
            'id', 'codigo', 'código', 'empresa', 'apellidos', 'foto', 'gmailid', 'token',
            'password', 'restablecer', 'solicitud', 'nacimiento'
        ];
        return patronesDescriptivos.some(patron => nombreLower.includes(patron));
    }

    // Identificar columnas clave de forma más precisa
    // Buscar columna de fecha (puede ser "Fecha", "Mes", etc.)
    const columnaFecha = columnas.find(c => {
        const cLower = c.toLowerCase();
        // Buscar columnas que sean exactamente "fecha" o contengan "fecha" pero no sean descriptivas
        if (cLower === 'fecha' || (cLower.includes('fecha') && !esColumnaDescriptiva(c))) {
            return true;
        }
        // También considerar "mes" o "año" si no hay una columna "fecha" explícita
        if (cLower === 'mes' || cLower === 'año' || cLower === 'ano') {
            return true;
        }
        return false;
    });
    
    // Filtrar columnas numéricas (excluyendo descriptivas)
    const columnasNumericas = columnas.filter(c => 
        !esColumnaDescriptiva(c) && esColumnaNumerica(c, resultadoDatos)
    );
    
    // Columna de valor: busca columnas numéricas, priorizando "Total", "Cantidad", etc.
    const columnaValor = columnasNumericas.find(c => {
        const cLower = c.toLowerCase();
        return cLower === 'total' || 
               cLower.includes('total') || 
               cLower === 'cantidad' ||
               cLower.includes('cantidad');
    }) || (columnasNumericas.length > 0 ? columnasNumericas[0] : null);
    
    // Columna de categoría: primera columna que no sea fecha, valor numérico, ni descriptiva
    // Priorizar columnas que parecen categorías (Actividad, Tipo, Categoría, etc.)
    const columnaCategoria = columnas.find(c => {
        if (c === columnaFecha || c === columnaValor) return false;
        if (esColumnaDescriptiva(c)) return false;
        if (esColumnaNumerica(c, resultadoDatos)) return false;
        // Priorizar columnas que parecen categorías
        const cLower = c.toLowerCase();
        if (cLower === 'actividad' || cLower === 'tipo' || cLower === 'categoria' || cLower === 'categoría') {
            return true;
        }
        return true;
    }) || columnas.find(c => {
        // Si no encontramos una columna obvia de categoría, buscar cualquier columna no numérica ni fecha
        if (c === columnaFecha || c === columnaValor) return false;
        if (esColumnaDescriptiva(c)) return false;
        if (esColumnaNumerica(c, resultadoDatos)) return false;
        return true;
    });

    // Si hay columna "Fecha", usar gráfico de líneas
    if (columnaFecha) {
        tipoGrafico = 'line';
    }

    // Si solo hay 2 columnas y una contiene "Total" o "Cantidad", puede ser pastel
    if (columnas.length === 2 && resultadoDatos.length <= 10 && columnaValor) {
        tipoGrafico = 'pie';
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
    } else if (columnaFecha && columnaCategoria && columnaValor) {
        // Caso especial: hay fecha, categoría y valor (ej: Actividad por mes y tipo)
        // Agrupar por fecha y crear una serie por cada categoría
        const fechasUnicas = [...new Set(resultadoDatos.map(r => String(r[columnaFecha])))].sort();
        const categoriasUnicas = [...new Set(resultadoDatos.map(r => String(r[columnaCategoria])))].sort();
        
        labels = fechasUnicas;
        
        // Colores predefinidos para las series (más colores para más series)
        const colores = [
            { bg: 'rgba(255, 99, 132, 0.6)', border: 'rgba(255, 99, 132, 1)' },
            { bg: 'rgba(54, 162, 235, 0.6)', border: 'rgba(54, 162, 235, 1)' },
            { bg: 'rgba(255, 206, 86, 0.6)', border: 'rgba(255, 206, 86, 1)' },
            { bg: 'rgba(75, 192, 192, 0.6)', border: 'rgba(75, 192, 192, 1)' },
            { bg: 'rgba(153, 102, 255, 0.6)', border: 'rgba(153, 102, 255, 1)' },
            { bg: 'rgba(255, 159, 64, 0.6)', border: 'rgba(255, 159, 64, 1)' },
            { bg: 'rgba(199, 199, 199, 0.6)', border: 'rgba(199, 199, 199, 1)' },
            { bg: 'rgba(83, 102, 255, 0.6)', border: 'rgba(83, 102, 255, 1)' },
            { bg: 'rgba(255, 99, 255, 0.6)', border: 'rgba(255, 99, 255, 1)' },
            { bg: 'rgba(99, 255, 132, 0.6)', border: 'rgba(99, 255, 132, 1)' },
            { bg: 'rgba(255, 159, 192, 0.6)', border: 'rgba(255, 159, 192, 1)' },
            { bg: 'rgba(159, 255, 192, 0.6)', border: 'rgba(159, 255, 192, 1)' },
            { bg: 'rgba(192, 159, 255, 0.6)', border: 'rgba(192, 159, 255, 1)' },
            { bg: 'rgba(255, 192, 99, 0.6)', border: 'rgba(255, 192, 99, 1)' },
            { bg: 'rgba(99, 192, 255, 0.6)', border: 'rgba(99, 192, 255, 1)' }
        ];
        
        categoriasUnicas.forEach((categoria, index) => {
            const data = fechasUnicas.map(fecha => {
                const registro = resultadoDatos.find(r => 
                    String(r[columnaFecha]) === fecha && String(r[columnaCategoria]) === categoria
                );
                if (registro) {
                    const valor = registro[columnaValor];
                    return valor != null ? (parseFloat(valor) || 0) : 0;
                }
                return 0;
            });
            
            const color = colores[index % colores.length];
            datasets.push({
                label: categoria,
                data: data,
                backgroundColor: color.bg,
                borderColor: color.border,
                borderWidth: 2,
                tension: tipoGrafico === 'line' ? 0.4 : 0,
                // Guardar el índice del dataset para usar en datalabels
                _datasetIndex: index
            });
        });
    } else {
        // Caso estándar: labels son la primera columna no descriptiva, datasets son las columnas numéricas
        // Encontrar la primera columna que no sea descriptiva ni numérica para los labels
        const columnaLabel = columnas.find(c => 
            !esColumnaDescriptiva(c) && !esColumnaNumerica(c, resultadoDatos)
        ) || columnas[0]; // Si no hay, usar la primera
        
        labels = [...new Set(resultadoDatos.map(r => String(r[columnaLabel] || '')))];
        
        // Crear datasets solo para columnas numéricas (excluyendo descriptivas)
        columnasNumericas.forEach((columna, index) => {
            const data = labels.map(label => {
                const registro = resultadoDatos.find(r => String(r[columnaLabel] || '') === label);
                if (registro) {
                    const valor = registro[columna];
                    return valor != null ? (parseFloat(valor) || 0) : 0;
                }
                return 0;
            });
            
            datasets.push({
                label: columna,
                data: data,
                backgroundColor: `rgba(${Math.floor(Math.random() * 255)}, ${Math.floor(Math.random() * 255)}, ${Math.floor(Math.random() * 255)}, 0.5)`,
                borderColor: `rgba(${Math.floor(Math.random() * 255)}, ${Math.floor(Math.random() * 255)}, ${Math.floor(Math.random() * 255)}, 1)`,
                borderWidth: 2,
                // Guardar el índice del dataset para usar en datalabels
                _datasetIndex: index
            });
        });
    }

    // Crear gráfico
    const ctx = document.getElementById('chartKpi');
    if (ctx) {
        // Contar número de datasets para evitar solapamientos
        const numDatasets = datasets.length;
        
        // Configuración de datalabels según el tipo de gráfico
        let datalabelsConfig = {};
        
        if (tipoGrafico === 'pie') {
            // Para gráficos de pastel: mostrar dentro del quesito
            datalabelsConfig = {
                color: '#fff',
                font: {
                    weight: 'bold',
                    size: 12
                },
                formatter: (value, context) => {
                    // Para gráficos de pastel, value es el valor del dato
                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                    const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                    return value.toLocaleString('es-ES') + '\n(' + percentage + '%)';
                }
            };
        } else if (tipoGrafico === 'line') {
            // Para gráficos de líneas: alternar posición arriba/abajo según el dataset para evitar solapamientos
            datalabelsConfig = {
                color: '#333',
                font: {
                    weight: 'bold',
                    size: 10
                },
                formatter: (value) => {
                    return value != null ? value.toLocaleString('es-ES') : '';
                },
                display: (context) => {
                    // Mostrar solo si el valor es significativo
                    return context.dataset.data[context.dataIndex] != null;
                },
                // Alternar posición según el índice del dataset
                anchor: (context) => {
                    const datasetIndex = context.dataset._datasetIndex !== undefined 
                        ? context.dataset._datasetIndex 
                        : context.datasetIndex;
                    // Si hay múltiples datasets, alternar arriba/abajo
                    return datasetIndex % 2 === 0 ? 'end' : 'start';
                },
                align: (context) => {
                    const datasetIndex = context.dataset._datasetIndex !== undefined 
                        ? context.dataset._datasetIndex 
                        : context.datasetIndex;
                    // Si hay múltiples datasets, alternar arriba/abajo
                    return datasetIndex % 2 === 0 ? 'top' : 'bottom';
                },
                offset: (context) => {
                    const datasetIndex = context.dataset._datasetIndex !== undefined 
                        ? context.dataset._datasetIndex 
                        : context.datasetIndex;
                    // Aumentar el offset cuando hay múltiples datasets
                    const baseOffset = numDatasets > 1 ? 8 : 4;
                    return datasetIndex % 2 === 0 ? baseOffset : -baseOffset;
                },
                clamp: true, // Evitar que las etiquetas se salgan del área del gráfico
                padding: {
                    top: 2,
                    bottom: 2,
                    left: 2,
                    right: 2
                }
            };
        } else {
            // Para gráficos de barras: usar offset vertical para separar cuando hay múltiples datasets
            datalabelsConfig = {
                anchor: 'end',
                align: 'top',
                color: '#333',
                font: {
                    weight: 'bold',
                    size: 10
                },
                formatter: (value) => {
                    return value != null ? value.toLocaleString('es-ES') : '';
                },
                display: (context) => {
                    // Mostrar solo si el valor es significativo
                    return context.dataset.data[context.dataIndex] != null;
                },
                // Aplicar offset vertical cuando hay múltiples datasets
                offset: (context) => {
                    if (numDatasets <= 1) return 4;
                    const datasetIndex = context.dataset._datasetIndex !== undefined 
                        ? context.dataset._datasetIndex 
                        : context.datasetIndex;
                    // Separar verticalmente según el índice del dataset
                    return 4 + (datasetIndex * 15);
                },
                clamp: true, // Evitar que las etiquetas se salgan del área del gráfico
                padding: {
                    top: 2,
                    bottom: 2,
                    left: 2,
                    right: 2
                }
            };
        }
        
        new Chart(ctx.getContext('2d'), {
            type: tipoGrafico,
            data: {
                labels: labels,
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true,
                        position: 'top',
                    },
                    title: {
                        display: true,
                        text: nombreKpi,
                        font: {
                            size: 18,
                            weight: 'bold'
                        }
                    },
                    tooltip: {
                        enabled: true,
                        mode: 'index',
                        intersect: false
                    },
                    datalabels: datalabelsConfig
                },
                scales: tipoGrafico !== 'pie' ? {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            precision: 0
                        }
                    },
                    x: {
                        ticks: {
                            maxRotation: 45,
                            minRotation: 0
                        }
                    }
                } : {}
            }
        });
    }
}

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function() {
    inicializarKpisData();
    
    // Inicializar ModalCargando
    let oCargando = null;
    if (typeof ModalCargando !== 'undefined') {
        oCargando = new ModalCargando();
    }
    
    const kpiSelect = document.getElementById('kpiSeleccionado');
    if (kpiSelect) {
        kpiSelect.addEventListener('change', actualizarFormularioParametros);
        if (kpiSelect.value) {
            actualizarFormularioParametros();
        }
    }

    // Botón Ejecutar KPI
    const formKpi = document.getElementById('formKpi');
    if (formKpi && oCargando) {
        formKpi.addEventListener('submit', function() {
            const kpiSeleccionado = document.getElementById('kpiSeleccionado');
            if (kpiSeleccionado && kpiSeleccionado.value) {
                oCargando.Mostrar();
            }
        });
    }

    // Botón Limpiar
    const btnLimpiar = document.getElementById('btnLimpiar');
    if (btnLimpiar) {
        btnLimpiar.addEventListener('click', function() {
            if (oCargando) {
                oCargando.Mostrar();
            }
            limpiarFormulario();
            // Redirigir a la página sin parámetros
            window.location.href = window.location.pathname;
        });
    }

    // Botones de exportación
    const btnExportarJson = document.getElementById('btnExportarJson');
    if (btnExportarJson && oCargando) {
        btnExportarJson.addEventListener('click', function(e) {
            e.preventDefault();
            const url = btnExportarJson.getAttribute('href');
            if (url) {
                // Mostrar modal de carga
                oCargando.Mostrar();
                
                // Crear un elemento <a> temporal para iniciar la descarga
                const link = document.createElement('a');
                link.href = url;
                link.style.display = 'none';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                
                // Ocultar el modal después de un tiempo razonable
                // Nota: No podemos detectar cuando termina la descarga del navegador,
                // así que ocultamos después de un tiempo estimado
                setTimeout(function() {
                    oCargando.Ocultar();
                }, 2000);
            }
        });
    }

    const btnExportarCsv = document.getElementById('btnExportarCsv');
    if (btnExportarCsv && oCargando) {
        btnExportarCsv.addEventListener('click', function(e) {
            e.preventDefault();
            const url = btnExportarCsv.getAttribute('href');
            if (url) {
                // Mostrar modal de carga
                oCargando.Mostrar();
                
                // Crear un elemento <a> temporal para iniciar la descarga
                const link = document.createElement('a');
                link.href = url;
                link.style.display = 'none';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                
                // Ocultar el modal después de un tiempo razonable
                // Nota: No podemos detectar cuando termina la descarga del navegador,
                // así que ocultamos después de un tiempo estimado
                setTimeout(function() {
                    oCargando.Ocultar();
                }, 2000);
            }
        });
    }

    // Toggle de tabla
    const btnToggleTabla = document.getElementById('btnToggleTabla');
    const contenidoTabla = document.getElementById('contenidoTabla');
    const textoToggleTabla = document.getElementById('textoToggleTabla');
    if (btnToggleTabla && contenidoTabla && textoToggleTabla) {
        btnToggleTabla.addEventListener('click', function () {
            const estaVisible = !contenidoTabla.classList.contains('tabla-contenido-oculto');
            if (estaVisible) {
                contenidoTabla.classList.add('tabla-contenido-oculto');
                textoToggleTabla.textContent = 'Mostrar tabla';
            } else {
                contenidoTabla.classList.remove('tabla-contenido-oculto');
                textoToggleTabla.textContent = 'Ocultar tabla';
            }
        });
    }

    // Función para decodificar entidades HTML
    function decodificarEntidadesHTML(texto) {
        if (!texto) return texto;
        const textarea = document.createElement('textarea');
        textarea.innerHTML = texto;
        return textarea.value;
    }

    // Inicializar gráfico si hay datos
    const resultadoDataElement = document.getElementById('resultado-datos');
    const nombreKpiElement = document.getElementById('nombre-kpi');
    const kpiIdElement = document.getElementById('kpi-id');
    if (resultadoDataElement && nombreKpiElement) {
        const resultadoDatos = JSON.parse(resultadoDataElement.textContent);
        const nombreKpi = decodificarEntidadesHTML(nombreKpiElement.textContent);
        const kpiId = kpiIdElement ? kpiIdElement.textContent : null;
        if (resultadoDatos && resultadoDatos.length > 0) {
            // Función para intentar inicializar el gráfico
            function intentarInicializarGrafico() {
                if (typeof Chart !== 'undefined') {
                    // Registrar el plugin de datalabels si está disponible
                    // El plugin puede estar disponible como ChartDataLabels o como parte de Chart
                    if (typeof ChartDataLabels !== 'undefined') {
                        Chart.register(ChartDataLabels);
                    } else if (typeof window.ChartDataLabels !== 'undefined') {
                        Chart.register(window.ChartDataLabels);
                    }
                    inicializarGrafico(resultadoDatos, nombreKpi, kpiId);
                } else {
                    // Si Chart.js aún no está cargado, esperar un poco más
                    setTimeout(intentarInicializarGrafico, 100);
                }
            }
            
            // Esperar a que Chart.js esté cargado (puede tener defer)
            intentarInicializarGrafico();
        }
    }
});

