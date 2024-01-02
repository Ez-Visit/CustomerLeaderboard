using CustomerLeaderboard.BizService;

internal class Program
{
    private static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        host.Run();

    }

    static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.ConfigureServices(services =>
                        {
                            services.AddControllers();
                            services.AddSingleton<CustomerRankingService>();
                            services.AddSingleton<CustomerRankingBySkipListService>();
                        });

                        _ = webBuilder.Configure(app =>
                        {
                            _ = app.UseAuthorization();
                            app.UseRouting();
                            app.UseEndpoints(endpoints => endpoints.MapControllers());
                        });
                    });
    }
}