using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("TipuriFactura")]
    public class TipuriFactura
    {
        [Key]
        public int Id { get; set; }
        public string Denumire { get; set; }
        public string UM { get; set; }
        [JsonIgnore]
        public ICollection<Facturi>? Facturi { get; } = new List<Facturi>();
        [JsonIgnore]
        public ICollection<Furnizori>? Furnizori { get; } = new List<Furnizori>();
    }
}
