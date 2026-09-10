using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Restaurante.Services
{
    public static class ImpressoraService
    {
        // Epson TM-T20 / TM-T20II / TM-T20III usam ESC/POS.
        // Modo "Rede": envia diretamente para a porta TCP 9100.
        // Modo "Windows": envia RAW para uma impressora instalada no Windows.

        public static bool ImprimirCozinha(IConfiguration configuration, string texto)
        {
            try
            {
                string modo = configuration["Impressora:Modo"]?.Trim() ?? "Rede";

                return modo.Equals("Windows", StringComparison.OrdinalIgnoreCase)
                    ? ImprimirWindows(configuration, texto)
                    : ImprimirRede(configuration, texto);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro na impressão: " + ex.Message);
                return false;
            }
        }

        private static bool ImprimirRede(IConfiguration configuration, string texto)
        {
            string ip = configuration["Impressora:IP"]?.Trim() ?? "";
            int porta = configuration.GetValue<int?>("Impressora:Porta") ?? 9100;

            if (string.IsNullOrWhiteSpace(ip))
            {
                Console.WriteLine("Impressora:IP não configurado.");
                return false;
            }

            using var client = new TcpClient();
            client.Connect(ip, porta);

            using var stream = client.GetStream();

            byte[] dados = MontarEscPos(texto);
            stream.Write(dados, 0, dados.Length);
            stream.Flush();

            return true;
        }

        private static bool ImprimirWindows(IConfiguration configuration, string texto)
        {
            string nomeImpressora = configuration["Impressora:NomeWindows"]?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(nomeImpressora))
            {
                Console.WriteLine("Impressora:NomeWindows não configurado.");
                return false;
            }

            byte[] dados = MontarEscPos(texto);

            IntPtr printerHandle;
            if (!OpenPrinter(nomeImpressora, out printerHandle, IntPtr.Zero))
            {
                Console.WriteLine("Não foi possível abrir a impressora Windows: " + nomeImpressora);
                return false;
            }

            try
            {
                var documento = new DOCINFO
                {
                    pDocName = "Sabores de Mi Tierra - Cozinha",
                    pDataType = "RAW"
                };

                if (StartDocPrinter(printerHandle, 1, documento) == 0)
                    return false;

                try
                {
                    if (!StartPagePrinter(printerHandle))
                        return false;

                    try
                    {
                        IntPtr unmanagedBytes = Marshal.AllocHGlobal(dados.Length);
                        try
                        {
                            Marshal.Copy(dados, 0, unmanagedBytes, dados.Length);
                            return WritePrinter(printerHandle, unmanagedBytes, dados.Length, out _);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(unmanagedBytes);
                        }
                    }
                    finally
                    {
                        EndPagePrinter(printerHandle);
                    }
                }
                finally
                {
                    EndDocPrinter(printerHandle);
                }
            }
            finally
            {
                ClosePrinter(printerHandle);
            }
        }

        private static byte[] MontarEscPos(string texto)
        {
            // CP850 é uma codificação comum nas Epson térmicas e preserva
            // caracteres como ç e acentos do português.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = Encoding.GetEncoding(850);

            using var ms = new MemoryStream();

            // Inicializa a TM-T20.
            ms.Write(new byte[] { 0x1B, 0x40 });

            // Centraliza o cabeçalho.
            ms.Write(new byte[] { 0x1B, 0x61, 0x01 });
            ms.Write(encoding.GetBytes("SABORES DE MI TIERRA\n"));

            // Volta para alinhamento à esquerda.
            ms.Write(new byte[] { 0x1B, 0x61, 0x00 });
            ms.Write(encoding.GetBytes(texto));
            ms.Write(encoding.GetBytes("\n\n"));

            // Corta o papel.
            ms.Write(new byte[] { 0x1D, 0x56, 0x41, 0x03 });

            return ms.ToArray();
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class DOCINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pDocName = "";
            [MarshalAs(UnmanagedType.LPWStr)] public string? pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDataType = "RAW";
        }

        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFO di);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(
            IntPtr hPrinter,
            IntPtr pBytes,
            int dwCount,
            out int dwWritten);
    }
}
