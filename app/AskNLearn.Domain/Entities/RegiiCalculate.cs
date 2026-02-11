using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities
{
    [Table("RegiiCalculate")]
    public class RegiiCalculate
    {
        [Key]
        public int Id { get; set; }
        public int IdCamin { get; set; }
        public Camine? Camine { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal RegieCalculataStudTaxa { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal RegieCalculataStudBuget { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal RegieCalculataStudBuget15 { get; set; }
        public DateOnly DataInregistrare { get; set; }
    }
}
