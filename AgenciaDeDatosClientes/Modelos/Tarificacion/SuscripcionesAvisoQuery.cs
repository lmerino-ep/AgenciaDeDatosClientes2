namespace Modelos.Tarificacion
{
    /// <summary>
    /// Valores del query en <c>/clientes/suscripciones</c> tras redirigir por cuota de descargas agotada (EpData).
    /// Debe coincidir con <c>EpData/Modelos/Tarificacion/SuscripcionesAvisoQuery.cs</c>.
    /// </summary>
    public static class SuscripcionesAvisoQuery
    {
        public const string Parametro = "aviso";

        public const string ValorLimiteDescargas = "limite_descargas";
    }
}
