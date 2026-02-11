using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("Consumuri")]
    public class Consumuri
    {
        [Key]
        public int Id { get; set; }
        public int IdTipConsum { get; set; }
        [JsonIgnore]
        public TipuriConsum? TipuriConsum { get; set; } = null!;
        public int IdOrganigrama { get; set; }
        [JsonIgnore]
        public Organigrama? Organigrama { get; set; } = null!;
        [Column(TypeName = "decimal(18,2)")]
        public decimal Valoare { get; set; }
        public DateOnly DataInregistrare { get; set; }


    }
}
    