using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities
{
    [Table("Evenimente")]
    public class Evenimente
    {
        public int Id { get; set; }
        public string Utilizator { get; set; }
        public string Actiune { get; set; }
        public DateTime Data { get; set; }
    }
}
