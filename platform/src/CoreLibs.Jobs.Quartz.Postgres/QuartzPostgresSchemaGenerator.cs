using Npgsql;

namespace CoreLibs.Jobs.Quartz.Postgres;

/// <summary>
/// Ensures the <c>quartz</c> schema and all required Quartz.NET tables exist in PostgreSQL.
/// Executed synchronously during service registration, before the Quartz scheduler starts
/// and validates the schema.
/// </summary>
internal static class QuartzPostgresSchemaGenerator
{
    /// <summary>Schema name used for all Quartz tables.</summary>
    internal const string Schema = "quartz";

    /// <summary>Table prefix passed to Quartz â€” schema-qualified, no extra prefix needed.</summary>
    internal const string TablePrefix = $"{Schema}.";

    // All statements use IF NOT EXISTS so subsequent runs are no-ops.
    private static readonly string Sql = $"""
        CREATE SCHEMA IF NOT EXISTS {Schema};

        CREATE TABLE IF NOT EXISTS {Schema}.JOB_DETAILS (
            SCHED_NAME        VARCHAR(120) NOT NULL,
            JOB_NAME          VARCHAR(200) NOT NULL,
            JOB_GROUP         VARCHAR(200) NOT NULL,
            DESCRIPTION       VARCHAR(250) NULL,
            JOB_CLASS_NAME    VARCHAR(250) NOT NULL,
            IS_DURABLE        BOOL         NOT NULL,
            IS_NONCONCURRENT  BOOL         NOT NULL,
            IS_UPDATE_DATA    BOOL         NOT NULL,
            REQUESTS_RECOVERY BOOL         NOT NULL,
            JOB_DATA          BYTEA        NULL,
            PRIMARY KEY (SCHED_NAME, JOB_NAME, JOB_GROUP)
        );

        CREATE TABLE IF NOT EXISTS {Schema}.TRIGGERS (
            SCHED_NAME          VARCHAR(120) NOT NULL,
            TRIGGER_NAME        VARCHAR(200) NOT NULL,
            TRIGGER_GROUP       VARCHAR(200) NOT NULL,
            JOB_NAME            VARCHAR(200) NOT NULL,
            JOB_GROUP           VARCHAR(200) NOT NULL,
            DESCRIPTION         VARCHAR(250) NULL,
            NEXT_FIRE_TIME      BIGINT       NULL,
            PREV_FIRE_TIME      BIGINT       NULL,
            PRIORITY            INTEGER      NULL,
            TRIGGER_STATE       VARCHAR(16)  NOT NULL,
            TRIGGER_TYPE        VARCHAR(8)   NOT NULL,
            START_TIME          BIGINT       NOT NULL,
            END_TIME            BIGINT       NULL,
            CALENDAR_NAME       VARCHAR(200) NULL,
            MISFIRE_INSTR       SMALLINT     NULL,
            JOB_DATA            BYTEA        NULL,
            MISFIRE_ORIG_FIRE_TIME BIGINT    NULL,
            PRIMARY KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP),
            FOREIGN KEY (SCHED_NAME, JOB_NAME, JOB_GROUP)
                REFERENCES {Schema}.JOB_DETAILS (SCHED_NAME, JOB_NAME, JOB_GROUP)
        );

        -- Add column to existing tables (no-op if column already exists)
        ALTER TABLE {Schema}.TRIGGERS
            ADD COLUMN IF NOT EXISTS MISFIRE_ORIG_FIRE_TIME BIGINT NULL;

        CREATE TABLE IF NOT EXISTS {Schema}.SIMPLE_TRIGGERS (
            SCHED_NAME       VARCHAR(120) NOT NULL,
            TRIGGER_NAME     VARCHAR(200) NOT NULL,
            TRIGGER_GROUP    VARCHAR(200) NOT NULL,
            REPEAT_COUNT     BIGINT       NOT NULL,
            REPEAT_INTERVAL  BIGINT       NOT NULL,
            TIMES_TRIGGERED  BIGINT       NOT NULL,
            PRIMARY KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP),
            FOREIGN KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)
                REFERENCES {Schema}.TRIGGERS (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)
        );

        CREATE TABLE IF NOT EXISTS {Schema}.SIMPROP_TRIGGERS (
            SCHED_NAME    VARCHAR(120)   NOT NULL,
            TRIGGER_NAME  VARCHAR(200)   NOT NULL,
            TRIGGER_GROUP VARCHAR(200)   NOT NULL,
            STR_PROP_1    VARCHAR(512)   NULL,
            STR_PROP_2    VARCHAR(512)   NULL,
            STR_PROP_3    VARCHAR(512)   NULL,
            INT_PROP_1    INT            NULL,
            INT_PROP_2    INT            NULL,
            LONG_PROP_1   BIGINT         NULL,
            LONG_PROP_2   BIGINT         NULL,
            DEC_PROP_1    NUMERIC(13, 4) NULL,
            DEC_PROP_2    NUMERIC(13, 4) NULL,
            BOOL_PROP_1   BOOL           NULL,
            BOOL_PROP_2   BOOL           NULL,
            TIME_ZONE_ID  VARCHAR(80)    NULL,
            PRIMARY KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP),
            FOREIGN KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)
                REFERENCES {Schema}.TRIGGERS (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)
        );

        CREATE TABLE IF NOT EXISTS {Schema}.CRON_TRIGGERS (
            SCHED_NAME      VARCHAR(120) NOT NULL,
            TRIGGER_NAME    VARCHAR(200) NOT NULL,
            TRIGGER_GROUP   VARCHAR(200) NOT NULL,
            CRON_EXPRESSION VARCHAR(120) NOT NULL,
            TIME_ZONE_ID    VARCHAR(80)  NULL,
            PRIMARY KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP),
            FOREIGN KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)
                REFERENCES {Schema}.TRIGGERS (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)
        );

        CREATE TABLE IF NOT EXISTS {Schema}.BLOB_TRIGGERS (
            SCHED_NAME    VARCHAR(120) NOT NULL,
            TRIGGER_NAME  VARCHAR(200) NOT NULL,
            TRIGGER_GROUP VARCHAR(200) NOT NULL,
            BLOB_DATA     BYTEA        NULL,
            PRIMARY KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP),
            FOREIGN KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)
                REFERENCES {Schema}.TRIGGERS (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)
        );

        CREATE TABLE IF NOT EXISTS {Schema}.CALENDARS (
            SCHED_NAME    VARCHAR(120) NOT NULL,
            CALENDAR_NAME VARCHAR(200) NOT NULL,
            CALENDAR      BYTEA        NOT NULL,
            PRIMARY KEY (SCHED_NAME, CALENDAR_NAME)
        );

        CREATE TABLE IF NOT EXISTS {Schema}.PAUSED_TRIGGER_GRPS (
            SCHED_NAME    VARCHAR(120) NOT NULL,
            TRIGGER_GROUP VARCHAR(200) NOT NULL,
            PRIMARY KEY (SCHED_NAME, TRIGGER_GROUP)
        );

        CREATE TABLE IF NOT EXISTS {Schema}.FIRED_TRIGGERS (
            SCHED_NAME         VARCHAR(120) NOT NULL,
            ENTRY_ID           VARCHAR(140) NOT NULL,
            TRIGGER_NAME       VARCHAR(200) NOT NULL,
            TRIGGER_GROUP      VARCHAR(200) NOT NULL,
            INSTANCE_NAME      VARCHAR(200) NOT NULL,
            FIRED_TIME         BIGINT       NOT NULL,
            SCHED_TIME         BIGINT       NOT NULL,
            PRIORITY           INTEGER      NOT NULL,
            STATE              VARCHAR(16)  NOT NULL,
            JOB_NAME           VARCHAR(200) NULL,
            JOB_GROUP          VARCHAR(200) NULL,
            IS_NONCONCURRENT   BOOL         NULL,
            REQUESTS_RECOVERY  BOOL         NULL,
            PRIMARY KEY (SCHED_NAME, ENTRY_ID)
        );

        CREATE TABLE IF NOT EXISTS {Schema}.SCHEDULER_STATE (
            SCHED_NAME        VARCHAR(120) NOT NULL,
            INSTANCE_NAME     VARCHAR(200) NOT NULL,
            LAST_CHECKIN_TIME BIGINT       NOT NULL,
            CHECKIN_INTERVAL  BIGINT       NOT NULL,
            PRIMARY KEY (SCHED_NAME, INSTANCE_NAME)
        );

        CREATE TABLE IF NOT EXISTS {Schema}.LOCKS (
            SCHED_NAME VARCHAR(120) NOT NULL,
            LOCK_NAME  VARCHAR(40)  NOT NULL,
            PRIMARY KEY (SCHED_NAME, LOCK_NAME)
        );

        CREATE INDEX IF NOT EXISTS IDX_J_REQ_RECOVERY
            ON {Schema}.JOB_DETAILS (SCHED_NAME, REQUESTS_RECOVERY);
        CREATE INDEX IF NOT EXISTS IDX_J_GRP
            ON {Schema}.JOB_DETAILS (SCHED_NAME, JOB_GROUP);
        CREATE INDEX IF NOT EXISTS IDX_T_J
            ON {Schema}.TRIGGERS (SCHED_NAME, JOB_NAME, JOB_GROUP);
        CREATE INDEX IF NOT EXISTS IDX_T_JG
            ON {Schema}.TRIGGERS (SCHED_NAME, JOB_GROUP);
        CREATE INDEX IF NOT EXISTS IDX_T_C
            ON {Schema}.TRIGGERS (SCHED_NAME, CALENDAR_NAME);
        CREATE INDEX IF NOT EXISTS IDX_T_G
            ON {Schema}.TRIGGERS (SCHED_NAME, TRIGGER_GROUP);
        CREATE INDEX IF NOT EXISTS IDX_T_STATE
            ON {Schema}.TRIGGERS (SCHED_NAME, TRIGGER_STATE);
        CREATE INDEX IF NOT EXISTS IDX_T_NF_STATE
            ON {Schema}.TRIGGERS (SCHED_NAME, TRIGGER_STATE, NEXT_FIRE_TIME);
        CREATE INDEX IF NOT EXISTS IDX_T_NF_MISFIRE
            ON {Schema}.TRIGGERS (SCHED_NAME, MISFIRE_INSTR, NEXT_FIRE_TIME);
        CREATE INDEX IF NOT EXISTS IDX_FT_TRIG_INST_NAME
            ON {Schema}.FIRED_TRIGGERS (SCHED_NAME, INSTANCE_NAME);
        CREATE INDEX IF NOT EXISTS IDX_FT_INST_JOB_REQ_RCVRY
            ON {Schema}.FIRED_TRIGGERS (SCHED_NAME, INSTANCE_NAME, REQUESTS_RECOVERY);
        CREATE INDEX IF NOT EXISTS IDX_FT_J_G
            ON {Schema}.FIRED_TRIGGERS (SCHED_NAME, JOB_NAME, JOB_GROUP);
        CREATE INDEX IF NOT EXISTS IDX_FT_JG
            ON {Schema}.FIRED_TRIGGERS (SCHED_NAME, JOB_GROUP);
        CREATE INDEX IF NOT EXISTS IDX_FT_T_G
            ON {Schema}.FIRED_TRIGGERS (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP);
        CREATE INDEX IF NOT EXISTS IDX_FT_TG
            ON {Schema}.FIRED_TRIGGERS (SCHED_NAME, TRIGGER_GROUP);
        """;

    /// <summary>
    /// Applies the schema migration synchronously.
    /// Safe to call multiple times â€” all statements use <c>IF NOT EXISTS</c>.
    /// </summary>
    internal static void Apply(string connectionString)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = Sql;
        cmd.ExecuteNonQuery();
    }
}
