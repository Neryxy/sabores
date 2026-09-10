using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Restaurante.Models;
using SaboresDeMiTierra.Filters;

namespace Restaurante.Controllers
{
    public class ProdutoController : Controller
    {
        private readonly IConfiguration _configuration;

        public ProdutoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            List<Produto> produtos = new List<Produto>();

            string conexao =
                _configuration.GetConnectionString("DefaultConnection")!;

            using (MySqlConnection conn = new MySqlConnection(conexao))
            {
                conn.Open();

                string sql = "SELECT* FROM Produtos WHERE Ativo = 1";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Produto produto = new Produto
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nome = reader["Nome"].ToString()!,
                                Preco = Convert.ToDecimal(reader["Preco"]),
                                Categoria = reader["Categoria"].ToString()!
                            };

                            produtos.Add(produto);
                        }
                    }
                }
            }

            return View(produtos);
        }
        [HttpGet]
        [AdminOnly]
        public IActionResult Criar()
        {
            return View();
        }

        [HttpPost]
        [AdminOnly]
        public IActionResult Criar(Produto produto)
        {
            string conexao =
                _configuration.GetConnectionString("DefaultConnection")!;

            using (MySqlConnection conn = new MySqlConnection(conexao))
            {
                conn.Open();

                string sql = @"
                 INSERT INTO Produtos (Nome, Preco, Categoria, Ativo)
                 VALUES (@Nome, @Preco, @Categoria, 1)";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Nome", produto.Nome);
                    cmd.Parameters.AddWithValue("@Preco", produto.Preco);
                    cmd.Parameters.AddWithValue("@Categoria", produto.Categoria);

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [AdminOnly]
        public IActionResult Editar(int id)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            Produto produto = null;

            using (MySqlConnection conn = new MySqlConnection(conexao))
            {
                conn.Open();

                string sql = "SELECT * FROM Produtos WHERE Id = @Id";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            produto = new Produto
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nome = reader["Nome"].ToString(),
                                Preco = Convert.ToDecimal(reader["Preco"]),
                                Categoria = reader["Categoria"].ToString()
                            };
                        }
                    }
                }
            }

            return View(produto);
        }

        [HttpPost]
        [AdminOnly]
        public IActionResult Editar(Produto p)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using (MySqlConnection conn = new MySqlConnection(conexao))
            {
                conn.Open();

                string sql = @"
        UPDATE Produtos
        SET Nome = @Nome,
            Preco = @Preco,
            Categoria = @Categoria
        WHERE Id = @Id";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", p.Id);
                    cmd.Parameters.AddWithValue("@Nome", p.Nome);
                    cmd.Parameters.AddWithValue("@Preco", p.Preco);
                    cmd.Parameters.AddWithValue("@Categoria", p.Categoria);

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [AdminOnly]
        public IActionResult Excluir(int id)
        {
            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using (MySqlConnection conn = new MySqlConnection(conexao))
            {
                conn.Open();

                string sql = @"
        UPDATE Produtos
        SET Ativo = 0
        WHERE Id = @Id";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }
    }
}