using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VSuiteLab.Migrations
{
    /// <inheritdoc />
    public partial class InitalCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DavConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    httpUrl = table.Column<string>(type: "TEXT", nullable: false),
                    username = table.Column<string>(type: "TEXT", nullable: true),
                    password = table.Column<string>(type: "TEXT", nullable: true),
                    SupportsVtodo = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupportsVjournal = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSyncToken = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DavConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SyncAuto = table.Column<float>(type: "REAL", nullable: false),
                    SyncOnChange = table.Column<bool>(type: "INTEGER", nullable: false),
                    DebugEnabled = table.Column<bool>(type: "INTEGER", nullable: true),
                    ConflictStrategy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalDavItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DavConfigId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Uri = table.Column<string>(type: "TEXT", nullable: true),
                    Etag = table.Column<string>(type: "TEXT", nullable: false),
                    Uid = table.Column<string>(type: "TEXT", nullable: true),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDirty = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalDavItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalDavItems_DavConfigs_DavConfigId",
                        column: x => x.DavConfigId,
                        principalTable: "DavConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alarms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    SelectedDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Repeat = table.Column<int>(type: "INTEGER", nullable: true),
                    HasRan = table.Column<bool>(type: "INTEGER", nullable: false),
                    CalDavItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alarms_CalDavItems_CalDavItemId",
                        column: x => x.CalDavItemId,
                        principalTable: "CalDavItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Uri = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    CalDavItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_CalDavItems_CalDavItemId",
                        column: x => x.CalDavItemId,
                        principalTable: "CalDavItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attendees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    CalDavItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendees_CalDavItems_CalDavItemId",
                        column: x => x.CalDavItemId,
                        principalTable: "CalDavItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalDavJournals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Classification = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Contact = table.Column<string>(type: "TEXT", nullable: false),
                    PublishedDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalDavJournals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalDavJournals_CalDavItems_Id",
                        column: x => x.Id,
                        principalTable: "CalDavItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalDavNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Classification = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Contact = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalDavNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalDavNotes_CalDavItems_Id",
                        column: x => x.Id,
                        principalTable: "CalDavItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalDavTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Contact = table.Column<string>(type: "TEXT", nullable: false),
                    Classification = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalDavTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalDavTasks_CalDavItems_Id",
                        column: x => x.Id,
                        principalTable: "CalDavItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    CalDavItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_CalDavItems_CalDavItemId",
                        column: x => x.CalDavItemId,
                        principalTable: "CalDavItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    CalDavItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_CalDavItems_CalDavItemId",
                        column: x => x.CalDavItemId,
                        principalTable: "CalDavItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_CalDavItemId",
                table: "Alarms",
                column: "CalDavItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_CalDavItemId",
                table: "Attachments",
                column: "CalDavItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_CalDavItemId",
                table: "Attendees",
                column: "CalDavItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CalDavItems_DavConfigId",
                table: "CalDavItems",
                column: "DavConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CalDavItemId",
                table: "Categories",
                column: "CalDavItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CalDavItemId",
                table: "Comments",
                column: "CalDavItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alarms");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "Attendees");

            migrationBuilder.DropTable(
                name: "CalDavJournals");

            migrationBuilder.DropTable(
                name: "CalDavNotes");

            migrationBuilder.DropTable(
                name: "CalDavTasks");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "CalDavItems");

            migrationBuilder.DropTable(
                name: "DavConfigs");
        }
    }
}
