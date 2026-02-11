using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("Amortizari")]
    public class Amortizari
    {
        [Key]
        public int Id { get; set; }
        public double Valoare { get; set; }
        public DateTime DataInregistrare { get; set; }
        public int IdCamin {  get; set; }
        [JsonIgnore]
        public virtual Camine? Camin {  get; set; }
    }
}
