using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class Pagamento
    {
        public string NumeroDoCartao { get; set; }
        public string Validade { get; set; }
        public string CVV { get; set; }
    }
}
