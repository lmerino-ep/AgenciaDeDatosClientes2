using Datos;
using Microsoft.Extensions.Logging;
using NugetPortable.Helper;
using Modelos.DTO.Usuarios;
using AgenciaDeDatosBD;
using AgenciaDeDatosBD.Herramientas;
using Modelos.InyeccionDeDependencias;

namespace Negocio
{
    /// <summary>
    /// Servicio para gestión de usuarios
    /// </summary>
    public class ServicioUsuarios : IServicioUsuarios
    {
        private readonly RepositorioUsuarios _repositorio;
        private readonly ILogger<ServicioUsuarios> _logger;
        private readonly Negocio.Autenticacion _negocioAutenticacion;
        private readonly Aurora _aurora;

        public ServicioUsuarios(
            RepositorioUsuarios repositorio,
            ILogger<ServicioUsuarios> logger,
            Aurora aurora)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aurora = aurora ?? throw new ArgumentNullException(nameof(aurora));
            _negocioAutenticacion = new Negocio.Autenticacion(aurora);
        }

        /// <summary>
        /// Lista usuarios con filtros y paginación
        /// </summary>
        public async Task<(List<UsuarioListado> usuarios, int total)> ListarUsuariosAsync(
            int? fkUsuarioTipo = null,
            bool? activo = null,
            string busqueda = null,
            int pagina = 0,
            int tamanoPagina = 25)
        {
            try
            {
                _logger.LogInformation("Listando usuarios - Tipo: {Tipo}, Activo: {Activo}, Búsqueda: {Busqueda}, Página: {Pagina}",
                    fkUsuarioTipo, activo, busqueda, pagina);

                return await _repositorio.ListarUsuariosAsync(fkUsuarioTipo, activo, busqueda, pagina, tamanoPagina);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar usuarios");
                throw;
            }
        }

