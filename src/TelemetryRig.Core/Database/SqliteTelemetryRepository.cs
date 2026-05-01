using Microsoft.Data.Sqlite;
using TelemetryRig.Core.Models;

namespace TelemetryRig.Core.Database;

/// <summary>
/// SQLite repository.
///
/// SQLite is a small local database engine. It is perfect for desktop apps when you need
/// local storage without installing SQL Server.
/// </summary>
public sealed class SqliteTelemetryRepository : ITelemetryRepository
{
    private readonly string _connectionString;

    public SqliteTelemetryRepository(string databaseFilePath)
    {
        var directory = Path.GetDirectoryName(databaseFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databaseFilePath
        }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS TelemetryPackets
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TimestampUtc TEXT NOT NULL,
                FrameId INTEGER NOT NULL,
                GameName TEXT NOT NULL,
                SpeedKph REAL NOT NULL,
                Rpm INTEGER NOT NULL,
                Gear INTEGER NOT NULL,
                Throttle REAL NOT NULL,
                Brake REAL NOT NULL,
                Steering REAL NOT NULL,
                SuspensionTravelMm REAL NOT NULL,
                WheelSlip REAL NOT NULL,
                Surface TEXT NOT NULL,
                RawBytesLength INTEGER NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_TelemetryPackets_TimestampUtc
            ON TelemetryPackets(TimestampUtc);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task InsertBatchAsync(IReadOnlyList<TelemetryPacket> packets, CancellationToken cancellationToken)
    {
        if (packets.Count == 0)
            return;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Performance optimisation: use one transaction for the whole batch.
        // Without a transaction, SQLite may flush to disk per row, which is much slower.
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction as SqliteTransaction;
        command.CommandText = """
            INSERT INTO TelemetryPackets
            (
                TimestampUtc, FrameId, GameName, SpeedKph, Rpm, Gear,
                Throttle, Brake, Steering, SuspensionTravelMm, WheelSlip,
                Surface, RawBytesLength
            )
            VALUES
            (
                $TimestampUtc, $FrameId, $GameName, $SpeedKph, $Rpm, $Gear,
                $Throttle, $Brake, $Steering, $SuspensionTravelMm, $WheelSlip,
                $Surface, $RawBytesLength
            );
            """;

        var pTimestamp = command.Parameters.Add("$TimestampUtc", SqliteType.Text);
        var pFrameId = command.Parameters.Add("$FrameId", SqliteType.Integer);
        var pGameName = command.Parameters.Add("$GameName", SqliteType.Text);
        var pSpeedKph = command.Parameters.Add("$SpeedKph", SqliteType.Real);
        var pRpm = command.Parameters.Add("$Rpm", SqliteType.Integer);
        var pGear = command.Parameters.Add("$Gear", SqliteType.Integer);
        var pThrottle = command.Parameters.Add("$Throttle", SqliteType.Real);
        var pBrake = command.Parameters.Add("$Brake", SqliteType.Real);
        var pSteering = command.Parameters.Add("$Steering", SqliteType.Real);
        var pSuspension = command.Parameters.Add("$SuspensionTravelMm", SqliteType.Real);
        var pWheelSlip = command.Parameters.Add("$WheelSlip", SqliteType.Real);
        var pSurface = command.Parameters.Add("$Surface", SqliteType.Text);
        var pRawBytesLength = command.Parameters.Add("$RawBytesLength", SqliteType.Integer);

        foreach (var packet in packets)
        {
            pTimestamp.Value = packet.TimestampUtc.ToString("O");
            pFrameId.Value = packet.FrameId;
            pGameName.Value = packet.GameName;
            pSpeedKph.Value = packet.SpeedKph;
            pRpm.Value = packet.Rpm;
            pGear.Value = packet.Gear;
            pThrottle.Value = packet.Throttle;
            pBrake.Value = packet.Brake;
            pSteering.Value = packet.Steering;
            pSuspension.Value = packet.SuspensionTravelMm;
            pWheelSlip.Value = packet.WheelSlip;
            pSurface.Value = packet.Surface;
            pRawBytesLength.Value = packet.RawBytesLength;

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TelemetryPacket>> ListRecentAsync(int take, CancellationToken cancellationToken)
    {
        var result = new List<TelemetryPacket>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TimestampUtc, FrameId, GameName, SpeedKph, Rpm, Gear,
                   Throttle, Brake, Steering, SuspensionTravelMm, WheelSlip,
                   Surface, RawBytesLength
            FROM TelemetryPackets
            ORDER BY Id DESC
            LIMIT $Take;
            """;
        command.Parameters.AddWithValue("$Take", take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result.Add(new TelemetryPacket(
                DateTimeOffset.Parse(reader.GetString(0)),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetDouble(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetDouble(6),
                reader.GetDouble(7),
                reader.GetDouble(8),
                reader.GetDouble(9),
                reader.GetDouble(10),
                reader.GetString(11),
                reader.GetInt32(12)));
        }

        return result;
    }
}
