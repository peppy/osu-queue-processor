// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using StackExchange.Redis;

namespace osu.Server.QueueProcessor
{
    /// <summary>
    /// Extensions to manage schema deployments on redis.
    /// </summary>
    public static class ConnectionMultiplexerExtensions
    {
        private static string allActiveSchemasKey => $"osu-queue:score-index:{Environment.GetEnvironmentVariable("ES_INDEX_PREFIX") ?? string.Empty}active-schemas";
        private static string mainSchemaKey => $"osu-queue:score-index:{Environment.GetEnvironmentVariable("ES_INDEX_PREFIX") ?? string.Empty}schema";

        /// <summary>
        /// Add a new schema version to the active list. Note that it will not be set to the current schema (call <see cref="SetCurrentSchema"/> for that).
        /// </summary>
        public static bool AddActiveSchema(this ConnectionMultiplexer connection, string value)
        {
            return connection.GetDatabase().SetAdd(allActiveSchemasKey, value);
        }

        /// <summary>
        /// Clears the current live schema.
        /// </summary>
        public static void ClearCurrentSchema(this ConnectionMultiplexer connection)
        {
            connection.GetDatabase().KeyDelete(mainSchemaKey);
        }

        /// <summary>
        /// Get all active schemas (including past or future).
        /// </summary>
        public static string?[] GetActiveSchemas(this ConnectionMultiplexer connection)
        {
            return connection.GetDatabase().SetMembers(allActiveSchemasKey).ToStringArray();
        }

        /// <summary>
        /// Get the current (live) schema version.
        /// </summary>
        public static string GetCurrentSchema(this ConnectionMultiplexer connection)
        {
            return connection.GetDatabase().StringGet(mainSchemaKey).ToString();
        }

        /// <summary>
        /// Removes a specified schema from the active list.
        /// </summary>
        public static bool RemoveActiveSchema(this ConnectionMultiplexer connection, string value)
        {
            if (connection.GetCurrentSchema() == value)
                throw new InvalidOperationException($"Specified schema is current. Call {nameof(ClearCurrentSchema)} first");

            return connection.GetDatabase().SetRemove(allActiveSchemasKey, value);
        }

        /// <summary>
        /// Set the current (live) schema version.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="value"></param>
        public static void SetCurrentSchema(this ConnectionMultiplexer connection, string value)
        {
            IDatabase database = connection.GetDatabase();

            if (connection.GetActiveSchemas().All(s => s != value))
                throw new InvalidOperationException($"Attempted to set current schema without schema being in active list. Call {nameof(AddActiveSchema)} first");

            database.StringSet(mainSchemaKey, value);
        }
    }
}
