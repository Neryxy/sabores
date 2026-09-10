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
                    SELECT Id, Numero, Nome, Status
                    FROM Mesas
                    ORDER BY Nome ASC, Numero ASC";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    mesas.Add(new Mesa
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Numero = Convert.ToInt32(reader["Numero"]),
                        Nome = reader["Nome"]?.ToString(),
                        Status = reader["Status"]?.ToString() ?? "Livre"
                    });
                }
            }

            return View(mesas);
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
        // CREATE
        // =========================
        [HttpGet]
        [AdminOnly]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [AdminOnly]
        public IActionResult Create(Mesa mesa)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            string sql = @"
                INSERT INTO Mesas (Numero, Nome, Status)
                VALUES (@Numero, @Nome, @Status)";

            using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@Numero", mesa.Numero);
            cmd.Parameters.AddWithValue("@Nome", mesa.Nome ?? "");
            cmd.Parameters.AddWithValue("@Status", mesa.Status ?? "Livre");

            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        // =========================
        // EDIT (GET)
        // =========================
        [HttpGet]
        [AdminOnly]
        public IActionResult Edit(int id)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            Mesa mesa = new();

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            string sql = "SELECT Id, Numero, Nome, Status FROM Mesas WHERE Id = @Id";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                mesa.Id = Convert.ToInt32(reader["Id"]);
                mesa.Numero = Convert.ToInt32(reader["Numero"]);
                mesa.Nome = reader["Nome"]?.ToString();
                mesa.Status = reader["Status"]?.ToString() ?? "Livre";
            }

            return View(mesa);
        }

        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        [AdminOnly]
        public IActionResult Edit(Mesa mesa)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            string sql = @"
                UPDATE Mesas
                SET Numero = @Numero,
                    Nome = @Nome,
                    Status = @Status
                WHERE Id = @Id";

            using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@Id", mesa.Id);
            cmd.Parameters.AddWithValue("@Numero", mesa.Numero);
            cmd.Parameters.AddWithValue("@Nome", mesa.Nome ?? "");
            cmd.Parameters.AddWithValue("@Status", mesa.Status ?? "Livre");

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

            using var cmd = new MySqlCommand("DELETE FROM Mesas WHERE Id = @Id", conn);
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

            string sql = "UPDATE Mesas SET Status = 'Reservada' WHERE Id = @Id";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }
    }
}