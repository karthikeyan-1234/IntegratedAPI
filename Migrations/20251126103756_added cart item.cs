using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegratedAPI.Migrations
{
    /// <inheritdoc />
    public partial class addedcartitem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "product_id",
                table: "cartItem",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "Productid",
                table: "cartItem",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_cartItem_product_id",
                table: "cartItem",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_cartItem_Productid",
                table: "cartItem",
                column: "Productid");

            migrationBuilder.AddForeignKey(
                name: "FK_cartItem_product_Productid",
                table: "cartItem",
                column: "Productid",
                principalTable: "product",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_cartItem_product_product_id",
                table: "cartItem",
                column: "product_id",
                principalTable: "product",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cartItem_product_Productid",
                table: "cartItem");

            migrationBuilder.DropForeignKey(
                name: "FK_cartItem_product_product_id",
                table: "cartItem");

            migrationBuilder.DropIndex(
                name: "IX_cartItem_product_id",
                table: "cartItem");

            migrationBuilder.DropIndex(
                name: "IX_cartItem_Productid",
                table: "cartItem");

            migrationBuilder.DropColumn(
                name: "Productid",
                table: "cartItem");

            migrationBuilder.AlterColumn<string>(
                name: "product_id",
                table: "cartItem",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
