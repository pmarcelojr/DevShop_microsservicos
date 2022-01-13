using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Compartilhado;
using Compartilhado.Model.Enums;
using Model;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Reservador
{
    public class Function
    {
        private AmazonDynamoDBClient client { get;  }
        public Function()
        {
            client = new AmazonDynamoDBClient(RegionEndpoint.USEast1);
        }

        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            if (evnt.Records.Count > 1) throw new InvalidOperationException("Somente uma mensagem pode ser tratada por vez");
            var message = evnt.Records.FirstOrDefault();
            if (message == null) return;

            await ProcessMessageAsync(message, context);
        }

        private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            var pedido = JsonConvert.DeserializeObject<Pedido>(message.Body);
            pedido.Status = StatusDoPedido.Reservado;
                
            foreach (var produto in pedido.Produtos)
            {
                try
                {
                    await BaixarEstoque(produto.Id, produto.Quantidade);
                    produto.Reservado = true;
                    context.Logger.LogLine($"Produto baixado do estoque: {produto.Id} - {produto.Nome}");
                }
                catch (ConditionalCheckFailedException)
                {
                    pedido.JustificativaDeCancelamento = $"Produto indisponível no estoque: {produto.Id} - {produto.Nome}";
                    pedido.Cancelado = true;
                    context.Logger.LogLine($"Erro: {pedido.JustificativaDeCancelamento}");
                    break;
                }
            }

            if (pedido.Cancelado)
            {
                foreach (var produto in pedido.Produtos)
                {
                    // devolver para estoque
                    if (produto.Reservado)
                    {
                        await DevolverAoEstoque(produto.Id, produto.Quantidade);
                        produto.Reservado = false;
                        context.Logger.LogLine($"Produto devolvido ao estoque: {produto.Id} - {produto.Nome}");
                    }
                }

                // adicionar na filha de falha
                await AmazonUtil.EnviarParaFila(EnumFilaSNS.falha, pedido);
                await pedido.SalvarAsync();
            }
            else
            {
                await AmazonUtil.EnviarParaFila(EnumFilaSQS.reservado, pedido);
                await pedido.SalvarAsync();
            }

        }

        private async Task BaixarEstoque(string id, int quantidade)
        {
            var request = new UpdateItemRequest
            {
                TableName = "estoque",
                ReturnValues = "NONE",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { S = id } }
                },
                UpdateExpression = "SET Quantidade = (Quantidade - :quantidadeDoPedido)",
                ConditionExpression = "Quantidade >= :quantidadeDoPedido",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":quantidadeDoPedido", new AttributeValue { N = quantidade.ToString() } }
                }
            };

            await client.UpdateItemAsync(request);
        }

        private async Task DevolverAoEstoque(string id, int quantidade)
        {
            var request = new UpdateItemRequest
            {
                TableName = "estoque",
                ReturnValues = "NONE",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { S = id } }
                },
                UpdateExpression = "SET Quantidade = (Quantidade + :quantidadeDoPedido)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":quantidadeDoPedido", new AttributeValue { N = quantidade.ToString() } }
                }
            };

            await client.UpdateItemAsync(request);
        }
    }
}
