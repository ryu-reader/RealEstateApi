using Microsoft.EntityFrameworkCore;
using RealEstateAPI;


var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    );
});




var app = builder.Build();

// Configure the HTTP request pipeline.


///SWAGGER
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "API Working!!");


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
