using System;

namespace Backend.Helper
{
    /// <summary>
    /// Simple Snowflake ID generator (time + datacenter + worker + sequence).
    /// Thread-safe; call NextId() to get a unique 64-bit number.
    /// </summary>
    public class Snowflake
    {
        private const long Twepoch = 1609459200000L; // 2021-01-01 UTC in ms

        private const int WorkerIdBits = 5;
        private const int DatacenterIdBits = 5;
        private const int SequenceBits = 12;

        private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
        private const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);
        private const long SequenceMask = -1L ^ (-1L << SequenceBits);

        private const int WorkerIdShift = SequenceBits;
        private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
        private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;

        private readonly object _sync = new();
        private long _lastTimestamp = -1L;
        private long _sequence;

        public long WorkerId { get; }
        public long DatacenterId { get; }

        public Snowflake(long workerId, long datacenterId)
        {
            if (workerId < 0 || workerId > MaxWorkerId)
            {
                throw new ArgumentOutOfRangeException(nameof(workerId), $"workerId must be between 0 and {MaxWorkerId}");
            }

            if (datacenterId < 0 || datacenterId > MaxDatacenterId)
            {
                throw new ArgumentOutOfRangeException(nameof(datacenterId), $"datacenterId must be between 0 and {MaxDatacenterId}");
            }

            WorkerId = workerId;
            DatacenterId = datacenterId;
        }

        public long NextId()
        {
            lock (_sync)
            {
                var timestamp = CurrentTimeMillis();

                if (timestamp < _lastTimestamp)
                {
                    throw new InvalidOperationException($"Clock moved backwards. Refusing to generate id for {_lastTimestamp - timestamp}ms");
                }

                if (timestamp == _lastTimestamp)
                {
                    _sequence = (_sequence + 1) & SequenceMask;
                    if (_sequence == 0)
                    {
                        timestamp = WaitForNextMillis(_lastTimestamp);
                    }
                }
                else
                {
                    _sequence = 0;
                }

                _lastTimestamp = timestamp;

                return ((timestamp - Twepoch) << TimestampLeftShift)
                    | (DatacenterId << DatacenterIdShift)
                    | (WorkerId << WorkerIdShift)
                    | _sequence;
            }
        }

        private static long WaitForNextMillis(long lastTimestamp)
        {
            var timestamp = CurrentTimeMillis();
            while (timestamp <= lastTimestamp)
            {
                timestamp = CurrentTimeMillis();
            }
            return timestamp;
        }

        private static long CurrentTimeMillis()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
