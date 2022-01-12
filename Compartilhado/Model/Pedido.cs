using Amazon.DynamoDBv2.DataModel;
using Compartilhado.Model.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Model
{
    [DynamoDBTable("pedidos")]
    public class Pedido
    {
        public string id { get; set; }
        public decimal ValorTotal { get; set; }
        public DateTime DataDeCriacao { get; set; }
        public List<Produto> Produtos { get; set; }
        public Cliente Cliente { get; set; }
        public Pagamento Pagamento { get; set; }
        public string JustificativaDeCancelamento { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public StatusDoPedido Status { get; set; }
        public bool Cancelado { get; set; }
    }
}
