using System;
using System.Linq;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.Backups;
using Raven.Client.Exceptions;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace ConsoleApp1
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("wait for ravendb up");

			await Task.Delay(10_000);

			Console.WriteLine("run");

			var store = new DocumentStore()
			{
				Urls = new[] { "http://ravendb:8080" },
				Database = "test"
			}.Initialize();

			try
			{
				store.Maintenance.Server.Send(new DeleteDatabasesOperation(store.Database, true));
				store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(store.Database)));
			}
			catch (ConcurrencyException)
			{
				// The database was already created before calling CreateDatabaseOperation
			}

			var count = store.OpenSession().Query<Employee>().Count();

			var data = 100_000 - count;
			var lorem = await System.IO.File.ReadAllTextAsync("lorem.txt");

			Console.WriteLine($"start upload {data}");

			for (var i = 0; i < data; i++)
			{
				await using (var bulkInsert = store.BulkInsert())
				{
					for (var z = 0; z < 1000; z++)
					{
						await bulkInsert.StoreAsync(new Employee
						{
							Data = lorem
						});
						i++;
					}

					i--;
				}

				Console.WriteLine(i);
			}

			Console.WriteLine($"start backup");

			var config = new PeriodicBackupConfiguration
			{
				Name = "test-backup",
				FullBackupFrequency = "0 */3 * * *",
				BackupType = BackupType.Backup,
				S3Settings = new S3Settings
				{
					CustomServerUrl = "http://minio:9000",
					AwsAccessKey = "access_key",
					AwsSecretKey = "secret_key",
					BucketName = "test",
					RemoteFolderName = "test"
				},
			};

			//Create a new backup task
			var operation = new UpdatePeriodicBackupOperation(config);

			var result = await store.Maintenance.SendAsync(operation);

			//Run the backup task immediately
			var o = await store.Maintenance.SendAsync(new StartBackupOperation(true, result.TaskId));

			await o.WaitForCompletionAsync();

			Console.WriteLine($"end backup");

			Console.ReadLine();
		}
	}

	internal class Employee
	{
		public string Data { get; set; }
	}
}
