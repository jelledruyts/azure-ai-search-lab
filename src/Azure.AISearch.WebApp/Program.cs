using Azure.AISearch.WebApp;
using Azure.AISearch.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

// Inject application services.
var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);
builder.Services.AddSingleton(appSettings);
builder.Services.AddSingleton<IEmbeddingService, AzureOpenAIEmbeddingService>();
builder.Services.AddSingleton<SearchService>();
builder.Services.AddSingleton<AzureCognitiveSearchService>();
builder.Services.AddSingleton<AzureOpenAISearchService>();
builder.Services.AddSingleton<AzureCognitiveSearchConfigurationService>();
builder.Services.AddSingleton<AzureStorageConfigurationService>();

// Asynchronously initialize the application on startup.
builder.Services.AddHostedService<AppInitializationHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
