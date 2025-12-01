using System.Collections.Concurrent;
using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace MenuManager;

public class DataBaseService
{
    private readonly PluginConfig _config;
    private readonly string _connectionString;
    private readonly ILogger<DataBaseService> _logger;
    private readonly bool _useSqlite;

    public DataBaseService(PluginConfig config, string modulePath)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<DataBaseService>();
        _config = config;

        if (string.IsNullOrWhiteSpace(_config.DatabaseHost) ||
            string.IsNullOrWhiteSpace(_config.DatabaseUser) ||
            string.IsNullOrWhiteSpace(_config.DatabasePassword) ||
            string.IsNullOrWhiteSpace(_config.DatabaseName))
        {
            _useSqlite = true;
            var sqliteDbFile = Path.Combine(modulePath, "menumanager.db");
            _connectionString = $"Data Source={sqliteDbFile}";
            _logger.LogWarning("MySQL configuration missing. Using SQLite database at: {Path}", sqliteDbFile);
        }
        else
        {
            _useSqlite = false;
            _connectionString = BuildMySqlConnectionString();
        }
    }

    private string BuildMySqlConnectionString()
    {
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

    private Task<IDbConnection> GetOpenConnectionAsync()
    {
        try
        {
            IDbConnection connection;
            if (_useSqlite)
                connection = new SqliteConnection(_connectionString);
            else
                connection = new MySqlConnection(_connectionString);

            if (connection.State != ConnectionState.Open)
                connection.Open();

            return Task.FromResult(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while opening database connection ({DbType})", _useSqlite ? "SQLite" : "MySQL");
            throw;
        }
    }

    public async Task TestAndCheckDataBaseTableAsync()
    {
        try
        {
            using var connection = await GetOpenConnectionAsync();

            if (_useSqlite)
            {
                const string createTableQuery = """
                                                CREATE TABLE IF NOT EXISTS player_menus (
                                                    steamid INTEGER PRIMARY KEY,
                                                    menu_type TEXT NOT NULL DEFAULT 'Default',
                                                    pagination INTEGER NULL DEFAULT NULL,
                                                    sounds_enabled INTEGER NULL DEFAULT NULL,
                                                    volume REAL NULL DEFAULT NULL
                                                );
                                                """;
                await connection.ExecuteAsync(createTableQuery);
                _logger.LogInformation("SQLite Database checked/created successfully.");
            }
            else
            {
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
                    _logger.LogInformation("Table 'player_menus' created successfully (MySQL).");
                }
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
            using var connection = await GetOpenConnectionAsync();

            var rows = await connection.QueryAsync("SELECT * FROM player_menus");

            foreach (var row in rows)
            {
                var steamId = Convert.ToUInt64(row.steamid);
                var settings = new PlayerSettings();

                string typeStr = row.menu_type.ToString();
                settings.MenuType = Enum.TryParse(typeStr, true, out MenuType parsedType)
                    ? parsedType
                    : MenuType.Default;

                if (row.pagination != null)
                    settings.UsePagination = Convert.ToInt32(row.pagination) == 1;

                if (row.sounds_enabled != null)
                    settings.SoundsEnabled = Convert.ToInt32(row.sounds_enabled) == 1;

                if (row.volume != null)
                    settings.Volume = Convert.ToSingle(row.volume);

                result.TryAdd(steamId, settings);
            }

            _logger.LogInformation("Loaded {ResultCount} player menu preferences from {DbType}.", result.Count,
                _useSqlite ? "SQLite" : "MySQL");
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
            using var connection = await GetOpenConnectionAsync();

            var query = _useSqlite
                ? """
                  INSERT OR REPLACE INTO player_menus (steamid, menu_type, pagination, sounds_enabled, volume) 
                  VALUES (@SteamId, @MenuType, @Pagination, @SoundsEnabled, @Volume);
                  """
                : """
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
                Pagination = settings.UsePagination == true ? 1 : 0,
                SoundsEnabled = settings.SoundsEnabled == true ? 1 : 0,
                settings.Volume
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving menu setting for SteamID {SteamId}", steamId);
        }
    }
}