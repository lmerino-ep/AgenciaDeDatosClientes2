/**
 * Script para inicializar los gráficos del dashboard principal
 */

// Cargar datos de los gráficos desde el elemento JSON
let datosGraficos = {};
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', cargarDatosGraficos);
} else {
    cargarDatosGraficos();
}

function cargarDatosGraficos() {
    const datosElement = document.getElementById('datos-graficos');
    if (datosElement) {
        try {
            datosGraficos = JSON.parse(datosElement.textContent);
        } catch (e) {
            console.error('Error al cargar datos de gráficos:', e);
            datosGraficos = {};
        }
    }
    
    // Inicializar gráficos cuando el DOM esté listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', inicializarGraficos);
    } else {
        inicializarGraficos();
    }
}

function inicializarGraficos() {
    if (typeof Chart !== 'undefined') {
        inicializarGraficoActividadMensual();
        inicializarGraficoActividadDiaria();
        inicializarGraficoAccionesPorModulo();
    }
}

/**
 * Inicializa el gráfico de actividad mensual (últimos 6 meses)
 */
function inicializarGraficoActividadMensual() {
    const canvas = document.getElementById('graficoActividadMensual');
    if (!canvas) return;

    const datos = datosGraficos.actividadMensual || {};
    
    // Ordenar fechas y preparar datos
    const fechas = Object.keys(datos).sort();
    const valores = fechas.map(fecha => datos[fecha] || 0);
    
    // Formatear fechas para mostrar (MM/YYYY)
    const fechasFormateadas = fechas.map(fecha => {
        const [anio, mes] = fecha.split('-');
        return `${mes}/${anio}`;
    });

    new Chart(canvas, {
        type: 'line',
        data: {
            labels: fechasFormateadas,
            datasets: [{
                label: 'Actividades',
                data: valores,
                borderColor: 'rgb(75, 192, 192)',
                backgroundColor: 'rgba(75, 192, 192, 0.2)',
                tension: 0.1,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: true,
                    position: 'top'
                },
                tooltip: {
                    mode: 'index',
                    intersect: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        precision: 0
                    }
                }
            }
        }
    });
}

/**
 * Inicializa el gráfico de actividad diaria (últimos 7 días)
 */
function inicializarGraficoActividadDiaria() {
    const canvas = document.getElementById('graficoActividadDiaria');
    if (!canvas) return;

    const datos = datosGraficos.actividadDiaria || {};
    
    // Ordenar fechas y preparar datos
    const fechas = Object.keys(datos).sort();
    const valores = fechas.map(fecha => datos[fecha] || 0);
    
    // Formatear fechas para mostrar (DD/MM)
    const fechasFormateadas = fechas.map(fecha => {
        const [anio, mes, dia] = fecha.split('-');
        return `${dia}/${mes}`;
    });

    new Chart(canvas, {
        type: 'line',
        data: {
            labels: fechasFormateadas,
            datasets: [{
                label: 'Actividades',
                data: valores,
                borderColor: 'rgb(54, 162, 235)',
                backgroundColor: 'rgba(54, 162, 235, 0.2)',
                tension: 0.1,
                fill: true,
                pointRadius: 4,
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: true,
                    position: 'top'
                },
                tooltip: {
                    mode: 'index',
                    intersect: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        precision: 0
                    }
                }
            }
        }
    });
}

/**
 * Inicializa el gráfico de acciones por módulo (hoy)
 */
function inicializarGraficoAccionesPorModulo() {
    const canvas = document.getElementById('graficoAccionesPorModulo');
    if (!canvas) return;

    const datos = datosGraficos.accionesPorModulo || {};
    
    const modulos = Object.keys(datos);
    const valores = modulos.map(modulo => datos[modulo] || 0);

    // Colores para cada módulo
    const colores = [
        'rgba(255, 99, 132, 0.8)',
        'rgba(54, 162, 235, 0.8)',
        'rgba(255, 206, 86, 0.8)',
        'rgba(75, 192, 192, 0.8)',
        'rgba(153, 102, 255, 0.8)',
        'rgba(255, 159, 64, 0.8)'
    ];

    new Chart(canvas, {
        type: 'bar',
        data: {
            labels: modulos,
            datasets: [{
                label: 'Acciones',
                data: valores,
                backgroundColor: modulos.map((_, i) => colores[i % colores.length]),
                borderColor: modulos.map((_, i) => colores[i % colores.length].replace('0.8', '1')),
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    mode: 'index',
                    intersect: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        precision: 0
                    }
                }
            }
        }
    });
}


