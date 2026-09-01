using LolAnalyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LolAnalyzer.Infrastructure.Migrations;

[DbContext(typeof(LolAnalyzerDbContext))]
[Migration("202608310002_PreventDuplicateActiveJobs")]
public partial class PreventDuplicateActiveJobs : Migration
{
    private static readonly string[] ActiveRequestColumns = ["puuid", "requested_count"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ux_analysis_jobs_active_request",
            table: "analysis_jobs",
            columns: ActiveRequestColumns,
            unique: true,
            filter: "status IN ('Queued', 'Running')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ux_analysis_jobs_active_request", table: "analysis_jobs");
    }
}
