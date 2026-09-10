using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Restaurante.Models;
using SaboresDeMiTierra.Filters;

namespace Restaurante.Controllers
{
    public class MesaController : Controller
    {
        private readonly IConfiguration _configuration;

        public MesaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // =========================
        // LISTAR MESAS
        // =========================
        public IActionResult Index()
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            List<Mesa> mesas = new();

            using (var conn = new MySqlConnection(conexao))
            {
                conn.Open();

                string sql = @"
                    SELECT Id, Numero, Nome, Status, Local
                    FROM Mesas
                    ORDER BY Local ASC, Numero ASC";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    mesas.Add(new Mesa
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Numero = Convert.ToInt32(reader["Numero"]),
                        Nome = reader["Nome"]?.ToString(),
                        Status = reader["Status"]?.ToString() ?? "Livre",
                        Local = reader["Local"]?.ToString() ?? "Dentro"
                    });
                }
            }

            Console.WriteLine("========== TESTE MESAS ==========");

            foreach (var mesa in mesas)
            {
                Console.WriteLine(
                    $"ID: {mesa.Id} | Numero: {mesa.Numero} | Nome: {mesa.Nome} | Local: {mesa.Local}");
            }

            Console.WriteLine("=================================");

            return Content("TESTE MESA CONTROLLER NOVO");
        }

        // =========================
        // LIBERAR MESA
        // =========================
        [HttpPost]
        public IActionResult Liberar(int mesaId)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            string sqlGetPedidos = @"
                SELECT Id 
                FROM Pedidos 
                WHERE MesaId = @MesaId AND Status = 'Aberto'";

            List<int> pedidos = new();

            using (var cmd = new MySqlCommand(sqlGetPedidos, conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    pedidos.Add(Convert.ToInt32(reader["Id"]));
                }
            }

            foreach (var pedidoId in pedidos)
            {
                using var cmdItens = new MySqlCommand(
                    "DELETE FROM ItensPedido WHERE PedidoId = @PedidoId", conn);

                cmdItens.Parameters.AddWithValue("@PedidoId", pedidoId);
                cmdItens.ExecuteNonQuery();
            }

            using (var cmdPedidos = new MySqlCommand(
                "DELETE FROM Pedidos WHERE MesaId = @MesaId AND Status = 'Aberto'", conn))
            {
                cmdPedidos.Parameters.AddWithValue("@MesaId", mesaId);
                cmdPedidos.ExecuteNonQuery();
            }

            using (var cmdMesa = new MySqlCommand(
                "UPDATE Mesas SET Status = 'Livre' WHERE Id = @MesaId", conn))
            {
                cmdMesa.Parameters.AddWithValue("@MesaId", mesaId);
                cmdMesa.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        // =========================
        // CREATE - TELA
        // =========================
        [HttpGet]
        [AdminOnly]
        public IActionResult Create()
        {
            return View();
        }

        // =========================
        // CREATE - SALVAR
        // =========================
        [HttpPost]
        [AdminOnly]
        public IActionResult Create(Mesa mesa)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            string local = string.IsNullOrWhiteSpace(mesa.Local)
                ? "Dentro"
                : mesa.Local;

            // =========================
            // DESCOBRIR PRÓXIMO NÚMERO
            // =========================
            string sqlNumero = @"
                SELECT COALESCE(MAX(Numero), 0) + 1
                FROM Mesas
                WHERE Local = @Local";

            int proximoNumero;

            using (var cmdNumero = new MySqlCommand(sqlNumero, conn))
            {
                cmdNumero.Parameters.AddWithValue("@Local", local);

                proximoNumero = Convert.ToInt32(
                    cmdNumero.ExecuteScalar());
            }

            // =========================
            // GERAR NOME AUTOMÁTICO
            // =========================
            string nome = $"Mesa {proximoNumero}";

            // =========================
            // INSERIR MESA
            // =========================
            string sql = @"
                INSERT INTO Mesas
                    (Numero, Nome, Status, Local)
                VALUES
                    (@Numero, @Nome, @Status, @Local)";

            using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@Numero", proximoNumero);
            cmd.Parameters.AddWithValue("@Nome", nome);
            cmd.Parameters.AddWithValue("@Status", "Livre");
            cmd.Parameters.AddWithValue("@Local", local);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        // =========================
        // EDIT - TELA
        // =========================
        [HttpGet]
        [AdminOnly]
        public IActionResult Edit(int id)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            Mesa mesa = new();

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            string sql = @"
                SELECT Id, Numero, Nome, Status, Local
                FROM Mesas
                WHERE Id = @Id";

            using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                mesa.Id = Convert.ToInt32(reader["Id"]);
                mesa.Numero = Convert.ToInt32(reader["Numero"]);
                mesa.Nome = reader["Nome"]?.ToString();
                mesa.Status = reader["Status"]?.ToString() ?? "Livre";
                mesa.Local = reader["Local"]?.ToString() ?? "Dentro";
            }

            return View(mesa);
        }

        // =========================
        // EDIT - SALVAR
        // =========================
        [HttpPost]
        [AdminOnly]
        public IActionResult Edit(Mesa mesa)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            string local = string.IsNullOrWhiteSpace(mesa.Local)
                ? "Dentro"
                : mesa.Local;

            string sql = @"
                UPDATE Mesas
                SET Numero = @Numero,
                    Nome = @Nome,
                    Status = @Status,
                    Local = @Local
                WHERE Id = @Id";

            using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@Id", mesa.Id);
            cmd.Parameters.AddWithValue("@Numero", mesa.Numero);
            cmd.Parameters.AddWithValue("@Nome", mesa.Nome ?? "");
            cmd.Parameters.AddWithValue("@Status", mesa.Status ?? "Livre");
            cmd.Parameters.AddWithValue("@Local", local);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        // =========================
        // DELETE
        // =========================
        [AdminOnly]
        public IActionResult Delete(int id)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            string checkSql = "SELECT Status FROM Mesas WHERE Id = @Id";

            string status = "";

            using (var cmdCheck = new MySqlCommand(checkSql, conn))
            {
                cmdCheck.Parameters.AddWithValue("@Id", id);

                var result = cmdCheck.ExecuteScalar();

                if (result != null)
                    status = result.ToString()!;
            }

            if (status == "Ocupada")
            {
                TempData["Erro"] = "Não é possível excluir uma mesa ocupada!";
                return RedirectToAction("Index");
            }

            using var cmd = new MySqlCommand(
                "DELETE FROM Mesas WHERE Id = @Id",
                conn);

            cmd.Parameters.AddWithValue("@Id", id);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        // =========================
        // RESERVAR MESA
        // =========================
        public IActionResult Reservar(int id)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            string sql = @"
                UPDATE Mesas
                SET Status = 'Reservada'
                WHERE Id = @Id";

            using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@Id", id);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }
    }
}