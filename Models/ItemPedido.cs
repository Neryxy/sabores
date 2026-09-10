namespace Restaurante.Models
{
    public class ItemPedido
    {
        public int Id { get; set; }

        public string? Nome { get; set; }
        public int PedidoId { get; set; }

        public int ProdutoId { get; set; }

        public int Quantidade { get; set; }

        // Quantidade que já foi enviada para a cozinha.
        // Permite imprimir somente o que foi acrescentado depois.
        public int QuantidadeImpressa { get; set; }

        // Mantido para compatibilidade com a estrutura antiga do banco.
        public bool Impresso { get; set; }

        public decimal PrecoUnitario { get; set; }
    }
}
