using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("Facturi")]
    public class Facturi
    {
        [Key]
        public int Id { get; set; }
        public int IdTipFactura { get; set; }
        [JsonIgnore]
        public TipuriFactura? TipuriFactura { get; set; } = null!;
        public int IdFurnizor { get; set; }
        [JsonIgnore]
        public Furnizori? Furnizori { get; set; } = null!;
        public string NrFactura { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Valoare { get; set; }
        public DateOnly? DataScadenta { get; set; }
        public DateOnly? DataEmitere { get; set; }
        public string Perioada { get; set; }
        public string PerioadaFacturata { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValoareAchitata { get; set; }
        public int IdCamin { get; set; }
        [JsonIgnore]
        public Camine? Camine { get; set; } = null!;
    }
}
