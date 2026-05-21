using Modelos;
using Modelos.InyeccionDeDependencias;
using AgenciaDeDatosBD;
using AgenciaDeDatosBD.Herramientas;
using Modelos.DTO;
using System.Linq;

namespace Datos
{
    /// <summary>
    /// Repositorio para operaciones de auditoría
    /// </summary>
    public class RepositorioAuditoria : BaseRepositorio
    {
        public RepositorioAuditoria(Aurora aurora) : base(aurora)
        {
        }

        /// <summary>
        /// Registra una acción de auditoría
        /// </summary>
        public async Task<long> RegistrarAccionAsync(AuditoriaDTO auditoria)
        {
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                Retorno retorno = await bd.Auditoria().RegistrarAccion(
                    auditoria.FK_Usuario,
                    auditoria.FK_AuditoriaAccion,
                    auditoria.Descripcion
                );

                if (!retorno.Correcto)
                {
                    throw new Exception(retorno.Mensaje);
                }

                var resultado = retorno.Resultado<AgenciaDeDatosBD.Auditoria>();
                return resultado.FirstOrDefault()?.Id ?? 0;
            }
        }

        /// <summary>
        /// Obtiene registros de auditoría con filtros
        /// </summary>
        public async Task<List<AuditoriaConUsuario>> ObtenerAuditoriasAsync(
            int? usuarioId = null,
            string accion = null,
            string modulo = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            int limit = 100,
            int offset = 0)
        {
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                Retorno retorno = await bd.Auditoria().ObtenerAuditorias(
                    usuarioId,
                    accion,
                    modulo,
                    fechaInicio,
                    fechaFin,
                    limit,
                    offset
                );

                if (!retorno.Correcto)
                {
                    throw new Exception(retorno.Mensaje);
                }

                var resultadoNuGet = retorno.Resultado<AgenciaDeDatosBD.Auditoria.AuditoriaConUsuario>();
                
                // Convertir de DTOs del NuGet a DTOs del Cliente
                return resultadoNuGet.Select(a => new AuditoriaConUsuario
                {
                    Id = a.Id,
                    LastUpdate = a.LastUpdate,
                    UsuarioEmail = a.UsuarioEmail,
                    UsuarioNombre = a.UsuarioNombre,
                    AccionNombre = a.AccionNombre,
                    CategoriaNombre = a.CategoriaNombre,
                    Descripcion = a.Descripcion
                }).ToList();
            }
        }

        /// <summary>
        /// Obtiene el detalle completo de una acción de auditoría
        /// </summary>
        public async Task<AuditoriaDetalle> ObtenerAuditoriaPorIdAsync(long id)
        {
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                Retorno retorno = await bd.Auditoria().ObtenerAuditoriaPorId(id);

                if (!retorno.Correcto)
                {
                    throw new Exception(retorno.Mensaje);
                }

                var resultadoNuGet = retorno.Resultado<AgenciaDeDatosBD.Auditoria.AuditoriaDetalle>().FirstOrDefault();
                
                if (resultadoNuGet == null)
                {
                    return null;
                }

                // Convertir de DTO del NuGet a DTO del Cliente
                return new AuditoriaDetalle
                {
                    Id = resultadoNuGet.Id,
                    FK_Usuario = resultadoNuGet.FK_Usuario,
                    AccionNombre = resultadoNuGet.AccionNombre,
                    CategoriaNombre = resultadoNuGet.CategoriaNombre,
                    Descripcion = resultadoNuGet.Descripcion,
                    LastUpdate = resultadoNuGet.LastUpdate
                };
            }
        }

        /// <summary>
        /// Obtiene el ID de una acción de auditoría por su nombre
        /// </summary>
        public async Task<int?> ObtenerIdAccionPorNombreAsync(string nombreAccion)
        {
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                Retorno retorno = await bd.Auditoria().ObtenerIdAccionPorNombre(nombreAccion);

                if (!retorno.Correcto)
                {
                    throw new Exception(retorno.Mensaje);
                }

                var resultado = retorno.Resultado<int?>();
                return resultado.FirstOrDefault();
            }
        }

        /// <summary>
        /// Obtiene el nombre de la acción y categoría por ID de acción
        /// </summary>
        public async Task<(string AccionNombre, string CategoriaNombre)> ObtenerInfoAccionPorIdAsync(int idAccion)
        {
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                Retorno retorno = await bd.Auditoria().ObtenerInfoAccionPorId(idAccion);

                if (!retorno.Correcto)
                {
                    throw new Exception(retorno.Mensaje);
                }

                var resultado = retorno.Resultado<(string AccionNombre, string CategoriaNombre)>();
                return resultado.FirstOrDefault();
            }
        }

        /// <summary>
        /// Obtiene el total de registros de auditoría con filtros
        /// </summary>
        public async Task<int> ObtenerTotalAuditoriasAsync(
            int? usuarioId = null,
            string accion = null,
            string modulo = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null)
        {
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                Retorno retorno = await bd.Auditoria().ObtenerTotalAuditorias(
                    usuarioId,
                    accion,
                    modulo,
                    fechaInicio,
                    fechaFin
                );

                if (!retorno.Correcto)
                {
                    throw new Exception(retorno.Mensaje);
                }

                var resultado = retorno.Resultado<int>();
                return resultado.FirstOrDefault();
            }
        }

        /// <summary>
        /// Obtiene acciones agrupadas por módulo de hoy
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerAccionesPorModuloHoyAsync()
        {
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                Retorno retorno = await bd.Auditoria().ObtenerAccionesPorModuloHoy();

                if (!retorno.Correcto)
                {
                    throw new Exception(retorno.Mensaje);
                }

                var resultado = retorno.Resultado<Dictionary<string, int>>();
                return resultado.FirstOrDefault() ?? new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Obtiene tendencia de acciones de los últimos 7 días
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerTendenciaAccionesUltimos7DiasAsync()
        {
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                Retorno retorno = await bd.Auditoria().ObtenerTendenciaAccionesUltimos7Dias();

                if (!retorno.Correcto)
                {
                    throw new Exception(retorno.Mensaje);
                }

                var resultado = retorno.Resultado<Dictionary<string, int>>();
                return resultado.FirstOrDefault() ?? new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Obtiene todas las acciones de auditoría disponibles
        /// </summary>
        public async Task<List<string>> ObtenerAccionesDisponiblesAsync()
        {
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                Retorno retorno = await bd.Auditoria().ObtenerAccionesDisponibles();

                if (!retorno.Correcto)
                {
                    throw new Exception(retorno.Mensaje);
                }

                return retorno.Resultado<string>();
            }
        }

        /// <summary>
        /// Obtiene todas las categorías de auditoría disponibles
        /// </summary>
        public async Task<List<string>> ObtenerCategoriasDisponiblesAsync()
        {
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                Retorno retorno = await bd.Auditoria().ObtenerCategoriasDisponibles();

                if (!retorno.Correcto)
                {
                    throw new Exception(retorno.Mensaje);
                }

                return retorno.Resultado<string>();
            }
        }
    }
}
