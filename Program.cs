namespace waterfall;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Do Stuff

        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");
        app.MapGet("/health", () => Results.Ok());

        app.Run();
    }
}