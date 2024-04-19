using Blazored.Toast;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace OrdelFusk
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            //For Toast Messages
            builder.Services.AddBlazoredToast();

            //For language switch (Ordel / Wordle as well)
            builder.Services.AddLocalization();

            //Set default to Swedish
            //CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("sv-SE");
            //CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("sv-SE");

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            await builder.Build().RunAsync();
        }
    }
}
