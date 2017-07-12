using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using jsreport.Local;
using Shouldly;

namespace jsreport.Client.Test
{
    [TestFixture]
    public class ReportingServiceTest
    {
        private IReportingService _reportingService;
        private ILocalWebServerReportingService _localReportingService;        

        [SetUp]
        public void SetUp()
        {           
            _localReportingService = new LocalReporting().AsWebServer().Create();
            _localReportingService.StartAsync().Wait();
            _reportingService = new ReportingService("http://localhost:5488");
        }

        [TearDown]
        public void TearDown()
        {
            _localReportingService.Kill();          
        }

        [Test]
        public async Task HtmlTest()
        {
            var result = await _reportingService.RenderAsync(new
            {
                template = new
                {
                    content = "foo",
                    engine = "none",
                    recipe = "html"
                }
            });

            using (var reader = new StreamReader(result.Content))
            {
                Assert.AreEqual("foo", reader.ReadToEnd());
            }
        }

        [Test]
        public async Task PhantomPdfTest()
        {
            var result = await _reportingService.RenderAsync(new
            {
                template = new
                {
                    content = "foo",
                    engine = "none",
                    recipe = "phantom-pdf"
                }
            });

            using (var reader = new StreamReader(result.Content))
            {
                Assert.IsTrue(reader.ReadToEnd().StartsWith("%PDF"));
            }
        }      

        [Test]
        public async Task JsRenderTest()
        {
            var result = await _reportingService.RenderAsync(new
            {
                template = new
                {
                    content = "{{:foo}}",
                    engine = "jsrender",
                    recipe = "html"
                },
                data = new
                    {
                        foo = "hello"
                    }
            });

            using (var reader = new StreamReader(result.Content))
            {
                Assert.IsTrue(reader.ReadToEnd().StartsWith("hello"));
            }
        }
     
        [Test]
        public async Task GetRecipesTest()
        {
            var recipes = await _reportingService.GetRecipesAsync();

            Assert.IsTrue(recipes.Count() > 1);
        }

        [Test]
        public async Task GetEnginesTest()
        {
            var engines = await _reportingService.GetRecipesAsync();

            Assert.IsTrue(engines.Count() > 1);
        }
     
        [Test]
        public async Task ThrowReadableExceptionForInvalidEngineTest()
        {
            try
            {
                var result = await _reportingService.RenderAsync(new {
                    template = new {
                        content = "foo",
                        engine = "NOT_EXISTING",
                        recipe = "phantom-pdf"
                    }
                });
            }
            catch (JsReportException ex)
            {
                Assert.IsTrue(ex.ResponseErrorMessage.Contains("NOT_EXISTING"));
            }
        }

        [Test]        
        public void HttpClientTimeoutCancelsTaskTest()
        {
            _reportingService.HttpClientTimeout = new TimeSpan(1);

            var aggregate = Should.Throw<AggregateException>(() => _reportingService.RenderAsync(new
            {
                template = new
                {
                    content = "foo",
                    engine = "none",
                    recipe = "phantom-pdf"
                }
            }).Wait());

            aggregate.InnerExceptions.Single().ShouldBeOfType<TaskCanceledException>();
        }

        [Test]
        public void RenderCancelTokenParameterCancelsTaskTest()
        {
            var ts = new CancellationTokenSource();
            ts.CancelAfter(1);

            var aggregate = Should.Throw<AggregateException>(() => _reportingService.RenderAsync(new
            {
                template = new
                {
                    content = "foo",
                    engine = "none",
                    recipe = "phantom-pdf"
                }
            }, ts.Token).Wait());

            aggregate.InnerExceptions.Single().ShouldBeOfType<TaskCanceledException>();
        
        }

        [Test]        
        public async Task SlowRenderingThrowsJsReportExceptionTest()
        {
            await _reportingService.RenderAsync(new
            {
                template = new
                {
                    content = "{{:~foo()}}",
                    helpers = "function foo() { while(true) { } }",
                    engine = "jsrender",
                    recipe = "phantom-pdf"
                }
            }).ShouldThrowAsync<JsReportException>();
        }

        [Test]
        public async Task GetServerVersionTest()
        {
            var result = await _reportingService.GetServerVersionAsync();
            Assert.IsTrue(result.Contains("."));
        }

        [Test]
        public async Task RenderInPreviewReturnsExcelOnlineTest()
        {
            var report = await _reportingService.RenderAsync(new
            {

                template = new { content = "<table><tr><td>a</td></tr></table>", recipe = "html-to-xlsx", engine = "jsrender" },
                options = new
                {
                    preview = true
                }
            });


            var reader = new StreamReader(report.Content);

            var str = reader.ReadToEnd();
            Assert.IsTrue(str.Contains("iframe"));
        }
    }

    
    [TestFixture]
    public class AuthenticatedReportingServiceTest
    {
        private IReportingService _reportingService;
        private ILocalWebServerReportingService _localReportingService;

        [SetUp]
        public void SetUp()
        {
            _localReportingService = new LocalReporting().Configure(cfg => cfg.Authenticated("admin", "password")).AsWebServer().Create();
            _localReportingService.StartAsync().Wait();
            _reportingService = new ReportingService("http://localhost:5488", "admin", "password");
        }

        [TearDown]
        public void TearDown()
        {
            _localReportingService.Kill();
        }

        [Test]
        public async Task CallWithCredentialsWorksTest()
        {
            await _reportingService.GetServerVersionAsync();
        }

        [Test]        
        public async Task CallWithoutCredentialsThrows()
        {
            _reportingService.Username = null;
            _reportingService.Password = null;
            await _reportingService.GetServerVersionAsync().ShouldThrowAsync<HttpRequestException>();
        }        
    }   
}
