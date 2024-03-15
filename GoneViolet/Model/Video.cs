﻿using System.Collections.Generic;

namespace GoneViolet.Model
{
    public class Video
    {
        public DateTime? PublishedTimestammp { get; set; }
        public string Title { get; set; }
        public string VideoId { get; set; }
        public string BlobName { get; set; }
        public bool IsStored { get; set; }
        public bool? Skip { get; set; }
        public List<string> Tags { get; set; }
    }
}
