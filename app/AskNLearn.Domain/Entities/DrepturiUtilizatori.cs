using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities
{
    [Table("DrepturiUtilizatori")]
    public class DrepturiUtilizatori
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string TipUtilizator { get; set; }
    }
}
