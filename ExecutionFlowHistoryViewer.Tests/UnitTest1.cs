using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ExecutionFlowHistoryViewer.Services;
using ExecutionFlowHistoryViewer.Models;

namespace ExecutionFlowHistoryViewer.Tests
{
    [TestClass]
    public class FlowClientTests
    {
        [TestMethod]
        public void GetFlowRuns_ReturnsListOfFlowRuns_WhenApiCallIsSuccessful()
        {
            // 1. ARRANGE : N-wjedo l'environnement w n-kdbo 3la l'API
            // Hada houwa l'JSON li ghadi y-tkhyel l'code rah jabo mn internet
            var fakeJsonResponse = @"{
                ""value"":[
                    {
                        ""name"": ""run-m3lm-123"",
                        ""properties"": {
                            ""status"": ""Succeeded"",
                            ""startTime"": ""2024-01-01T10:00:00Z"",
                            ""endTime"": ""2024-01-01T10:05:00Z""
                        }
                    }
                ]
            }";

            // N-saybo faux HttpMessageHandler b Moq
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fakeJsonResponse)
                });

            // N-3tiw l-faux handler l HttpClient w n-saybo FlowClient dialna
            var httpClient = new HttpClient(handlerMock.Object);
            var flowClient = new FlowClient("fake-env", "fake-token", "https://fake-url.com", httpClient);

            // 2. ACT : N-lanciwo la méthode li bghina n-testiw
            var result = flowClient.GetFlowRuns("fake-flow-id");

            // 3. ASSERT : N-t2kdo wach jabat l-Code w resultat s7i7!
            Assert.IsNotNull(result, "L-liste makhasach tkon null");
            Assert.AreEqual(1, result.Count, "Khass ykon fiha 1 run");
            Assert.AreEqual("run-m3lm-123", result[0].Id, "L'ID dyal l'run machi s7i7");
            Assert.AreEqual("Succeeded", result[0].Status, "Le status machi s7i7");
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))] // Kantwe93o anaha ghadi t-lancer Exception
        public void GetFlowRuns_ThrowsException_WhenApiCallFails()
        {
            // 1. ARRANGE
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized, // Erreur 401 (Matalan token m-perimé)
                    Content = new StringContent("Token invalide")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var flowClient = new FlowClient("fake-env", "fake-token", "https://fake-url.com", httpClient);

            // 2. ACT : Hna l'code khasso y-crashi w ytl3 Exception
            flowClient.GetFlowRuns("fake-flow-id");
        }
    }
}