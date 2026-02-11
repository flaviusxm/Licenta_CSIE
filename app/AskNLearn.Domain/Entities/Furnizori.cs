using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("Furnizori")]
    public class Furnizori
    {
        [Key]
        public int Id { get; set; }
        public string Denumire { get; set; }
        public int IdTipFactura { get; set; }
        public TipuriFactura? TipuriFactura { get; set; } = null!;
        [JsonIgnore]
        public ICollection<Facturi>? Facturi { get; } = new List<Facturi>();
    }
}
