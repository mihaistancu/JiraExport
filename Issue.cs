using System;
using System.Collections.Generic;

namespace JiraExport
{
	public class Issue
	{
		public string Key { get; set; }
		public string Summary { get; set; }
		public string Description { get; set; }
		public string Type { get; set; }
		public string Priority { get; set; }
		public string Status { get; set; }
		public string Resolution { get; set; }
		public string Assignee { get; set; }
		public string Reporter { get; set; }
		public DateTimeOffset Created { get; set; }
		public DateTimeOffset Updated { get; set; }
		public List<string> Versions { get; set; }
		public List<string> Labels { get; set; }
		public List<string> Components { get; set; }
		public List<Comment> Comments { get; set; }

		public override string ToString()
		{
			return Key;
		}
	}
}
