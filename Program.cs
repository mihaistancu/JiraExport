using System;
using System.Net;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace JiraExport
{
    class Program
    {
        static void Main(string[] args)
        {
			var project = "EESSIDRY";
			Export(project);
			Import(project);
		}

		private static void Import(string project)
		{
			var files = Directory.GetFiles("c:\\temp\\", $"{project}-*.xml");

			foreach (var file in files)
			{
				var issues = Parse(file);

				foreach (var issue in issues)
				{
					Import(project, issue);
				}
			}
		}

		private static void Import(string project, Issue issue)
		{
			var connectionString = "server=.;database=JIRA;integrated security=sspi";
			var insertIssue = @"
				INSERT INTO [dbo].[Issue]
					   ([Key]
					   ,[Project]
					   ,[Summary]
					   ,[Description]
					   ,[Type]
					   ,[Priority]
					   ,[Status]
					   ,[Resolution]
					   ,[Assignee]
					   ,[Reporter]
					   ,[Created]
					   ,[Updated])
				 VALUES
					   (@Key
					   ,@Project
					   ,@Summary
					   ,@Description
					   ,@Type
					   ,@Priority
					   ,@Status
					   ,@Resolution
					   ,@Assignee
					   ,@Reporter
					   ,@Created
					   ,@Updated)";

			using (var connection = new SqlConnection(connectionString))
			using (var command = new SqlCommand(insertIssue, connection))
			{
				connection.Open();

				command.Parameters.AddWithValue("Key", issue.Key);
				command.Parameters.AddWithValue("Project", project);
				command.Parameters.AddWithValue("Summary", issue.Summary);
				command.Parameters.AddWithValue("Description", issue.Description);
				command.Parameters.AddWithValue("Type", issue.Type);
				command.Parameters.AddWithValue("Priority", issue.Priority ?? "");
				command.Parameters.AddWithValue("Status", issue.Status);
				command.Parameters.AddWithValue("Resolution", issue.Resolution);
				command.Parameters.AddWithValue("Assignee", issue.Assignee);
				command.Parameters.AddWithValue("Reporter", issue.Reporter);
				command.Parameters.AddWithValue("Created", issue.Created);
				command.Parameters.AddWithValue("Updated", issue.Updated);

				command.ExecuteNonQuery();
			}

			var insertLabel = @"
				INSERT INTO [dbo].[Label]
					   ([Key]
					   ,[Label])
				 VALUES
					   (@Key
					   ,@Label)";

			foreach (var label in issue.Labels)
			{	
				using (var connection = new SqlConnection(connectionString))
				using (var command = new SqlCommand(insertLabel, connection))
				{
					connection.Open();

					command.Parameters.AddWithValue("Key", issue.Key);
					command.Parameters.AddWithValue("Label", label);

					command.ExecuteNonQuery();
				}
			}

			var insertComponent = @"
				INSERT INTO [dbo].[Component]
					   ([Key]
					   ,[Component])
				 VALUES
					   (@Key
					   ,@Component)";

			foreach (var component in issue.Components)
			{
				using (var connection = new SqlConnection(connectionString))
				using (var command = new SqlCommand(insertComponent, connection))
				{
					connection.Open();

					command.Parameters.AddWithValue("Key", issue.Key);
					command.Parameters.AddWithValue("Component", component);

					command.ExecuteNonQuery();
				}
			}

			var insertComment = @"
				INSERT INTO [dbo].[Comment]
					   ([Key]
					   ,[Author]
					   ,[Created]
					   ,[Comment])
				 VALUES
					   (@Key
					   ,@Author
					   ,@Created
					   ,@Comment)";

			if (issue.Comments != null)
			{
				foreach (var comment in issue.Comments)
				{
					using (var connection = new SqlConnection(connectionString))
					using (var command = new SqlCommand(insertComment, connection))
					{
						connection.Open();

						command.Parameters.AddWithValue("Key", issue.Key);
						command.Parameters.AddWithValue("Author", comment.Author);
						command.Parameters.AddWithValue("Created", comment.Created);
						command.Parameters.AddWithValue("Comment", comment.Text);

						command.ExecuteNonQuery();
					}
				}
			}
		}

		private static void Export(string project)
		{
			var start = 0;
			var length = 500;
			var url = $"https://citnet.tech.ec.europa.eu/CITnet/jira/sr/jira.issueviews:searchrequest-xml/temp/SearchRequest.xml?jqlQuery=project%3D{project}&tempMax={length}&pager/start=";
			var auth = "JSESSIONID=0CD0EE1EA07804E303CB65B0ABF175A5.pissenlit; atlassian.xsrf.token=AM6Z-FOEK-VHM0-47I5_9b6cbeae5fc8b69adc13dc7b62a8a28c373b536a_lin; AJS.conglomerate.cookie=\" | timesheet.235185.page = 1 | timesheet.235186.page = 1\"; mywork.tab.tasks=false; BCSI-CS-4d846444b0ccd088=1; crowd.token_key=RPlZ0Ry0-MDmJPudgu_wRgAAAAAABIABc3Rhbm1paA";

			var client = new WebClient();
			client.Headers.Add(HttpRequestHeader.Cookie, auth);

			while (true)
			{
				var end = start + length;
				var filename = $"c:\\temp\\{project}-{start.ToString().PadLeft(5, '0')}-{end.ToString().PadLeft(5, '0')}.xml";

				client.DownloadFile(url + start.ToString(), filename);

				var items = Parse(filename);
				var count = items.Count();
				if (count == 0) return;

				start = end;
			}
		}

		private static IEnumerable<Issue> Parse(string filename)
		{
			XElement rss = XElement.Load(filename);

			return from item in rss.Element("channel").Descendants("item")
						select new Issue
						{
							Key = item.Element("key").Value,
							Summary = item.Element("summary").Value,
							Description = item.Element("description").Value,
							Type = item.Element("type").Value,
							Priority = item.Element("priority")?.Value,
							Status = item.Element("status").Value,
							Resolution = item.Element("resolution").Value,
							Assignee = item.Element("assignee").Value,
							Reporter = item.Element("reporter").Value,
							Created = DateTimeOffset.Parse(item.Element("created").Value),
							Updated = DateTimeOffset.Parse(item.Element("updated").Value),
							Versions = (from version in item.Elements("version") select version.Value).ToList(),
							Labels = (from label in item.Element("labels").Descendants("label") select label.Value).ToList(),
							Components = (from component in item.Elements("component") select component.Value).ToList(),
							Comments = item.Element("comments") != null ? (from comment in item.Element("comments").Descendants("comment")
																		   select new Comment
																		   {
																			   Author = comment.Attribute("author").Value,
																			   Created = DateTimeOffset.Parse(comment.Attribute("created").Value),
																			   Text = comment.Value
																		   }).ToList() : null

						};
		}
    }
}
