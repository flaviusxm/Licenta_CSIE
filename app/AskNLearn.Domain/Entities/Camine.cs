using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    [Table("Camine")]
    public class Camine
    {
        [Key]
        public int Id { get; set; }
        public string Denumire { get; set; }
        public string? Adresa { get; set; }
        public int? RangConfort { get; set; }
        public int? ComplexId { get; set; }
        public string? DenominareSex { get; set; }
        public int IdManageCommodation { get; set; }
        public bool EsteCaminContorizat { get; set; }
        public bool EstePentruRegieCamine { get; set; } = true;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CoeficientConfort { get; set; }
        public bool AreRefacturari { get; set; }

        [JsonIgnore]
        public ICollection<Facturi> Facturi { get; } = new List<Facturi>();
        [JsonIgnore]
        public ICollection<Preturi> Preturi { get; } = new List<Preturi>();
        [JsonIgnore]
        public ICollection<Organigrama> Organigrama { get; } = new List<Organigrama>();
        [JsonIgnore]
        public ICollection<RegiiCalculate> RegiiCalculate { get; } = new List<RegiiCalculate>();
        [JsonIgnore]
        public ICollection<NumarStudenti> NumarStudenti { get; } = new List<NumarStudenti>();
        [JsonIgnore]
        public ICollection<NumarTotalStudenti> NumarTotalStudenti { get; } = new List<NumarTotalStudenti>();
        [JsonIgnore]
        public ICollection<Salarii> Salarii { get; } = new List<Salarii>();
        [JsonIgnore]
        public ICollection<Refacturari> Refacturari { get; } = new List<Refacturari>();
        [JsonIgnore]
        public ICollection<Amortizari> Amortizari { get; } = new List<Amortizari>();
    }
}
