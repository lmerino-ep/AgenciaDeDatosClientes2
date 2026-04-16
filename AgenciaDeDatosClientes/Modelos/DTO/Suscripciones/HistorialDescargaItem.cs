using System;

namespace Modelos.DTO.Suscripciones
{
    public class HistorialDescargaItem
    {
        public long FkRepresentacion { get; set; }

        public Guid RepresentacionGuid { get; set; }

        public string? RepresentacionTitulo { get; set; }

        public string? RepresentacionTituloUrl { get; set; }

        public DateTime? FechaUltimaDescargaCsv { get; set; }

        public DateTime? FechaUltimaDescargaJson { get; set; }

        public DateTime? FechaUltimaDescargaImagen { get; set; }

        public DateTime FechaUltimaActividad { get; set; }
    }
}
