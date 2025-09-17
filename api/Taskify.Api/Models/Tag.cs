﻿using System.Collections.Generic;

namespace Taskify.Api.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    }
}
