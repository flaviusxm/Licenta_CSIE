using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("TipuriRefacturari")]
    public class TipuriRefacturari
    {
        public int Id { get; set; }
        public string Denumire { get; set; }
        public bool EsteActiv { get; set; }
        [JsonIgnore]
        public ICollection<Refacturari> Refacturari { get; } = new List<Refacturari>();
    }
}
