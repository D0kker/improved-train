using LolAnalyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LolAnalyzer.Infrastructure.Migrations;

[DbContext(typeof(LolAnalyzerDbContext))]
[Migration("202608310001_AddAnalysisJobs")]
public partial class AddAnalysisJobs : Migration
{
    private static readonly string[] StatusQueueColumns = ["status", "created_at"];
    private static readonly string[] PlayerHistoryColumns = ["puuid", "created_at"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "analysis_jobs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                puuid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                requested_count = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                matches_processed = table.Column<int>(type: "integer", nullable: false),
                error_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_analysis_jobs", item => item.id);
                table.CheckConstraint(
                    "ck_analysis_jobs_progress",
                    "matches_processed >= 0 AND matches_processed <= requested_count");
                table.CheckConstraint(
                    "ck_analysis_jobs_requested_count",
                    "requested_count >= 1 AND requested_count <= 200");
            });

        migrationBuilder.CreateIndex(
            name: "ix_analysis_jobs_puuid_created_at",
            table: "analysis_jobs",
            columns: PlayerHistoryColumns);
        migrationBuilder.CreateIndex(
            name: "ix_analysis_jobs_status_created_at",
            table: "analysis_jobs",
            columns: StatusQueueColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "analysis_jobs");
    }
}
