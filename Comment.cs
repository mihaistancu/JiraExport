using System;

namespace JiraExport
{
	public class Comment
	{	public string Author { get; set; }
		public DateTimeOffset Created { get; set; }
		public string Text { get; set; }
	}
}
