using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("Salarii")]
    public class Salarii
    {
        [Key]
        public int Id { get; set; }
        public int IdTipSalarii { get; set; }
        public TipuriSalarii? TipuriSalarii { get; set; } = null!;
        //public int IdCamin { get; set; }
        //public Camine? Camine { get; set; } = null!;
        //[Column(TypeName = "decimal(18,2)")]
        public double Valoare { get; set; }
        public DateTime DataInregistrare { get; set; }
    }
}
