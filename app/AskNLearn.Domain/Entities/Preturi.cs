using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("Preturi")]
    public class Preturi
    {
        [Key]
        public int Id { get; set; }
        public int IdCamin { get; set; }
        [JsonIgnore]
        public Camine? Camine { get; set; } = null!;
        [Column(TypeName = "decimal(18,2)")]
        public decimal PretElectricitate { get; set; }
        public DateOnly DataInregistrare { get; set; }
    }
}
