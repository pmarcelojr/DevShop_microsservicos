using System;
using System.Threading.Tasks;
using Compartilhado;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace Cadastrador.Controllers
{
    [Route("api/[controller]")]
    public class PedidoController : ControllerBase
    {
        [HttpPost]
        public async Task PostAsync([FromBody] Pedido pedido)
        {
            pedido.id = Guid.NewGuid().ToString();
            pedido.DataDeCriacao = DateTime.UtcNow;

            await pedido.SalvarAsync();

            Console.WriteLine($"Pedido salvo com sucesso: Id {pedido.id}");
        }
    }
}
