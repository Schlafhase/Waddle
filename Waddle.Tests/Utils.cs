using Waddle.Config;

namespace Waddle.Tests
{
    public static class Utils
    {
        public static WaddleConfig ClientOnlyConfig { get; } = new() { DefaultWorkflow = "" };
        public static WaddleContext ClientOnlyContext { get; } =
            new(ClientOnlyConfig, () => throw new NotImplementedException());

        public static WaddleConfig ServerConfig { get; } =
            new()
            {
                DefaultWorkflow = "",
                Server = new()
                {
                    Host = "localhost",
                    Port = 9284,
                    Username = "root",
                    UsePassword = true,
                },
            };
        public static WaddleContext ServerContext { get; } = new(ServerConfig, () => "Docker!");

        public static string ReadAllClientOutput(WaddleContext context)
        {
            context.ClientOutputWriter.Flush();
            context.ClientOutput.Position = 0;
            return new StreamReader(context.ClientOutput).ReadToEnd();
        }
        public static string ReadAllServerOutput() {
            ServerContext.Server!.ServerOutputWriter.Flush();
            ServerContext.Server!.ServerOutput.Position = 0;
            return new StreamReader(ServerContext.Server!.ServerOutput).ReadToEnd();
        }

        public static void ResetClientOutputStream()
        {
            Stream old = ServerContext.ClientOutput;
            StreamWriter oldWriter = ServerContext.ClientOutputWriter;
            Stream oldClientOnly = ClientOnlyContext.ClientOutput;
            StreamWriter oldWriterClientOnly = ClientOnlyContext.ClientOutputWriter;

            ServerContext.ClientOutputWriter.Flush();
            ServerContext.ClientOutput = new MemoryStream();
            ServerContext.ClientOutputWriter = new(ServerContext.ClientOutput);
            ClientOnlyContext.ClientOutputWriter.Flush();
            ClientOnlyContext.ClientOutput = new MemoryStream();
            ClientOnlyContext.ClientOutputWriter = new(ClientOnlyContext.ClientOutput);

            oldWriter.Dispose();
            old.Dispose();
            oldWriterClientOnly.Dispose();
            oldClientOnly.Dispose();
        }

        public static void ResetServerOutputStream()
        {
            Stream old = ServerContext.Server!.ServerOutput;
            ServerContext.Server!.ServerOutput = new MemoryStream();
            old.Dispose();
        }
    }
}
