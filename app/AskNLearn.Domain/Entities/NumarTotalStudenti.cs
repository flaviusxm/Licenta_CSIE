using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    public class NumarTotalStudenti
    {
        public int Id { get; set; }
        public int IdCamin { get; set; }
        [JsonIgnore]
        public Camine? Camin { get; set; } = null!;
        public int StudentiTaxa { get; set; }
        public int StudentiBuget { get; set; }
        public int StudentiTaxaCopilCadruDidactic { get; set; }
        public int StudentiTaxaFamilieMonoparentala { get; set; }
        public int StudentiTaxaPlasament { get; set; }
        public int StudentiTaxaStrainiBursieri { get; set; }
        public int StudentiTaxaCPNV { get; set; }
        public int StudentiTaxaCazSocial { get; set; }
        public int StudentiBugetCopilCadruDidactic { get; set; }
        public int StudentiBugetFamilieMonoparentala { get; set; }
        public int StudentiBugetPlasament { get; set; }
        public int StudentiBugetStrainiBursieri { get; set; }
        public int StudentiBugetCPNV { get; set; }
        public int StudentiBugetCazSocial { get; set; }
        public DateTime DataInregistrare { get; set; }

        [NotMapped]
        public int TotalStudentiBugetSubventie15 { get => StudentiBugetCopilCadruDidactic + StudentiBugetFamilieMonoparentala + StudentiBugetPlasament + StudentiBugetStrainiBursieri + StudentiBugetCPNV + StudentiBugetCazSocial; }

        [NotMapped]
        public int TotalStudentiTaxaSubventie15 { get => StudentiTaxaCopilCadruDidactic + StudentiTaxaFamilieMonoparentala + StudentiTaxaPlasament + StudentiTaxaStrainiBursieri + StudentiTaxaCPNV + StudentiTaxaCazSocial; }

        [NotMapped]
        public int TotalStudentiSubventie15
        {
            get => StudentiBugetCopilCadruDidactic + StudentiTaxaCopilCadruDidactic + StudentiBugetFamilieMonoparentala + StudentiTaxaFamilieMonoparentala
                + StudentiBugetPlasament + StudentiTaxaPlasament + StudentiBugetCazSocial + StudentiTaxaCazSocial;
        }

        [NotMapped]
        public int TotalStudentiCamin
        {
            get => StudentiTaxa + StudentiBuget + StudentiTaxaCopilCadruDidactic + StudentiBugetCopilCadruDidactic
                + StudentiTaxaFamilieMonoparentala + StudentiBugetFamilieMonoparentala + StudentiTaxaPlasament + StudentiBugetPlasament
                + StudentiTaxaStrainiBursieri + StudentiBugetStrainiBursieri + StudentiTaxaCPNV + StudentiBugetCPNV
                + StudentiTaxaCazSocial + StudentiBugetCazSocial;
        }
    }
}
