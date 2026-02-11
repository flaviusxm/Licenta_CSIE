using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("Organigrama")]
    public class Organigrama
    {
        [Key]
        public int Id { get; set; }
        public string NrCamera { get; set; } = null!;
        public int NrPersoane { get; set; }
        public bool EsteDezafectata { get; set; }
        public int Etaj { get; set; }
        public DateTime DataInregistrare { get; set; }
        public int IdCamin { get; set; }
        [JsonIgnore]
        public Camine? Camine { get; set; } = null!;
        [JsonIgnore]
        public ICollection<Consumuri>? Consumuri { get; } = new List<Consumuri>();
    }
}
