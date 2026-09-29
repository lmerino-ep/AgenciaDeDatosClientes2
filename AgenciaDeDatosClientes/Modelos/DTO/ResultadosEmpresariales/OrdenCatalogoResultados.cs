namespace Modelos.DTO.ResultadosEmpresariales
{
    // Enum y no texto libre: el ORDER BY no se puede parametrizar y esto acota lo que llega por query string.
    public enum OrdenCatalogoResultados
    {
        EmpresaAscendente = 0,
        EmpresaDescendente = 1,
        SectorAscendente = 2,
        MasRecientes = 3
    }
}
