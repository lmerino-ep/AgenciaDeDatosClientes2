using Datos;
using Modelos.Entidades;
using Microsoft.Extensions.Logging;
using NugetPortable.Helper;
using Modelos.DTO.Usuarios;

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

        public ServicioUsuarios(
            RepositorioUsuarios repositorio,
            ILogger<ServicioUsuarios> logger,
            Modelos.InyeccionDeDependencias.Aurora aurora)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                _logger.LogInformation("Creando usuario con email {Email}", dto.Email);

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
                _logger.LogError(ex, "Error al crear usuario con email {Email}", dto.Email);
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

                        _logger.LogInformation("Email de nueva contraseña enviado a {Email}", usuario.Email);
                    }
                    catch (Exception exEmail)
                    {
                        _logger.LogWarning(exEmail, "No se pudo enviar el email de nueva contraseña a {Email}", usuario.Email);
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
                _logger.LogInformation("Eliminando usuario {UsuarioId}", idUsuario);

                // Verificar que el usuario existe
                var usuario = await _negocioAutenticacion.GetUsuarioAsync(idUsuario, false);
                if (usuario == null)
                {
                    throw new InvalidOperationException($"Usuario {idUsuario} no encontrado");
                }

                await _repositorio.EliminarUsuarioAsync(idUsuario);

                _logger.LogInformation("Usuario {UsuarioId} eliminado exitosamente", idUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Obtiene todos los tipos de usuario
        /// </summary>
        public async Task<List<UsuarioTipo>> ObtenerTiposUsuarioAsync()
        {
            try
            {
                return await _repositorio.ObtenerTiposUsuarioAsync();
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
                _logger.LogError(ex, "Error al verificar existencia de email {Email}", email);
                throw;
            }
        }
    }
}
