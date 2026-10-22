using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabajarasInterview.Api.Data.Migrations;

/// <inheritdoc />
public partial class _20261020145007_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Candidates",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FirstName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Candidates", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Locations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                City = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Locations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Stacks",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_CI_AI"),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Stacks", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FirstName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_CI_AI"),
                PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Questions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                StackId = table.Column<int>(type: "int", nullable: false),
                Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                AcceptableAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Level = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Questions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Questions_Stacks_StackId",
                    column: x => x.StackId,
                    principalTable: "Stacks",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "Positions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CreatedBy = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Status = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                WorkModel = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Positions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Positions_Users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "Users",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(type: "int", nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_RefreshTokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "CandidateApplications",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CandidateId = table.Column<int>(type: "int", nullable: false),
                PositionId = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                FinalScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                FinalComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CandidateApplications", x => x.Id);
                table.ForeignKey(
                    name: "FK_CandidateApplications_Candidates_CandidateId",
                    column: x => x.CandidateId,
                    principalTable: "Candidates",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_CandidateApplications_Positions_PositionId",
                    column: x => x.PositionId,
                    principalTable: "Positions",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "PositionLocations",
            columns: table => new
            {
                PositionId = table.Column<int>(type: "int", nullable: false),
                LocationId = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PositionLocations", x => new { x.PositionId, x.LocationId });
                table.ForeignKey(
                    name: "FK_PositionLocations_Locations_LocationId",
                    column: x => x.LocationId,
                    principalTable: "Locations",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_PositionLocations_Positions_PositionId",
                    column: x => x.PositionId,
                    principalTable: "Positions",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "PositionStacks",
            columns: table => new
            {
                PositionId = table.Column<int>(type: "int", nullable: false),
                StackId = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PositionStacks", x => new { x.PositionId, x.StackId });
                table.ForeignKey(
                    name: "FK_PositionStacks_Positions_PositionId",
                    column: x => x.PositionId,
                    principalTable: "Positions",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_PositionStacks_Stacks_StackId",
                    column: x => x.StackId,
                    principalTable: "Stacks",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "Interviews",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CandidateApplicationId = table.Column<int>(type: "int", nullable: false),
                InterviewerId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                InterviewType = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                Status = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Interviews", x => x.Id);
                table.ForeignKey(
                    name: "FK_Interviews_CandidateApplications_CandidateApplicationId",
                    column: x => x.CandidateApplicationId,
                    principalTable: "CandidateApplications",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Interviews_Users_InterviewerId",
                    column: x => x.InterviewerId,
                    principalTable: "Users",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "InterviewQuestions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                InterviewId = table.Column<int>(type: "int", nullable: false),
                QuestionId = table.Column<int>(type: "int", nullable: false),
                QuestionOrder = table.Column<int>(type: "int", nullable: false),
                Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InterviewQuestions", x => x.Id);
                table.ForeignKey(
                    name: "FK_InterviewQuestions_Interviews_InterviewId",
                    column: x => x.InterviewId,
                    principalTable: "Interviews",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_InterviewQuestions_Questions_QuestionId",
                    column: x => x.QuestionId,
                    principalTable: "Questions",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "InterviewReviewers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                InterviewId = table.Column<int>(type: "int", nullable: false),
                ReviewerId = table.Column<int>(type: "int", nullable: false),
                ReviewScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                ReviewComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InterviewReviewers", x => x.Id);
                table.ForeignKey(
                    name: "FK_InterviewReviewers_Interviews_InterviewId",
                    column: x => x.InterviewId,
                    principalTable: "Interviews",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_InterviewReviewers_Users_ReviewerId",
                    column: x => x.ReviewerId,
                    principalTable: "Users",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_CandidateApplications_CandidateId_PositionId",
            table: "CandidateApplications",
            columns: new[] { "CandidateId", "PositionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CandidateApplications_PositionId",
            table: "CandidateApplications",
            column: "PositionId");

        migrationBuilder.CreateIndex(
            name: "IX_InterviewQuestions_InterviewId_QuestionId",
            table: "InterviewQuestions",
            columns: new[] { "InterviewId", "QuestionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InterviewQuestions_InterviewId_QuestionOrder",
            table: "InterviewQuestions",
            columns: new[] { "InterviewId", "QuestionOrder" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InterviewQuestions_QuestionId",
            table: "InterviewQuestions",
            column: "QuestionId");

        migrationBuilder.CreateIndex(
            name: "IX_InterviewReviewers_InterviewId_ReviewerId",
            table: "InterviewReviewers",
            columns: new[] { "InterviewId", "ReviewerId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InterviewReviewers_ReviewerId",
            table: "InterviewReviewers",
            column: "ReviewerId");

        migrationBuilder.CreateIndex(
            name: "IX_Interviews_CandidateApplicationId",
            table: "Interviews",
            column: "CandidateApplicationId");

        migrationBuilder.CreateIndex(
            name: "IX_Interviews_InterviewerId",
            table: "Interviews",
            column: "InterviewerId");

        migrationBuilder.CreateIndex(
            name: "IX_PositionLocations_LocationId",
            table: "PositionLocations",
            column: "LocationId");

        migrationBuilder.CreateIndex(
            name: "IX_Positions_CreatedBy",
            table: "Positions",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_PositionStacks_StackId",
            table: "PositionStacks",
            column: "StackId");

        migrationBuilder.CreateIndex(
            name: "IX_Questions_StackId",
            table: "Questions",
            column: "StackId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Stacks_Name",
            table: "Stacks",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "InterviewQuestions");

        migrationBuilder.DropTable(
            name: "InterviewReviewers");

        migrationBuilder.DropTable(
            name: "PositionLocations");

        migrationBuilder.DropTable(
            name: "PositionStacks");

        migrationBuilder.DropTable(
            name: "RefreshTokens");

        migrationBuilder.DropTable(
            name: "Questions");

        migrationBuilder.DropTable(
            name: "Interviews");

        migrationBuilder.DropTable(
            name: "Locations");

        migrationBuilder.DropTable(
            name: "Stacks");

        migrationBuilder.DropTable(
            name: "CandidateApplications");

        migrationBuilder.DropTable(
            name: "Candidates");

        migrationBuilder.DropTable(
            name: "Positions");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
