using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.Common.Web
{
	public class HttpError : Exception, IHttpError
	{

        public HttpError(HttpStatusCode statusCode = HttpStatusCode.InternalServerError, string errorMessage = null,
            string errorCode = null, object responseDto = null, HttpError innerException = null)
            : base(errorMessage ?? errorCode ?? statusCode.ToString(), innerException)
		{
            this.Response = responseDto;
			this.ErrorCode = errorCode;
			this.StatusCode = statusCode;
			this.Headers = new Dictionary<string, string>();
            this.StatusDescription = errorCode;
		}

		public string ErrorCode { get; set; }

		public string ContentType { get; set; }

		public Dictionary<string, string> Headers { get; set; }

		public HttpStatusCode StatusCode { get; set; }

        public string StatusDescription { get; set; }

		public object Response { get; set; }

		public string TemplateName { get; set; }

		public IContentTypeWriter ResponseFilter { get; set; }
		
		public IRequestContext RequestContext { get; set; }

		public IDictionary<string, string> Options
		{
			get { return this.Headers; }
		}

		public ResponseStatus ResponseStatus
		{
			get
			{
				return this.Response.ToResponseStatus();
			}
		}

		public List<ResponseError> GetFieldErrors()
		{
			var responseStatus = ResponseStatus;
			if (responseStatus != null)
				return responseStatus.Errors ?? new List<ResponseError>();
			
			return new List<ResponseError>();
		}

		public static Exception NotFound(string message)
		{
			return new HttpError(HttpStatusCode.NotFound, message);
		}

        public static Exception Unauthorized(string message)
        {
            return new HttpError(HttpStatusCode.Unauthorized, message);
        }

        public static Exception Conflict(string message)
        {
            return new HttpError(HttpStatusCode.Conflict, message);
        }

    }
}