namespace Restaurante.Models
{
    public class Pedido
    {
        public int Id { get; set; }

        public int MesaId { get; set; }

        public DateTime DataPedido { get; set; }

        public string Status { get; set; } = "";

        public decimal Total { get; set; }
    }
}