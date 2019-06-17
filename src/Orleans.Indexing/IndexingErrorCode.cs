using System;
using Orleans.Runtime;
using Microsoft.Extensions.Logging;

namespace Orleans.Indexing
{
    internal enum IndexingErrorCode
    {
        /// <summary>
        /// Start of orlean servicebus errocodes
        /// </summary>
        Indexing = 1 << 19,

        IndexingIndexIsNotReadyYet_GrainBucket1 = Indexing + 1,
        IndexingIndexIsNotReadyYet_GrainBucket2 = Indexing + 2,
        IndexingIndexIsNotReadyYet_GrainBucket3 = Indexing + 3,
        IndexingIndexIsNotReadyYet_GrainBucket4 = Indexing + 4,
        IndexingIndexIsNotReadyYet_GrainBucket5 = Indexing + 5,
        IndexingIndexIsNotReadyYet_GrainServiceBucket1 = Indexing + 6,
        IndexingIndexIsNotReadyYet_GrainServiceBucket2 = Indexing + 7,
        IndexingIndexIsNotReadyYet_GrainServiceBucket3 = Indexing + 8,
        IndexingIndexIsNotReadyYet_GrainServiceBucket4 = Indexing + 9,
        IndexingIndexIsNotReadyYet_GrainServiceBucket5 = Indexing + 10,
    }

    internal static class LoggerExtensions
    {
        internal static void Debug(this ILogger logger, IndexingErrorCode errorCode, string format, params object[] args)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.Debug((int)errorCode, format, args, null);
        }

        internal static void Trace(this ILogger logger, IndexingErrorCode errorCode, string format, params object[] args)
        {
            if (logger.IsEnabled(LogLevel.Trace)) logger.Trace((int)errorCode, format, args, null);
        }

        internal static void Info(this ILogger logger, IndexingErrorCode errorCode, string format, params object[] args)
        {
            if (logger.IsEnabled(LogLevel.Information)) logger.Info((int)errorCode, format, args, null);
        }

        internal static void Warn(this ILogger logger, IndexingErrorCode errorCode, string format, params object[] args)
        {
            if (logger.IsEnabled(LogLevel.Warning)) logger.Warn((int)errorCode, format, args, null);
        }

        internal static void Warn(this ILogger logger, IndexingErrorCode errorCode, string message, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Warning)) logger.Warn((int)errorCode,  message, new object[] { }, exception);
        }

        internal static void Error(this ILogger logger, IndexingErrorCode errorCode, string message, Exception exception = null)
        {
            if (logger.IsEnabled(LogLevel.Error)) logger.Error((int)errorCode, message, exception);
        }
    }
}