        /// <summary>
        /// Obtiene un usuario por ID
        /// </summary>
        public async Task<UsuarioDetalle> ObtenerUsuarioAsync(int idUsuario)
        {
            try
            {
                _logger.LogInformation("Obteniendo usuario {UsuarioId}", idUsuario);
                UsuarioDetalle usuario = await _repositorio.ObtenerUsuarioAsync(idUsuario);

                if (usuario == null)
                {
                    _logger.LogWarning("Usuario {UsuarioId} no encontrado", idUsuario);
                    throw new InvalidOperationException($"Usuario {idUsuario} no encontrado");
                }

                return usuario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        public async Task<int> CrearUsuarioAsync(CrearUsuario dto)
        {
            try
            {
                _logger.LogInformation("Creando usuario");

                // Verificar si el email ya existe
                if (await _repositorio.ExisteEmailAsync(dto.Email))
                {
                    throw new InvalidOperationException($"El email {dto.Email} ya está en uso");
                }

                // La contraseña debe venir ya hasheada desde la capa de presentación
                // Crear usuario usando el negocio de autenticación existente
                var usuario = new Modelos.Autenticacion.Usuario
                {
                    Email = dto.Email,
                    Nombre = dto.Nombre,
                    Apellidos = dto.Apellidos,
                    Empresa = dto.Empresa,
                    Password = dto.Password, // Ya viene hasheada desde la capa de presentación
                    FK_UsuarioTipo = dto.FK_UsuarioTipo,
                    Activo = dto.Activo
                };

                int idUsuario = await _negocioAutenticacion.CrearUsuarioAsync(usuario);

                _logger.LogInformation("Usuario creado exitosamente con ID {UsuarioId}", idUsuario);
                return idUsuario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un usuario existente
        /// </summary>
        public async Task ActualizarUsuarioAsync(EditarUsuario dto)
        {
            try
            {
                _logger.LogInformation("Actualizando usuario {UsuarioId}", dto.Id);

                // Obtener usuario actual
                var usuarioActual = await _negocioAutenticacion.GetUsuarioAsync(dto.Id, false);
                if (usuarioActual == null)
                {
                    throw new InvalidOperationException($"Usuario {dto.Id} no encontrado");
                }

                // Actualizar campos permitidos
                usuarioActual.Nombre = dto.Nombre;
                usuarioActual.Apellidos = dto.Apellidos;
                usuarioActual.Empresa = dto.Empresa;
                usuarioActual.FK_UsuarioTipo = dto.FK_UsuarioTipo;
                usuarioActual.Activo = dto.Activo;

                await _negocioAutenticacion.ModificarUsuarioAsync(usuarioActual);

                _logger.LogInformation("Usuario {UsuarioId} actualizado exitosamente", dto.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario {UsuarioId}", dto.Id);
                throw;
            }
        }

        /// <summary>
        /// Cambia la contraseña de un usuario
        /// </summary>
        public async Task CambiarContrasenaAsync(CambiarContrasena dto)
        {
            try
            {
                _logger.LogInformation("Cambiando contraseña del usuario {UsuarioId}", dto.IdUsuario);

                // Verificar que el usuario existe
                var usuario = await _negocioAutenticacion.GetUsuarioAsync(dto.IdUsuario, false);
                if (usuario == null)
                {
                    throw new InvalidOperationException($"Usuario {dto.IdUsuario} no encontrado");
                }

                // La contraseña debe venir ya hasheada desde la capa de presentación
                // Actualizar contraseña
                await _repositorio.ActualizarContrasenaAsync(dto.IdUsuario, dto.NuevaPassword);

                // Si se solicita, enviar por email
                if (dto.EnviarPorEmail)
                {
                    try
                    {
                        HelperEmail helperEmail = new HelperEmail();
                        string resultado = await helperEmail.Enviar(
                            asunto: "Nueva contraseña - EpData Dashboard",
                            cuerpoHtml: $"<p>Su nueva contraseña es: <strong>{dto.NuevaPassword}</strong></p><p>Por favor, cámbiela después de iniciar sesión.</p>",
                            cuerpoTexto: $"Su nueva contraseña es: {dto.NuevaPassword}\n\nPor favor, cámbiela después de iniciar sesión.",
                            adjuntos: null,
                            nombreOrigen: "EpData Dashboard",
                            emailOrigen: "noreply@epdata.es",
                            destinatarios: new List<string> { usuario.Email },
                            destinatariosCC: null,
                            destinatariosBCC: null
                        );

                        _logger.LogInformation("Email de nueva contraseña enviado al usuario {UsuarioId}", dto.IdUsuario);
                    }
                    catch (Exception exEmail)
                    {
                        _logger.LogWarning(exEmail, "No se pudo enviar el email de nueva contraseña al usuario {UsuarioId}", dto.IdUsuario);
                        // No lanzamos excepción, solo registramos el warning
                    }
                }

                _logger.LogInformation("Contraseña del usuario {UsuarioId} cambiada exitosamente", dto.IdUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña del usuario {UsuarioId}", dto.IdUsuario);
                throw;
            }
        }

        /// <summary>
        /// Cambia el tipo de usuario
        /// </summary>
        public async Task CambiarTipoUsuarioAsync(CambiarTipoUsuario dto)
        {
            try
            {
                _logger.LogInformation("Cambiando tipo de usuario {UsuarioId} a tipo {TipoUsuario}", dto.IdUsuario, dto.FK_UsuarioTipo);

                // Verificar que el usuario existe
                var usuario = await _negocioAutenticacion.GetUsuarioAsync(dto.IdUsuario, false);
                if (usuario == null)
                {
                    throw new InvalidOperationException($"Usuario {dto.IdUsuario} no encontrado");
                }

                await _repositorio.ActualizarTipoUsuarioAsync(dto.IdUsuario, dto.FK_UsuarioTipo);

                _logger.LogInformation("Tipo de usuario {UsuarioId} cambiado exitosamente", dto.IdUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar tipo de usuario {UsuarioId}", dto.IdUsuario);
                throw;
            }
        }

        /// <summary>
        /// Elimina un usuario (soft delete)
        /// </summary>
        public async Task EliminarUsuarioAsync(int idUsuario)
        {
            try
            {
                _logger.LogInformation("Desactivando usuario {UsuarioId}", idUsuario);

                // Verificar que el usuario existe
                var usuario = await _negocioAutenticacion.GetUsuarioAsync(idUsuario, false);
                if (usuario == null)
                {
                    throw new InvalidOperationException($"Usuario {idUsuario} no encontrado");
                }

                await _repositorio.EliminarUsuarioAsync(idUsuario);

                _logger.LogInformation("Usuario {UsuarioId} desactivado exitosamente", idUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Elimina un usuario definitivamente (hard delete) usando AgenciaDeDatosBD
        /// </summary>
        public async Task EliminarUsuarioDefinitivamenteAsync(int idUsuario, int idUsuarioHereditario)
        {
            try
            {
                _logger.LogInformation("Eliminando usuario {UsuarioId} definitivamente. Usuario hereditario: {UsuarioHereditario}", idUsuario, idUsuarioHereditario);

                // Verificar que ambos usuarios existen
                var usuario = await _negocioAutenticacion.GetUsuarioAsync(idUsuario, false);
                if (usuario == null)
                {
                    throw new InvalidOperationException($"Usuario {idUsuario} no encontrado");
                }

                var usuarioHereditario = await _negocioAutenticacion.GetUsuarioAsync(idUsuarioHereditario, false);
                if (usuarioHereditario == null)
                {
                    throw new InvalidOperationException($"Usuario hereditario {idUsuarioHereditario} no encontrado");
                }

                // Verificar que el usuario hereditario tiene tipo 4 o 5
                if (usuarioHereditario.FK_UsuarioTipo != 4 && usuarioHereditario.FK_UsuarioTipo != 5)
                {
                    throw new InvalidOperationException($"El usuario hereditario debe tener tipo 4 o 5. Tipo actual: {usuarioHereditario.FK_UsuarioTipo}");
                }

                // Llamar a la función de AgenciaDeDatosBD para eliminar el usuario
                using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
                {
                    Retorno retorno = await bd.Usuario().Eliminar(idUsuario, idUsuarioHereditario);
                    
                    if (!retorno.Correcto)
                    {
                        throw new InvalidOperationException($"Error al eliminar usuario: {retorno.Mensaje}");
                    }
                }

                _logger.LogInformation("Usuario {UsuarioId} eliminado definitivamente exitosamente", idUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {UsuarioId} definitivamente", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Obtiene usuarios con tipo 4 o 5 para herencia de datos al eliminar
        /// </summary>
        public async Task<List<UsuarioListado>> ObtenerUsuariosParaHerenciaAsync(int? excluirUsuarioId = null)
        {
            try
            {
                return await _repositorio.ObtenerUsuariosParaHerenciaAsync(excluirUsuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios para herencia");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todos los tipos de usuario
        /// </summary>
        public async Task<List<Modelos.DTO.Usuarios.UsuarioTipo>> ObtenerTiposUsuarioAsync()
        {
            try
            {
                var entidades = await _repositorio.ObtenerTiposUsuarioAsync();
                // Mapear de entidad a DTO
                return entidades.Select(e => new Modelos.DTO.Usuarios.UsuarioTipo
                {
                    Id = e.Id,
                    Nombre = e.Nombre
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de usuario");
                throw;
            }
        }

        /// <summary>
        /// Verifica si un email ya existe
        /// </summary>
        public async Task<bool> ExisteEmailAsync(string email, int? idUsuarioExcluir = null)
        {
            try
            {
                return await _repositorio.ExisteEmailAsync(email, idUsuarioExcluir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de email");
                throw;
            }
        }
    }
}
