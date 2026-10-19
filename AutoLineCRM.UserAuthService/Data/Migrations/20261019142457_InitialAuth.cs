using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoLineCRM.UserAuthService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "Car",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VIN = table.Column<string>(type: "text", nullable: false),
                    Brand = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    Trim = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Mileage = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Car", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Login = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Administrator_FullName = table.Column<string>(type: "text", nullable: true),
                    Client_FullName = table.Column<string>(type: "text", nullable: true),
                    Client_Phone = table.Column<string>(type: "text", nullable: true),
                    PassportData = table.Column<string>(type: "text", nullable: true),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    CommissionRate = table.Column<decimal>(type: "numeric", nullable: true),
                    SalesCount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarImage",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Url = table.Column<string>(type: "text", nullable: false),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false),
                    CarId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarImage_Car_CarId",
                        column: x => x.CarId,
                        principalSchema: "auth",
                        principalTable: "Car",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Request",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    CarId = table.Column<int>(type: "integer", nullable: false),
                    ManagerID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Request", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Request_Car_CarId",
                        column: x => x.CarId,
                        principalSchema: "auth",
                        principalTable: "Car",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Request_Users_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Request_Users_ManagerID",
                        column: x => x.ManagerID,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Review",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    CarId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Review", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Review_Car_CarId",
                        column: x => x.CarId,
                        principalSchema: "auth",
                        principalTable: "Car",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Review_Users_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contract",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Number = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    DigitalSignature = table.Column<string>(type: "text", nullable: true),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    ManagerId = table.Column<int>(type: "integer", nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contract", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contract_Request_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "auth",
                        principalTable: "Request",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contract_Users_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contract_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    TransactionId = table.Column<string>(type: "text", nullable: true),
                    ProviderName = table.Column<string>(type: "text", nullable: true),
                    RequestId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payment_Request_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "auth",
                        principalTable: "Request",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarImage_CarId",
                schema: "auth",
                table: "CarImage",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_ClientId",
                schema: "auth",
                table: "Contract",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_ManagerId",
                schema: "auth",
                table: "Contract",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_RequestId",
                schema: "auth",
                table: "Contract",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_RequestId",
                schema: "auth",
                table: "Payment",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Request_CarId",
                schema: "auth",
                table: "Request",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_Request_ClientId",
                schema: "auth",
                table: "Request",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Request_ManagerID",
                schema: "auth",
                table: "Request",
                column: "ManagerID");

            migrationBuilder.CreateIndex(
                name: "IX_Review_CarId",
                schema: "auth",
                table: "Review",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_Review_ClientId",
                schema: "auth",
                table: "Review",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "auth",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                schema: "auth",
                table: "Users",
                column: "Login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarImage",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Contract",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Payment",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Review",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Request",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Car",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "auth");
        }
    }
}
