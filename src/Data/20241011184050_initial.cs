using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsultaCnpjReceita.src.Data
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CnpjEstoque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocCedente = table.Column<string>(type: "text", nullable: false),
                    DocSacado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CnpjEstoque", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RetornoReceita",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ultima_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cnpj = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    porte = table.Column<string>(type: "text", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    fantasia = table.Column<string>(type: "text", nullable: false),
                    abertura = table.Column<string>(type: "text", nullable: false),
                    natureza_juridica = table.Column<string>(type: "text", nullable: false),
                    logradouro = table.Column<string>(type: "text", nullable: false),
                    numero = table.Column<string>(type: "text", nullable: false),
                    complemento = table.Column<string>(type: "text", nullable: false),
                    cep = table.Column<string>(type: "text", nullable: false),
                    bairro = table.Column<string>(type: "text", nullable: false),
                    municipio = table.Column<string>(type: "text", nullable: false),
                    uf = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    telefone = table.Column<string>(type: "text", nullable: false),
                    efr = table.Column<string>(type: "text", nullable: false),
                    situacao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetornoReceita", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CnpjEstoque");

            migrationBuilder.DropTable(
                name: "RetornoReceita");
        }
    }
}
