using Modelos.InyeccionDeDependencias;
using MySql.Data.MySqlClient;
using System.Data;

namespace Datos
{
    /// <summary>
    /// Clase base para todos los repositorios con conexiones Aurora
    /// </summary>
    public abstract class BaseRepositorio
    {
        protected readonly Aurora _aurora;

        protected BaseRepositorio(Aurora aurora)
        {
            _aurora = aurora ?? throw new ArgumentNullException(nameof(aurora));
        }

        /// <summary>
        /// Obtiene una conexión de lectura (SELECT)
        /// Usar para consultas que no modifican datos
        /// </summary>
        protected MySqlConnection ObtenerConexionLectura()
        {
            return new MySqlConnection(_aurora.Lectura);
        }

        /// <summary>
        /// Obtiene una conexión de escritura (INSERT, UPDATE, DELETE)
        /// Usar para operaciones que modifican datos
        /// </summary>
        protected MySqlConnection ObtenerConexionEscritura()
        {
            return new MySqlConnection(_aurora.Escritura);
        }

        /// <summary>
        /// Ejecuta una operación de escritura con transacción
        /// </summary>
        protected async Task<T> EjecutarConTransaccionAsync<T>(Func<MySqlConnection, MySqlTransaction, Task<T>> operacion)
        {
            using (MySqlConnection conexion = ObtenerConexionEscritura())
            {
                if (conexion.State == ConnectionState.Closed)
                {
                    await conexion.OpenAsync();
                }

                using (MySqlTransaction transaccion = await conexion.BeginTransactionAsync())
                {
                    try
                    {
                        T resultado = await operacion(conexion, transaccion);
                        await transaccion.CommitAsync();
                        return resultado;
                    }
                    catch
                    {
                        if (transaccion.Connection?.State == ConnectionState.Open)
                        {
                            await transaccion.RollbackAsync();
                        }
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Ejecuta una operación de escritura sin retorno con transacción
        /// </summary>
        protected async Task EjecutarConTransaccionAsync(Func<MySqlConnection, MySqlTransaction, Task> operacion)
        {
            using (MySqlConnection conexion = ObtenerConexionEscritura())
            {
                if (conexion.State == ConnectionState.Closed)
                {
                    await conexion.OpenAsync();
                }

                using (MySqlTransaction transaccion = await conexion.BeginTransactionAsync())
                {
                    try
                    {
                        await operacion(conexion, transaccion);
                        await transaccion.CommitAsync();
                    }
                    catch
                    {
                        if (transaccion.Connection?.State == ConnectionState.Open)
                        {
                            await transaccion.RollbackAsync();
                        }
                        throw;
                    }
                }
            }
        }
    }
}
