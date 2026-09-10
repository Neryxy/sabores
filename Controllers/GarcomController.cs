using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SaboresDeMiTierra.Filters;
using SaboresDeMiTierra.Models;

namespace SaboresDeMiTierra.Controllers
{
    [AdminOnly]
    public class GarcomController : Controller
    {
        private readonly IConfiguration _configuration;

        public GarcomController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var usuarios = new List<Usuario>();
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            const string sql = @"
                SELECT Id, Nome, Login, Cargo
                FROM Usuarios
                WHERE LOWER(Cargo) IN ('garçom', 'garcom')
                ORDER BY Nome";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                usuarios.Add(new Usuario
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nome = reader["Nome"]?.ToString() ?? "",
                    Login = reader["Login"]?.ToString() ?? "",
                    Cargo = reader["Cargo"]?.ToString() ?? "Garçom"
                });
            }

            return View(usuarios);
        }

        [HttpGet]
        public IActionResult Criar()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Criar(string nome, string login, string senha)
        {
            if (string.IsNullOrWhiteSpace(nome) ||
                string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(senha))
            {
                ViewBag.Erro = "Preencha nome, login e senha.";
                return View();
            }

            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            using (var check = new MySqlCommand(
                "SELECT COUNT(*) FROM Usuarios WHERE Login = @Login", conn))
            {
                check.Parameters.AddWithValue("@Login", login.Trim());

                if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                {
                    ViewBag.Erro = "Esse login já está sendo usado.";
                    return View();
                }
            }

            var usuario = new Usuario
            {
                Nome = nome.Trim(),
                Login = login.Trim(),
                Cargo = "Garçom"
            };

            var hasher = new PasswordHasher<Usuario>();
            usuario.Senha = hasher.HashPassword(usuario, senha);

            const string sql = @"
                INSERT INTO Usuarios (Nome, Login, Senha, Cargo)
                VALUES (@Nome, @Login, @Senha, 'Garçom')";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Nome", usuario.Nome);
            cmd.Parameters.AddWithValue("@Login", usuario.Login);
            cmd.Parameters.AddWithValue("@Senha", usuario.Senha);
            cmd.ExecuteNonQuery();

            TempData["Sucesso"] = "Garçom criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
    }
}
