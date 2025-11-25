using System.Collections.Concurrent;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace MenuManager;

public class DataBaseService
{
    private readonly PluginConfig _config;
    private readonly string _connectionString;
    private readonly ILogger<DataBaseService> _logger;

    public DataBaseService(PluginConfig config)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<DataBaseService>();

        _config = config;
        _connectionString = BuildDatabaseConnectionString();
    }

    private string BuildDatabaseConnectionString()
    {
        if (string.IsNullOrWhiteSpace(_config.DatabaseHost) ||
            string.IsNullOrWhiteSpace(_config.DatabaseUser) ||
            string.IsNullOrWhiteSpace(_config.DatabaseName))
            throw new InvalidOperationException("Database configuration is incomplete.");

        var builder = new MySqlConnectionStringBuilder
        {
            Server = _config.DatabaseHost,
            Port = (uint)_config.DatabasePort,
            UserID = _config.DatabaseUser,
            Password = _config.DatabasePassword,
            Database = _config.DatabaseName,
            Pooling = true
        };

        return builder.ConnectionString;
    }

    private async Task<MySqlConnection> GetOpenConnectionAsync()
    {
        try
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while opening database connection");
            throw;
        }
    }

    public async Task TestAndCheckDataBaseTableAsync()
    {
        try
        {
            await using var connection = await GetOpenConnectionAsync();
            _logger.LogInformation("Database connection successful!");

            var tableExists = await connection.QueryFirstOrDefaultAsync<string>(
                "SHOW TABLES LIKE 'player_menus';") != null;

            if (!tableExists)
            {
                const string createTableQuery = """
                                                    CREATE TABLE `player_menus` (
                                                        `steamid` BIGINT UNSIGNED PRIMARY KEY, 
                                                        `menu_type` VARCHAR(64) NOT NULL DEFAULT 'Default',
                                                        `pagination` TINYINT NULL DEFAULT NULL,
                                                        `sounds_enabled` TINYINT NULL DEFAULT NULL,
                                                        `volume` FLOAT NULL DEFAULT NULL
                                                    );
                                                """;

                await connection.ExecuteAsync(createTableQuery);
                _logger.LogInformation("Table 'player_menus' created successfully with new schema.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection failed or table creation error");
        }
    }

    public async Task<ConcurrentDictionary<ulong, PlayerSettings>> LoadAllMenuSettings()
    {
        var result = new ConcurrentDictionary<ulong, PlayerSettings>();
        try
        {
            await using var connection = await GetOpenConnectionAsync();

            var rows = await connection.QueryAsync("SELECT * FROM `player_menus`");

            foreach (var row in rows)
            {
                var steamId = (ulong)row.steamid;
                var settings = new PlayerSettings();

                string typeStr = row.menu_type.ToString();
                settings.MenuType = Enum.TryParse(typeStr, true, out MenuType parsedType)
                    ? parsedType
                    : MenuType.Default;

                if (row.pagination != null)
                    settings.UsePagination = (int)row.pagination == 1;

                if (row.sounds_enabled != null)
                    settings.SoundsEnabled = (int)row.sounds_enabled == 1;

                if (row.volume != null)
                    settings.Volume = (float)row.volume;

                result.TryAdd(steamId, settings);
            }

            _logger.LogInformation("Loaded {ResultCount} player menu preferences.", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading menu settings");
        }

        return result;
    }

    public async Task SaveMenuSetting(ulong steamId, PlayerSettings settings)
    {
        try
        {
            await using var connection = await GetOpenConnectionAsync();

            const string query = """
                                     INSERT INTO `player_menus` (`steamid`, `menu_type`, `pagination`, `sounds_enabled`, `volume`) 
                                     VALUES (@SteamId, @MenuType, @Pagination, @SoundsEnabled, @Volume)
                                     ON DUPLICATE KEY UPDATE 
                                        `menu_type` = @MenuType,
                                        `pagination` = @Pagination,
                                        `sounds_enabled` = @SoundsEnabled,
                                        `volume` = @Volume;
                                 """;

            await connection.ExecuteAsync(query, new
            {
                SteamId = steamId,
                MenuType = settings.MenuType.ToString(),
                Pagination = settings.UsePagination,
                settings.SoundsEnabled,
                settings.Volume
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving menu setting for SteamID {SteamId}", steamId);
        }
    }
}