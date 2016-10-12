using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Repositories;
using NUnit.Framework;
using TransactionHandlerJob.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Tests
{
	[TestFixture]
	public class MonitoringJobTests : BaseTest
	{
		[Test]
		public async Task TestMonitoringJobExecute()
		{
			var repo = Config.Services.GetService<IMonitoringRepository>();
			var job = Config.Services.GetService<MonitoringJob>();
			job.Execute().Wait();

			var records = (await repo.GetList()).ToList();
			Assert.AreEqual(1, records.Count);
			Assert.AreEqual(Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion, records[0].Version);
		}

	}
}
