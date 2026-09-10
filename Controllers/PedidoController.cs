using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Restaurante.Models;
using Restaurante.Services;
using SaboresDeMiTierra.Filters;


namespace Restaurante.Controllers
{
    public class PedidoController : Controller
    {
        private readonly IConfiguration _configuration;

        public PedidoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // =====================
        // INDEX (SÓ ABERTOS)
        // =====================
        [HttpGet]
        public IActionResult Index()
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            var pedidos = new List<dynamic>();

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            string sql = @"
                SELECT p.Id, p.MesaId, m.Numero, m.Nome, p.Status
                FROM Pedidos p
                INNER JOIN Mesas m ON m.Id = p.MesaId
                WHERE p.Status = 'Aberto'
                ORDER BY p.Id DESC";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                pedidos.Add(new
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    MesaId = Convert.ToInt32(reader["MesaId"]),
                    MesaNumero = Convert.ToInt32(reader["Numero"]),
                    MesaNome = reader["Nome"]?.ToString(),
                    Status = reader["Status"].ToString()
                });
            }

            return View(pedidos);
        }

        // =====================
        // ABRIR PEDIDO (CORRIGIDO)
        // =====================
        [HttpGet]
        public IActionResult Abrir(int mesaId)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            int pedidoId = 0;

            // 🔥 pega ou cria pedido aberto
            using (var cmd = new MySqlCommand(@"
                SELECT Id FROM Pedidos 
                WHERE MesaId=@MesaId AND Status='Aberto'
                LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);
                var result = cmd.ExecuteScalar();

                if (result != null)
                    pedidoId = Convert.ToInt32(result);
            }

            if (pedidoId == 0)
            {
                using var cmd = new MySqlCommand(@"
                    INSERT INTO Pedidos (MesaId, Status)
                    VALUES (@MesaId, 'Aberto');
                    SELECT LAST_INSERT_ID();", conn);

                cmd.Parameters.AddWithValue("@MesaId", mesaId);

                pedidoId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // mesa ocupada
            using (var cmdMesa = new MySqlCommand(
                "UPDATE Mesas SET Status='Ocupada' WHERE Id=@MesaId", conn))
            {
                cmdMesa.Parameters.AddWithValue("@MesaId", mesaId);
                cmdMesa.ExecuteNonQuery();
            }

            return RedirectToAction("Detalhes", new { mesaId });
        }

        // =====================
        // DETALHES (igual teu)
        // =====================
        public IActionResult Detalhes(int mesaId)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            int pedidoId = 0;
            var produtos = new List<Produto>();
            var itens = new List<ItemPedido>();

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            using (var cmd = new MySqlCommand(
                "SELECT Id FROM Pedidos WHERE MesaId=@MesaId AND Status='Aberto' LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);
                var result = cmd.ExecuteScalar();
                if (result != null)
                    pedidoId = Convert.ToInt32(result);
            }

            using (var cmd = new MySqlCommand(
                "SELECT * FROM Produtos WHERE Ativo = 1", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    produtos.Add(new Produto
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Nome = reader["Nome"].ToString()!,
                        Preco = Convert.ToDecimal(reader["Preco"]),
                        Categoria = reader["Categoria"].ToString()!
                    });
                }
            }

            using (var cmd = new MySqlCommand(@"
                SELECT ip.Id, p.Nome, ip.Quantidade, ip.QuantidadeImpressa, ip.PrecoUnitario
                FROM ItensPedido ip
                INNER JOIN Produtos p ON p.Id = ip.ProdutoId
                INNER JOIN Pedidos ped ON ped.Id = ip.PedidoId
                WHERE ped.MesaId=@MesaId AND ped.Status='Aberto'", conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    itens.Add(new ItemPedido
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Nome = reader["Nome"].ToString()!,
                        Quantidade = Convert.ToInt32(reader["Quantidade"]),
                        QuantidadeImpressa = Convert.ToInt32(reader["QuantidadeImpressa"]),
                        PrecoUnitario = Convert.ToDecimal(reader["PrecoUnitario"])
                    });
                }
            }

            decimal subtotal = itens.Sum(i => i.Quantidade * i.PrecoUnitario);

            ViewBag.MesaId = mesaId;
            ViewBag.PedidoId = pedidoId;
            ViewBag.Itens = itens;
            ViewBag.Total = subtotal;
            ViewBag.Taxa = subtotal * 0.10m;
            ViewBag.TotalFinal = subtotal * 1.10m;

            return View(produtos);
        }

        // =====================
        // ADICIONAR ITEM (CORRIGIDO)
        // =====================
        [HttpPost]
        public IActionResult AdicionarItem(int pedidoId, int produtoId, int quantidade, int mesaId)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            decimal preco = 0;

            using (var cmd = new MySqlCommand(
                "SELECT Preco FROM Produtos WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", produtoId);
                var result = cmd.ExecuteScalar();
                if (result != null)
                    preco = Convert.ToDecimal(result);
            }

            int itemId = 0;
            int qtdAtual = 0;

            using (var cmd = new MySqlCommand(@"
                SELECT Id, Quantidade
                FROM ItensPedido
                WHERE PedidoId=@PedidoId AND ProdutoId=@ProdutoId", conn))
            {
                cmd.Parameters.AddWithValue("@PedidoId", pedidoId);
                cmd.Parameters.AddWithValue("@ProdutoId", produtoId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    itemId = Convert.ToInt32(reader["Id"]);
                    qtdAtual = Convert.ToInt32(reader["Quantidade"]);
                }
            }

            if (itemId > 0)
            {
                using var cmd = new MySqlCommand(@"
                    UPDATE ItensPedido
                    SET Quantidade = @Qtd
                    WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Qtd", qtdAtual + quantidade);
                cmd.Parameters.AddWithValue("@Id", itemId);

                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new MySqlCommand(@"
                    INSERT INTO ItensPedido (PedidoId, ProdutoId, Quantidade, QuantidadeImpressa, Impresso, PrecoUnitario)
                    VALUES (@PedidoId, @ProdutoId, @Quantidade, 0, 0, @Preco)", conn);

                cmd.Parameters.AddWithValue("@PedidoId", pedidoId);
                cmd.Parameters.AddWithValue("@ProdutoId", produtoId);
                cmd.Parameters.AddWithValue("@Quantidade", quantidade);
                cmd.Parameters.AddWithValue("@Preco", preco);

                cmd.ExecuteNonQuery();
            }

            // Mantém Subtotal, Taxa (10%) e TotalFinal atualizados no banco.
            using (var cmd = new MySqlCommand(@"
                SELECT COALESCE(SUM(Quantidade * PrecoUnitario), 0)
                FROM ItensPedido
                WHERE PedidoId=@PedidoId", conn))
            {
                cmd.Parameters.AddWithValue("@PedidoId", pedidoId);
                decimal subtotal = Convert.ToDecimal(cmd.ExecuteScalar());
                decimal taxa = Math.Round(subtotal * 0.10m, 2);
                decimal totalFinal = subtotal + taxa;

                using var atualizar = new MySqlCommand(@"
                    UPDATE Pedidos
                    SET Subtotal=@Subtotal,
                        Taxa=@Taxa,
                        TotalFinal=@TotalFinal
                    WHERE Id=@PedidoId", conn);

                atualizar.Parameters.AddWithValue("@Subtotal", subtotal);
                atualizar.Parameters.AddWithValue("@Taxa", taxa);
                atualizar.Parameters.AddWithValue("@TotalFinal", totalFinal);
                atualizar.Parameters.AddWithValue("@PedidoId", pedidoId);
                atualizar.ExecuteNonQuery();
            }

            return RedirectToAction("Detalhes", new { mesaId });
        }

        // =====================
        // HISTÓRICO
        // =====================
        [AdminOnly]
        public IActionResult Historico()
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            var pedidos = new List<PedidoHistoricoVM>();

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            string sql = @"
                SELECT Id, MesaId, Subtotal, Taxa, TotalFinal, DataFechamento
                FROM Pedidos
                WHERE Status='Fechado'
                ORDER BY DataFechamento DESC";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                pedidos.Add(new PedidoHistoricoVM
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    MesaId = Convert.ToInt32(reader["MesaId"]),
                    Subtotal = Convert.ToDecimal(reader["Subtotal"]),
                    Taxa = Convert.ToDecimal(reader["Taxa"]),
                    TotalFinal = Convert.ToDecimal(reader["TotalFinal"]),
                    Data = reader["DataFechamento"] == DBNull.Value
                        ? DateTime.MinValue
                        : Convert.ToDateTime(reader["DataFechamento"])
                });
            }

            return View(pedidos);
        }

        [AdminOnly]
        public IActionResult Dashboard(string periodo = "hoje")
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            DateTime inicio;
            DateTime fim = DateTime.Now;

            if (periodo == "semana")
            {
                inicio = DateTime.Now.AddDays(-7);
            }
            else if (periodo == "mes")
            {
                inicio = DateTime.Now.AddMonths(-1);
            }
            else
            {
                inicio = DateTime.Today;
                fim = DateTime.Today.AddDays(1);
            }

            decimal totalVendas = 0;
            int totalPedidos = 0;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            string sql = @"
        SELECT 
            COUNT(*) AS TotalPedidos,
            COALESCE(SUM(TotalFinal), 0) AS TotalVendas
        FROM Pedidos
        WHERE Status = 'Fechado'
        AND DataFechamento BETWEEN @Inicio AND @Fim";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Inicio", inicio);
            cmd.Parameters.AddWithValue("@Fim", fim);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                totalPedidos = Convert.ToInt32(reader["TotalPedidos"]);
                totalVendas = Convert.ToDecimal(reader["TotalVendas"]);
            }

            ViewBag.TotalPedidos = totalPedidos;
            ViewBag.TotalVendas = totalVendas;
            ViewBag.Periodo = periodo;

            return View();
        }

        // Garçom e administrador podem fechar o pedido.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FecharPedido(int mesaId)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            int pedidoId = 0;
            decimal subtotal = 0;
            decimal taxa = 0;
            decimal totalFinal = 0;

            // 1. pegar pedido aberto
            using (var cmd = new MySqlCommand(@"
        SELECT Id FROM Pedidos 
        WHERE MesaId=@MesaId AND Status='Aberto'
        LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);
                var result = cmd.ExecuteScalar();

                if (result != null)
                    pedidoId = Convert.ToInt32(result);
            }

            if (pedidoId == 0)
                return RedirectToAction("Index");

            // 2. calcular total
            using (var cmd = new MySqlCommand(@"
        SELECT SUM(Quantidade * PrecoUnitario)
        FROM ItensPedido
        WHERE PedidoId=@PedidoId", conn))
            {
                cmd.Parameters.AddWithValue("@PedidoId", pedidoId);

                var result = cmd.ExecuteScalar();
                subtotal = result == DBNull.Value || result == null ? 0 : Convert.ToDecimal(result);
            }

            taxa = Math.Round(subtotal * 0.10m, 2);
            totalFinal = subtotal + taxa;

            // 3. fechar pedido
            using (var cmd = new MySqlCommand(@"
        UPDATE Pedidos
        SET Status='Fechado',
            Subtotal=@Subtotal,
            Taxa=@Taxa,
            TotalFinal=@TotalFinal,
            DataFechamento=NOW()
        WHERE Id=@PedidoId", conn))
            {
                cmd.Parameters.AddWithValue("@PedidoId", pedidoId);
                cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                cmd.Parameters.AddWithValue("@Taxa", taxa);
                cmd.Parameters.AddWithValue("@TotalFinal", totalFinal);

                cmd.ExecuteNonQuery();
            }

            // 4. liberar mesa
            using (var cmd = new MySqlCommand(@"
        UPDATE Mesas 
        SET Status='Livre'
        WHERE Id=@MesaId", conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index", "Mesa");
        }

        private int GetMesaId(int pedidoId, MySqlConnection conn)
        {
            using var cmd = new MySqlCommand(@"
        SELECT MesaId FROM Pedidos WHERE Id=@Id", conn);

            cmd.Parameters.AddWithValue("@Id", pedidoId);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImprimirCozinha(int mesaId)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            int pedidoId = 0;
            var itens = new List<(int Id, int QuantidadePendente, string Nome)>();

            // Localiza o pedido aberto.
            using (var cmd = new MySqlCommand(@"
                SELECT Id FROM Pedidos
                WHERE MesaId=@MesaId AND Status='Aberto'
                LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);
                var result = cmd.ExecuteScalar();

                if (result != null)
                    pedidoId = Convert.ToInt32(result);
            }

            if (pedidoId == 0)
                return Content("Nenhum pedido aberto");

            // QuantidadeImpressa é a quantidade que já saiu para a cozinha.
            // Ex.: quantidade=5 e impressa=3 => imprime somente 2.
            using (var cmd = new MySqlCommand(@"
                SELECT ip.Id, p.Nome,
                       GREATEST(ip.Quantidade - ip.QuantidadeImpressa, 0) AS QuantidadePendente
                FROM ItensPedido ip
                INNER JOIN Produtos p ON p.Id = ip.ProdutoId
                WHERE ip.PedidoId = @PedidoId
                  AND ip.Quantidade > ip.QuantidadeImpressa
                ORDER BY ip.Id", conn))
            {
                cmd.Parameters.AddWithValue("@PedidoId", pedidoId);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int quantidadePendente = Convert.ToInt32(reader["QuantidadePendente"]);

                    if (quantidadePendente > 0)
                    {
                        itens.Add((
                            Convert.ToInt32(reader["Id"]),
                            quantidadePendente,
                            reader["Nome"].ToString() ?? "Produto"
                        ));
                    }
                }
            }

            if (itens.Count == 0)
            {
                TempData["SucessoImpressao"] = "Não há itens novos para imprimir.";
                return RedirectToAction("Detalhes", new { mesaId });
            }

            string ticket =
                $@"
========================
COZINHA - MESA {mesaId}
========================
PEDIDO #{pedidoId}
------------------------
{string.Join("\n", itens.Select(i => $"{i.QuantidadePendente}x {i.Nome}"))}
------------------------
";

            // Só atualizamos QuantidadeImpressa se o envio para a impressora
            // for aceito. Assim uma falha de impressão pode ser tentada novamente.
            bool impresso = ImpressoraService.ImprimirCozinha(_configuration, ticket);

            if (!impresso)
            {
                TempData["ErroImpressao"] = "Não foi possível imprimir. Verifique a impressora e a configuração.";
                return RedirectToAction("Detalhes", new { mesaId });
            }

            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    foreach (var item in itens)
                    {
                        using var cmd = new MySqlCommand(@"
                            UPDATE ItensPedido
                            SET QuantidadeImpressa = LEAST(Quantidade, QuantidadeImpressa + @QuantidadeImpressa),
                                Impresso = CASE
                                    WHEN LEAST(Quantidade, QuantidadeImpressa + @QuantidadeImpressa) >= Quantidade THEN 1
                                    ELSE 0
                                END
                            WHERE Id=@Id AND PedidoId=@PedidoId", conn, transaction);

                        cmd.Parameters.AddWithValue("@QuantidadeImpressa", item.QuantidadePendente);
                        cmd.Parameters.AddWithValue("@Id", item.Id);
                        cmd.Parameters.AddWithValue("@PedidoId", pedidoId);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            TempData["SucessoImpressao"] = "Itens novos enviados para a cozinha.";
            return RedirectToAction("Detalhes", new { mesaId });
        }

    }
}