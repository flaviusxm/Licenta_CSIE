using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    public class NumarStudenti
    {
            public int Id { get; set; }
            public int IdCamin { get; set; }
            [JsonIgnore]
            public Camine? Camin { get; set; } = null!;
            public int TotalStudentiCazati { get; set; }
            public int TotalStudentiBugetati { get; set; }
            public int TotalStudentiSubventieDubla { get; set; }
            public int TotalStudentiTaxa { get; set; }
            public int TotalStudentiPersonalDidacticOccidentului { get; set; }
            public int TotalBursieriOccidentului { get; set; }
            public int TotalDoctoranziBugetOccidentului { get; set; }
            public DateTime DataInregistrare { get; set; }
    }
}
