using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Repositories;
using Core.Timers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Services;
using Services.Models;

namespace Tests
{
    [TestFixture]
    public class TransactionSigningTests : BaseTest
    {
        private IncomingSwapRequest _requestSwap = new IncomingSwapRequest
        {
            AmountA = 1,
            AmountB = 2,
            ClientA = "0xa",
            ClientB = "0xb",
            CoinA = "Eth",
            CoinB = "Lykke",
            SignA = "signA",
            SignB = "signB",
            TransactionId = new Guid("b40c09bc-3623-4e32-9ede-699bc07774f7")
        };

        [Test]
        public async Task TestSignatureRequest()
        {
            _requestSwap.SignA = null;
            _requestSwap.SignB = null;

            var queueListenerService = Config.Services.GetService<IQueueListenerService>();
            var requestRepo = Config.Services.GetService<IConfirmationRequestRepository>();

            var id = Guid.NewGuid();
            var swapId = Guid.NewGuid();
            var listener = await queueListenerService.PutToListenerQueue(_requestSwap, swapId);

            var taskSwap = listener.Execute();

            var requestA = requestRepo.GetConfirmationRequest(_requestSwap.TransactionId, _requestSwap.ClientA);
            var requestB = requestRepo.GetConfirmationRequest(_requestSwap.TransactionId, _requestSwap.ClientB);

            Assert.IsNotNull(requestA);
            Assert.IsNotNull(requestB);

            await Task.Delay(300);

            var queueCount = await Config.Services.GetService<Func<string, IQueueExt>>()(Constants.ConfirmationRequestQueue).Count();

            Assert.AreEqual(2, queueCount);
        }
    }
}
