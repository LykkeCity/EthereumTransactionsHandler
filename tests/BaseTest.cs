
using System;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Repositories;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
namespace Tests
{
    public class BaseTest
    {
        [SetUp]
        public async Task Up()
        {
            Config.Services.GetService<ICoinTransactionRepository>().DeleteTable();
            Config.Services.GetService<ITransactionRequestMappingRepository>().DeleteTable();
            Config.Services.GetService<IConfirmationRequestRepository>().DeleteTable();

            var listenerRepo = Config.Services.GetService<IQueueListenerRepository>();

            var listeners = await listenerRepo.GetListeners();
            foreach (var dbQueueListener in listeners)
            {
                Config.ListenerQueueFactory(dbQueueListener.Name).DeleteIfExists();
            }
            listenerRepo.DeleteTable();

            var queueFactory = Config.Services.GetService<Func<string, IQueueExt>>();
            await queueFactory(Constants.CoinIncomingRequestsQueue).ClearAsync();
            await queueFactory(Constants.CoinTransactionQueue).ClearAsync();
            await queueFactory(Constants.EmailNotifierQueue).ClearAsync();
            await queueFactory(Constants.ConfirmationRequestQueue).ClearAsync();
            await queueFactory(Constants.ConfirmationResponseQueue).ClearAsync();


            var coinRepo = Config.Services.GetService<ICoinRepository>();
            await coinRepo.InsertOrReplace(new Coin { AssetAddress = "0xa", Id = "Eth", Multiplier = 1, Blockchain = "ethereum"});
            await coinRepo.InsertOrReplace(new Coin { AssetAddress = "0xb", Id = "Lykke", Multiplier = 1, Blockchain = "ethereum" });

            Console.WriteLine("Setup test");
        }


        [TearDown]
        public void TearDown()
        {
            Console.WriteLine("Tear down");
        }

    }
}
