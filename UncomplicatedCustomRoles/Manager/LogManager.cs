using Discord;
using Exiled.API.Features;
using Exiled.Loader;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using UncomplicatedCustomRoles.Interfaces;

namespace UncomplicatedCustomRoles.Manager
{
    internal class LogManager
    {
        // We should store the data here
        public static readonly List<KeyValuePair<KeyValuePair<long, LogLevel>, string>> History = new();

        public static bool MessageSent { get; internal set; } = false;

        public static void Debug(string message)
        {
            History.Add(new(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), LogLevel.Debug), message));
            Log.Debug(message);
        }

        public static void Info(string message)
        {
            History.Add(new(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), LogLevel.Debug), message));
            Log.Info(message);
        }

        public static void Warn(string message)
        {
            History.Add(new(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), LogLevel.Debug), message));
            Log.Warn(message);
        }

        public static void Error(string message)
        {
            History.Add(new(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), LogLevel.Debug), message));
            Log.Error(message);
        }
    }
}