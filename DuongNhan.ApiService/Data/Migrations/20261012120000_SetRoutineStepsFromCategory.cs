using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuongNhan.ApiService.Data.Migrations
{
    /// <summary>
    /// Data-only migration (no schema change). The routine now has 7 steps (makeup removal, cleansing, toner,
    /// serum, moisturizer, sunscreen, additional care), so every product's products."Step" is recalculated from the
    /// product itself (name + category). It does not depend on what the column holds today (0, the old 1-6 numbers,
    /// or already correct), so it never shifts anything twice and can safely run again.
    /// SkinType is not touched. Steps edited by hand in Admin are recalculated too.
    /// </summary>
    public partial class SetRoutineStepsFromCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE products
                SET "Step" = CASE
                    -- products that are clearly makeup removers, whatever their category says
                    WHEN LOWER("Name") ~ '(micellar|makeup remover|make-up remover|cleansing oil|cleansing balm|tẩy trang)' THEN 1
                    WHEN regexp_replace(LOWER("Category"), '[^a-z]', '', 'g') IN
                         ('makeupremover','makeupremoval','remover','cleansingoil','cleansingbalm','micellar','micellarwater') THEN 1
                    WHEN regexp_replace(LOWER("Category"), '[^a-z]', '', 'g') IN ('cleanser','cleanse','cleansing') THEN 2
                    WHEN regexp_replace(LOWER("Category"), '[^a-z]', '', 'g') IN ('toner','exfoliant') THEN 3
                    -- liquid exfoliants and toners filed under "Treatment" belong to the toner step
                    WHEN regexp_replace(LOWER("Category"), '[^a-z]', '', 'g') = 'treatment'
                         AND LOWER("Name") ~ '\y(toner|exfoliant|exfoliating|bha|aha|pha)\y' THEN 3
                    WHEN regexp_replace(LOWER("Category"), '[^a-z]', '', 'g') IN ('serum','essence','ampoule') THEN 4
                    WHEN regexp_replace(LOWER("Category"), '[^a-z]', '', 'g') IN ('moisturizer','moisturiser','cream') THEN 5
                    WHEN regexp_replace(LOWER("Category"), '[^a-z]', '', 'g') = 'sunscreen' THEN 6
                    ELSE 7
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: the previous values cannot be reconstructed, and the Step column itself is
            // owned by the AddSkinTypeAndStepToProducts migration.
        }
    }
}
