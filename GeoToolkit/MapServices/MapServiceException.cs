using System;

namespace EDIVE.GeoToolkit.MapServices
{
    public class MapServiceException : Exception
    {
        public string Url { get; }
        public long ResponseCode { get; }

        public MapServiceException(string url, long responseCode, string detail, Exception innerException = null)
            : base($"Map service request failed with HTTP {responseCode}{(string.IsNullOrEmpty(detail) ? "" : $" - {detail}")}\n{url}", innerException)
        {
            Url = url;
            ResponseCode = responseCode;
        }
    }
}
