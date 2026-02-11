using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities
{
    [Table("Refacturari")]
    public class Refacturari
    {
        [Key]
        public int Id { get; set; }
        public int IdTipRefacturari { get; set; }
        public TipuriRefacturari? TipuriRefacturari { get; set; } = null!;
        public int IdCamin { get; set; }
        public Camine? Camine { get; set; } = null!;
        [Column(TypeName = "decimal(18,2)")]
        public decimal Valoare { get; set; }
        public DateOnly DataInregistrare { get; set; }

    }
}
