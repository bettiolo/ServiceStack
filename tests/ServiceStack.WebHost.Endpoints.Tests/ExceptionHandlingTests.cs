using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.ServiceInterface;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using NUnit.Framework;
using Funq;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using System.Collections;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[RestService("/users")]
	public class User { }
	public class UserResponse : IHasResponseStatus
	{
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class UserService : RestServiceBase<User>
	{
		public override object  OnGet(User request)
		{
			return new HttpError(HttpStatusCode.BadRequest, "Failed to execute!", errorCode: "CanNotExecute");
		}

		public override object OnPost(User request)
		{
            throw new HttpError(HttpStatusCode.BadRequest, "Failed to execute!", errorCode: "CanNotExecute");
		}

		public override object OnPut(User request)
		{
			throw new ArgumentException();
		}

        public override object OnDelete(User request) 
        {
            throw new ArgumentException("Exception 1", 
                new DivideByZeroException("Inner Exception 1", 
                    new NotSupportedException("Inner Exception 2",
                        new FileNotFoundException("Inner Exception 3"))));

        }

	}

	[TestFixture]
	public class ExceptionHandlingTests
	{
		private const string ListeningOn = "http://localhost:82/";

		public class ExceptionHandlingAppHostHttpListener
			: AppHostHttpListenerBase
		{

			public ExceptionHandlingAppHostHttpListener()
				: base("Exception handling tests", typeof(UserService).Assembly) { }

			public override void Configure(Container container)
			{
			}
		}

		ExceptionHandlingAppHostHttpListener appHost;

		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			appHost = new ExceptionHandlingAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[TestFixtureTearDown]
		public void OnTestFixtureTearDown()
		{
			appHost.Dispose();
		}

		static IRestClient[] ServiceClients = 
        {
            new JsonServiceClient(ListeningOn),
            new XmlServiceClient(ListeningOn),
            new JsvServiceClient(ListeningOn)
			//SOAP not supported in HttpListener
			//new Soap11ServiceClient(ServiceClientBaseUri),
			//new Soap12ServiceClient(ServiceClientBaseUri)
        };


		[Test, TestCaseSource("ServiceClients")]
		public void Handles_Returned_Http_Error(IRestClient client)
		{
			try
			{
				client.Get<UserResponse>("/users");
			}
			catch (WebServiceException ex)
			{
				Assert.That(ex.ErrorCode, Is.EqualTo("CanNotExecute"));
				Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
				Assert.That(ex.Message, Is.EqualTo("CanNotExecute"));
			}
		}

		[Test, TestCaseSource("ServiceClients")]
		public void Handles_Thrown_Http_Error(IRestClient client)
		{
			try
			{
				client.Post<UserResponse>("/users", new User());
			}
			catch (WebServiceException ex)
			{
				Assert.That(ex.ErrorCode, Is.EqualTo("CanNotExecute"));
				Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
				Assert.That(ex.Message, Is.EqualTo("CanNotExecute"));
			}
		}

		[Test, TestCaseSource("ServiceClients")]
		public void Handles_Normal_Exception(IRestClient client)
		{
			try
			{
				client.Put<UserResponse>("/users", new User());
			}
			catch (WebServiceException ex)
			{
				Assert.That(ex.ErrorCode, Is.EqualTo("ArgumentException"));
				Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
			}
		}

        [Test, TestCaseSource("ServiceClients")]
        public void Handles_Nested_Exceptions(IRestClient client) {
            try {
                client.Delete<UserResponse>("/users");
            } catch (WebServiceException ex) {
                var mainException = ex;
                Assert.That(mainException.ErrorCode, Is.EqualTo("ArgumentException"));
                Assert.That(mainException.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(mainException.ErrorMessage, Is.EqualTo("Exception 1"));
                
                Assert.That(mainException.InnerException, Is.AssignableTo<WebServiceException>());
                var innerException1 = (WebServiceException)mainException.InnerException;
                Assert.That(innerException1.ErrorCode, Is.EqualTo("DivideByZeroException"));
                Assert.That(innerException1.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
                Assert.That(innerException1.ErrorMessage, Is.EqualTo("Inner Exception 1"));


            }
        }
	}
}
