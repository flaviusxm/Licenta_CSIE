using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("TipuriConsum")]
    public class TipuriConsum
    {
        [Key]
        public int Id { get; set; }
        public string Denumire { get; set; }
        public string UM { get; set; }
        [JsonIgnore]
        public ICollection<Consumuri>? Consumuri { get; } = new List<Consumuri>();
    }
}
