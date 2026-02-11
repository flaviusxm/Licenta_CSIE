using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("TipuriSubventii")]
    public class TipuriSubventii
    {
        [Key]
        public int Id { get; set; }
        public string Denumire { get; set; }
        [JsonIgnore]
        public ICollection<Subventii>? Subventii { get; } = new List<Subventii>();
    }
}
