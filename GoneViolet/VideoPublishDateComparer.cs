using GoneViolet.Model;
using System.Collections.Generic;

namespace GoneViolet
{
    public class VideoPublishDateComparer : IComparer<Video>
    {
        private readonly bool _ascending;

        public VideoPublishDateComparer(bool ascending = true)
        {
            _ascending = ascending;
        }

        public int Compare(Video x, Video y)
        {
            int result = 0;
            if (x.PublishedTimestammp.HasValue == y.PublishedTimestammp.HasValue && x.PublishedTimestammp.HasValue)
                result = x.PublishedTimestammp.Value.CompareTo(y.PublishedTimestammp.Value);
            if (result == 0)
                result = string.Compare(x.VideoId ?? string.Empty, y.VideoId ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            int ascedningMagnitude = _ascending ? 1 : -1;
            return result * ascedningMagnitude;
        }
    }
}
