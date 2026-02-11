using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AskNLearn.Domain.Entities
{
    public class TipFacturaPerCamin
    {
        public string numeCamin { get; set; }
        public string tipFactura { get; set; }

        public TipFacturaPerCamin(string numeCamin, string tipFactura)
        {
            this.numeCamin = numeCamin;
            this.tipFactura = tipFactura;
        }
    }
}
