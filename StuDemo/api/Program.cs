using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

string Issuer = "https://localhost:7194";
string Audience = "https://localhost:7194";
string Secret = "some secret phrase";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthorization();
builder.Services.AddAuthentication("jwt").AddJwtBearer("jwt", options=>
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));

    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = key
    };
});
builder.Services.AddDbContext<ApplicationDbContext>();
var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/students", async (ApplicationDbContext context) =>
{
    var all = await context.Students.ToListAsync();
    return Results.Json(all);
});

app.MapPost("/student", [Authorize] async ([FromBody] Student student, ApplicationDbContext context) =>
{
    student.Id = Guid.NewGuid();

    context.Students.Add(student);
    await context.SaveChangesAsync();

    return Results.Json(student);
});

app.MapGet("/receive-token/{nick}", (string nick, ApplicationDbContext context) =>
{
    Student? foundStudent = context
    .Students
    .FirstOrDefault(s=> s.Nick == nick);


    if (foundStudent != null)
    {
        var secretBytes = Encoding.UTF8.GetBytes(Secret);
        var securityKey = new SymmetricSecurityKey(secretBytes);

        var jwttoken = new JwtSecurityToken(
            claims: new Claim[] {
                new Claim("surname", foundStudent.Surname),
                new Claim("name", foundStudent.Name),
            },
            signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256),
            issuer: Issuer,
            audience: Audience,
            expires: DateTime.Now.AddMinutes(45),
            notBefore: DateTime.Now);

        var tokenAsString = new JwtSecurityTokenHandler().WriteToken(jwttoken);
        return Results.Text(tokenAsString);
    }

    return Results.NotFound();
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();
    
    db.Students.Add(new Student
    {
        Id = Guid.NewGuid(),
        Name = "Bohdan",
        Surname = "Vynnychuk",
        Nick = "bodya"
    });

    db.SaveChanges();
}

app.Run();


internal class Student
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string Nick { get; set; } = null!;
}