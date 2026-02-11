using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities
{

    public class Subventii
    {
        [Key]
        public int Id { get; set; }
        public int IdTipSubventie { get; set; }
        public TipuriSubventii? TipuriSubventii { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Valoare { get; set; }
        public DateOnly DataInregistrare { get; set; }
    }
}
