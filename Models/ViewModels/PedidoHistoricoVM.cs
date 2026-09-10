namespace Restaurante.Models
{
    public class PedidoHistoricoVM
    {
        public int Id { get; set; }
        public int MesaId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Taxa { get; set; }
        public decimal TotalFinal { get; set; }
        public DateTime Data { get; set; }
    }
}