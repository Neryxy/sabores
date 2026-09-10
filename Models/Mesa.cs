namespace Restaurante.Models
{
    public class Mesa
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public string? Nome { get; set; }
        public string Status { get; set; } = "Livre";
        public string Local { get; set; } = "Dentro";
    }
}