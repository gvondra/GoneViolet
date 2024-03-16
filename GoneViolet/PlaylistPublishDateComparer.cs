using GoneViolet.Model;
using System.Collections.Generic;

namespace GoneViolet
{
    public class PlaylistPublishDateComparer : IComparer<Playlist>
    {
        private readonly bool _ascending;

        public PlaylistPublishDateComparer(bool ascending = true)
        {
            _ascending = ascending;
        }

        public int Compare(Playlist x, Playlist y)
        {
            int result = 0;
            if (x.PublishedTimestamp.HasValue == y.PublishedTimestamp.HasValue && x.PublishedTimestamp.HasValue)
                result = x.PublishedTimestamp.Value.CompareTo(y.PublishedTimestamp.Value);
            if (result == 0)
                result = string.Compare(x.Id ?? string.Empty, y.Id ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            int ascedningMagnitude = _ascending ? 1 : -1;
            return result * ascedningMagnitude;
        }
    }
}
