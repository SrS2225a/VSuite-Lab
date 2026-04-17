using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VSuiteLab.Migrations
{
    /// <inheritdoc />
    public partial class InitalDB : Migration
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
                name: "Journals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Classification = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Contact = table.Column<string>(type: "TEXT", nullable: false),
                    PublishedDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_Journals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Journals_DavConfigs_DavConfigId",
                        column: x => x.DavConfigId,
                        principalTable: "DavConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Classification = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Contact = table.Column<string>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notes_DavConfigs_DavConfigId",
                        column: x => x.DavConfigId,
                        principalTable: "DavConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
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
                    Classification = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_DavConfigs_DavConfigId",
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
                    CalDavTaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CalDavJournalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CalDavNoteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    HasRan = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alarms_Journals_CalDavJournalId",
                        column: x => x.CalDavJournalId,
                        principalTable: "Journals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alarms_Notes_CalDavNoteId",
                        column: x => x.CalDavNoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alarms_Tasks_CalDavTaskId",
                        column: x => x.CalDavTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Uri = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    CalDavTaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CalDavJournalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CalDavNoteId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_Journals_CalDavJournalId",
                        column: x => x.CalDavJournalId,
                        principalTable: "Journals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attachments_Notes_CalDavNoteId",
                        column: x => x.CalDavNoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attachments_Tasks_CalDavTaskId",
                        column: x => x.CalDavTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attendees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CalDavTaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CalDavJournalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CalDavNoteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Role = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendees_Journals_CalDavJournalId",
                        column: x => x.CalDavJournalId,
                        principalTable: "Journals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attendees_Notes_CalDavNoteId",
                        column: x => x.CalDavNoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attendees_Tasks_CalDavTaskId",
                        column: x => x.CalDavTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CalDavTaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CalDavJournalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CalDavNoteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Journals_CalDavJournalId",
                        column: x => x.CalDavJournalId,
                        principalTable: "Journals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Categories_Notes_CalDavNoteId",
                        column: x => x.CalDavNoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Categories_Tasks_CalDavTaskId",
                        column: x => x.CalDavTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CalDavTaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CalDavJournalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CalDavNoteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Journals_CalDavJournalId",
                        column: x => x.CalDavJournalId,
                        principalTable: "Journals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Notes_CalDavNoteId",
                        column: x => x.CalDavNoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Tasks_CalDavTaskId",
                        column: x => x.CalDavTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_CalDavJournalId",
                table: "Alarms",
                column: "CalDavJournalId");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_CalDavNoteId",
                table: "Alarms",
                column: "CalDavNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_CalDavTaskId",
                table: "Alarms",
                column: "CalDavTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_CalDavJournalId",
                table: "Attachments",
                column: "CalDavJournalId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_CalDavNoteId",
                table: "Attachments",
                column: "CalDavNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_CalDavTaskId",
                table: "Attachments",
                column: "CalDavTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_CalDavJournalId",
                table: "Attendees",
                column: "CalDavJournalId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_CalDavNoteId",
                table: "Attendees",
                column: "CalDavNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_CalDavTaskId",
                table: "Attendees",
                column: "CalDavTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CalDavJournalId",
                table: "Categories",
                column: "CalDavJournalId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CalDavNoteId",
                table: "Categories",
                column: "CalDavNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CalDavTaskId",
                table: "Categories",
                column: "CalDavTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CalDavJournalId",
                table: "Comments",
                column: "CalDavJournalId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CalDavNoteId",
                table: "Comments",
                column: "CalDavNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CalDavTaskId",
                table: "Comments",
                column: "CalDavTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Journals_DavConfigId",
                table: "Journals",
                column: "DavConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_DavConfigId",
                table: "Notes",
                column: "DavConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DavConfigId",
                table: "Tasks",
                column: "DavConfigId");
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
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Journals");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "DavConfigs");
        }
    }
}
