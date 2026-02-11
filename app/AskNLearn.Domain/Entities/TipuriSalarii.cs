
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("TipuriSalarii")]
    public class TipuriSalarii
    {
        [Key]
        public int Id { get; set; }
        public string Denumire { get; set; }
        [JsonIgnore]
        public ICollection<Salarii> Salarii { get; } = new List<Salarii>();
    }
}
