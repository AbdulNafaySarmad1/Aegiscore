using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <summary>
    /// Fifteen columns were created by V2_0_ExtendedPlatformModules with the types their entities had
    /// then (enum integers, doubles, jsonb); the entities were later changed to strings and decimals
    /// and the model snapshot was edited to match, so no migration was ever generated and every
    /// deployed database kept the old types. PostgreSQL then rejected the queries the code writes
    /// ("operator does not exist: integer = character varying"). This migration brings the columns to
    /// the model's types. The affected tables are empty on every known database; the USING casts keep
    /// any rows that do exist (an enum ordinal becomes its digits).
    /// </summary>
    public partial class AlignColumnTypesWithModel : Migration
    {
        private static readonly (string Table, string Column, string Type, string Using)[] Columns =
        [
            ("BoardAlerts", "Category", "character varying(100)", "\"Category\"::text"),
            ("CashFlowForecasts", "RecoveryRateEstimate", "numeric(5,4)", "\"RecoveryRateEstimate\"::numeric(5,4)"),
            ("DeansListEntries", "Cgpa", "numeric(4,2)", "\"Cgpa\"::numeric(4,2)"),
            ("DeansListEntries", "Status", "character varying(20)", "\"Status\"::text"),
            ("Elections", "Status", "character varying(20)", "\"Status\"::text"),
            ("Elections", "VotingMethod", "character varying(20)", "\"VotingMethod\"::text"),
            ("LeaderboardEntries", "Scope", "character varying(20)", "\"Scope\"::text"),
            ("LeaderboardEntries", "Score", "numeric(4,2)", "\"Score\"::numeric(4,2)"),
            ("MentalHealthTickets", "ConcernType", "character varying(50)", "\"ConcernType\"::text"),
            ("MentalHealthTickets", "Severity", "character varying(20)", "\"Severity\"::text"),
            ("MenuItems", "Category", "character varying(50)", "\"Category\"::text"),
            ("VoiceSessions", "AsrConfidence", "numeric(5,4)", "\"AsrConfidence\"::numeric(5,4)"),
            ("VoiceSessions", "DetectedLanguage", "character varying(10)", "\"DetectedLanguage\"::text"),
            ("VoiceSessions", "EntitiesJson", "character varying(2000)", "\"EntitiesJson\"::text"),
            ("VoiceSessions", "Intent", "character varying(100)", "\"Intent\"::text"),
        ];

        private static readonly (string Table, string Column, string Type, string Using)[] Previous =
        [
            ("BoardAlerts", "Category", "integer", "0"),
            ("CashFlowForecasts", "RecoveryRateEstimate", "double precision", "\"RecoveryRateEstimate\"::double precision"),
            ("DeansListEntries", "Cgpa", "double precision", "\"Cgpa\"::double precision"),
            ("DeansListEntries", "Status", "integer", "0"),
            ("Elections", "Status", "integer", "0"),
            ("Elections", "VotingMethod", "integer", "0"),
            ("LeaderboardEntries", "Scope", "integer", "0"),
            ("LeaderboardEntries", "Score", "double precision", "\"Score\"::double precision"),
            ("MentalHealthTickets", "ConcernType", "integer", "0"),
            ("MentalHealthTickets", "Severity", "integer", "0"),
            ("MenuItems", "Category", "integer", "0"),
            ("VoiceSessions", "AsrConfidence", "integer", "0"),
            ("VoiceSessions", "DetectedLanguage", "integer", "0"),
            ("VoiceSessions", "EntitiesJson", "jsonb", "\"EntitiesJson\"::jsonb"),
            ("VoiceSessions", "Intent", "integer", "0"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (table, column, type, use) in Columns)
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ALTER COLUMN \"{column}\" TYPE {type} USING {use};");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (table, column, type, use) in Previous)
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ALTER COLUMN \"{column}\" TYPE {type} USING {use};");
        }
    }
}
